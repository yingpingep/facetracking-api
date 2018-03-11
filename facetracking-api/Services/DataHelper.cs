using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using facetracking_api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace facetracking_api.Services
{
    // Use web api to get user information.
    public class DataHelper
    {
        private Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private string _endPoint;

        public DataHelper()
        {
            if (_localSettings.Values[Constants.WebEndPoint] == null)
            {
                throw new Exception("You have to settup web endpoint for this page.");
            }
            else
            {
                _endPoint = _localSettings.Values[Constants.WebEndPoint].ToString();
            }
        }

        public async Task ChangeAttendStatusAsync(string aname, bool newStatus)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string jsonString = JsonConvert.SerializeObject(new { name = aname, status = newStatus });
                var res = await httpClient.PutAsync(_endPoint + "/api/actday/", new StringContent(jsonString, Encoding.UTF8, "application/json"));
                if (res.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("Cannot change status.");
                }

                return;
            }
        }

        public async Task<List<CostModel>> GetNumbers()
        {
            List<CostModel> costList = new List<CostModel>();
            using (HttpClient httpClient = new HttpClient())
            {
                var res = await httpClient.GetAsync(_endPoint + "/api/actday");
                if (res.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("Cannot get now.");
                }

                string resultString = await res.Content.ReadAsStringAsync();
                var aa = JsonConvert.DeserializeObject<CostModel[]>(resultString);
                return aa.ToList();
            }
        }
    }
}
