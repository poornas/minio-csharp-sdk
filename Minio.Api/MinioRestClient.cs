﻿using System;
using System.Collections.Generic;
using MinioApi.Exceptions;
using System.Text.RegularExpressions;
using RestSharp;
using System.Net;
using Minio.Api.Exceptions;
using System.Linq;
using System.Text;
using RestSharp.Extensions;
using System.IO;
using Minio.Api.DataModel;
using System.Xml.Serialization;

namespace Minio.Api
{
    public sealed class MinioRestClient : IMinioClient
    {
        public string AccessKey { get; private set; }
        public string SecretKey { get; private set; }
        public string Endpoint { get; private set; }
        public string BaseUrl { get; private set; }
        public bool Secure { get; private set; }

        private Uri uri;
        private RestClient client;
        private V4Authenticator authenticator;

        public IBucketOperations Buckets { get; }

        internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers = Enumerable.Empty<ApiResponseErrorHandlingDelegate>();

        private readonly ApiResponseErrorHandlingDelegate _defaultErrorHandlingDelegate = (response) =>
        {
            if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.BadRequest)
            {
                throw new MinioApiException(response);
            }
        };
        private static string SystemUserAgent
        {
            get
            {
                string arch = System.Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
                string release = "minio-dotnet/0.2.1";
                return String.Format("Minio ({0};{1}) {2}", System.Environment.OSVersion.ToString(), arch, release);
            }
        }
        private static string CustomUserAgent = "";
        private string FullUserAgent
        {
            get
            {
                return SystemUserAgent + " " + CustomUserAgent;
            }

        }

        internal UriBuilder GetUriBuilder(string methodPath)
        {
            var uripath = new UriBuilder(this.Endpoint);
            uripath.Path += methodPath;
            return uripath;
        }
        private void _constructUri()
        {
         
            var scheme = this.Secure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            this.Endpoint = string.Format("{0}://{1}", scheme, this.BaseUrl);
               
            this.uri = new Uri(this.Endpoint);

        }
        private void _validateEndPoint()
        {
            if (string.IsNullOrEmpty(this.Endpoint))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }
            
            if (!this.isValidEndpoint(this.uri.Host))
            {
                throw new InvalidEndpointException(this.Endpoint, "Invalid endpoint.");
            }

            if (!this.uri.AbsolutePath.Equals("/", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new InvalidEndpointException(this.Endpoint, "No path allowed in endpoint.");
            }

            if (!string.IsNullOrEmpty(this.uri.Query))
            {
                throw new InvalidEndpointException(this.Endpoint, "No query parameter allowed in endpoint.");
            }

            if (!(this.uri.Scheme.Equals(Uri.UriSchemeHttp) || this.uri.Scheme.Equals(Uri.UriSchemeHttps)))
            {
                throw new InvalidEndpointException(this.Endpoint, "Invalid scheme detected in endpoint.");
            }
            string amzHost = this.uri.Host;
            if ((amzHost.EndsWith(".amazonaws.com", StringComparison.CurrentCultureIgnoreCase))
                && !(amzHost.Equals("s3.amazonaws.com", StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new InvalidEndpointException(this.Endpoint, "For Amazon S3, host should be \'s3.amazonaws.com\' in endpoint.");
            }
        }
        private bool isValidEndpoint(string endpoint)
        {
            // endpoint may be a hostname
            // refer https://en.wikipedia.org/wiki/Hostname#Restrictions_on_valid_host_names
            // why checks are as shown below.
            if (endpoint.Length < 1 || endpoint.Length > 253)
            {
                return false;
            }

            foreach (var label in endpoint.Split('.'))
            {
                if (label.Length < 1 || label.Length > 63)
                {
                    return false;
                }

                Regex validLabel = new Regex("^[a-zA-Z0-9][a-zA-Z0-9-]*");
                Regex validEndpoint = new Regex(".*[a-zA-Z0-9]$");

                if (!(validLabel.IsMatch(label) && validEndpoint.IsMatch(endpoint)))
                {
                    return false;
                }
            }

            return true;
        }
        public void SetAppInfo(string appName, string appVersion)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException("Appname cannot be null or empty");
            }
            if (string.IsNullOrEmpty(appVersion))
            {
                throw new ArgumentException("Appversion cannot be null or empty");
            }
            string customAgent = appName + "/" + appVersion;
           
            this.client.UserAgent = this.FullUserAgent;
        }
        public MinioRestClient(string endpoint,string accessKey, string secretKey)
        {
            
            this.Secure = false;
            this.BaseUrl = endpoint;
            _constructUri();
            _validateEndPoint();
            client = new RestSharp.RestClient(this.uri);
            client.UserAgent = this.FullUserAgent;
           
            authenticator = new V4Authenticator(accessKey, secretKey);
            client.Authenticator = authenticator;

            this.Buckets = new BucketOperations(this);
            return;

        }
        public MinioRestClient WithSSL()
        {
            this.Secure = true;
            _constructUri();
            this.client.BaseUrl = this.uri;
           // Console.Out.WriteLine(this.uri.ToString(), this.client.BaseUrl);
            return this;
        }
        public void ExecuteAsync<T>(IRestRequest request, Action<T> callback) where T : new()
        {
            request.OnBeforeDeserialization = (resp) =>
            {
                // for individual resources when there's an error to make
                // sure that RestException props are populated
                if (((int)resp.StatusCode) >= 400)
                {
                    // have to read the bytes so .Content doesn't get populated
                    var restException = "{{ \"RestException\" : {0} }}";
                    var content = resp.RawBytes.AsString(); //get the response content
                    var newJson = string.Format(restException, content);

                    resp.Content = null;
                    resp.RawBytes = Encoding.UTF8.GetBytes(newJson.ToString());
                }
            };

            request.DateFormat = "ddd, dd MMM yyyy HH:mm:ss '+0000'";

            this.client.ExecuteAsync<T>(request, (response) => callback(response.Data));
        }

        /// <summary>
        /// Execute a manual REST request
        /// </summary>
        /// <param name="request">The RestRequest to execute (will use client credentials)</param>
        /// <param name="callback">The callback function to execute when the async request completes</param>
        public void ExecuteAsync(IRestRequest request, Action<IRestResponse> callback)
        {
            this.client.ExecuteAsync(request, callback);
        }

        public bool BucketExists(string bucketName)
        {
            var request = new RestRequest(bucketName, Method.HEAD);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            var ex = ParseError(response);
            
            throw ex;
        }
        private ClientException ParseError(IRestResponse response)
        {
            Console.Out.WriteLine("there was an exception");
            return new ClientException("parseerror");
        }
      
    }
internal delegate void ApiResponseErrorHandlingDelegate(IRestResponse response);

}
