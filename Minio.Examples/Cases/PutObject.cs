using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class PutObject
    {
        //get object in a bucket
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes("hello world");

                await minio.Objects.PutObjectAsync("asiatrip", "hellotext", new MemoryStream(data), 11, "application/octet-stream");

            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
      
    }
}
