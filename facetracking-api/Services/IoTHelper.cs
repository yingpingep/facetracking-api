using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using facetracking_api.Models;
using Newtonsoft.Json;

namespace facetracking_api.Services
{    
    public class IoTHelper
    {
        private HttpClient _httpClient;
        private string baseurl = "https://hackustapi.azurewebsites.net";        

        public IoTHelper()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> GoRegisterAsync(string id)
        {
            RegisterModel data = new RegisterModel()
            {
                DeviceId = id,
                DeviceKey = ""
            };

            string endpoint = string.Format("{0}/api/register", baseurl);
            string content = JsonConvert.SerializeObject(data);


            var response = await _httpClient.PostAsync(endpoint, new StringContent(content, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PostMessageAsync(EmotionModel data)
        {
            string endpoint = string.Format("{0}/api/receive", baseurl);
            string payload = JsonConvert.SerializeObject(data);
            var response = await _httpClient.PostAsync(endpoint, new StringContent(payload, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }     
    }
}
