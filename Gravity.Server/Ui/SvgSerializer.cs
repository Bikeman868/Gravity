using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinFramework.Pages.Restful.Interfaces;
using OwinFramework.Pages.Restful.Serializers;
using Svg;

namespace Gravity.Server.Ui
{
    public class SvgSerializer: SerializerBase, IResponseSerializer
    {
        public Task HttpStatus(IOwinContext context, HttpStatusCode statusCode, string message = null)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ReasonPhrase = message ?? statusCode.ToString();
            return context.Response.WriteAsync(string.Empty);
        }

        public Task Success<T>(IOwinContext context, T data)
        {
            if (typeof (T) != typeof (SvgDocument))
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ReasonPhrase = "Can not serialize " + typeof(T).FullName + " to SVG";
                return context.Response.WriteAsync(string.Empty);
            }

            var document = (SvgDocument)(object)data;

            string svg;
            using (var stream = new MemoryStream())
            {
                document.Write(stream);
                svg = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ReasonPhrase = "OK";
            context.Response.ContentType = "image/svg+xml";
            return context.Response.WriteAsync(svg);
        }

        public Task Success(IOwinContext context)
        {
            var document = new SvgDocument();
            return Success(context, document);
        }
    }
}