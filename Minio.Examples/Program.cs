﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples
{
    class Program
    {
        public static void Main(string[] args)
        {
            var minioClient = new Minio.Api.MinioRestClient("play.minio.io:9000",
              "Q3AM3UQ867SPQQA43P2F",
              "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
              ).WithSSL();
            //Cases.ListBuckets.Run(minioClient).Wait();
            //Cases.MakeBucket.Run(minioClient).Wait();
            Console.ReadLine();
        }

    }
}