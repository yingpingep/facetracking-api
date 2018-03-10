using facetracking_api.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// 空白頁項目範本已記錄在 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x404

namespace facetracking_api
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();            
        }        

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {            
            if (args.IsSettingsInvoked)
            {
                NavigationView.Header = "Settings";
                ContentFrame.Navigate(typeof(SettingPage));
            }
            else
            {
                var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
                NavigationView_Navigate(item);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            NavigationView.Header = "Microsoft Student Partners in Taiwan";
            ContentFrame.Navigate(typeof(HomePage));
        }

        private void NavigationView_Navigate(NavigationViewItem item)
        {            
            switch (item.Tag)
            {
                case "home":
                    NavigationView.Header = "Microsoft Student Partners in Taiwan";
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                case "enroll":
                    NavigationView.Header = "Enroll";
                    ContentFrame.Navigate(typeof(EnrollPage));
                    break;
                case "test":
                    NavigationView.Header = "Test";
                    ContentFrame.Navigate(typeof(Test));
                    break;
                default:
                    break;
            }
        }
    }
}
