using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Juniper.HTTP.Server.Controllers;
using Juniper.Logging;

using System.Text;
using System.Collections.Specialized;
using Juniper.Processes;
using System.IO;

namespace Juniper.HTTP.Server
{
    /// <summary>
    /// A wrapper around <see cref="HttpListener"/> that handles
    /// routing, HTTPS, and a default start page for DEBUG running.
    /// </summary>
    public class HttpServer :
        IDisposable,
        ILoggingSource,
        INCSALogSource
    {

        public static bool IsAdministrator
        {
            get
            {
#if !NETCOREAPP && !NETSTANDARD
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
#else
                return false;
#endif
            }
        }

        private readonly Thread serverThread;
        private readonly HttpListener listener;
        private readonly List<object> controllers = new List<object>();
        private readonly List<AbstractResponse> routes = new List<AbstractResponse>();

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
            ListenerCount = 100;
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
        public int ListenerCount { get; set; }

        /// <summary>
        /// The domain name that this server serves. This is necessary to be able
        /// to automatically assign a TLS certificate to the process to handle HTTPS
        /// connections. The TLS certifcate and authority chain must be installed
        /// in the Windows Certificate Store (certmgr).
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
        public string Domain { get; set; }

        /// <summary>
        /// The port on which to listen for HTTPS connections.
        /// </summary>
        /// <value>
        /// The HTTPS port.
        /// </value>
        public ushort? HttpsPort { get; set; }

        public bool SetOptions(Dictionary<string, string> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Echo options
            OnInfo("Startup parameters are:");
            foreach (var param in options)
            {
                OnInfo($"\t{param.Key} = {param.Value}");
            }

            var hasHttpsPort = options.TryGetUInt16("https", out var httpsPort);

            var hasHttpPort = options.TryGetUInt16("http", out var httpPort);

            var hasPort = Check(
                hasHttpsPort || hasHttpPort,
                "Must specify at least one of --https or --http");

            var hasDomain = Check(
                options.TryGetValue("domain", out var domain),
                "No domain specified");

            var hasLogPath = Check(
                options.TryGetValue("log", out var logPath),
                "No logging path");

            var hasBansPath = Check(
                options.TryGetValue("bans", out var bansPath),
                "Path to ban file does not exist.");

            var hasContentPath = options.TryGetValue("path", out var contentPath);

            var isValidContentPath = Check(
                !hasContentPath || Directory.Exists(contentPath),
                "Path to static content directory does not exist");

            if (hasContentPath && !isValidContentPath)
            {
                OnWarning($"Content directory was attempted from: {new DirectoryInfo(contentPath).FullName}");
            }

            if (hasDomain && hasPort)
            {
                // Set options on server
                Domain = domain;

                if (hasHttpsPort)
                {
                    HttpsPort = httpsPort;
                }

                if (hasHttpPort)
                {
                    HttpPort = httpPort;
                    Add(new HttpToHttpsRedirect());
                }

                if (hasLogPath)
                {
                    Add(new NCSALogger(logPath));
                }

                if (IsAdministrator)
                {
                    var banController = hasBansPath
                        ? new BanHammer(bansPath)
                        : new BanHammer();

#if !NETCOREAPP && !NETSTANDARD
                    banController.BanAdded += BanController_BanAdded;
                    banController.BanRemoved += BanController_BanRemoved;
#endif

                    Add(banController);
                }


                if (isValidContentPath
                    && hasContentPath)
                {
                    Add(new StaticFileServer(contentPath));
                }

                return true;
            }
            return false;
        }

        private bool Check(bool isValid, string message)
        {
            if (!isValid)
            {
                OnError(new ArgumentException(message));
            }

            return isValid;
        }

#if !NETCOREAPP && !NETSTANDARD
        private void BanController_BanAdded(object sender, EventArgs<CIDRBlock> e)
        {
            _ = Task.Run(() => AddBanAsync(e.Value));
        }

        private static Task<bool> BanExistsAsync(string name)
        {
            return new GetFirewallRule(name).ExistsAsync();
        }

        private async Task AddBanAsync(CIDRBlock block)
        {
            OnInfo($"Adding ban to firewall: {block}");
            var name = $"Ban {block}";
            var exists = await BanExistsAsync(name).ConfigureAwait(false);
            if (!exists)
            {
                var add = new AddFirewallRule(name, FirewallRuleDirection.In, FirewallRuleAction.Block, block);
                await add.RunAsync()
                    .ConfigureAwait(false);
            }
        }

        private void BanController_BanRemoved(object sender, EventArgs<CIDRBlock> e)
        {
            _ = Task.Run(() => RemoveBanAsync(e.Value));
        }

        private static async Task RemoveBanAsync(CIDRBlock e)
        {
            var name = $"Ban {e}";
            var exists = await BanExistsAsync(name).ConfigureAwait(false);
            if (exists)
            {
                var delete = new DeleteFirewallRule(name);
                await delete.RunAsync()
                    .ConfigureAwait(false);
            }
        }

        private void GetTLSParameters(out Guid guid, out string certHash)
        {
            var asm = Assembly.GetExecutingAssembly();
            guid = Marshal.GetTypeLibGuidForAssembly(asm);
            certHash = null;
            using var store = new X509Store(StoreLocation.LocalMachine);
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


        private string AssignCertToApp(string certHash, Guid appGuid)
        {
            var addCert = new AddSslCert(
                "0.0.0.0",
                HttpsPort.Value,
                certHash,
                appGuid);

            addCert.Info += OnInfo;
            addCert.Warning += OnWarning;
            addCert.Err += OnError;

            _ = addCert.Run();

            addCert.Info -= OnInfo;
            addCert.Warning -= OnWarning;
            addCert.Err -= OnError;

            return addCert.TotalStandardOutput;
        }
#endif

        /// <summary>
        /// Set to true if the server should attempt to run netsh to assign
        /// a certificate to the application before starting the server.
        /// </summary>
        public bool AutoAssignCertificate { get; set; }

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
        public ushort? HttpPort { get; set; }

        /// <summary>
        /// Event for handling Common Log Format logs.
        /// </summary>
        public event EventHandler<StringEventArgs> Log;

        /// <summary>
        /// Event for handling information-level logs.
        /// </summary>
        public event EventHandler<StringEventArgs> Info;

        /// <summary>
        /// Event for handling error logs that don't stop execution.
        /// </summary>
        public event EventHandler<StringEventArgs> Warning;

        /// <summary>
        /// Event for handling error logs that prevent execution.
        /// </summary>
        public event EventHandler<ErrorEventArgs> Err;

        public void Add(params object[] controllers)
        {
            if (controllers is null)
            {
                throw new ArgumentNullException(nameof(controllers));
            }

            for (var i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];

                if (controller is null)
                {
                    throw new NullReferenceException($"Encountered a null value at index {i}.");
                }

                var flags = BindingFlags.Public | BindingFlags.Static;
                var type = controller as Type ?? controller.GetType();
                if (controller is Type)
                {
                    controller = null;
                }
                else
                {
                    flags |= BindingFlags.Instance;
                    AddController(controller);
                }

                foreach (var method in type.GetMethods(flags))
                {
                    var route = method.GetCustomAttribute<RouteAttribute>();
                    if (route is object)
                    {
                        var parameters = method.GetParameters();
                        if (method.ReturnType == typeof(Task)
                            && parameters.Length > 0
                            && parameters.Length == route.ParameterCount
                            && parameters.Skip(1).All(p => p.ParameterType == typeof(string)))
                        {
                            var contextParamType = parameters[0].ParameterType;
                            var isHttp = contextParamType == typeof(HttpListenerContext);
                            var isWebSocket = contextParamType == typeof(ServerWebSocketConnection);

                            object source = null;
                            if (!method.IsStatic)
                            {
                                source = controller;
                            }

                            if (isHttp)
                            {
                                AddController(new HttpRoute(source, method, route));
                            }
                            else if (isWebSocket)
                            {
                                if (route.Authentication != AuthenticationSchemes.Anonymous)
                                {
                                    throw new InvalidOperationException("WebSockets do not support authentication");
                                }

                                AddController(new WebSocketRoute(source, method, route));
                            }
                            else
                            {
                                throw new InvalidOperationException($@"Method {type.Name}::{method.Name} must have a signature:
    (System.Net.HttpListenerContext, string...) => Task
or
    (Juniper.HTTP.Server.ServerWebSocketConnection, string...) => Task");
                            }
                        }
                    }
                }
            }
        }

        private void AddController<T>(T controller) where T : class
        {
            if (controller is AbstractResponse handler)
            {
                handler.Server = this;
                routes.Add(handler);
            }

            if (controller is IInfoSource infoSource)
            {
                infoSource.Info += OnInfo;
            }

            if (controller is IWarningSource warningSource)
            {
                warningSource.Warning += OnWarning;
            }

            if (controller is IErrorSource errorSource)
            {
                errorSource.Err += OnError;
            }

            if (controller is INCSALogSource nCSALogSource)
            {
                nCSALogSource.Log += OnLog;
            }

            controllers.Add(controller);
        }

        public T GetController<T>()
            where T : class
        {
            return (T)controllers
                .Find(c => c is T);
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
        public async Task Stop()
        {
            OnInfo("Stopping server");
            foreach (var controller in controllers)
            {
                if (controller is IInfoSource infoSource)
                {
                    infoSource.Info -= OnInfo;
                }

                if (controller is IWarningSource warningSource)
                {
                    warningSource.Warning -= OnWarning;
                }

                if (controller is IErrorSource errorSource)
                {
                    errorSource.Err -= OnError;
                }

                if (controller is INCSALogSource nCSALogSource)
                {
                    nCSALogSource.Log -= OnLog;
                }
            }

            listener.Stop();
            listener.Close();
            var end = DateTime.Now.AddSeconds(3);
            while (serverThread.IsAlive && DateTime.Now < end)
            {
                await Task.Yield();
            }
        }

        public virtual void Start()
        {
            OnInfo("Starting server");

#if DEBUG
            var redirector = GetController<HttpToHttpsRedirect>();
            redirector.Enabled = false;
#endif

            if (routes.Count > 0)
            {
                routes.Sort();
                ShowRoutes();
            }

            var platform = Environment.OSVersion.Platform;
            if (HttpsPort is object
                && AutoAssignCertificate
                && (platform == PlatformID.Win32NT
                    || platform == PlatformID.Win32Windows
                    || platform == PlatformID.Win32S))
            {
                if (string.IsNullOrEmpty(Domain))
                {
                    OnWarning("No domain was specified. Can't auto-assign a TLS certificate.");
                }
#if !NETCOREAPP && !NETSTANDARD
                else
                {
                    GetTLSParameters(out var guid, out var certHash);

                    if (string.IsNullOrEmpty(certHash))
                    {
                        OnWarning("No TLS cert found!");
                    }
                    else
                    {
                        var message = AssignCertToApp(certHash, guid);
                        if (message.Equals("The parameter is incorrect.", StringComparison.OrdinalIgnoreCase))
                        {
                            OnWarning($@"Couldn't configure the certificate correctly:
    Application GUID: {guid}
    TLS cert: {certHash}
    {message}");
                        }
                    }
                }
#endif
            }

            if (HttpPort is null
                && HttpsPort is null)
            {
                OnError(new InvalidOperationException("No HTTP or HTTPS port specified."));
            }
            else
            {
                if (HttpsPort is object)
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    SetPrefix("https", HttpsPort.Value);
                }

                if (HttpPort is object)
                {
                    SetPrefix("http", HttpPort.Value);
#if !DEBUG
                    var httpRoutes = from route in routes
                                     where !(route is HttpToHttpsRedirect)
                                        && !(route is BanHammer)
                                        && !(route is NCSALogger)
                                        && !(route is UnhandledRequestTrap)
                                        && route.Protocols.HasFlag(HttpProtocols.HTTP)
                                     select route;

                    if (httpRoutes.Any())
                    {
                        OnWarning("Maybe don't run unencrypted HTTP in production, k?");
                    }
#endif
                }


                if (!listener.IsListening)
                {
                    OnInfo($"Listening on:");
                    foreach (var prefix in listener.Prefixes)
                    {
                        OnInfo($"\t{prefix}");
                    }

                    listener.Start();
                }

                if (!serverThread.IsAlive)
                {
                    serverThread.Start();
                }
            }
        }

#if DEBUG
        public System.Diagnostics.Process StartBrowser(string startPage = null)
        {
            if (HttpsPort is null
                && HttpPort is null)
            {
                throw new InvalidOperationException("Server is not listening on any ports");
            }

            startPage ??= string.Empty;

            var protocol = "http";
            var port = "";

            if (HttpsPort is object)
            {
                protocol = "https";
                if (HttpsPort != 443)
                {
                    port = HttpsPort.Value.ToString(CultureInfo.InvariantCulture);
                }
            }
            else if (HttpPort is object
                && HttpPort != 80)
            {
                port = HttpPort.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (port.Length > 0)
            {
                port = ":" + port;
            }

            var page = $"{protocol}://localhost{port}/{startPage}";

            return System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"explorer", $"\"{page}\"")
            {
                UseShellExecute = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized
            });
        }
#endif

        private void SetPrefix(string protocol, ushort port)
        {
            if (!string.IsNullOrEmpty(protocol)
                && port >= 0)
            {
                listener.Prefixes.Add($"{protocol}://localhost:{port.ToString(CultureInfo.InvariantCulture)}/");
            }
        }

        private void Listen()
        {
            while (listener.IsListening)
            {
                try
                {
                    if (ListenerCount > 0)
                    {
                        _ = HandleConnectionAsync();
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception exp)
                {
                    OnError(exp);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        private AuthenticationSchemes GetAuthenticationSchemeForRequest(HttpListenerRequest request)
        {
            var auth = (from route in routes
                        where route.IsRequestMatch(request)
                        orderby Math.Abs(route.Priority)
                        select route.Authentication)
                .FirstOrDefault();

            if (auth == AuthenticationSchemes.None)
            {
                auth = AuthenticationSchemes.Anonymous;
            }

            return auth;
        }

        private async Task HandleConnectionAsync()
        {
            --ListenerCount;

            try
            {
                var context = await listener.GetContextAsync()
                    .ConfigureAwait(false);
                var response = context.Response;
                var request = context.Request;
                var headers = request.Headers;

                response.SetStatus(HttpStatusCode.Continue);

#if DEBUG
                PrintHeader(context, headers);
#endif

                try
                {
                    await ExecuteRoutesAsync(context)
                        .ConfigureAwait(false); ;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception exp)
                {
                    OnError(exp);
                    response.SetStatus(HttpStatusCode.InternalServerError);
#if DEBUG
                    await response.SendTextAsync(exp.Unroll())
                        .ConfigureAwait(false);
#endif
                }
#pragma warning restore CA1031 // Do not catch general exception types
                finally
                {
                    await CleanupConnectionAsync(response, request, headers)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                ++ListenerCount;
            }
        }

        private void PrintHeader(HttpListenerContext context, NameValueCollection headers)
        {
            OnInfo(NCSALogger.FormatLogMessage(context));
            OnInfo("Headers:");
            foreach (var key in headers.AllKeys)
            {
                var value = headers[key];
                OnInfo($"\t{key} = {value}");
            }
        }

        private async Task ExecuteRoutesAsync(HttpListenerContext context)
        {
            foreach (var route in routes)
            {
                if (route.IsContextMatch(context))
                {
                    await route.ExecuteAsync(context)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task HandleErrorsAsync(HttpListenerResponse response, HttpListenerRequest request)
        {
            if (response.StatusCode >= 400)
            {
                var message = $"{request.RawUrl}: {response.StatusDescription}";
                if (response.StatusCode >= 500)
                {
                    OnError(new HttpListenerException(response.StatusCode, message));
                }
                else
                {
                    OnWarning(message);
                }

#if DEBUG
                await response.SendTextAsync(message)
                    .ConfigureAwait(false);
#endif
            }
        }

        private async Task CleanupConnectionAsync(HttpListenerResponse response, HttpListenerRequest request, NameValueCollection headers)
        {
            response.StatusDescription = HttpStatusDescription.Get(response.StatusCode);

            await HandleErrorsAsync(response, request)
                .ConfigureAwait(false);

            await response
                .OutputStream
                .FlushAsync()
                .ConfigureAwait(true);

            var connection = headers["Connection"];
            var closeConnection = connection?.Equals("Close", StringComparison.InvariantCultureIgnoreCase) != false;
            var status = response.GetStatus();
            if (status >= HttpStatusCode.InternalServerError
                || closeConnection)
            {
                response.Close();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnLog(object sender, StringEventArgs e)
        {
            Log?.Invoke(sender, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnInfo(string message)
        {
            Info?.Invoke(this, new StringEventArgs(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnInfo(object sender, StringEventArgs e)
        {
            Info?.Invoke(sender, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnWarning(string message)
        {
            Warning?.Invoke(this, new StringEventArgs(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnWarning(object sender, StringEventArgs e)
        {
            Warning?.Invoke(sender, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnError(Exception exp)
        {
            Err?.Invoke(this, new ErrorEventArgs(exp));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnError(object sender, ErrorEventArgs e)
        {
            Err?.Invoke(sender, e);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (IsRunning)
                    {
                        Stop();
                    }

                    using (listener) { }

                    foreach (var controller in controllers)
                    {
                        if (controller is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }

                    controllers.Clear();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private void ShowRoutes()
        {
            OnInfo("Found routes:");
            var table = new string[routes.Count + 1, 8];
            table[0, 0] = "";
            table[0, 1] = "Type";
            table[0, 2] = "Method";
            table[0, 3] = "Priority";
            table[0, 4] = "Protocol";
            table[0, 5] = "Method";
            table[0, 6] = "Status";
            table[0, 7] = "Authentication";

            for (var i = 0; i < routes.Count; i++)
            {
                var route = routes[i];

                table[i + 1, 0] = route.Enabled ? "Y" : "N";

                var sepIndex = route.Name.IndexOf("::", StringComparison.Ordinal);
                if (sepIndex < 0)
                {
                    table[i + 1, 1] = route.Name;
                }
                else
                {
                    table[i + 1, 1] = route.Name.Substring(0, sepIndex);
                    table[i + 1, 2] = route.Name.Substring(sepIndex + 2);
                }

                table[i + 1, 3] = route.Priority.ToString(CultureInfo.InvariantCulture);
                table[i + 1, 4] = route.Protocols.ToString();
                table[i + 1, 5] = route.Methods.ToString();
                table[i + 1, 6] = route.StatusCodes.ToString();

                if (route.Authentication == AbstractResponse.AllAuthSchemes)
                {
                    table[i + 1, 7] = "Any";
                }
                else
                {
                    table[i + 1, 7] = route.Authentication.ToString();
                }
            }

            var columnSizes = new int[table.GetLength(1)];
            for (var x = 0; x < table.GetLength(0); ++x)
            {
                for (var y = 0; y < table.GetLength(1); ++y)
                {
                    if (table[x, y] is null)
                    {
                        table[x, y] = string.Empty;
                    }
                    columnSizes[y] = Math.Max(columnSizes[y], table[x, y].Length);
                }
            }

            var totalWidth = columnSizes.Sum() + (columnSizes.Length * 3) + 1;

            var sb = new StringBuilder();
            for (var x = 0; x < table.GetLength(0); ++x)
            {
                _ = sb.Clear();
                for (var y = 0; y < table.GetLength(1); ++y)
                {
                    var formatter = $"| {{0,-{columnSizes[y]}}} ";
                    _ = sb.AppendFormat(CultureInfo.InvariantCulture, formatter, table[x, y]);
                }

                _ = sb.Append("|");
                OnInfo(sb.ToString());

                if (x == 0)
                {
                    _ = sb.Clear();
                    for (var y = 0; y < totalWidth; ++y)
                    {
                        _ = sb.Append("-");
                    }
                    OnInfo(sb.ToString());
                }
            }
        }
    }
}