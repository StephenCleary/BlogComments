using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;

namespace BlogComments
{
    public class Function1
    {
        private readonly ILogger _logger;
        private readonly string _recaptchaSecret;
        private readonly string _githubToken;

        public Function1(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
            _recaptchaSecret = config["RECAPTCHA_SECRET"] ?? throw new InvalidOperationException("Missing config key RECAPTCHA_SECRET");
            _githubToken = config["GITHUB_TOKEN"] ?? throw new InvalidOperationException("Missing config key GITHUB_TOKEN");
        }

        [Function("Function1")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            using var _ = Globals.SetCurrentRequest(req);
            try
            {
                var parameters = ParseParameters();

                await GoogleRecaptchaApi.VerifyAsync(_recaptchaSecret, parameters.RecaptchaResponse);

                var commentId = Guid.NewGuid().ToString();
                var date = DateTime.UtcNow;
                var ghc = new GitHubClient(new ProductHeaderValue("blog.stephencleary.com")) { Credentials = new(_githubToken) };
                var commitMessage = $"(Staticman) {parameters.AuthorName}: {parameters.Message}\n\n{parameters.PostUri}#comment-{commentId}";
                var path = $"raw/{parameters.PostId}/{date.ToString("yyyy-MM-dd")}-{commentId}.json";
                var content = new JsonObject()
                {
                    ["_id"] = commentId,
                    ["postId"] = parameters.PostId,
                    ["postUri"] = parameters.PostUri,
                    ["replyTo"] = parameters.ReplyTo,
                    ["authorEmailEncrypted"] = parameters.AuthorEmailEncrypted,
                    ["authorEmailMD5"] = parameters.AuthorEmailMD5,
                    ["authorName"] = parameters.AuthorName,
                    ["authorUri"] = parameters.AuthorUri,
                    ["message"] = parameters.Message,
                    ["date"] = date.ToString("O"),
                }.ToJsonString();
                await ghc.Repository.Content.CreateFile("StephenCleary", "comments.stephencleary.com", path, new(commitMessage, content));

                return req.CreateResponse();
            }
            catch (BadRequestException ex)
            {
                return ex.Response;
            }
        }

        private static (string AuthorName, string AuthorEmailEncrypted, string AuthorEmailMD5, string AuthorUri, string Message, string PostId, string PostUri, string ReplyTo, string RecaptchaResponse) ParseParameters()
        {
            try
            {
                var json = JsonDocument.Parse(Globals.CurrentRequest.Body).RootElement;
                var authorName = json.GetProperty("authorName").GetString() ?? throw new InvalidOperationException("Missing field authorName");
                var authorEmailEncrypted = json.GetProperty("authorEmailEncrypted").GetString() ?? throw new InvalidOperationException("Missing field authorEmailEncrypted");
                var authorEmailMD5 = json.GetProperty("authorEmailMD5").GetString() ?? throw new InvalidOperationException("Missing field authorEmailMD5");
                var authorUri = json.GetProperty("authorUri").GetString() ?? throw new InvalidOperationException("Missing field authorUri");
                var message = json.GetProperty("message").GetString() ?? throw new InvalidOperationException("Missing field message");
                var postId = json.GetProperty("postId").GetString() ?? throw new InvalidOperationException("Missing field postId");
                var postUri = json.GetProperty("postUri").GetString() ?? throw new InvalidOperationException("Missing field postUri");
                var replyTo = json.GetProperty("replyTo").GetString() ?? throw new InvalidOperationException("Missing field replyTo");
                var recaptchaResponse = json.GetProperty("g-recaptcha-response").GetString() ?? throw new InvalidOperationException("Missing field g-recaptcha-response");
                return (authorName, authorEmailEncrypted, authorEmailMD5, authorUri, message, postId, postUri, replyTo, recaptchaResponse);
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                throw new BadRequestException(ex);
            }
        }
    }
}
