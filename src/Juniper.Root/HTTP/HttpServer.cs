using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Juniper.HTTP
{
    /// <summary>
    /// A wrapper around <see cref="HttpListener"/> that handles
    /// routing, HTTPS, and a default start page for DEBUG running.
    /// </summary>
    public class HttpServer
    {
        private readonly Thread serverThread;
        private readonly HttpListener listener;
        private readonly List<Task> waiters = new List<Task>();
        private readonly List<AbstractRouteHandler> routes = new List<AbstractRouteHandler>();
        private readonly List<WebSocketConnection> sockets = new List<WebSocketConnection>();

        /// <summary>
        /// <para>
        /// Begin constructing a server.
        ///     * MaxConnections = 100.
        ///     * ListenAddress =
        ///         * DEBUG: localhost
        ///         * RELEASE: *
        /// </para>
        /// <para>
        /// Server doesn't start listening until <see cref="Start"/>
        /// is called.
        /// </para>
        /// </summary>
        public HttpServer()
        {
            MaxConnections = 100;
            EnableWebSockets = true;

#if DEBUG
            ListenAddress = "localhost";
#else
            ListenAddress = "*";
#endif

            serverThread = new Thread(Listen);

            listener = new HttpListener
            {
                AuthenticationSchemeSelectorDelegate = GetAuthenticationSchemeForRequest
            };
        }

        /// <summary>
        /// Gets or sets the maximum connections. Any connections beyond the max
        /// will wait indefinitely until a connection becomes available.
        /// </summary>
        /// <value>
        /// The maximum connections.
        /// </value>
        public int MaxConnections
        {
            get;
            set;
        }

        /// <summary>
        /// Set to false to disable handling websockets
        /// </summary>
        public bool EnableWebSockets
        {
            get;
            set;
        }

        /// <summary>
        /// When set to true, HTTP request will automatically be handled as Redirect
        /// responses that point to the HTTPS version of the URL.
        /// </summary>
        /// <seealso cref="HttpsRedirectController"/>
        public bool RedirectHttp2Https
        {
            get;
            set;
        }

        /// <summary>
        /// The IP address at which to listen for requests. By default, this is set
        /// to "*".
        /// </summary>
        /// <value>
        /// The listen address.
        /// </value>
        public string ListenAddress
        {
            get;
            set;
        }

        /// <summary>
        /// The domain name that this server serves. This is necessary to be able
        /// to automatically assign a TLS certificate to the process to handle HTTPS
        /// connections. The TLS certifcate and authority chain must be installed
        /// in the Windows Certificate Store (certmgr).
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
        public string Domain
        {
            get;
            set;
        }

        /// <summary>
        /// The port on which to listen for HTTPS connections.
        /// </summary>
        /// <value>
        /// The HTTPS port.
        /// </value>
        public ushort HttpsPort
        {
            get;
            set;
        }

        /// <summary>
        /// <para>The port on which to listen for insecure HTTP connections.</para>
        /// <para>
        /// WARNING: only use this in testing, or to redirect users to
        /// the HTTPS version of the request.
        /// </para>
        /// </summary>
        /// <value>
        /// The HTTP port.
        /// </value>
        public ushort HttpPort
        {
            get;
            set;
        }

#if DEBUG        
        /// <summary>
        /// When running in DEBUG mode, sets the page that will be opened
        /// by the default web browser when the server process starts.
        /// </summary>
        /// <value>
        /// The start page.
        /// </value>
        public string StartPage
        {
            get;
            set;
        }
#endif

        /// <summary>
        /// Set a directory for static content serving.
        /// </summary>
        /// <param name="directory">The directory in which to find the static content.</param>
        public void AddContentPath(DirectoryInfo directory)
        {
            if (directory is null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (directory.Exists)
            {
                OnInfo($"Serving content from path {directory.FullName}");
                var defaultFileHandler = new DefaultFileController(directory);
                defaultFileHandler.Warning += OnWarning;
                AddRoutesFrom(defaultFileHandler);
            }
        }

        /// <summary>
        /// Set a directory for static content serving.
        /// </summary>
        /// <param name="path">The name of the directory in which to find the static content.</param>
        public void AddContentPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                AddContentPath(new DirectoryInfo(path));
            }
        }

        public event EventHandler<string> Info;

        private void OnInfo(string message)
        {
            Info?.Invoke(this, message);
        }

        public event EventHandler<string> Warning;

        private void OnWarning(object sender, string message)
        {
            Warning?.Invoke(this, message);
        }

        public event EventHandler<string> Error;

        private void OnError(string message)
        {
            Error?.Invoke(this, message);
        }

        public event EventHandler Update;

        private AuthenticationSchemes GetAuthenticationSchemeForRequest(HttpListenerRequest request)
        {
            return (from route in routes
                    where route.IsMatch(request)
                    select route.Authentication)
                .FirstOrDefault();
        }

        public void AddRoutesFrom<T>()
        {
            AddRoutesFrom(typeof(T));
        }

        public void AddRoutesFrom(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            AddRoutesFrom(null, type);
        }

        public void AddRoutesFrom(object controller)
        {
            if (controller is null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            AddRoutesFrom(controller, controller.GetType());
        }

        private void AddRoutesFrom(object controller, Type type)
        {
            var flags = BindingFlags.Public | BindingFlags.Static;
            if (controller is object)
            {
                flags |= BindingFlags.Instance;
            }

            var handler = controller as AbstractRouteHandler;

            if (handler != null)
            {
                routes.Add(handler);
            }

            foreach (var method in type.GetMethods(flags))
            {
                var route = method.GetCustomAttribute<RouteAttribute>();
                if (route != null)
                {
                    var parameters = method.GetParameters();
                    if (method.ReturnType == typeof(Task)
                        && parameters.Length > 0
                        && parameters.Length == route.parameterCount
                        && parameters.Skip(1).All(p => p.ParameterType == typeof(string)))
                    {
                        var name = $"{type.Name}::{method.Name}";
                        var contextParamType = parameters[0].ParameterType;
                        var isHttp = contextParamType == typeof(HttpListenerContext);
                        var isWebSocket = contextParamType == typeof(WebSocketConnection);
                        var source = method.IsStatic ? null : controller;

                        if (!isHttp && !isWebSocket)
                        {
                            OnError($@"Method {type.Name}::{method.Name} must hava a signature:
    (System.Net.HttpListenerContext, string...) => Task
or
    (Juniper.HTTP.WebSocketConnection, string...) => Task");
                            handler = null;
                        }
                        else if (isHttp)
                        {
                            handler = new HttpRouteHandler(name, route, source, method);
                        }
                        else if (isWebSocket)
                        {
                            var wsHandler = new WebSocketRouteHandler(name, route, source, method);
                            wsHandler.SocketConnected += sockets.Add;
                            handler = wsHandler;
                        }

                        if (handler != null)
                        {
                            OnInfo($"Found controller {handler}");
                            routes.Add(handler);
                        }
                    }
                    else
                    {
                    }
                }
            }

            routes.Sort();
        }

        public bool IsRunning
        {
            get
            {
                return listener.IsListening
                    && serverThread.IsAlive;
            }
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            OnInfo("Stopping server");
            listener.Stop();
            listener.Close();
            serverThread.Abort();
        }

        public void Start()
        {
            if (!string.IsNullOrEmpty(Domain)
                && HttpsPort > 0)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                GetTLSParameters(out var guid, out var certHash);

                if (string.IsNullOrEmpty(guid))
                {
                    OnWarning(this, "Couldn't find application GUID");
                }
                else if (string.IsNullOrEmpty(certHash))
                {
                    OnWarning(this, "No TLS cert found!");
                }
                else
                {
                    OnInfo($"Application GUID: {guid}");
                    OnInfo($"TLS cert: {certHash}");
                    var message = AssignCertToApp(certHash, guid);

                    OnInfo(message);

                    if (message.Equals("SSL Certificate added successfully", StringComparison.InvariantCultureIgnoreCase)
                        || message.StartsWith("SSL Certificate add failed, Error: 183", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SetPrefix("https", HttpsPort);
                    }
                    else if (message.Equals("The parameter is incorrect.", StringComparison.InvariantCultureIgnoreCase))
                    {
                        OnWarning(this, "Couldn't configure the certificate correctly");
                    }
                }
            }

            if (HttpPort > 0 || RedirectHttp2Https)
            {
                if (RedirectHttp2Https)
                {
                    if (HttpPort == 0)
                    {
                        HttpPort = 80;
                    }

                    AddRoutesFrom<HttpsRedirectController>();
                }

                SetPrefix("http", HttpPort);
            }

            if (!listener.IsListening)
            {
                var prefixes = string.Join(", ", listener.Prefixes);
                OnInfo($"Listening on: {prefixes}");
                listener.Start();
            }

            if (!serverThread.IsAlive)
            {
                serverThread.Start();
            }

#if DEBUG
            if (!string.IsNullOrEmpty(StartPage))
            {
                var protocol = HttpsPort > 0
                    ? "https"
                    : "http";

                var port = HttpsPort > 0
                    ? HttpsPort == 443
                        ? ""
                        : ":" + HttpsPort
                    : HttpPort == 80
                        ? ""
                        : ":" + HttpPort;

                var page = $"{protocol}://{ListenAddress}{port}/{StartPage}";

                Process.Start(new ProcessStartInfo($"explorer", $"\"{page}\"")
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Maximized
                });
            }
#endif
        }

        private void SetPrefix(string protocol, ushort port)
        {
            if (!string.IsNullOrEmpty(protocol))
            {
                if (port > 0)
                {
                    OnInfo($"Listening for {protocol} on port {port}");
                    listener.Prefixes.Add($"{protocol}://{ListenAddress}:{port}/");
                }
                else
                {
                    var prefix = listener.Prefixes.FirstOrDefault(p => p.EndsWith(":" + port));
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        OnInfo($"Deleting prefix {prefix}");
                        listener.Prefixes.Remove(prefix);
                    }
                }
            }
        }

        private void GetTLSParameters(out string guid, out string certHash)
        {
            var asm = Assembly.GetExecutingAssembly();
            guid = Marshal.GetTypeLibGuidForAssembly(asm).ToString();
            certHash = null;
            using (var store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);

                certHash = (from cert in store.Certificates.Cast<X509Certificate2>()
                            where cert.Subject == "CN=" + Domain
                              && DateTime.TryParse(cert.GetEffectiveDateString(), out var effectiveDate)
                              && DateTime.TryParse(cert.GetExpirationDateString(), out var expirationDate)
                              && effectiveDate <= DateTime.Now
                              && DateTime.Now < expirationDate
                            select cert.Thumbprint)
                           .FirstOrDefault();
            }
        }

        private string AssignCertToApp(string certHash, string appGuid)
        {
            var listenAddress = ListenAddress;
            if (listenAddress == "*")
            {
                listenAddress = "0.0.0.0";
            }

            var procInfo = new ProcessStartInfo("netsh", $"http add sslcert ipport={listenAddress}:{HttpsPort} certhash={certHash} appid={{{appGuid}}}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                ErrorDialog = false,
                LoadUserProfile = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var proc = Process.Start(procInfo);
            var message = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return message;
        }

        private void Listen()
        {
            while (listener.IsListening)
            {
                try
                {
                    Update?.Invoke(this, EventArgs.Empty);

                    for (var i = sockets.Count - 1; i >= 0; --i)
                    {
                        var socket = sockets[i];
                        if (socket.State == WebSocketState.CloseReceived)
                        {
                            socket.Close();
                        }
                        else if (socket.State == WebSocketState.Closed
                            || socket.State == WebSocketState.Aborted)
                        {
                            sockets.RemoveAt(i);
                            socket.Dispose();
                        }
                        else
                        {
                            socket.Update();
                        }
                    }

                    for (int i = waiters.Count - 1; i >= 0; --i)
                    {
                        var waiter = waiters[i];
                        if (!waiter.IsRunning())
                        {
                            waiters.RemoveAt(i);
                        }
                    }

                    while (waiters.Count < MaxConnections)
                    {
                        waiters.Add(HandleConnection());
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception exp)
                {
                    OnError($"ERRROR: {exp.Message}");
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        private async Task HandleConnection()
        {
            var context = await listener.GetContextAsync()
                .ConfigureAwait(false);
            var requestID = $"{{{DateTime.Now.ToShortTimeString()}}} {context.Request.UrlReferrer} [{context.Request.HttpMethod}] {context.Request.Url.PathAndQuery} => {context.Request.RemoteEndPoint}";
            try
            {
                OnInfo(requestID);

                var handled = false;

                foreach (var route in routes)
                {
                    if (route.IsMatch(context.Request))
                    {
                        await route.Invoke(context)
                            .ConfigureAwait(false);

                        handled = true;
                        if (!route.Continue)
                        {
                            break;
                        }
                    }
                }

                if (!handled)
                {
                    var message = $"Not found: {requestID}";
                    OnWarning(this, message);
                    context.Response.Error(HttpStatusCode.NotFound, message);
                }

                context.Response.OutputStream.Flush();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exp)
            {
                OnError(exp.Message);
                context.Response.Error(HttpStatusCode.InternalServerError, "Internal error");
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                context.Response.StatusDescription = HttpStatusDescription.Get(context.Response.StatusCode);

                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.Close();
                }
            }
        }
    }
}