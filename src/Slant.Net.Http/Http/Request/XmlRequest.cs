using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Slant.Net.Http.Response;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http.Request
{
    /// <summary>
    /// Base XML request
    /// </summary>
    public class XmlRequestBase : RestRequest
    {
        public Encoding Encoding { get; set; }

        protected XmlRequestBase(HttpMethod method) 
            : base(method)
        {
            Encoding = Encoding.UTF8;

            Method = method;
            ContentType = HttpContentTypes.TextXml;
        }
    }

    /// <summary>
    /// XML request with content as string (text/xml)
    /// </summary>
    public class XmlRequest : XmlRequestBase
    {
        public XmlRequest(HttpMethod method) : base(method)
        {
            CreateMessage = () => DefaultCreateRequestMessage(new StringContent(Body, Encoding, this.ContentType));
        }

        public string Body { get; set; }
    }

    public class XmlRequest<T> : XmlRequest, IRestRequest<T> where T : class 
    {
        public XmlRequest(HttpMethod method) 
            : base(method)
        {
            ReadDataMessageAsync = this.DefaultReadDataMessageAsync<T>;
        }

        public Func<HttpResponseMessage, IObjectConverter, Task<IRestResponse<T>>> ReadDataMessageAsync { get; set; }
    }

    public class XElementRequest : XmlRequestBase
    {
        public XElement Root { get; set; }

        public XElementRequest(HttpMethod method, XElement xe) 
            : base(method)
        {
            Root = xe;
            CreateMessage = () => DefaultCreateRequestMessage(ToHttpContent());
        }

        //public HttpContent ToHttpContent()
        //{
        //    return new PushStreamContent((stream, content, ctx) =>
        //    {
        //        using (var writer = XmlWriter.Create(stream,
        //            new XmlWriterSettings() { CloseOutput = true }))
        //        {
        //            Root.WriteTo(writer);
        //        }
        //    }, ContentType);
        //}

        public HttpContent ToHttpContent()
        {
            return new StringContent(Root.ToString(), Encoding.UTF8, ContentType);
        }
    }
}