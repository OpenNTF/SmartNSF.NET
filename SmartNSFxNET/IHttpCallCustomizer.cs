using System.Net.Http;
namespace OpenNTF.SmartNSFxNET
{
    public interface IHttpCallCustomizer
    {
        void OnClientCreate(HttpClient client);
        void BeforeHttpCall(HttpRequestMessage requestMessage);
    }
}