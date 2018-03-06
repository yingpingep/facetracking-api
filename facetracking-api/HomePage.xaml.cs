using facetracking_api.Models;
using facetracking_api.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// 空白頁項目範本已記錄在 https://go.microsoft.com/fwlink/?LinkId=234238

namespace facetracking_api
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private enum StreamingState
        {
            Idle,
            Streaming
        }

        private StreamingState _state;
        private MediaCapture _mediaCapture;
        private FaceTracker _faceTracker;

        // To initiate face tracking on a define interval.
        private ThreadPoolTimer _threadPoolTimer;

        // Make sure only one face tracking at a time.
        private SemaphoreSlim _semaphoreSlim;

        private Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private string _cameraId;
        private DeviceHelper _deviceHelper;

        public HomePage()
        {
            this.InitializeComponent();
            App.Current.Suspending += this.OnSuspending;
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_faceTracker == null)
            {
                _faceTracker = await FaceTracker.CreateAsync();
            }
            
        }

        private async Task<bool> StartStreamingAsync()
        {
            bool result = true;
            try
            {
                MediaCaptureInitializationSettings initializationSettings = new MediaCaptureInitializationSettings();
                if (_localSettings.Values["CameraId"] == null)
                {
                    _deviceHelper = new DeviceHelper();                
                    var cameraList = await _deviceHelper.GetCameraDevicesAsync();
                    _cameraId = cameraList.Where(x => x.Position == CameraPosition.Front).Select(p => p.CameraId).FirstOrDefault();
                    _localSettings.Values["CameraId"] = _cameraId;                    
                
                }

                initializationSettings.VideoDeviceId = _localSettings.Values["CameraId"].ToString();
                initializationSettings.StreamingCaptureMode = StreamingCaptureMode.Video;

                // Prepare MediaCapture.
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(initializationSettings);
                _mediaCapture.Failed += MediaCapture_Failed;

                CameraPreview.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
            }
            catch (UnauthorizedAccessException)
            {
                result = false;
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            throw new NotImplementedException();
        }

        private async void ButtonStream_Click(object sender, RoutedEventArgs e)
        {
            await StartStreamingAsync();
        }
    }
}
