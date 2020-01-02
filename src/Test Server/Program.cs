using System;
using System.Globalization;
using System.Threading.Tasks;

using Juniper.HTTP.Server;
using Juniper.HTTP.Server.Controllers;

using static System.Console;

namespace Juniper.HTTP
{
    public static class Program
    {
        public static async Task Main()
        {
            using var server = new HttpServer
            {
                HttpPort = 8080,
                ListenerCount = 10
            };

            server.Info += Server_Info;
            server.Warning += Server_Warning;
            server.Err += Server_Error;

            _ = server.AddRoutesFrom(new DefaultFileController("..\\..\\..\\content"));
            _ = server.AddRoutesFrom(new IPBanController("testBans.txt"));
            _ = server.AddRoutesFrom(typeof(Program));

            server.Start();

#if DEBUG
            using var browserProc = server.StartBrowser("index.html");
#endif

            while (server.IsRunning)
            {
                await Task.Yield();
            }
        }

        [Route("connect/")]
        public static Task AcceptWebSocketAsync(WebSocketConnection socket)
        {
            if (socket is object)
            {
                socket.Message += Socket_Message;
                socket.Error += Socket_Error;
                var code = socket
                    .GetHashCode()
                    .ToString(CultureInfo.InvariantCulture);
                WriteLine($"Got socket {code}");
            }

            return Task.CompletedTask;
        }

        private static void Socket_Error(object sender, ErrorEventArgs e)
        {
            Error.WriteLine($"[SOCKET ERROR] {e.Exception.Unroll()}");
        }

        private static void Socket_Message(object sender, string msg)
        {
            var socket = (WebSocketConnection)sender;
            WriteLine($"[SOCKET] {msg}");
            msg += " from server";
            Task.Run(() => socket.SendAsync(msg));
        }

        private static void Server_Info(object sender, string e)
        {
            WriteLine(e);
        }

        private static void Server_Warning(object sender, string e)
        {
            WriteLine($"[WARNING] {e}");
        }

        private static void Server_Error(object sender, ErrorEventArgs e)
        {
            Error.WriteLine(e.Exception.Unroll());
        }
    }
}
