using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlogComments;

public static class GoogleRecaptchaApi
{
    public static async Task VerifyAsync(string secret, string clientResponse)
    {
        var uri = QueryHelpers.AddQueryString("https://www.google.com/recaptcha/api/siteverify", new Dictionary<string, string>
        {
            { "secret", secret },
            { "response", clientResponse },
        });
        var response = await Constants.HttpClient.PostAsync(uri, null);
        try
        {
            response.EnsureSuccessStatusCode();
            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (responseJson.RootElement.GetProperty("success").GetBoolean())
                return;
            throw new InvalidOperationException($"Recaptcha validation failed: {responseJson.RootElement.GetProperty("error-codes")}");
        }
        catch (Exception ex)
        {
            throw new BadRequestException(ex);
        }
    }
}
