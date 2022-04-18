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
    public class SessionAsSignerProxy
    {
        private SmartNSFProxyClient _snsfpClient;
        
        public SessionAsSignerProxy(SmartNSFProxyClient client)
        {
            this._snsfpClient =client;
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
            try {
                return new OkObjectResult( await this._snsfpClient.SendAsync<dynamic>(request, log));
            } catch(SmartNSFProxyException e) {
                return new BadRequestObjectResult(new
                {
                    error = e.Message,
                    errorCode = e.SmartNSFStatusCode,
                    errorReason = e.SmartNSFError,
                    errorTrace = e.SmartNSFTrace
                });
            }
            catch(Exception e) {
                return new BadRequestObjectResult(new
                {
                    error = e.Message,
                    errorTrace = e.StackTrace
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
            try{
                HttpResponseMessage responseMessage = await this._snsfpClient.SendRecieveResponseAsync(request, log);
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
            }catch(SmartNSFProxyException e) {
                return new BadRequestObjectResult(new
                {
                    error = e.Message,
                    errorCode = e.SmartNSFStatusCode,
                    errorReason = e.SmartNSFError,
                    errorTrace = e.SmartNSFTrace
                });
            }
            catch(Exception e) {
                return new BadRequestObjectResult(new
                {
                    error = e.Message,
                    errorTrace = e.StackTrace
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
