﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.Api.DataModel;

namespace Minio.Examples
{
    class ListBuckets
    {
        public async static Task Run(Minio.Api.MinioRestClient minio)
        {
            try
            {
                var list = await minio.Buckets.ListBucketsAsync();
                foreach (Bucket bucket in list.Buckets)
                {
                    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Redeem]  Exception: {0}", e);
            }
        }

       
    }
}