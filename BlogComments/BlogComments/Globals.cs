using Microsoft.Azure.Functions.Worker.Http;
using Nito.Disposables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogComments
{
    public static class Globals
    {
        public static HttpRequestData CurrentRequest => _currentRequest.Value ?? throw new InvalidOperationException("No current request!");

        public static IDisposable SetCurrentRequest(HttpRequestData req)
        {
            var currentRequest = _currentRequest.Value;
            _currentRequest.Value = req;
            return Disposable.Create(() => _currentRequest.Value = currentRequest!);
        }

        private static readonly AsyncLocal<HttpRequestData> _currentRequest = new();
    }
}
