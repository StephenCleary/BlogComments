using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BlogComments
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("Function1")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            using var _ = Globals.SetCurrentRequest(req);
            try
            {
                var parameters = ParseParameters();

                _logger.LogInformation("C# HTTP trigger function processed a request.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                response.WriteString("Welcome to Azure Functions!");

                return response;
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
