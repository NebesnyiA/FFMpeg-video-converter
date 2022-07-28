using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Xabe.FFmpeg;
using System.IO;

namespace FFMpeg_video_converter.Models
{
    public class FileModel
    {
        public string fileName;

        public string originalFilePath;
        public string convertedFilePath;

        public IConversion conversion;
        public CancellationTokenSource token;
    }
}
