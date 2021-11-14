using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using APSSAMCognito.Web.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace APSSAMCognito.Web.Pages
{
    public class CallbackModel : PageModel
    {
        private readonly ILogger _logger;
        private class CognitoToken
        {
            public string id_token{ get; set; }
            public string access_token { get; set; }
            public string refresh_token { get; set; }
        }

        private class RedirectResult
        {
            public string ErrorText { get; set; }
            public string Url { get; set; }
        }

        private readonly IConfiguration _configuration;

        public string IdToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string AuthCode { get; set; }

        public CallbackModel(IConfiguration configuration, ILogger<IndexModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> OnGet(string code)
        {
            AuthCode = code;
            var cognitoToken = await GetCognitoTokens(code);
            IdToken = cognitoToken.id_token;
            _logger.LogInformation($"IdToken : {IdToken}");

            AccessToken = cognitoToken.access_token;
            _logger.LogInformation($"AccessToken : {AccessToken}");
            
            RefreshToken = cognitoToken.refresh_token;
            _logger.LogInformation($"RefreshToken : {RefreshToken}");

            var response = await GetRedirectUrl(cognitoToken.id_token);

            if (response.IsSuccessful)
            {
                var body = JsonSerializer.Deserialize<RedirectResult>(response.Content);
                var redirectUrl = body.Url;
                return Redirect(redirectUrl);
            }
            else
            {
                return StatusCode((int)response.StatusCode, response.Content);
            }
        }

        private async Task<CognitoToken> GetCognitoTokens(string code)
        {
            var tokenUrl = _configuration["AppConfig:Cognito:tokenUrl"];
            var clientId = _configuration["AppConfig:Cognito:Clients:UatClient:ClientId"];
            var clientSecret = _configuration["AppConfig:Cognito:Clients:UatClient:ClientSecret"]; ;
            var redirectUrl = this.GetCallbackUrl();
            _logger.LogInformation($"Callback Url : {redirectUrl}");

            var uriBuilder = new UriBuilder(tokenUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["grant_type"] = "authorization_code";
            query["client_id"] = clientId;
            query["code"] = code;
            query["redirect_uri"] = redirectUrl;
            uriBuilder.Query = query.ToString();

            var authHeaderString = clientId + ":" + clientSecret;
            var bytes = System.Text.Encoding.UTF8.GetBytes(authHeaderString);
            var authHeader = Convert.ToBase64String(bytes);


            _logger.LogInformation($"Token Url : {uriBuilder.Uri.AbsoluteUri}");

            var restClient = new RestClient();
            var restRequest = new RestRequest(uriBuilder.Uri);
            restRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            restRequest.AddHeader("Authorization", "Basic " + authHeader);

            var cognitoToken = await restClient.PostAsync<CognitoToken>(restRequest);
            return cognitoToken;
        }

        private  async Task<IRestResponse>  GetRedirectUrl(string idToken)
        {
            var apiEndPoint = _configuration["AppConfig:AppStream:GenerateStreamingUrlEndPoint"];
            var env = _configuration["AppConfig:Environment"];

            _logger.LogInformation($"GenerateStreamingUrlEndPoint Url : {apiEndPoint}");
            var client = new RestClient();
            var request = new RestRequest(apiEndPoint);
            request.AddHeader("Authorization", idToken);
            var queryParams = this.Request.GetCognitoState();   

            _logger.LogInformation($"Url Parameters :");
            foreach (var param in queryParams)
            {
                request.AddParameter(param.Key, param.Value);
                _logger.LogInformation($"name : {param.Key} value : param.Value");
            }

            request.AddParameter("environment", env);            
            _logger.LogInformation($"environment : {env}");



            /*
            request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

            //to make this work I had to maitain the same json type in reponse to success or failure

            var response = await client.GetAsync<RedirectResult>(request);

            //without using ExecuteAsync like in comments below, I dont know of a way to check for successful vs unsuccessful http response code.
            //so I am going by the non empty Url response to determine success or failure
            if (!string.IsNullOrWhiteSpace(response.Url))
            {
                return response.Url;
            }
            else
            {
                throw new Exception($"Appstream temp url generation error : {response.ErrorText}");
            }
            */

            //Note:GetAsync troubled me quite a bit to work.. It worked only only when response from lambda end point is stringified json and after adding the 
            //OnBeforeDeserialization line above. So using generic ExecuteAsync for better control

            var cancellationTokenSource = new CancellationTokenSource();
            var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);
            return response;
        }
    }
}