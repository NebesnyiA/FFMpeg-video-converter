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

        private string _input;
        private string _output;
        private string _format;
        private string _resolution;

        public ConvertClass(string input, string output, string format, string resolution)
        {
            _input = input;
            _output = output;
            _format = format;
            _resolution = resolution;
        }

        public FileModel GetFileObject()
        {
            // current model includes vars to control conversion

            FileModel file = new FileModel();

            // token is used to stop conversion process

            file.token = new CancellationTokenSource();
            file.convertedFilePath = _output;
            file.originalFilePath = _input;
            file.conversion = GetConversion().GetAwaiter().GetResult();

            return file;
        }

        private async Task<IConversion> GetConversion()
        {
            await SetCodecs();

            // set conversion object parameters
            // includes file output and streams
            //

            IConversion conversion = FFmpeg.Conversions.New()
                .AddStream(_audioStream, _videoStream)
                .SetOutput(_output)
                .SetOverwriteOutput(true);

            return conversion;
        }

        private async Task SetCodecs()
        {
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(_input);
            int width = GetWidth(mediaInfo);
            int height = GetHeight(mediaInfo);


            // set audio and video codecs acording to the format
            //

            switch (_format) 
            {
                case "avi":
                    _videoStream = mediaInfo.VideoStreams.FirstOrDefault()?.SetCodec(VideoCodec.mjpeg)?.SetSize(width, height);
                    _audioStream = mediaInfo.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.mp3);
                    break;
                case "mp4":
                    _videoStream = mediaInfo.VideoStreams.FirstOrDefault()?.SetCodec(VideoCodec.h264)?.SetSize(width, height);
                    _audioStream = mediaInfo.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.aac);
                    break;
                case "mov":
                    _videoStream = mediaInfo.VideoStreams.FirstOrDefault()?.SetCodec(VideoCodec.mpeg4)?.SetSize(width, height);
                    _audioStream = mediaInfo.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.mp3);
                    break;
            }
        }

        // method uses resolution string to get new video width
        private int GetWidth(IMediaInfo mediaInfo)
        {
            if (_resolution == "original")
            {
                return mediaInfo.VideoStreams.FirstOrDefault().Width;
            }
            else
            {
                return Convert.ToInt32(_resolution.Split("x").First());
            }
        }

        // method uses resolution string to get new video height
        private int GetHeight(IMediaInfo mediaInfo)
        {
            if (_resolution == "original")
            {
                return mediaInfo.VideoStreams.FirstOrDefault().Height;
            }
            else
            {
                return Convert.ToInt32(_resolution.Split("x").Last());
            }

        }
    }
}