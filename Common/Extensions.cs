using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APSSAMCognito.Web.Common
{
    public static class Extensions
    {
        public static string GetCallbackUrl(this PageModel pageModel)
        {
            var callbackUrl =  $"{pageModel.Request.Scheme}://{pageModel.Request.Host}";

            var pathBase = pageModel.Request.PathBase.Value;
            if (!string.IsNullOrWhiteSpace(pathBase) && pathBase != "/")
                callbackUrl = callbackUrl + $"{pathBase}/Callback";
            else
                callbackUrl = callbackUrl + "/Callback";

            return callbackUrl;
        }

        public static Dictionary<string, string> GetCognitoState(this HttpRequest request)
        {
            var dict = new Dictionary<string, string>();

            var query = request.Query;
            if (query.ContainsKey("state"))
            {
                string stateValue = query["state"][0].ToString();
                //the state value will be like  state=param1=value1-param2=value2
                //I am not using + sign. Instaed I am using - sign. Bcos + is escaped as space in HttpRequest object.
                //reference : https://stackoverflow.com/questions/51143646/querystring-parameters-in-callback-url-for-aws-cognito/51308969
                var tokens = stateValue.Split("-").ToList();
                foreach (var token in tokens)
                {
                    var parts = token.Split("=");
                    dict.Add(parts[0], parts[1]);
                }
            }
            return dict;
        }
        public static string SetCognitoState(this HttpRequest request)
        {
            string state;
            var query = request.Query;
            state = string.Join("-", query.Select(x => $"{x.Key}={x.Value[0]}"));
            return (!string.IsNullOrWhiteSpace(state)) ? state : null;
        }
    }
}

