using FFMpeg_video_converter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using FFMpeg_video_converter.SignalRHub;
using Microsoft.AspNetCore.SignalR;
using FFMpeg_video_converter.Utils;

namespace FFMpeg_video_converter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IWebHostEnvironment _appEnvironment;

        public static IHubContext<ProgressHub> hub;
        public static CancellationTokenSource token = new CancellationTokenSource();

        static Dictionary<string, FileModel> filesToConvert = new Dictionary<string, FileModel>();

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment appEnvironment, IHubContext<ProgressHub> hubContext)
        {
            _logger = logger;
            _appEnvironment = appEnvironment;
            hub = hubContext;

            string ffmpegPass = Path.Combine(_appEnvironment.ContentRootPath, "ffmpeg");

            if (!Directory.Exists(ffmpegPass))
            {
                Directory.CreateDirectory(ffmpegPass);
            }
            if (!(Directory.Exists(Path.Combine(ffmpegPass, "ffmpeg.exe"))
                && Directory.Exists(Path.Combine(ffmpegPass, "ffprobe.exe"))))
            {
                FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPass).GetAwaiter().GetResult();
            }
            FFmpeg.SetExecutablesPath(ffmpegPass);
        }

        public IActionResult Main()
        {
            return View();
        }


        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1073741824)]
        [RequestSizeLimit(1073741824)]
        public async Task<string> UploadFile(IFormFile file)
        {
            if(file != null)
            {
                string path = Path.Combine(_appEnvironment.WebRootPath, "Files");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                using (FileStream stream = new FileStream(Path.Combine(path, file.FileName), FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    stream.Flush();
                }

                //return RedirectToAction("Converter", new { fileName = file.FileName });
                return file.FileName;
            }
            return "Upload_error";
        }

        public IActionResult Converter(string fileName)
        {
            FileModel file = new FileModel();
            file.fileName = fileName;
            file.convertedFilePath = Path.Combine(_appEnvironment.WebRootPath, "ConvertedFiles", Path.ChangeExtension(fileName, "avi"));

            return View(file);
        }

        public async Task ConvertFile(string fileName, string connectionId)
        {
            string inputFile = Path.Combine(_appEnvironment.WebRootPath, "Files", fileName);
            string outputFile = Path.Combine(_appEnvironment.WebRootPath, "ConvertedFiles", Path.ChangeExtension(fileName, "avi"));

            ConvertClass convertProcesses = new ConvertClass();
            //FileModel fileForConvert = convertProcesses.GetFileObject(inputFile, outputFile);

            filesToConvert.Add(fileName, convertProcesses.GetFileObject(inputFile, outputFile));
            filesToConvert[fileName].fileName = fileName;

            filesToConvert[fileName].conversion.OnProgress += (sender, args) =>
            {
                var percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
                //hub.Clients.All.SendAsync("Progress", percent, Path.ChangeExtension(fileName, "avi"));
                hub.Clients.Client(connectionId).SendAsync("Progress", percent, Path.ChangeExtension(fileName, "avi"));
                Debug.WriteLine(percent);
            };

            await filesToConvert[fileName].conversion.Start(filesToConvert[fileName].token.Token);
        }

        public void RemoveConversion(string fileName)
        {
            filesToConvert.Remove(fileName);
        }

        public string CancelConversion(string fileName)
        {
            filesToConvert[fileName].token.Cancel();
            RemoveConversion(fileName);

            return "Stoped conversion";
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
