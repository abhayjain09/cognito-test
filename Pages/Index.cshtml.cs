using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using APSSAMCognito.Web.Common;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace APSSAMCognito.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger _logger;

        private readonly IConfiguration _configuration;
        public string CognitoLoginUri { get; set; }

        public IndexModel(IConfiguration configuration, ILogger<IndexModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            _logger.LogInformation($"Request Parts: {Request.Path.Value};{Request.Path.Value};{Request.Host};{Request.PathBase};{Request.PathBase.Value};{Request.Scheme}");
            CognitoLoginUri = GetCognitoLoginUri();
            return Redirect(CognitoLoginUri);
        }

        private string GetCognitoLoginUri()
        {
            var baseUrl = _configuration["AppConfig:Cognito:loginUrl"];
            var clientId = _configuration["AppConfig:Cognito:Clients:UatClient:ClientId"];
            var redirectUrl = this.GetCallbackUrl();
            var state = Request.SetCognitoState();

            var baseUri = new UriBuilder(baseUrl);
            var queryToAppend = $"client_id={clientId}&response_type=code&scope=email+openid+profile&redirect_uri={redirectUrl}";
            if (!string.IsNullOrEmpty(state))
                queryToAppend = queryToAppend + $"&state={state}";

            if (baseUri.Query != null && baseUri.Query.Length > 1)
                baseUri.Query = baseUri.Query.Substring(1) + "&" + queryToAppend;
            else
                baseUri.Query = queryToAppend;

            _logger.LogInformation($"Cognito login uri is {baseUri.Uri.AbsoluteUri}");
            return baseUri.Uri.AbsoluteUri;
        }
    }
}
