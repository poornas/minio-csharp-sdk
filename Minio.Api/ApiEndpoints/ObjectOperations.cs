using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Minio.DataModel;
using RestSharp;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.Exceptions;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Minio
{
    class ObjectOperations : IObjectOperations
    {
        internal static readonly ApiResponseErrorHandlingDelegate NoSuchBucketHandler = (response) =>
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BucketNotFoundException();
            }
        };

        private const string RegistryAuthHeaderKey = "X-Registry-Auth";

        private readonly MinioRestClient _client;

        internal ObjectOperations(MinioRestClient client)
        {
            this._client = client;
        }
        public async Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb)
        {

            RestRequest request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.GET);
            request.ResponseWriter = cb;
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
              
                this._client.ParseError(response);
            }
         
            return;
        }

        public async Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType)
        {

        }
        public async Task RemoveObjectAsync(string bucketName, string objectName)
        {
            var request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.DELETE);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);


            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                throw this._client.ParseError(response);
            }
        }

        public async Task<ObjectStat> StatObjectAsync(string bucketName, string objectName)
        {
            var request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.HEAD);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw this._client.ParseError(response);
            }
         
            long size = 0;
            DateTime lastModified = new DateTime();
            string etag = "";
            string contentType = null;
            foreach (Parameter parameter in response.Headers)
            {
                switch (parameter.Name)
                {
                    case "Content-Length":
                        size = long.Parse(parameter.Value.ToString());
                        break;
                    case "Last-Modified":
                        lastModified = DateTime.Parse(parameter.Value.ToString());
                        break;
                    case "ETag":
                        etag = parameter.Value.ToString().Replace("\"", "");
                        break;
                    case "Content-Type":
                        contentType = parameter.Value.ToString();
                        break;
                    default:
                        break;
                }
            }
            return new ObjectStat(objectName, size, lastModified, etag, contentType);
           
        }

        private static string UrlEncode(string input)
        {
            return Uri.EscapeDataString(input).Replace("%2F", "/");
        }

    }
}
