using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using facetracking_api.Models;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace facetracking_api.Services
{    
    public class FaceApiHelper
    {
        private FaceServiceClient _serviceClient;        
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

        public async Task<bool> CreatePersonAsync(Stream picture, string userName)
        {
            bool successful = true;
            
            try
            {
                await WaitIfOverCallLimitAsync();
                CreatePersonResult createPerson = await _serviceClient.CreatePersonAsync(_groupId, userName);

                await WaitIfOverCallLimitAsync();
                await _serviceClient.AddPersonFaceAsync(_groupId, createPerson.PersonId, picture);

                await _serviceClient.TrainPersonGroupAsync(_groupId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                successful = false;
            }

            return successful;
        }

        public async Task<CustomFaceModel[]> GetIdentifyResultAsync(Stream picture)
        {
            CustomFaceModel[] customFaceModels = null;
            try
            {                
                // await WaitIfOverCallLimitAsync();
                Face[] detectResults = await _serviceClient.DetectAsync(picture);
                    
                Guid[] guids = detectResults.Select(x => x.FaceId).ToArray();
                IdentifyResult[] identifyResults = await _serviceClient.IdentifyAsync(_groupId, guids);

                customFaceModels = new CustomFaceModel[detectResults.Length];
                for (int i = 0; i < identifyResults.Length; i++)
                {
                    FaceRectangle rectangle = detectResults[i].FaceRectangle;

                    // await WaitIfOverCallLimitAsync();
                    string name = (await _serviceClient.GetPersonAsync(_groupId, identifyResults[i].Candidates[0].PersonId)).Name;
                    CustomFaceModel model = new CustomFaceModel()
                    {
                        Name = name,
                        Top = rectangle.Top,
                        Left = rectangle.Left,
                        Width = rectangle.Width,
                        Height = rectangle.Height
                    };

                    customFaceModels[i] = model;
                };                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);                
            }

            return customFaceModels;
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
