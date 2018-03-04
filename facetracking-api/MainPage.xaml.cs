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

            //List<MenuData> list = new List<MenuData>();
            //list.Add(new MenuData() { Icon = Symbol.GlobalNavigationButton, Description = "選單", Tag = Models.Control.Menu });
            //list.Add(new MenuData() { Icon = Symbol.Setting, Description = "設定", Tag = Models.Control.Setting });
            //Menu.ItemsSource = list;
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {            
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingPage));
            }
            else
            {
                var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
                NavigationView_Navigate(item);
            }
        }

        private void NavigationView_Navigate(NavigationViewItem item)
        {            
            switch (item.Tag)
            {
                case "home":
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                case "enroll":
                    ContentFrame.Navigate(typeof(EnrollPage));
                    break;
                default:
                    break;
            }
        }
    }
}
