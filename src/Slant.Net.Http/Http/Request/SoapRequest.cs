using System.Net.Http;
using System.Text;

namespace Slant.Net.Http.Request
{
    public class SoapRequest : XmlRequest
    {
        public string ServiceUrl { get; set; }

        public SoapRequest(HttpMethod method, string operation, string xmlRequest) 
            : base(method)
        {
            Encoding = Encoding.UTF8;
            Body = xmlRequest;
            Path = operation;

            CreateMessage = () =>
            {
                var message = DefaultCreateRequestMessage(new StringContent(Body, Encoding, this.ContentType));
                message.Headers.Add("SOAPAction", $"{ServiceUrl}/{Path}");
                return message;
            };
        }
    }

    public class SoapRequest<T> : XmlRequest<T> where T : class
    {
        public SoapRequest(HttpMethod method) 
            : base(method)
        {

        }
    }
}