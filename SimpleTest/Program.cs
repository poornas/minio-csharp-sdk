﻿using System;
using Minio;
using Minio.Api.DataModel;
namespace SimpleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            var client = new MinioClient("play.minio.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG");

            var buckets = client.ListBuckets();
            foreach (Bucket bucket in buckets)
            {
                Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
            }
            */
            //return 0;

            var minio = new Minio.Api.MinioRestClient("play.minio.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();
            Console.Out.WriteLine(minio);
            try
            {
                var found = minio.BucketExists("yoyo");
            } catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
            //Console.Out.WriteLine(found);
            minio.Buckets.ListBucketsAsync(result => {
                foreach (Bucket bucket in result.Buckets)
                {
                    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
                }
            });

            Console.ReadLine();
        }
    }
}