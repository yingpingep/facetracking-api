using facetracking_api.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace facetracking_api
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Test : Page
    {
        // To initiate face tracking on a define interval.
        private ThreadPoolTimer _threadPoolTimer;

        // Make sure only one face tracking at a time.
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        private DataHelper _dataHelper;

        public Test()
        {
            this.InitializeComponent();
            App.Current.Suspending += this.OnSuspending;
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            CloseWork();  
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CloseWork();
        }

        private void CloseWork()
        {
            try
            {
                if (_threadPoolTimer != null)
                {
                    _threadPoolTimer.Cancel();
                }
            }
            catch (Exception)
            {
            }

            _threadPoolTimer = null;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_dataHelper == null)
            {
                try
                {
                    _dataHelper = new DataHelper();
                }
                catch (Exception ex)
                {
                    ShowAlertHelper.ShowDialog(ex.Message);
                }
            }

            try
            {
                await T();
                TimeSpan period = TimeSpan.FromMinutes(1);
                _threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(ShowScore, period);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async void ShowScore(ThreadPoolTimer timer)
        {
            if (!_semaphoreSlim.Wait(0))
            {
                return;
            }

            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await
                    T());
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    ShowAlertHelper.ShowDialog(ex.Message));
            }
            finally
            {
                _semaphoreSlim.Release();
            }                      
        }

        private async Task T()
        {
            var result = (await _dataHelper.GetNumbers()).OrderByDescending(x => x.Numbers);

            foreach (var item in result)
            {
                if (item.NickName.Equals(""))
                {
                    item.NickName = item.Name;
                }

                switch (item.Numbers)
                {
                    case 10:
                        item.Color = new SolidColorBrush(Windows.UI.Colors.LightSlateGray);
                        break;
                    case 30:
                        item.Color = new SolidColorBrush(Windows.UI.Colors.OrangeRed);
                        break;
                    default:
                        item.Color = new SolidColorBrush(Windows.UI.Colors.DarkOrange);
                        break;
                }
            }
            PrintList.ItemsSource = result;
        }
    }
}
