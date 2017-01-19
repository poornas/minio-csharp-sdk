using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples
{
    public class MakeBucket
    {
        //Make a bucket
        public async static Task Run(Minio.Api.MinioRestClient minio)
        {
            try
            {
                await minio.Buckets.MakeBucketAsync("bucket-name");
            } 
            catch (Exception e)
            {
                Console.WriteLine("[Redeem]  Exception: {0}", e);
            }
        }

        
    }
}
