using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using FFMpeg_video_converter.Models;
using Xabe.FFmpeg;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace FFMpeg_video_converter.Utils
{
    public class ConvertClass
    {
        private IStream _videoStream;
        private IStream _audioStream;

        public FileModel GetFileObject(string inputFile, string outputFile)
        {
            FileModel fileForConvert = new FileModel();

            fileForConvert.token = new CancellationTokenSource();
            fileForConvert.conversion = GetConversionObject(inputFile, outputFile).GetAwaiter().GetResult();
            fileForConvert.convertedFilePath = outputFile;
            fileForConvert.originalFilePath = inputFile;

            return fileForConvert;
        }

        private async Task<IConversion> GetConversionObject(string inputFile, string outputFile)
        {
            string format = Path.GetExtension(outputFile);
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputFile);

            switch (format) 
            {
                case "avi":
                    GetAVIStream(mediaInfo);
                    break;
                case "mp4":
                    GetMP4Stream(mediaInfo);
                    break;
                case "mov":
                    GetMOVStream(mediaInfo);
                    break;
            }

            IConversion conversion = FFmpeg.Conversions.New()
                .AddStream(_audioStream, _videoStream)
                .SetOutput(outputFile);

            return conversion;
        }

        private void GetAVIStream(IMediaInfo info)
        {
            _audioStream = info.AudioStreams.FirstOrDefault()
                ?.SetCodec(AudioCodec.mp3);
            _videoStream = info.VideoStreams.FirstOrDefault()
                ?.SetCodec(VideoCodec.mjpeg);
        }

        private void GetMOVStream(IMediaInfo info)
        {
            _audioStream = info.AudioStreams.FirstOrDefault()
                ?.SetCodec(AudioCodec.mp3);
            _videoStream = info.VideoStreams.FirstOrDefault()
                ?.SetCodec(VideoCodec.mpeg4);
        }

        private void GetMP4Stream(IMediaInfo info)
        {
            _audioStream = info.AudioStreams.FirstOrDefault()
                ?.SetCodec(AudioCodec.aac);
            _videoStream = info.VideoStreams.FirstOrDefault()
                ?.SetCodec(VideoCodec.h264);
        }
    }
}
