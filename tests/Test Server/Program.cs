using System.Collections.Generic;
using System.Threading.Tasks;

using static System.Console;

namespace Juniper.HTTP
{
    public class Program
    {
        private static readonly List<WebSocketConnection> sockets = new List<WebSocketConnection>();

        public static void Main()
        {
            var server = new HttpServer
            {
                HttpPort = 8080,
                MaxConnections = 2,
                StartPage = "index.html"
            };

            server.Info += Server_Info;
            server.Warning += Server_Warning;
            server.Error += Server_Error;

            server.AddContentPath("content");
            server.AddRoutesFrom<Program>();

            server.Start();
        }

        [Route("connect/")]
        public static Task AcceptWebSocket(WebSocketConnection socket)
        {
            sockets.Add(socket);
            socket.Message += Socket_Message;
            WriteLine("Got socket");
            return Task.CompletedTask;
        }

        private static void Socket_Message(object sender, string e)
        {
            WriteLine($"[SOCKET] {e}");
            var socket = (WebSocketConnection)sender;
            socket.Send(e + "from server");
        }

        private static void Server_Info(object sender, string e)
        {
            WriteLine(e);
        }

        private static void Server_Warning(object sender, string e)
        {
            WriteLine($"[WARNING] {e}");
        }

        private static void Server_Error(object sender, string e)
        {
            Error.WriteLine(e);
        }
    }
}
