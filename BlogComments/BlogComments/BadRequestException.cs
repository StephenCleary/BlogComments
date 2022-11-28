using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlogComments
{
    public sealed class BadRequestException : Exception
    {
        public BadRequestException(Exception inner)
            : base($"{inner.GetType().Name}: {inner.Message}", inner)
        {
        }

        public BadRequestException(string message)
            : base(message)
        {
        }

        public HttpResponseData Response
        {
            get
            {
                var result = Globals.CurrentRequest.CreateResponse(HttpStatusCode.BadRequest);
                var stream = new MemoryStream();
                var bytes = Constants.Utf8.GetBytes(Message);
                stream.Write(bytes);
                stream.Position = 0;
                result.Body = stream;
                return result;
            }
        }
    }
}
