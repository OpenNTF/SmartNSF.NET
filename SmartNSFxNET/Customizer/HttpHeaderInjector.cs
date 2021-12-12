using System.Net.Http;
using System;
using OpenNTF.SmartNSFxNET;

namespace OpenNTF.SmartNSFxNET.Customizer
{
    public class HttpHeaderInjector : IHttpCallCustomizer
    {
        private string _smartNSFEndpoint;
        private string _headerName;
        private string _headerValue;
        public HttpHeaderInjector(string endpoint, string headerName, string headerValue)
        {
            _smartNSFEndpoint = endpoint;
            _headerName = headerName;
            _headerValue = headerValue;
        }
        public void BeforeHttpCall(HttpRequestMessage requestMessage)
        {
        }

        public void OnClientCreate(HttpClient client)
        {
            client.BaseAddress = new Uri(_smartNSFEndpoint);
            client.DefaultRequestHeaders.Add(_headerName, _headerValue);

        }
    }
}