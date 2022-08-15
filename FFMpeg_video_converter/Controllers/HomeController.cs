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
        // request size set to 134 megabytes
        private const int RequestSize = 1073741824;

        private readonly ILogger<HomeController> _logger;
        private IWebHostEnvironment _appEnvironment;

        // hub provides possibility to send server answers to several clients

        public static IHubContext<ProgressHub> _hub;

        // dictionary is used to store user's files and conversion objects

        private static Dictionary<string, FileModel> _filesToConvert = new Dictionary<string, FileModel>();

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment appEnvironment, IHubContext<ProgressHub> hubContext)
        {
            _logger = logger;
            _appEnvironment = appEnvironment;
            _hub = hubContext;

            string ffmpegPass = Path.Combine(_appEnvironment.ContentRootPath, "ffmpeg");

            // if statements are used to check ffmpeg binaries existence
            // and download them if it's needed (in this case first start will be long)
            //

            if (!Directory.Exists(ffmpegPass))
            {
                Directory.CreateDirectory(ffmpegPass);
            }
            if (!(Directory.Exists(Path.Combine(ffmpegPass, "ffmpeg.exe"))
                && Directory.Exists(Path.Combine(ffmpegPass, "ffprobe.exe"))))
            {
                // downloads ffmpeg.exe and ffprobe.exe

                FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPass).GetAwaiter().GetResult();
            }
            FFmpeg.SetExecutablesPath(ffmpegPass);
        }

        #region View controllers

        public IActionResult Main()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Converter(string resolution, string format, List<string> file)
        {
            // create dir to store converted files

            string outputPath = Path.Combine(_appEnvironment.WebRootPath, "ConvertedFiles");
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // model is used to display uploaded files on converter page

            ConvertPageModel model = new ConvertPageModel();
            model.resolution = resolution;
            model.FileList = file;
            model.format = format;

            return View(model);
        }

        #endregion

        #region File management

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = RequestSize)]
        [RequestSizeLimit(RequestSize)]

        // uploaded file to the server
        // event listener get file upload progress
        //

        public async Task<string> UploadFile(IFormFile file)
        {
            if (file != null)
            {
                // create dir to store uploaded file

                string path = Path.Combine(_appEnvironment.WebRootPath, "Files");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // create file stream

                using (FileStream stream = new FileStream(Path.Combine(path, file.FileName), FileMode.Create))
                {
                    // start file uploading

                    await file.CopyToAsync(stream);
                    stream.Flush();
                }

                return file.FileName;
            }
            return "Upload_error";
        }

        // function is called, when user presses remove button in amin form
        // also called when user leaves web-site
        //

        public void RemoveUploadedFile(string fileName)
        {
            string filePath = Path.Combine(_appEnvironment.WebRootPath, "Files", fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // removes conversion result when user leaves
        // or stops convert process
        //

        public void RemoveConvertedFile(string fileName, string format)
        {
            string filePath = Path.Combine(_appEnvironment.WebRootPath, "ConvertedFiles", Path.ChangeExtension(fileName, format));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            RemoveUploadedFile(fileName);
        }

        #endregion

        #region Conversion management

        // is called for every uploaded file when user opens converter page
        //

        public async Task ConvertFile(string fileName, string connectionId, string format, string resolution)
        {
            // vars are used to create conversion object

            string convertedFileName = Path.ChangeExtension(fileName, format);
            string inputFile = Path.Combine(_appEnvironment.WebRootPath, "Files", fileName);
            string outputFile = Path.Combine(_appEnvironment.WebRootPath, "ConvertedFiles", convertedFileName);

            // conversion object is created

            ConvertClass convertProcesses = new ConvertClass(inputFile, outputFile, format, resolution);

            // and saved to the dictionary

            _filesToConvert.Add(fileName, convertProcesses.GetFileObject());
            _filesToConvert[fileName].fileName = fileName;

            // created conversion progress sender

            _filesToConvert[fileName].conversion.OnProgress += (sender, args) =>
            {
                // hub sends progress and file info to the cliend
                // every second

                var percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
                _hub.Clients.Client(connectionId).SendAsync("Progress", percent, fileName, convertedFileName);
            };

            // start conversion, set token to control it

            await _filesToConvert[fileName].conversion.Start(_filesToConvert[fileName].token.Token);
        }

        // function removes conversion from the dictionary
        //

        public void RemoveConversion(string fileName)
        {
            _filesToConvert.Remove(fileName);
        }

        // function is called, when user presses stop button
        //

        public string CancelConversion(string fileName)
        {
            // use saved token to stop connected to it conversion

            _filesToConvert[fileName].token.Cancel();
            RemoveConversion(fileName);

            return "Stoped conversion";
        }

        #endregion

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}