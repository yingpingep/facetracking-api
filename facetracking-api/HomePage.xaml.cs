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
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;


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
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        private Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private VideoEncodingProperties _videoProperties;
        FaceApiHelper _faceApiHelper;

        public HomePage()
        {
            this.InitializeComponent();
            App.Current.Suspending += this.OnSuspending;
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            if (_state == StreamingState.Streaming)
            {
                ChangeStateAsync(StreamingState.Idle);
            }            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _state = StreamingState.Idle;
            if (_faceTracker == null)
            {
                _faceTracker = await FaceTracker.CreateAsync();
            }
            
            if (_faceApiHelper == null)
            {
                try
                {
                    _faceApiHelper = new FaceApiHelper();
                    await _faceApiHelper.CheckGroupExistAsync();
                }
                catch (Microsoft.ProjectOxford.Face.FaceAPIException faceEx)
                {
                    ShowErrorHelper.ShowDialog(faceEx.ErrorMessage, faceEx.ErrorCode);
                }
                catch (Exception ex)
                {
                    ShowErrorHelper.ShowDialog(ex.Message);
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ChangeStateAsync(StreamingState.Idle);
        }

        private async Task<bool> StartStreamingAsync()
        {
            bool result = true;
            try
            {
                MediaCaptureInitializationSettings initializationSettings = new MediaCaptureInitializationSettings();
                if (_localSettings.Values["CameraId"] == null)
                {
                    ShowErrorHelper.ShowDialog("Cannot get your CamreaId, plase check your setting.");
                    return false;
                }

                initializationSettings.VideoDeviceId = _localSettings.Values["CameraId"].ToString();
                initializationSettings.StreamingCaptureMode = StreamingCaptureMode.Video;

                // Select preview flow direction.
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
                
                // Prepare MediaCapture.
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(initializationSettings);
                _mediaCapture.Failed += MediaCapture_Failed;

                // Get preview video properties for FaceTracker.
                // e.g. hight and width.
                var deviceController = _mediaCapture.VideoDeviceController;
                _videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                
                CameraPreview.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();                

                TimeSpan period = TimeSpan.FromMilliseconds(66);
                _threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(ProcessCurrentVideoFrame), period);
            }
            catch (UnauthorizedAccessException)
            {
                result = false;
            }
            catch (Exception ex)
            {
                ShowErrorHelper.ShowDialog("Cannot start to preview because " + ex.Message);
                result = false;
            }

            return result;
        }

        private async void ProcessCurrentVideoFrame(ThreadPoolTimer timer)
        {
            // If state is not Streaming, return.
            if (_state != StreamingState.Streaming)
            {
                return;
            }

            // If there has a process still running, return.
            if (!_semaphoreSlim.Wait(0))
            {
                return;
            }

            const BitmapPixelFormat PixelFormat = BitmapPixelFormat.Nv12;
            try
            {
                using (VideoFrame currentFrame = new VideoFrame(PixelFormat, (int)_videoProperties.Width, (int)_videoProperties.Height))
                {
                    // Get current preview frame from _mediaCaputre and copy into currentFrame.               
                    await _mediaCapture.GetPreviewFrameAsync(currentFrame);                    

                    // Detected face by _faceTracker.
                    IList<DetectedFace> builtinFaces = await _faceTracker.ProcessNextFrameAsync(currentFrame);
                    SoftwareBitmap tempBitmap = SoftwareBitmap.Convert(currentFrame.SoftwareBitmap, BitmapPixelFormat.Bgra8);

                    if (builtinFaces.Count != 0)
                    {                        
                        var frameSize = new Size(currentFrame.SoftwareBitmap.PixelWidth, currentFrame.SoftwareBitmap.PixelHeight);
                        //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        //{
                        //    ShowResult(frameSize, builtinFaces);
                        //});

                        // Get picture from videoframe.                                       
                        IRandomAccessStream stream = new InMemoryRandomAccessStream();
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                        encoder.SetSoftwareBitmap(tempBitmap);
                        await encoder.FlushAsync();
                        
                        CustomFaceModel[] customFaces = await _faceApiHelper.GetIdentifyResultAsync(stream.AsStream());
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            ShowFromFaceApi(frameSize, customFaces));
                    }
                    else
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            PaintingCanvas.Children.Clear());
                    }
                }
            }
            catch (Microsoft.ProjectOxford.Face.FaceAPIException faceEx)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    ShowErrorHelper.ShowDialog(faceEx.ErrorMessage, faceEx.ErrorCode));                
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    ShowErrorHelper.ShowDialog(ex.Message));                
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void ShowFromBuiltIn(Size frameSize, IList<DetectedFace> faces)
        {   
            SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);
            double lineThickness = 2.0;
            SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

            double canvasWidth = PaintingCanvas.ActualWidth;
            double canvasHeight = PaintingCanvas.ActualHeight;

            // Clear.
            PaintingCanvas.Children.Clear();
            if (_state == StreamingState.Streaming && faces.Count != 0)
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
                };
            }
        }

        private void ShowFromFaceApi(Size frameSize, CustomFaceModel[] customFaces)
        {
            SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);
            double lineThickness = 3.0;
            SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

            PaintingCanvas.Children.Clear();
            if (_state == StreamingState.Streaming && customFaces != null)
            {
                double widthScale = frameSize.Width / PaintingCanvas.ActualWidth;
                double heightScale = frameSize.Height / PaintingCanvas.ActualHeight;

                foreach (var item in customFaces)
                {
                    Rectangle box = new Rectangle()
                    {
                        Width = (uint)(item.Width / widthScale),
                        Height = (uint)(item.Height / heightScale),
                        Fill = fillBrush,
                        StrokeThickness = lineThickness,
                        Stroke = lineBrush,
                        Margin = new Thickness((uint)(item.Left / widthScale), (uint)(item.Top / heightScale), 0, 0)
                    };
                    PaintingCanvas.Children.Add(box);

                    Grid grid = new Grid()
                    {
                        Background = lineBrush,
                        Width = (item.Width / widthScale),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness((uint)(item.Left / widthScale), (uint)(item.Top / heightScale), 0, 0)
                    };

                    TextBlock t = new TextBlock()
                    {
                        Text = item.Name,
                        FontSize = 28,
                        HorizontalTextAlignment = TextAlignment.Right,
                        Foreground = new SolidColorBrush(Windows.UI.Colors.Black)            
                    };
                    grid.Children.Add(t);
                    PaintingCanvas.Children.Add(grid);
                }
            }
        }

        private async void ChangeStateAsync(StreamingState newState)
        {
            switch (newState)
            {
                case StreamingState.Idle:
                    // Clear canvas and stop preview.
                    await ShutdownCameraAsync();
                    _state = newState;
                    break;
                case StreamingState.Streaming:                  
                    bool isStartPreview = await StartStreamingAsync();
                    // If failure to start preview, change state to idle.
                    if (!isStartPreview)
                    {
                        ChangeStateAsync(StreamingState.Idle);
                        return;
                    }

                    _state = StreamingState.Streaming;
                    break;
                default:
                    break;
            }
        }

        private async Task ShutdownCameraAsync()
        {
            try
            {
                if (_threadPoolTimer != null)
                {
                    _threadPoolTimer.Cancel();
                }

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
            }

            CameraPreview.Source = null;
            PaintingCanvas.Children.Clear();
            _mediaCapture = null;
            _threadPoolTimer = null;
        }

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            if (_state == StreamingState.Streaming)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ChangeStateAsync(StreamingState.Idle);
                });
            }
        }

        private void ButtonStream_Click(object sender, RoutedEventArgs e)
        {
            if (_state == StreamingState.Idle)
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
