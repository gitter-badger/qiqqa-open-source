﻿using Qiqqa.Common.Configuration;
using Qiqqa.UtilisationTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Utilities.Internet;
using Utilities.Misc;

namespace Qiqqa.FeatureTracking
{
    class GoogleAnalysicsSubmitter
    {                                                      
        private static readonly string TRACKING_ID = "UA-16059803-8";
        private static readonly string URL = "http://www.google-analytics.com/collect";

        private static DateTime last_ga_failure_time = DateTime.MinValue;

        
        public static void Submit(Feature feature)
        {
            // Do not track if we have been asked not to
            if (!ConfigurationManager.Instance.ConfigurationRecord.Feedback_UtilisationInfo) return;

            // Do not try if we have failed recently
            if (15 > DateTime.UtcNow.Subtract(last_ga_failure_time).TotalMinutes) return;

            // Queue in background
            SafeThreadPool.QueueUserWorkItem(o => Submit_BACKGROUND(feature));
        }

        private static void Submit_BACKGROUND(Feature feature)
        {
            try
            {
                // Build up the request
                StringBuilder sb = new StringBuilder();
                {
                    sb.Append("v=1");
                    sb.AppendFormat("&tid={0}", TRACKING_ID);
                    sb.AppendFormat("&cid={0}", ConfigurationManager.Instance.ConfigurationRecord.Feedback_GATrackingCode);
                    sb.AppendFormat("&t={0}", "pageview");
                    sb.AppendFormat("&dp=/Client/Windows/Feature/{0}", feature.Name);
                }
                string request = sb.ToString();

                // Send
                WebClient wc = new WebClient();
                wc.Proxy = ConfigurationManager.Instance.Proxy;
                string response = wc.UploadString(URL, request);
            }
            catch (Exception)
            {
                last_ga_failure_time = DateTime.UtcNow;
            }
        }
    }
}
