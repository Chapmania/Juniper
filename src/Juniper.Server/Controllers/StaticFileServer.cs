using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Juniper.HTTP.Server.Controllers
{
    public class StaticFileServer : AbstractResponse
    {
        private static readonly string[] INDEX_FILES = {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        private static readonly MediaType[] DEFAULT_MEDIA_TYPES =
        {
            MediaType.Application.Javascript,
            MediaType.Application.Json,
            MediaType.Application.Xml,
            MediaType.Text.Html,
            MediaType.Text.Css,
            MediaType.Text.Plain,
            MediaType.Text.Xml,
            MediaType.Image.Png,
            MediaType.Image.Jpeg,
            MediaType.Image.Gif,
            MediaType.Image.SvgXml
        };

        private static string MakeShortName(string rootDirectory, string filename)
        {
            var shortName = filename.Replace(rootDirectory, "");
            if (shortName.Length > 0 && shortName[0] == Path.DirectorySeparatorChar)
            {
                shortName = shortName.Substring(1);
            }

            return shortName;
        }

        private static string FindDefaultFile(string filename)
        {
            if (Directory.Exists(filename))
            {
                for (var i = 0; i < INDEX_FILES.Length; ++i)
                {
                    var test = Path.Combine(filename, INDEX_FILES[i]);
                    if (File.Exists(test))
                    {
                        filename = test;
                        break;
                    }
                }
            }

            return filename;
        }

        private static DirectoryInfo ValidateDirectoryPath(string rootDirectoryPath)
        {
            if (rootDirectoryPath is null)
            {
                throw new ArgumentNullException(nameof(rootDirectoryPath));
            }

            return new DirectoryInfo(rootDirectoryPath);
        }

        private static string MassageRequestPath(string requestPath)
        {
            requestPath = requestPath.Substring(1);

            if (requestPath.Length > 0 && requestPath[requestPath.Length - 1] == '/')
            {
                requestPath = requestPath.Substring(0, requestPath.Length - 1);
            }

            return requestPath.Replace('/', Path.DirectorySeparatorChar);
        }

        private readonly DirectoryInfo rootDirectory;
        private readonly MediaType[] mediaTypeWhiteList;

        public StaticFileServer(DirectoryInfo rootDirectory, params MediaType[] mediaTypeWhiteList)
            : base(int.MaxValue - 2)
        {
            if (rootDirectory is null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }

            if (!rootDirectory.Exists)
            {
                throw new InvalidOperationException($"Directory {rootDirectory.FullName} does not exist");
            }

            Verb = HttpMethods.GET;

            this.rootDirectory = rootDirectory;

            this.mediaTypeWhiteList = mediaTypeWhiteList;
            if (this.mediaTypeWhiteList is null
                || this.mediaTypeWhiteList.Length == 0)
            {
                this.mediaTypeWhiteList = DEFAULT_MEDIA_TYPES;
            }
        }

        public StaticFileServer(string rootDirectoryPath, params MediaType[] mediaTypeWhiteList)
            : this(ValidateDirectoryPath(rootDirectoryPath),
                  mediaTypeWhiteList)
        { }

        public override async Task InvokeAsync(HttpListenerContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.Request;
            var response = context.Response;
            var requestPath = request.Url.AbsolutePath;
            var requestFile = MassageRequestPath(requestPath);
            var filename = Path.Combine(rootDirectory.FullName, requestFile);
            var isDirectory = Directory.Exists(filename);

            if (isDirectory)
            {
                filename = FindDefaultFile(filename);
            }

            var file = new FileInfo(filename);
            var type = MediaType.GuessByExtension(file);
            var isSupportedMediaType = Array.IndexOf(mediaTypeWhiteList, type) >= 0;
            var shortName = MakeShortName(rootDirectory.FullName, filename);

            if (!rootDirectory.Contains(file))
            {
                response.SetStatus(HttpStatusCode.Unauthorized);
            }
            else if (isDirectory && requestPath[requestPath.Length - 1] != '/')
            {
                response.Redirect(requestPath + "/");
            }
            else if (!file.Exists && isDirectory)
            {
                await ListDirectoryAsync(response, new DirectoryInfo(filename))
                    .ConfigureAwait(false);
            }
            else if (!file.Exists && !isDirectory)
            {
                var message = $"request '{shortName}'";
                OnWarning(message);
                response.SetStatus(HttpStatusCode.NotFound);
            }
            else if (isSupportedMediaType)
            {
                await SendFileAsync(response, file, shortName)
                    .ConfigureAwait(false);
            }
            else
            {
                response.SetStatus(HttpStatusCode.Unauthorized);
            }
        }

        private async Task ListDirectoryAsync(HttpListenerResponse response, DirectoryInfo dir)
        {
            var sb = new StringBuilder();
            var shortName = MakeShortName(rootDirectory.FullName, dir.FullName);
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>")
                .Append(shortName)
                .Append("</title></head><body><h1>Directory Listing: ")
                .Append(shortName)
                .Append("</h1><ul>");

            var paths = from subPath in dir.GetFileSystemInfos()
                        select MakeShortName(dir.FullName, subPath.FullName);

            if (string.CompareOrdinal(dir.Parent.FullName, rootDirectory.FullName) == 0)
            {
                paths = paths.Prepend("..");
            }

            foreach (var subPath in paths)
            {
                _ = sb.Append("<li><a href=\"")
                  .Append(subPath)
                  .Append("\">")
                  .Append(subPath)
                  .Append("</a></li>");
            }

            _ = sb.Append("</ul></body></html>");

            response.ContentLength64 = sb.Length;
            response.ContentType = MediaType.Text.Html;
            using var writer = new StreamWriter(response.OutputStream);
            await writer.WriteAsync(sb.ToString())
                .ConfigureAwait(false);
        }

        private async Task SendFileAsync(HttpListenerResponse response, FileInfo file, string shortName)
        {
            try
            {
                await response
                    .SendFileAsync(file)
                    .ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exp)
            {
                var message = $"ERRRRRROR: '{shortName}' > {exp.Message}";
                OnWarning(message);
                response.SetStatus(HttpStatusCode.InternalServerError);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}