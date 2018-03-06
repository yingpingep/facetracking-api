using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using facetracking_api.Models;

namespace facetracking_api.Services
{
    public class DeviceHelper
    {

        public async Task<bool> IsCameraExistAsync()
        {
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (devices.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<List<CameraModel>> GetCameraDevicesAsync()
        {
            List<CameraModel> lists = new List<CameraModel>();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (devices.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("Cannot find any camera.");
                throw new NullReferenceException();
            }

            foreach (var item in devices) 
            {
                CameraModel cm = new CameraModel()
                {
                    Name = item.Name,
                    CameraId = item.Id,
                };

                if (item.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front)
                {
                    cm.Position = CameraPosition.Front;
                }
                else if (item.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back)
                {
                    cm.Position = CameraPosition.Back;
                }
                else
                {
                    cm.Position = CameraPosition.Unknown;
                }

                lists.Add(cm);
            }

            return lists;
        }                
    }
}
