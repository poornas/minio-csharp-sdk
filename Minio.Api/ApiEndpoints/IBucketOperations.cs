using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minio.DataModel;
using RestSharp;
namespace Minio
{
    public interface IBucketOperations
    {

        Task<bool> MakeBucketAsync(string bucketName, string location = "us-east-1");

        Task<ListAllMyBucketsResult> ListBucketsAsync();

        Task<bool> BucketExistsAsync(string bucketName);

        Task RemoveBucketAsync(string bucketName);
        Task<PolicyType> GetPolicyAsync(String bucketName, String objectPrefix);

        Task SetPolicyAsync(String bucketName, String objectPrefix, PolicyType policyType);
        
    }
}