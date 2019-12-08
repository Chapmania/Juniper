using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Juniper.HTTP
{
    public class DefaultFileController
    {
        private static readonly string[] INDEX_FILES = {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
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

        private static string MassageRequestPath(string requestPath)
        {
            requestPath = requestPath.Substring(1);

            if (requestPath.Length > 0 && requestPath[requestPath.Length - 1] == '/')
            {
                requestPath = requestPath.Substring(0, requestPath.Length - 1);
            }

            requestPath = requestPath.Replace('/', Path.DirectorySeparatorChar);
            return requestPath;
        }

        private readonly string rootDirectoryPath;
        private readonly DirectoryInfo rootDirectory;

        public event EventHandler<string> Warning;
        private void OnWarning(string message)
        {
            Warning?.Invoke(this, message);
        }

        public DefaultFileController(string rootDirectoryPath)
        {
            this.rootDirectoryPath = rootDirectoryPath;
            rootDirectory = new DirectoryInfo(rootDirectoryPath);
        }

        [Route(".*", Priority = int.MaxValue)]
        public async Task ServeFile(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var requestPath = request.Url.AbsolutePath;
            var requestFile = MassageRequestPath(requestPath);
            var filename = Path.Combine(rootDirectoryPath, requestFile);
            var isDirectory = Directory.Exists(filename);

            if (isDirectory)
            {
                filename = FindDefaultFile(filename);
            }

            var file = new FileInfo(filename);
            var shortName = MakeShortName(rootDirectoryPath, filename);

            if (!rootDirectory.Contains(file))
            {
                response.Error(HttpStatusCode.Unauthorized, "Unauthorized");
            }
            else if (isDirectory && requestPath[requestPath.Length - 1] != '/')
            {
                response.Redirect(requestPath + "/");
            }
            else if (file.Exists)
            {
                await SendFile(response, file, shortName)
                    .ConfigureAwait(false);
            }
            else if (isDirectory)
            {
                await ListDirectory(response, new DirectoryInfo(filename))
                    .ConfigureAwait(false);
            }
            else
            {
                var message = $"request '{shortName}'";
                OnWarning(message);
                response.Error(HttpStatusCode.NotFound, message);
            }
        }

        private async Task ListDirectory(HttpListenerResponse response, DirectoryInfo dir)
        {
            var sb = new StringBuilder();
            var shortName = MakeShortName(rootDirectory.FullName, dir.FullName);
            sb.AppendFormat("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>{0}</title></head><body><h1>Directory Listing: {0}</h1><ul>", shortName);

            var paths = (from subPath in dir.GetFileSystemInfos()
                         select MakeShortName(dir.FullName, subPath.FullName));

            if (!dir.Parent.FullName.Equals(rootDirectory.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                paths = paths.Prepend("..");
            }

            foreach (var subPath in paths)
            {
                sb.AppendFormat("<li><a href=\"{0}\">{0}</a></li>", subPath);
            }

            sb.Append("</ul></body></html>");

            response.ContentLength64 = sb.Length;
            response.ContentType = MediaType.Text.Html;
            using (var writer = new StreamWriter(response.OutputStream))
            {
                await writer.WriteAsync(sb.ToString())
                    .ConfigureAwait(false);
            }
        }

        private async Task SendFile(HttpListenerResponse response, FileInfo file, string shortName)
        {
            try
            {
                await response.SendFileAsync(file)
                    .ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exp)
            {
                var message = $"ERRRRRROR: '{shortName}' > {exp.Message}";
                OnWarning(message);
                response.Error(HttpStatusCode.InternalServerError, message);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}