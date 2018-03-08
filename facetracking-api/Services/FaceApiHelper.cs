using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace facetracking_api.Services
{    
    public class FaceApiHelper
    {
        private FaceServiceClient _serviceClient;
        private const int PonserCount = 10000;
        private const int CallLimitPerSecond = 10;
        private Queue<DateTime> _timeStampQueue = new Queue<DateTime>();
        private Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private string _groupId;
        
        public FaceApiHelper()
        {
            if (_localSettings.Values["FaceAPIKey"] ==null || _localSettings.Values["EndPoint"] == null)
            {
                throw new Exception("Cannot find api key or end point.");
            }

            _serviceClient = new FaceServiceClient(_localSettings.Values["FaceAPIKey"].ToString(), _localSettings.Values["EndPoint"].ToString());
        }

        public async Task<bool> CheckGroupExistAsync()
        {
            bool exist = true;
            if (_localSettings.Values["GroupId"] == null)
            {
                return false;
            }

            try
            {
                _groupId = _localSettings.Values["GroupId"].ToString();
                var group = await _serviceClient.GetPersonGroupAsync(_groupId);
                if (group == null)
                {
                    await _serviceClient.CreatePersonGroupAsync(_groupId, _groupId + "Name");                    
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                exist = false;
            }                                    

            return exist;
        }

        public async Task<bool> IsCreatedPersonAsync(Stream picture, string userName)
        {
            bool successful = true;            
            
            try
            {
                await _serviceClient.CreatePersonAsync(_groupId, userName);
                await _serviceClient.TrainPersonGroupAsync(_groupId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                successful = false;
            }

            return successful;
        }

        public async Task WaitIfOverCallLimitAsync()
        {
            Monitor.Enter(_timeStampQueue);
            try
            {                
                if (_timeStampQueue.Count >= CallLimitPerSecond)
                {
                    TimeSpan interval = DateTime.UtcNow - _timeStampQueue.Peek();
                    if (interval < TimeSpan.FromSeconds(1))
                    {
                        // Make sure only 10 times can be called per second.
                        await Task.Delay(TimeSpan.FromSeconds(1) - interval);
                    }
                    _timeStampQueue.Dequeue();
                }
            }
            finally
            {
                _timeStampQueue.Enqueue(DateTime.UtcNow);
            }
        }


    }
}
