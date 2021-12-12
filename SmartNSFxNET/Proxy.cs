using System;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using OpenNTF.SmartNSFxNET.Models;

namespace OpenNTF.SmartNSFxNET
{
    public class Proxy
    {
        private HttpClient _httpClient;
        private IHttpCallCustomizer _customizer;

        public Proxy(IHttpClientFactory httpClientFactory, IHttpCallCustomizer customizer)
        {
            this._customizer = customizer;
            this._httpClient = httpClientFactory.CreateClient();
            this._customizer.OnClientCreate(this._httpClient);
        }
        public async Task<IActionResult> MakeProxyCall(HttpRequest req, ILogger log)
        {
            string action = req.Query["action"];
            log.LogInformation("Action is: " + action);

            if (String.IsNullOrEmpty(action))
            {
                return new BadRequestObjectResult(new { error = "Need action parameter" });
            }
            HttpRequestMessage request = await BuildHttpRequestMessage(req, action);
            this._customizer.BeforeHttpCall(request);
            HttpResponseMessage responseMessage = await this._httpClient.SendAsync(request);
            if (responseMessage.IsSuccessStatusCode)
            {

                string respString = await responseMessage.Content.ReadAsStringAsync();
                dynamic respData = JsonConvert.DeserializeObject(respString);
                return new OkObjectResult(respData);
            }
            else
            {
                return new BadRequestObjectResult(new
                {
                    error = "SmartNSF Call failed",
                    errorCode = responseMessage.StatusCode,
                    errorResaon = responseMessage.ReasonPhrase
                });
            }
        }
        public async Task<IActionResult> MakeBinaryProxyCall(HttpRequest req, ILogger log)
        {
            string action = req.Query["action"];
            log.LogInformation("Action is: " + action);

            if (String.IsNullOrEmpty(action))
            {
                return new BadRequestObjectResult(new { error = "Need action parameter" });
            }

            HttpRequestMessage request = await BuildHttpRequestMessageForBinaryCalls(req, action);
            this._customizer.BeforeHttpCall(request);
            HttpResponseMessage responseMessage = await this._httpClient.SendAsync(request);
            if (responseMessage.IsSuccessStatusCode)
            {
                HttpResponseHeaders headers = responseMessage.Headers;
                IEnumerable<string> values;
                string contentType = "application/octet-stream";
                log.LogInformation(headers.ToString());
                if (headers.TryGetValues("Content-Type", out values))
                {
                    contentType = values.First();
                }
                log.LogInformation("Found: " + contentType);
                byte[] resData = await responseMessage.Content.ReadAsByteArrayAsync();
                return new FileContentResult(resData, contentType);
            }
            else
            {
                return new BadRequestObjectResult(new
                {
                    error = "SmartNSF Call failed",
                    errorCode = responseMessage.StatusCode,
                    errorResaon = responseMessage.ReasonPhrase
                });
            }
        }

        private async Task<HttpRequestMessage> BuildHttpRequestMessageForBinaryCalls(HttpRequest req, string action)
        {
            switch (req.Method)
            {
                case "GET":
                    return BuildGetHttpRequestMessage(action);
                case "POST":
                    return await BuildBinaryPostHttpRequestMessage(req, action);
            }
            throw new Exception("Methodhandling for " + req.Method + " not implemented.");

        }


        private async Task<HttpRequestMessage> BuildHttpRequestMessage(HttpRequest req, string action)
        {
            switch (req.Method)
            {
                case "GET":
                    return BuildGetHttpRequestMessage(action);
                case "POST":
                    return await BuildPostHttpRequestMessage(req, action);
            }
            throw new Exception("Methodhandling for " + req.Method + " not implemented.");
        }

        private HttpRequestMessage BuildGetHttpRequestMessage(string action)
        {
            return new HttpRequestMessage(HttpMethod.Get, action);
        }
        private async Task<HttpRequestMessage> BuildPostHttpRequestMessage(HttpRequest req, string action)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, action);
            message.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            return message;
        }
        private async Task<HttpRequestMessage> BuildBinaryPostHttpRequestMessage(HttpRequest req, string action)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            FileData data = JsonConvert.DeserializeObject<FileData>(requestBody);
            HttpContent content = new ByteArrayContent(Convert.FromBase64String(data.File));
            content.Headers.Add("Content-Type", data.Type);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, action);
            message.Content = content;
            return message;
        }
    }
}
