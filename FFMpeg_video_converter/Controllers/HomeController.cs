using FFMpeg_video_converter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Xabe.FFmpeg;

namespace FFMpeg_video_converter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IWebHostEnvironment _appEnvironment;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment appEnvironment)
        {
            _logger = logger;
            _appEnvironment = appEnvironment;

            FFmpeg.SetExecutablesPath(Path.Combine(appEnvironment.ContentRootPath, "ffmpeg"));
        }

        public IActionResult Main()
        {
            return View();
        }

        public IActionResult AddFile(IFormFile file)
        {
            if(file != null)
            {
                string path = Path.Combine(_appEnvironment.WebRootPath, "Files", file.FileName);
                using (FileStream fileStream = new FileStream(path, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                return RedirectToAction("Converter", new { fileName = file.FileName });
            }

            return Content("Upload error");
        }

        public async Task<IActionResult> Converter(string fileName)
        {
            FileModel model = new FileModel();

            string inputFile = Path.Combine(_appEnvironment.WebRootPath, "Files", fileName);
            string outputFile = Path.Combine(_appEnvironment.WebRootPath, "ConvertedFiles", Path.ChangeExtension(fileName, "avi"));

            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputFile);
            IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
                ?.SetCodec(AudioCodec.mp3);
            IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault()
                ?.SetCodec(VideoCodec.mjpeg);

            IConversion conversion = FFmpeg.Conversions.New()
                .AddStream(audioStream, videoStream)
                .SetOutput(outputFile);

            return Content(conversion.Start().ToString());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
