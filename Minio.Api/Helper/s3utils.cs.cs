﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Helper
{
    class s3utils
    {
        // Sentinel URL is the default url value which is invalid.
        static Uri sentinelURL = new Uri("");
        internal static bool isAmazonEndPoint(Uri uri)
        {
            if (isAmazonChinaEndPoint(uri) )
            {
                return true;
            }
            return uri.Host == "s3.amazonaws.com";
        }
        // IsAmazonChinaEndpoint - Match if it is exactly Amazon S3 China endpoint.
        // Customers who wish to use the new Beijing Region are required
        // to sign up for a separate set of account credentials unique to
        // the China (Beijing) Region. Customers with existing AWS credentials
        // will not be able to access resources in the new Region, and vice versa.
        // For more info https://aws.amazon.com/about-aws/whats-new/2013/12/18/announcing-the-aws-china-beijing-region/
        internal static bool isAmazonChinaEndPoint(Uri uri)
        {
            if (uri == sentinelURL)
            {
                return true;
            }
            return uri.Host == "s3.cn-north-1.amazonaws.com.cn";
        }
        // IsGoogleEndpoint - Match if it is exactly Google cloud storage endpoint.
        internal static bool isGoogleEndpoint(Uri endpointUri)
        {
            if (endpointUri == sentinelURL)
            {
                return false;
            }

            return endpointUri.Host == "storage.googleapis.com";
        }

    }
}
