using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FFMpeg_video_converter.Models
{
    public class ConvertPageModel
    {
        public List<string> FileList = new List<string>();
        public string format { get; set; }
        public string resolution { get; set; }
    }
}
