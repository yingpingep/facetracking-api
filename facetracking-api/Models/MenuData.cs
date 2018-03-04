using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace facetracking_api.Models
{
    public enum Control
    {
        Menu,
        Setting
    };

    public class MenuData
    {
        public Symbol Icon { get; set; }
        public string Description { get; set; }
        public Control Tag { get; set; }
    }
}
