﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class BucketExists
    {
        //Check if a bucket exists
        public async static Task Run(Minio.Api.MinioRestClient minio)
        {
            try
            {
                bool found = await minio.Buckets.BucketExistsAsync("bucket-name");
                Console.Out.WriteLine("bucket-name was " + ((found == true) ? "found" : "not found"));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}