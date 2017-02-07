
# Minio .NET Library for Amazon S3 Compatible Cloud Storage [![Slack](https://slack.minio.io/slack?type=svg)](https://slack.minio.io)

This quickstart guide will show you how to install the Minio client SDK, connect to Minio, and provide a walkthrough for a simple file uploader. For a complete list of APIs and examples, please take a look at the [Dotnet Client API Reference](https://docs.minio.io/docs/dotnet-client-api-reference).

This document assumes that you have a working VisualStudio development environment.  

## Install from NuGet [![Build Status](https://travis-ci.org/minio/minio-dotnet.svg?branch=master)](https://travis-ci.org/minio/minio-dotnet)

```powershell
To install Minio .NET package, run the following command in Nuget Package Manager Console

PM> Install-Package Minio
```

## Initialize Minio Client

Minio client requires the following four parameters specified to connect to an Amazon S3 compatible object storage.


| Parameter  | Description| 
| :---         |     :---     |
| endpoint   | URL to object storage service.   | 
| accessKeyID | Access key is the user ID that uniquely identifies your account. |   
| secretAccessKey | Secret key is the password to your account. |
| secure | Chain WithSSL() method to client object to enable secure (HTTPS) access. |


## Example
```cs
using Minio;

private static MinioClient minio = new MinioClient("play.minio.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();
var getListBucketsTask = minio.Api.ListBucketsAsync();
Task.WaitAll(getListBucketsTask); // block while the task completes
var list = getListBucketsTask.Result;

foreach (Bucket bucket in list.Buckets            
{                
    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
}

```
## Quick Start Example - File Uploader

This example program connects to an object storage server, creates a bucket and uploads a file to the bucket.
```cs
using System;
using Minio;
using Minio.Exceptions;
using Minio.DataModel;
using System.Threading.Tasks;

namespace FileUploader
{
    class FileUpload
    {
        static void Main(string[] args)
        {
            var endpoint  = "play.minio.io:9000";
            var accessKey = "Q3AM3UQ867SPQQA43P2F";
            var secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            try
            { 
                var minio = new MinioClient(endpoint, accessKey, secretKey).WithSSL();
                FileUpload.Run(minio).Wait();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }
        //Check if a bucket exists
        private async static Task Run(MinioClient minio)
        {
            // Make a new bucket called mymusic.
            var bucketName = "mymusic";
            var location   = "us-east-1";
            // Upload the zip file
            var objectName = "golden-oldies.zip";
            var filePath = "/tmp/golden-oldies.zip";
            var contentType = "application/zip";

            try
            {
                bool success = await minio.Api.MakeBucketAsync(bucketName, location);
                if (!success) {
                    bool found = await minio.Api.BucketExistsAsync(bucketName);
                    Console.Out.WriteLine("bucket-name was " + ((found == true) ? "found" : "not found"));
                }
                else { 
                    await minio.Api.PutObjectAsync(bucketName, objectName, filePath, contentType);  
                    Console.Out.WriteLine("Successfully uploaded " + objectName);
                }
               
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
   

    }
}
```
#### Run FileUploader
Click on FileUploader project and Start
### Additional Examples

## Full Examples

#### Full Examples : Bucket Operations

* [MakeBucket.cs](./Minio.Examples/Cases/MakeBucket.cs)
* [ListBuckets.cs](./Minio.Examples/Cases/ListBuckets.cs)
* [BucketExists.cs](./Minio.Examples/Cases/BucketExists.cs)
* [RemoveBucket.cs](./Minio.Examples/Cases/RemoveBucket.cs)
* [Listobjects.cs](./Minio.Examples/Cases/Listobjects.cs)
* [ListIncompleteUploads.cs](./Minio.Examples/Cases/ListIncompleteUploads.cs)

#### Full Examples : Bucket policy Operations
* [GetPolicy.cs](./Minio.Examples/Cases/GetPolicy.cs)
* [SetPolicy.cs](./Minio.Examples/Cases/SetPolicy.cs)

#### Full Examples : File Object Operations
* [FGetObject.cs](./Minio.Examples/Cases/FGetObject.cs)
* [FPutObject.cs](./Minio.Examples/Cases/FPutObject.cs)

#### Full Examples : Object Operations
* [GetObject.cs](./Minio.Examples/Cases/GetObject.cs)
* [PutObject.cs](./Minio.Examples/Cases/PutObject.cs)
* [StatObject.cs](./Minio.Examples/Cases/StatObject.cs)
* [RemoveObject.cs](./Minio.Examples/Cases/RemoveObject.cs)
* [CopyObject.cs](./Minio.Examples/Cases/CopyObject.cs)
* [RemoveIncompleteUpload.cs](./Minio.Examples/Cases/RemoveIncompleteUpload.cs)

#### Full Examples : Presigned Operations
* [PresignedGetObject.cs](./Minio.Examples/Cases/PresignedGetObject.cs)
* [PresignedPutObject.cs](./Minio.Examples/Cases/PresignedPutObject.cs)
* [PresignedPostPolicy.cs](./Minio.Examples/Cases/PresignedPostPolicy.cs)

#### Full Examples : Client Custom Settings
* [SetAppInfo](./Minio.Examples/Program.cs)
* [SetTraceOn](./Minio.Examples/Program.cs)
* [SetTraceOff](./Minio.Examples/Program.cs)

### How to run these examples?
### On Windows

•Build Minio solution

•Move into Minio.Examples directory and run the project. Uncomment cases that you want to run 
 in Program.cs to play with it.


## Explore Further
* [Complete Documentation](https://docs.minio.io)

## Contribute

[Contributors Guide](https://github.com/minio/minio-go/blob/master/CONTRIBUTING.md)

