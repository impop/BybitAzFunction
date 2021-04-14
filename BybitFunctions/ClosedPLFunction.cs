using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using RestSharp;
using System.Linq;

namespace ClosedPL
{
    public static class ClosedPLFunction
    {
        [FunctionName("ClosedPL")]
        public static async Task<JsonResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
               log.LogInformation("C# HTTP trigger function processed a request.");

            var client = new RestClient("https://api-testnet.bybit.com");
            client.UseJson();

            var restRequest = new RestRequest("/private/linear/trade/closed-pnl/list");

            string symbol = req.Query["symbol"];
            symbol ??= "BTCUSDT";
            restRequest.AddQueryParameter("symbol", symbol);
            restRequest.AddQueryParameter("api_key", Environment.GetEnvironmentVariable("BYBIT_API_KEY"));            
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();            
            restRequest.AddQueryParameter("timestamp", timestamp);
            
            restRequest.Parameters.Sort((x, y) => x.Name.CompareTo(y.Name));
            var querystring = "";
            foreach (var p in restRequest.Parameters)
            {
                querystring += p.Name + "=" + p.Value + "&";
            }
            if (querystring.Length > 1)
                querystring = querystring.Remove(querystring.Length - 1, 1);

            var sign = CreateSignature(Environment.GetEnvironmentVariable("BYBIT_API_SECRET"), querystring);

            restRequest.AddQueryParameter("sign", sign);
            var resp = client.Get(restRequest);
            dynamic jsonResponse = JsonConvert.DeserializeObject(resp.Content);

            return new JsonResult(jsonResponse);
        }

        public static string CreateSignature(string secret, string message)
        {
            var signatureBytes = Hmacsha256(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(message));

            return ByteArrayToString(signatureBytes);
        }

        private static byte[] Hmacsha256(byte[] keyByte, byte[] messageBytes)
        {
            using (var hash = new HMACSHA256(keyByte))
            {
                return hash.ComputeHash(messageBytes);
            }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);

            foreach (var b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }
}
