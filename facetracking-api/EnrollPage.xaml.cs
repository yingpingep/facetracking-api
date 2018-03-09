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
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Microsoft.ProjectOxford.Face;

// 空白頁項目範本已記錄在 https://go.microsoft.com/fwlink/?LinkId=234238

namespace facetracking_api
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class EnrollPage : Page
    {
        private enum StreamingState
        {
            Idle,
            Streaming,
            Took
        }

        private StreamingState _state;
        private MediaCapture _mediaCapture;
        private FaceDetector _faceDetector;       
        private Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private string _cameraId;
        private DeviceHelper _deviceHelper;
        private VideoEncodingProperties _properties;
        private FaceServiceClient _faceService;
        private FaceApiHelper _faceApiHelper;

        public EnrollPage()
        {
            this.InitializeComponent();            
            App.Current.Suspending += this.OnSuspending;
            _faceService = new FaceServiceClient(_localSettings.Values["FaceAPIKey"].ToString(), _localSettings.Values["EndPoint"].ToString());
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            ChangeStateAsync(StreamingState.Idle);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ChangeStateAsync(StreamingState.Idle);
        }

        private async void ChangeStateAsync(StreamingState newState)
        {
            switch (newState)
            {
                case StreamingState.Idle:
                    await ShutdownCameraAsync();
                    _state = newState;
                    break;
                case StreamingState.Streaming:
                    if (!await StartStreamingAsync())
                    {
                        ChangeStateAsync(StreamingState.Idle);
                        return;
                    }

                    _state = newState;
                    break;
                case StreamingState.Took:
                    if (!await TakePictureAsync())
                    {
                        ChangeStateAsync(StreamingState.Idle);
                        return;
                    }

                    _state = newState;
                    break;
                default:
                    break;
            }
        }

        private async Task<bool> TakePictureAsync()
        {
            bool successful = true;
            if (_state != StreamingState.Streaming)
            {
                return false;
            }

            try
            {
                IList<DetectedFace> faces;
                const BitmapPixelFormat PixelFormat = BitmapPixelFormat.Nv12;
                using (VideoFrame currentFrame = new VideoFrame(PixelFormat, (int)_properties.Width, (int)_properties.Height))
                {
                    await _mediaCapture.GetPreviewFrameAsync(currentFrame);
                    faces = await _faceDetector.DetectFacesAsync(currentFrame.SoftwareBitmap);
                    Size size = new Size(currentFrame.SoftwareBitmap.PixelWidth, currentFrame.SoftwareBitmap.PixelHeight);

                    if (faces == null)
                    {
                        return false;
                    }

                    using (SoftwareBitmap bitmap = SoftwareBitmap.Convert(currentFrame.SoftwareBitmap, BitmapPixelFormat.Bgra8))
                    {
                        WriteableBitmap source = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                        bitmap.CopyToBuffer(source.PixelBuffer);

                        IRandomAccessStream stream = new InMemoryRandomAccessStream();
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                        encoder.SetSoftwareBitmap(bitmap);
                        await encoder.FlushAsync();

                        if (UserName.Text.Equals(string.Empty))
                        {
                            return false;
                        }                        

                        ShowUp(size, faces, source);
                        _faceApiHelper = new FaceApiHelper();
                        if (await _faceApiHelper.CheckGroupExistAsync())
                        {
                            if (await _faceApiHelper.CreatePersonAsync(stream.AsStream(), UserName.Text))
                            {
                                System.Diagnostics.Debug.WriteLine("{0} has been created.", UserName.Text);
                            }
                        }
                    }                    
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                successful = false;
            }

            return successful;
        }

        private async Task ShutdownCameraAsync()
        {
            try
            {
                if (_mediaCapture != null)
                {
                    if (_state == StreamingState.Streaming)
                    {
                        await _mediaCapture.StopPreviewAsync();
                    }
                    _mediaCapture.Dispose();
                }
            }
            catch (Exception)
            {
                ;
            }

            CameraPreview.Source = null;
            _mediaCapture = null;            
        }

        private async Task<bool> StartStreamingAsync()
        {
            bool result = true;
            PaintingCanvas.Children.Clear();
            try
            {
                MediaCaptureInitializationSettings initializationSettings = new MediaCaptureInitializationSettings();
                if (_localSettings.Values["CameraId"] == null)
                {
                    _deviceHelper = new DeviceHelper();
                    var cameraList = await _deviceHelper.GetCameraDevicesAsync();
                    _cameraId = cameraList.Where(x => x.Position == CameraPosition.Front).Select(c => c.CameraId).FirstOrDefault();
                    _localSettings.Values["CameraId"] = _cameraId;
                    _localSettings.Values["CameraPosition"] = (int)CameraPosition.Front;
                }

                _cameraId = _localSettings.Values["CameraId"].ToString();
                initializationSettings.VideoDeviceId = _cameraId;
                initializationSettings.StreamingCaptureMode = StreamingCaptureMode.Video;

                var cp = (CameraPosition)_localSettings.Values["CameraPosition"];
                switch (cp)
                {
                    case CameraPosition.Front:
                        CameraPreview.FlowDirection = FlowDirection.RightToLeft;
                        PaintingCanvas.FlowDirection = FlowDirection.RightToLeft;
                        break;
                    case CameraPosition.Back:
                        CameraPreview.FlowDirection = FlowDirection.LeftToRight;
                        PaintingCanvas.FlowDirection = FlowDirection.LeftToRight;
                        break;
                    default:
                        break;
                }

                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(initializationSettings);
                _mediaCapture.Failed += _mediaCapture_Failed;

                CameraPreview.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _properties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;                
            }
            catch (UnauthorizedAccessException)
            {
                result = false;
            }
            catch (Exception ex)
            {

                result = false;
            }

            return result;
        }

        private void ShowUp(Size frameSize, IList<DetectedFace> faces, WriteableBitmap picture)
        {
            SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);
            double lineThickness = 2.0;
            SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

            double canvasWidth = PaintingCanvas.ActualWidth;
            double canvasHeight = PaintingCanvas.ActualHeight;            

            // Clear.
            PaintingCanvas.Children.Clear();

            ImageBrush brush = new ImageBrush();
            brush.ImageSource = picture;
            brush.Stretch = Stretch.Fill;
            PaintingCanvas.Background = brush;

            if (_state == StreamingState.Streaming && faces != null)
            {
                double widthScale = frameSize.Width / canvasWidth;
                double heightScale = frameSize.Height / canvasHeight;

                foreach (var item in faces)
                {
                    Rectangle box = new Rectangle()
                    {
                        Width = (uint)(item.FaceBox.Width / widthScale),
                        Height = (uint)(item.FaceBox.Height / heightScale),
                        Fill = fillBrush,
                        StrokeThickness = lineThickness,
                        Stroke = lineBrush,
                        Margin = new Thickness((uint)(item.FaceBox.X / widthScale), (uint)(item.FaceBox.Y / heightScale), 0, 0)
                    };

                    PaintingCanvas.Children.Add(box);
                }
            }

            ChangeStateAsync(StreamingState.Idle);
        }

        private void _mediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            ChangeStateAsync(StreamingState.Idle);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _state = StreamingState.Idle;

            if (_faceDetector == null)
            {
                _faceDetector = await FaceDetector.CreateAsync();
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            // Update To Face API and database.
        }

        private void Take_Click(object sender, RoutedEventArgs e)
        {
            if (_state == StreamingState.Streaming)
            {
                ChangeStateAsync(StreamingState.Took);
            }
            else
            {
                ChangeStateAsync(StreamingState.Idle);
            }
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (_state == StreamingState.Idle || _state == StreamingState.Took)
            {
                ChangeStateAsync(StreamingState.Streaming);
            }            
            else
            {
                ChangeStateAsync(StreamingState.Idle);
            }
        }
    }
}
