﻿using System;
using System.Collections.Generic;
using RestSharp;

namespace Minio.Api.Exceptions
{
   
    public class MinioApiException : Exception
    {
        
        public IRestResponse response { get; private set; }

        public MinioApiException(IRestResponse response)
            : base($"Minio API responded with status code={response.StatusCode}, response={response.Content}")
        {
            this.response = response;
        }
    }
    
}