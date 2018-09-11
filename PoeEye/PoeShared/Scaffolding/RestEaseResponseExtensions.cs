using System;
using System.Web;
using Newtonsoft.Json;
using RestEase;

namespace PoeShared.Scaffolding
{
    public static class RestEaseResponseExtensions
    {
        public static T GetContentEx<T>(this Response<T> response)
        {
            try
            {
                if (!response.ResponseMessage.IsSuccessStatusCode)
                {
                    throw new HttpException($"Expected HTTP 200 OK, got {response.ResponseMessage.StatusCode}");
                }
                
                var result = response.GetContent();
                if (result == null)
                {
                    throw new JsonException($"Failed to parse message {response.ResponseMessage}");
                }
                return result;
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to extract type {typeof(T)} from response {response.ResponseMessage}, body:\n{response.StringContent}\n\nRequest: {response?.ResponseMessage?.RequestMessage}", e);
            }
        }
    }
}