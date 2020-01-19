using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Juniper.HTTP.Server;
using Juniper.HTTP.Server.Controllers;
using Juniper.Processes;
using static System.Console;
using static Juniper.AnsiColor;

namespace Juniper.HTTP
{
    public static class Program
    {
        [Route("auth/", Methods = HttpMethods.POST, Authentication = AuthenticationSchemes.Basic)]
        public static async Task AuthenticateUserAsync(HttpListenerContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var response = context.Response;

            if (context.User.Identity is HttpListenerBasicIdentity user
                && user.Name == "sean"
                && user.Password == "ppyptky7")
            {
                var token = Guid.NewGuid().ToString();
                WebSocketPool.SetUserToken(user.Name, token);
                response.SetStatus(HttpStatusCode.OK);
                await response.SendTextAsync(token)
                    .ConfigureAwait(false);
            }
            else
            {
                response.SetStatus(HttpStatusCode.Unauthorized);
            }
        }

        [Route("connect/")]
        public static Task AcceptWebSocketAsync(ServerWebSocketConnection socket)
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

        private static HttpServer server;

        private static uint logLevel = 0;
        private static readonly Dictionary<ConsoleKey, NamedAction> actions = new Dictionary<ConsoleKey, NamedAction>
        {
            [ConsoleKey.UpArrow] = ("increase log level", () => SetLogLevel(1)),
            [ConsoleKey.DownArrow] = ("decrease log level", () => SetLogLevel(-1)),
            [ConsoleKey.Q] = ("quit server", () => server.Stop())
        };

        private static async Task Main(string[] args)
        {
            Log(0, WriteLine, Green, "Starting server");

            // Read options
            var options = new Dictionary<string, string>();

#if DEBUG
            //Set default options
            options.SetValues(
                ("path", Path.Combine("..", "..", "..", "content")),
                ("domain", "localhost"),
                ("http", HttpServer.IsAdministrator ? "80" : "8080"),
                ("https", HttpServer.IsAdministrator ? "443" : "8081"));
#endif

            options.SetValues(args);

            using var s = server = new HttpServer
            {
                ListenerCount = 10,
                AutoAssignCertificate = HttpServer.IsAdministrator
            };

            server.Info += Server_Info;
            server.Warning += Server_Warning;
            server.Err += Server_Error;
            server.Log += Server_Log;

            if (server.SetOptions(options))
            {
                server.Add(typeof(Program));

                server.Start();

#if DEBUG
                using var browserProc = server.StartBrowser("index.html");
#endif

                while (server.IsRunning)
                {
                    await Task.Yield();

                    if (KeyAvailable)
                    {
                        var keyInfo = ReadKey(true);
                        if (actions.ContainsKey(keyInfo.Key))
                        {
                            actions[keyInfo.Key].Invoke();
                        }
                        else
                        {
                            PrintUsage();
                        }
                    }
                }

                server.Info -= Server_Info;
                server.Warning -= Server_Warning;
                server.Err -= Server_Error;
                server.Log -= Server_Log;
            }
        }

        private static void SetLogLevel(int direction)
        {
            var nextLogLevel = (uint)(logLevel + direction);
            if (nextLogLevel < 4)
            {
                logLevel = nextLogLevel;
                WriteLine($"Logging level is now {GetName(logLevel)}");
            }
        }

        private static string GetName(uint logLevel)
        {
            return logLevel switch
            {
                0 => "Verbose",
                1 => "Log",
                2 => "Warning",
                3 => "Error",
                _ => "N/A"
            };
        }

        private static void PrintUsage()
        {
            WriteLine("Usage:");
            var maxKeyLen = (from key in actions.Keys
                             let keyName = key.ToString()
                             select keyName.Length)
                        .Max();

            var format = $"\t{{0,-{maxKeyLen}}} : {{1}}";
            foreach (var command in actions)
            {
                WriteLine(format, command.Key, command.Value.Name);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Log<T>(uint level, Action<string> logger, string color, T e)
        {
            if (level >= logLevel)
            {
                logger($"{color}[{GetName(level)}] {e.ToString()}{Reset}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Server_Info(object sender, StringEventArgs e)
        {
            Log(0, WriteLine, White, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Socket_Message(object sender, StringEventArgs e)
        {
            var socket = (ServerWebSocketConnection)sender;
            Log(0, WriteLine, Green, e);
            var msg = e.Value + " from server";
            _ = Task.Run(() => socket.SendAsync(msg));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Server_Log(object sender, StringEventArgs e)
        {
            Log(1, WriteLine, Cyan, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Server_Warning(object sender, StringEventArgs e)
        {
            Log(2, WriteLine, Yellow, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Socket_Error(object sender, ErrorEventArgs e)
        {
            Log(3, Error.WriteLine, Red, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Server_Error(object sender, ErrorEventArgs e)
        {
            Log(3, Error.WriteLine, Red, e);
        }
    }
}
