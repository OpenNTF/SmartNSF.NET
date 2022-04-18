using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Collections.Generic;

namespace OpenNTF.SmartNSFxNET{
    public interface ISmartNSFProxyClient
    {
        Task<T> GetAsync<T>(string actionUrl, ILogger log);
        Task<R> PostAsync<T, R>(string actionUrl, T data, ILogger log);
        Task<R> SendAsync<R>(HttpRequestMessage message, ILogger log);
        Task<HttpResponseMessage> SendRecieveResponseAsync(HttpRequestMessage message, ILogger log);

        Task<LTPAAnswer> GetLTPAToken(string userName, ILogger log);

        void AddLTPACookie(HttpRequestMessage message, string ltpaToken);

    }
    public class SmartNSFProxyClient : ISmartNSFProxyClient
    {
        private HttpClient _httpClient;
        private SmartNSFConfiguration _configuration;

        public SmartNSFProxyClient(HttpClient client, SmartNSFConfiguration config)
        {
            this._httpClient = client;
            this._configuration = config;
        }
        public async Task<T> GetAsync<T>(string actionUrl, ILogger log)
        {
            HttpResponseMessage response = await this._httpClient.GetAsync(actionUrl);
            return await ProcessHttpMessageResult<T>(response,log);
        }
        public async Task<R> SendAsync<R>(HttpRequestMessage message, ILogger log)
        {
            HttpResponseMessage response = await this._httpClient.SendAsync(message);
            return await ProcessHttpMessageResult<R>(response,log);
        }
        public async Task<HttpResponseMessage> SendRecieveResponseAsync(HttpRequestMessage message, ILogger log)
        {
            return await this._httpClient.SendAsync(message);
        }
        public async Task<R> PostAsync<T,R>(string actionUrl, T data, ILogger log)
        {
            var payload = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            HttpResponseMessage msg = await this._httpClient.PostAsync(actionUrl, payload);
            return await ProcessHttpMessageResult<R>(msg,log);
        }
        private async Task<T> ProcessHttpMessageResult<T>(HttpResponseMessage message, ILogger log) {
            string respString = await message.Content.ReadAsStringAsync();
            if (message.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(respString);
            }
            throw new SmartNSFProxyException("SmartNSF Call failed", message.StatusCode, message.ReasonPhrase, respString);

        }
        public async Task<LTPAAnswer> GetLTPAToken(string userName, ILogger log)
        {
            string idToken = CreateIDToken(userName, log);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "?authorization");
            message.Headers.Add(this._configuration.AuthHeaderName, idToken);

            HttpResponseMessage respond = await this._httpClient.SendAsync(message);
            string respondeString = await respond.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<LTPAAnswer>(respondeString);

        }

        public void AddLTPACookie(HttpRequestMessage message, string ltpaToken)
        {
            string host = this._httpClient.BaseAddress.Host;
            string protocol = this._httpClient.BaseAddress.Scheme;
            CookieContainer container = new CookieContainer();
            Uri ctUri = new Uri(protocol + "://" + host);
            int point = host.IndexOf(".");
            string domain = point > -1 ? host.Substring(point) : host;
            Cookie ltpaCookie = new Cookie("LtpaToken", ltpaToken, "/", domain);
            container.Add(ltpaCookie);
            message.Headers.Add("Cookie", container.GetCookieHeader(ctUri));
        }

        private string CreateIDToken(string email, ILogger log)
        {
            RSA rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(this._configuration.PrivatKey), out _);
            RsaSecurityKey rsaKey = new RsaSecurityKey(rsa);
            var signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };
            var now = DateTime.Now;
            var unixTimeSeconds = new DateTimeOffset(now).ToUnixTimeSeconds();
            var claims = BuildClaims(email, unixTimeSeconds.ToString());
            var jwt = new JwtSecurityToken(
                issuer: this._configuration.Issuer,
                claims: claims,
                notBefore: now,
                expires: now.AddSeconds(86000),
                signingCredentials: signingCredentials
            );
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        private Claim[] BuildClaims(string email, string timeStamp)
        {
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, timeStamp, ClaimValueTypes.Integer64));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim("email", email));
            return claims.ToArray();
        }

    }

    public class SmartNSFProxyException : Exception
    {
        public HttpStatusCode SmartNSFStatusCode { get; }
        public string SmartNSFError { get; }
        public string SmartNSFTrace { get; }
        public SmartNSFProxyException()
        {
        }

        public SmartNSFProxyException(string message)
            : base(message)
        {
        }

        public SmartNSFProxyException(string message, Exception inner)
            : base(message, inner)
        {
        }
        public SmartNSFProxyException(string message, HttpStatusCode statusCode, string smartNSFError, string smartNSFTrace) : base(message)
        {
            this.SmartNSFStatusCode = statusCode;
            this.SmartNSFError = smartNSFError;
            this.SmartNSFTrace = smartNSFTrace;
        }
    }

    public class LTPAAnswer
    {
        public string LtpaToken { set; get; }
        public string UserName { set; get; }
    }
}
