using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace facetracking_api.Models
{
    public enum CameraPosition
    {
        Front,
        Back,
        Unknown
    }
    public class CameraModel
    {
        public string Name { get; set; }
        public string CameraId { get; set; }
        public CameraPosition Position { get; set; }
    }
}
