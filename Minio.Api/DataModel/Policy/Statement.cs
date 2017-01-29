﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Minio.Policy;
namespace Minio.DataModel
{
    [DataContract]
    internal class Statement
    {
        [JsonProperty("Action")]
        public ISet<string> actions { get;  set; }
        [JsonProperty("Condition")]
        public ConditionMap conditions { get;  set; }

        [JsonProperty("Effect")]
        public string effect { get;  set; }

       [JsonProperty("Principal")]
        public Principal principal { get;  set; }

        [JsonProperty("Resource")]
        public Resources resources { get;  set; }

        [JsonProperty("Sid")]
        public string sid { get;  set; }
        /**
        * Returns whether given statement is valid to process for given bucket name.
         */
        public bool isValid(string bucketName)
        {
            ISet<string> intersection = new HashSet<string>(this.actions);
            intersection.IntersectWith(Constants.VALID_ACTIONS());
            if (intersection.Count == 0)
            {
                return false;
            }
            if (!this.effect.Equals("Allow"))
            {
                return false;
            }

            ISet<string> aws = this.principal.aws();
            if (aws == null || !aws.Contains("*"))
            {
                return false;
            }

            string bucketResource = Constants.AWS_RESOURCE_PREFIX + bucketName;

            if (this.resources.Contains(bucketResource))
            {
                return true;
            }

            if (this.resources.startsWith(bucketResource + "/").Count == 0)
            {
                return false;
            }

            return true;
        }

        /**
         * Removes object actions for given object resource.
         */
        public void removeObjectActions(string objectResource)
        {
            if (this.conditions != null)
            {
                return;
            }

            if (this.resources.Count > 1)
            {
                this.resources.Remove(objectResource);
            }
            else
            {
                this.actions.Except(Constants.READ_WRITE_OBJECT_ACTIONS());
            }
        }
        private void removeReadOnlyBucketActions(string prefix)
        {
            if (!this.actions.IsProperSupersetOf(Constants.READ_ONLY_BUCKET_ACTIONS))
            {
                return;
            }

            this.actions.Except(Constants.READ_ONLY_BUCKET_ACTIONS);

            if (this.conditions == null)
            {
                return;
            }

            if (prefix == null || prefix.Count() == 0)
            {
                return;
            }

            ConditionKeyMap stringEqualsValue;
            this.conditions.TryGetValue("StringEquals", out stringEqualsValue);
            if (stringEqualsValue == null)
            {
                return;
            }

            ISet<string> values;
            stringEqualsValue.TryGetValue("s3:prefix", out values);
            if (values != null)
            {
                values.Remove(prefix);
            }

            if (values == null || values.Count == 0)
            {
                stringEqualsValue.Remove("s3:prefix");
            }

            if (stringEqualsValue.Count == 0)
            {
                this.conditions.Remove("StringEquals");
            }

            if (this.conditions.Count == 0)
            {
                this.conditions = null;
            }
        }

        private void removeWriteOnlyBucketActions()
        {
            if (this.conditions == null)
            {
                this.actions.Except(Constants.WRITE_ONLY_BUCKET_ACTIONS);
            }
        }

        /**
    * Removes bucket actions for given prefix and bucketResource.
    */
        public void removeBucketActions(string prefix, string bucketResource,
                                    bool readOnlyInUse, bool writeOnlyInUse)
        {
            if (this.resources.Count > 1)
            {
                this.resources.Remove(bucketResource);
                return;
            }

            if (!readOnlyInUse)
            {
                removeReadOnlyBucketActions(prefix);
            }

            if (!writeOnlyInUse)
            {
                removeWriteOnlyBucketActions();
            }

            return;
        }

        /**
         * Returns bucket policy types for given prefix.
         */
        // [JsonIgnore]
        public bool[] getBucketPolicy(string prefix)
        {
            bool commonFound = false;
            bool readOnly = false;
            bool writeOnly = false;

            ISet<string> aws = this.principal.aws();
            if (!(this.effect.Equals("Allow") && aws != null && aws.Contains("*")))
            {
                return new bool[] { commonFound, readOnly, writeOnly };
            }

            if (this.actions.IsProperSupersetOf(Constants.COMMON_BUCKET_ACTIONS) && this.conditions == null)
            {
                commonFound = true;
            }

            if (this.actions.IsProperSupersetOf(Constants.WRITE_ONLY_BUCKET_ACTIONS) && this.conditions == null)
            {
                writeOnly = true;
            }

            if (this.actions.IsProperSupersetOf(Constants.READ_ONLY_BUCKET_ACTIONS))
            {
                if (prefix != null && prefix.Count() != 0 && this.conditions != null)
                {
                    ConditionKeyMap stringEqualsValue;
                    this.conditions.TryGetValue("StringEquals", out stringEqualsValue);
                    if (stringEqualsValue != null)
                    {
                        ISet<string> s3PrefixValues;
                        stringEqualsValue.TryGetValue("s3:prefix", out s3PrefixValues);
                        if (s3PrefixValues != null && s3PrefixValues.Contains(prefix))
                        {
                            readOnly = true;
                        }
                    }
                    else
                    {
                        ConditionKeyMap stringNotEqualsValue;
                        this.conditions.TryGetValue("StringNotEquals", out stringNotEqualsValue);
                        if (stringNotEqualsValue != null)
                        {
                            ISet<string> s3PrefixValues;
                            stringNotEqualsValue.TryGetValue("s3:prefix", out s3PrefixValues);
                            if (s3PrefixValues != null && !s3PrefixValues.Contains(prefix))
                            {
                                readOnly = true;
                            }
                        }
                    }
                }
                else if ((prefix == null || prefix.Count() == 0) && this.conditions == null)
                {
                    readOnly = true;
                }
                else if (prefix != null && prefix.Count() != 0 && this.conditions == null)
                {
                    readOnly = true;
                }
            }

            return new bool[] { commonFound, readOnly, writeOnly };
        }

        /**
        * Returns object policy types.
        */
       // [JsonIgnore]
        public bool[] getObjectPolicy()
        {
            bool readOnly = false;
            bool writeOnly = false;

            ISet<string> aws = null;
            if (this.principal != null)
            {
                aws = this.principal.aws();
            }

            if (this.effect.Equals("Allow")
                && aws != null && aws.Contains("*")
                && this.conditions == null)
            {
                if (this.actions.IsProperSupersetOf(Constants.READ_ONLY_OBJECT_ACTIONS))
                {
                    readOnly = true;
                }
                if (this.actions.IsProperSupersetOf(Constants.WRITE_ONLY_OBJECT_ACTIONS))
                {
                    writeOnly = true;
                }
            }

            return new bool[] { readOnly, writeOnly };
        }
        
    }
}
