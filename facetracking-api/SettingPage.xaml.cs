using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using facetracking_api.Services;
using facetracking_api.Models;

// 空白頁項目範本已記錄在 https://go.microsoft.com/fwlink/?LinkId=234238

namespace facetracking_api
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private DeviceHelper _deviceHelper;
        private List<CameraModel> _cameraList;
        private Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public SettingPage()
        {
            this.InitializeComponent();
            _deviceHelper = new DeviceHelper();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {            
            try
            {
                if (_cameraList == null)
                {
                    _cameraList = await _deviceHelper.GetCameraDevicesAsync();
                    foreach (var item in _cameraList)
                    {
                        RadioButton rb = new RadioButton()
                        {
                            Content = item.Name,
                            Tag = item
                        };
                        
                        if (_localSettings.Values[Constants.CameraId] == null)
                        {
                            rb.IsChecked = true;
                            _localSettings.Values[Constants.CameraId] = item.CameraId;
                            _localSettings.Values[Constants.CameraPosition] = (int)item.Position;
                        }
                        else if (item.CameraId.Equals(_localSettings.Values[Constants.CameraId].ToString()))
                        {
                            rb.IsChecked = true;
                        }

                        rb.Click += CameraRadios_Click;
                        StackCameraRadios.Children.Add(rb);
                    }
                }

                if (_localSettings.Values[Constants.FaceAPIKey] != null)
                {
                    SubscriptionKey.Text = _localSettings.Values[Constants.FaceAPIKey].ToString();
                }

                if (_localSettings.Values[Constants.EndPoint] != null)
                {
                    EndPoint.Text = _localSettings.Values[Constants.EndPoint].ToString();
                }

                if (_localSettings.Values[Constants.GroupId] != null)
                {
                    // GroupId.Text = _localSettings.Values[Constants.GroupId].ToString();
                }

                if (_localSettings.Values[Constants.WebEndPoint] != null)
                {
                    WebEndPoint.Text = _localSettings.Values[Constants.WebEndPoint].ToString();
                }
            }
            catch (Exception)
            {
                StackCameraRadios.Children.Add(new TextBlock() { Text = "Cannot find any camera." });
            }            
        }        

        public void CameraRadios_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            var item = rb.Tag as CameraModel;
            _localSettings.Values[Constants.CameraId] = item.CameraId;
            _localSettings.Values[Constants.CameraPosition] = (int)item.Position;
        }

        private void ButtonGetKey_Click(object sender, RoutedEventArgs e)
        {
            if (SubscriptionKey.Text != null && EndPoint.Text != null && WebEndPoint.Text != null)
            {
                _localSettings.Values[Constants.FaceAPIKey] = SubscriptionKey.Text;
                _localSettings.Values[Constants.EndPoint] = EndPoint.Text;
                // _localSettings.Values[Constants.GroupId] = GroupId.Text;
                _localSettings.Values[Constants.WebEndPoint] = WebEndPoint.Text;

                ShowAlertHelper.ShowDialog("Your settings have been store", "Done");
            }
            else
            {
                return;
            }
        }
    }        
}
