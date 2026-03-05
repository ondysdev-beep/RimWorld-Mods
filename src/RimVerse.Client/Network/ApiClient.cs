using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Verse;

namespace RimVerse.Client.Network
{
    public class ApiClient
    {
        private readonly string _baseUrl;
        private string _authToken;

        public ApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
        }

        public T Post<T>(string path, object body)
        {
            var url = _baseUrl + path;
            var json = JsonConvert.SerializeObject(body);

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 10000;

                if (!string.IsNullOrEmpty(_authToken))
                    request.Headers["Authorization"] = "Bearer " + _authToken;

                var bytes = Encoding.UTF8.GetBytes(json);
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var responseJson = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(responseJson);
                }
            }
            catch (WebException ex)
            {
                var errorMsg = ReadErrorResponse(ex);
                Log.Error($"[RimVerse] API POST {path} failed: {errorMsg}");
                throw new RimVerseApiException(errorMsg, ex);
            }
        }

        public T Get<T>(string path)
        {
            var url = _baseUrl + path;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 10000;

                if (!string.IsNullOrEmpty(_authToken))
                    request.Headers["Authorization"] = "Bearer " + _authToken;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var responseJson = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(responseJson);
                }
            }
            catch (WebException ex)
            {
                var errorMsg = ReadErrorResponse(ex);
                Log.Error($"[RimVerse] API GET {path} failed: {errorMsg}");
                throw new RimVerseApiException(errorMsg, ex);
            }
        }

        private static string ReadErrorResponse(WebException ex)
        {
            if (ex.Response == null)
                return ex.Message;

            try
            {
                using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return ex.Message;
            }
        }
    }

    public class RimVerseApiException : Exception
    {
        public RimVerseApiException(string message, Exception inner) : base(message, inner) { }
    }
}
