using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Models;
using Newtonsoft.Json;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Sender
{
    public static class SlackMessageSender
    {
        private static readonly HttpClient Client = new HttpClient();
        public static async Task SendMessageAsync(string token, SlackMessage msg)
        {
            // serialize method parameters to JSON
            var content = JsonConvert.SerializeObject(msg);
            var httpContent = new StringContent(
                content,
                Encoding.UTF8,
                "application/json"
            );

            // Use per-request headers to avoid thread-safety issues with DefaultRequestHeaders
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;

            // send message to API
            var response = await Client.SendAsync(request);

            // fetch response from API
            var responseJson = await response.Content.ReadAsStringAsync();

            // convert JSON response to object
            SlackMessageResponse messageResponse =
                JsonConvert.DeserializeObject<SlackMessageResponse>(responseJson);

            // throw exception if sending failed
            if (messageResponse.Ok == false)
            {
                throw new Exception(
                    "failed to send message. error: " + messageResponse.Error
                );
            }
        }
    }
}
