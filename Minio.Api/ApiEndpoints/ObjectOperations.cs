using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;

using System.Threading.Tasks;
using System.Linq;

using System.Reactive.Linq;
using Minio.DataModel;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.Exceptions;
using System.Globalization;
using Minio.Helper;

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

            RestRequest request = new RestRequest(bucketName + "/" + utils.UrlEncode(objectName), Method.GET);
            request.ResponseWriter = cb;
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
              
                this._client.ParseError(response);
            }
         
            return;
        }

    
    //todo

        public async Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType)
        {
            utils.validateBucketName(bucketName);
            utils.validateObjectName(objectName);
            if (data == null)
            {
                throw new ArgumentNullException("Invalid input stream,cannot be null");
            }
            var bytesRead = ReadFull(data, (int)size);

            if ((bytesRead.Length >= Constants.MaximumStreamObjectSize) || 
                    (size >= Constants.MaximumStreamObjectSize))
            {
                throw new ArgumentException("Input size is bigger than stipulated maximum of 50GB.");
            }
            if (size < Constants.MinimumPartSize && size >= 0)
            {
                if (bytesRead.Length != (int)size)
                {
                    throw new UnexpectedShortReadException("Data read " + bytesRead.Length + " is shorter than the size " + size + " of input buffer.");
                }
                await this.PutObjectAsync(bucketName, objectName, null, 0, bytesRead, contentType);
                return;
            }
            //TODO: skipping amazon s3 anonymous calls and google storage api overrides for now 
            //to refactor
            var partSize = Constants.MinimumPartSize;
            var uploadsObservable = this.ListIncompleteUploads(bucketName, objectName);
            
            string uploadId = null;
            Dictionary<int, string> etags = new Dictionary<int, string>();
            IDisposable subscription = uploadsObservable.Subscribe(
                    upload => 
                    {
                        if (objectName == upload.Key)
                        {
                            uploadId = upload.UploadId;
                            var partsObservable = this.ListParts(bucketName, objectName, uploadId);
                            IDisposable partsSubscription = partsObservable.Subscribe(part =>
                                {
                                    etags[part.PartNumber] = part.ETag;
                                });
                            partsSubscription.Dispose();
                        }                      
                    },
                    ex => Console.WriteLine("OnError: {0}", ex.Message),
                    () => Console.WriteLine("OnComplete: {0}"));
            subscription.Dispose();
           
               
            
           
            if (uploadId == null)
            {
                uploadId = await this.NewMultipartUploadAsync(bucketName, objectName, contentType);
            }
            int partNumber = 0;
            long totalWritten = 0;
            while (totalWritten < size)
            {
                partNumber++;
                byte[] dataToCopy = ReadFull(data, (int)partSize);
                if (dataToCopy == null)
                {
                    break;
                }
                if (dataToCopy.Length < partSize)
                {
                    var expectedSize = size - totalWritten;
                    if (expectedSize != dataToCopy.Length)
                    {
                        throw new UnexpectedShortReadException("Unexpected short read. Read only " + dataToCopy.Length + " out of " + expectedSize + "bytes");
                    }
                }
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] hash = md5.ComputeHash(dataToCopy);
                string etag = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                if (!etags.ContainsKey(partNumber) || !etags[partNumber].Equals(etag))
                {
                    etag = await this.PutObjectAsync(bucketName, objectName, uploadId, partNumber, dataToCopy, contentType);
                }
                etags[partNumber] = etag;
                totalWritten += dataToCopy.Length;
            }

            foreach (int curPartNumber in etags.Keys)
            {
                if (curPartNumber > partNumber)
                {
                    etags.Remove(curPartNumber);
                }
            }
            await this.CompleteMultipartUploadAsync(bucketName, objectName, uploadId, etags);
        }
        private async Task CompleteMultipartUploadAsync(string bucketName, string objectName, string uploadId, Dictionary<int, string> etags)
        {
            var path = bucketName + "/" + utils.UrlEncode(objectName) + "?uploadId=" + uploadId;
            var request = new RestRequest(path, Method.POST);

            List<XElement> parts = new List<XElement>();

            for (int i = 1; i <= etags.Count; i++)
            {
                parts.Add(new XElement("Part",
                                       new XElement("PartNumber", i),
                                       new XElement("ETag", etags[i])));
            }

            var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);

            var bodyString = completeMultipartUploadXml.ToString();

            var body = System.Text.Encoding.UTF8.GetBytes(bodyString);

            request.AddParameter("application/xml", body, RestSharp.ParameterType.RequestBody);

            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                return;
            }
            this._client.ParseError(response);
        }
        
        private long CalculatePartSize(long size)
        {
            // make sure to have enough buffer for last part, use 9999 instead of 10000
            long partSize = (size / 9999);
            if (partSize > Constants.MinimumPartSize)
            {
                if (partSize > Constants.MaximumPartSize)
                {
                    return Constants.MaximumPartSize;
                }
                return partSize;
            }
            return Constants.MinimumPartSize;
        }

        private IObservable<Part> ListParts(string bucketName, string objectName, string uploadId)
        {

            return Observable.Create<Part>(
              async obs =>
              {
                  int nextPartNumberMarker = 0;
                  bool isRunning = true;
                  while (isRunning)
                  {
                      var uploads = await this.GetListPartsAsync(bucketName, objectName, uploadId, nextPartNumberMarker);
                      foreach (Part part in uploads.Item2)
                      {
                          obs.OnNext(part);
                      }
                      nextPartNumberMarker = uploads.Item1.NextPartNumberMarker;
                      isRunning = uploads.Item1.IsTruncated;
                  }
              });
            
        }

        private async Task<Tuple<ListPartsResult, List<Part>>> GetListPartsAsync(string bucketName, string objectName, string uploadId, int partNumberMarker)
        {
            var path = bucketName + "/" + utils.UrlEncode(objectName) + "?uploadId=" + uploadId;
            if (partNumberMarker > 0)
            {
                path += "&part-number-marker=" + partNumberMarker;
            }
            path += "&max-parts=1000";
            var request = new RestRequest(path, Method.GET);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this._client.ParseError(response);
            }
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ListPartsResult listPartsResult = (ListPartsResult)(new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream));

            XDocument root = XDocument.Parse(response.Content);

            var uploads = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Part")
                            select new Part()
                            {
                                PartNumber = int.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}PartNumber").Value, CultureInfo.CurrentCulture),
                                ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value.Replace("\"", "")
                            });

            return new Tuple<ListPartsResult, List<Part>>(listPartsResult, uploads.ToList());
             
        }
        

        private async Task<string> NewMultipartUploadAsync(string bucketName, string objectName, string contentType)
        {
            var path = bucketName + "/" + utils.UrlEncode(objectName) + "?uploads";
            var request = new RestRequest(path, Method.POST);
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }
            request.AddHeader("Content-Type", contentType);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this._client.ParseError(response);
            }
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            InitiateMultipartUploadResult newUpload = (InitiateMultipartUploadResult)(new XmlSerializer(typeof(InitiateMultipartUploadResult)).Deserialize(stream));
            return newUpload.UploadId;
        }
        
        private async Task<string> PutObjectAsync(string bucketName, string objectName, string uploadId, int partNumber, byte[] data, string contentType)
        {
            var path = bucketName + "/" + utils.UrlEncode(objectName);
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                path += "?uploadId=" + uploadId + "&partNumber=" + partNumber;
            }
            var request = new RestRequest(path, Method.PUT);
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }

            request.AddHeader("Content-Type", contentType);
            request.AddParameter(contentType, data, RestSharp.ParameterType.RequestBody);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this._client.ParseError(response);
            }
            string etag = null;
            foreach (Parameter parameter in response.Headers)
            {
                if (parameter.Name == "ETag")
                {
                    etag = parameter.Value.ToString();
                }
            }
            return etag;
            
           
        }
        
        private async Task<Tuple<ListMultipartUploadsResult, List<Upload>>> GetMultipartUploadsListAsync(string bucketName,
                                                                                     string prefix,
                                                                                     string keyMarker,
                                                                                     string uploadIdMarker,
                                                                                     string delimiter)
        {
            var queries = new List<string>();
            queries.Add("uploads");
            if (prefix != null)
            {
                queries.Add("prefix=" + Uri.EscapeDataString(prefix));
            }
            if (keyMarker != null)
            {
                queries.Add("key-marker=" + Uri.EscapeDataString(keyMarker));
            }
            if (uploadIdMarker != null)
            {
                queries.Add("upload-id-marker=" + uploadIdMarker);
            }
            if (delimiter != null)
            {
                queries.Add("delimiter=" + delimiter);
            }

            queries.Add("max-uploads=1000");

            string query = string.Join("&", queries);
            string path = bucketName;
            path += "?" + query;

            var request = new RestRequest(path, Method.GET);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                this._client.ParseError(response);
            }
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ListMultipartUploadsResult listBucketResult = (ListMultipartUploadsResult)(new XmlSerializer(typeof(ListMultipartUploadsResult)).Deserialize(stream));

            XDocument root = XDocument.Parse(response.Content);

            var uploads = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload")
                            select new Upload()
                            {
                                Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                                UploadId = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}UploadId").Value,
                                Initiated = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Initiated").Value
                            });

            return new Tuple<ListMultipartUploadsResult, List<Upload>>(listBucketResult, uploads.ToList());
            
        }

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="bucketName">Bucket to list all incomplepte uploads from</param>
        /// <param name="prefix">prefix to list all incomplepte uploads</param>
        /// <param name="recursive">option to list incomplete uploads recursively</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        public  IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix="", bool recursive=true)
        {
            if (recursive)
            {
                return this.listIncompleteUploads(bucketName, prefix, null);
            }
            return this.listIncompleteUploads(bucketName, prefix, "/");
        }

    
        /// <summary>
        /// Lists all or delimited incomplete uploads in a given bucket with a given objectName
        /// </summary>
        /// <param name="bucketName">Bucket to list incomplete uploads from</param>
        /// <param name="objectName">Key of object to list incomplete uploads from</param>
        /// <param name="delimiter">delimiter of object to list incomplete uploads</param>
        /// <returns></returns>
        private IObservable<Upload> listIncompleteUploads(string bucketName, string prefix, string delimiter)
        {
            return Observable.Create<Upload>(
              async obs =>
              {
                  string nextKeyMarker = null;
                  string nextUploadIdMarker = null;
                  bool isRunning = true;

                  while (isRunning)
                  {
                      var uploads = await this.GetMultipartUploadsListAsync(bucketName, prefix, nextKeyMarker, nextUploadIdMarker, delimiter);
                      foreach (Upload upload in uploads.Item2)
                      {
                          obs.OnNext(upload);
                      }
                      nextKeyMarker = uploads.Item1.NextKeyMarker;
                      nextUploadIdMarker = uploads.Item1.NextUploadIdMarker;
                      isRunning = uploads.Item1.IsTruncated;
                  }
              });
          
        }

        //end to refactor
        public async Task RemoveObjectAsync(string bucketName, string objectName)
        {
            var request = new RestRequest(bucketName + "/" + utils.UrlEncode(objectName), Method.DELETE);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);


            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                this._client.ParseError(response);
            }
        }

        public async Task<ObjectStat> StatObjectAsync(string bucketName, string objectName)
        {
            var request = new RestRequest(bucketName + "/" + utils.UrlEncode(objectName), Method.HEAD);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                this._client.ParseError(response);
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

       
        internal byte[] ReadFull(Stream data, int currentPartSize)
        {
            byte[] result = new byte[currentPartSize];
            int totalRead = 0;
            while (totalRead < currentPartSize)
            {
                byte[] curData = new byte[currentPartSize - totalRead];
                int curRead = data.Read(curData, 0, currentPartSize - totalRead);
                if (curRead == 0)
                {
                    break;
                }
                for (int i = 0; i < curRead; i++)
                {
                    result[totalRead + i] = curData[i];
                }
                totalRead += curRead;
            }

            if (totalRead == 0) return null;

            if (totalRead == currentPartSize) return result;

            byte[] truncatedResult = new byte[totalRead];
            for (int i = 0; i < totalRead; i++)
            {
                truncatedResult[i] = result[i];
            }
            return truncatedResult;
        }
    }
}
