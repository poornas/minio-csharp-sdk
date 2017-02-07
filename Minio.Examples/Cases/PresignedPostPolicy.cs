/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Minio.DataModel;
using System;
using System.Collections.Generic;

namespace Minio.Examples.Cases
{
    public class PresignedPostPolicy
    {
        public static int Run()
        {
            /// Note: s3 AccessKey and SecretKey needs to be added in App.config file
            /// See instructions in README.md on running examples for more information.
            var client = new MinioClient(
                                 Environment.GetEnvironmentVariable("AWS_ENDPOINT"),
                                 Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"),
                                 Environment.GetEnvironmentVariable("AWS_SECRET_KEY")
                                 ).WithSSL();

            PostPolicy form = new PostPolicy();
            DateTime expiration = DateTime.UtcNow;
            form.SetExpires(expiration.AddDays(10));
            form.SetKey("my-objectname");
            form.SetBucket("my-bucketname");

            Dictionary<string, string> formData = client.Api.PresignedPostPolicy(form);
            string curlCommand = "curl ";
            foreach (KeyValuePair<string, string> pair in formData)
            {
                curlCommand = curlCommand + " -F " + pair.Key + "=" + pair.Value;
            }
            curlCommand = curlCommand + " -F file=@/etc/bashrc https://s3.amazonaws.com/my-bucketname";
            Console.Out.WriteLine(curlCommand);
            return 0;
        }
    }
}
