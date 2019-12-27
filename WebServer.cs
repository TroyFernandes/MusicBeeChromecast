using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Owin;


namespace MusicBeePlugin
{
    public class WebServer : IDisposable
    {
        private IDisposable webServer;
        bool disposed;

        private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);

        //public int? PORT { get; set; } = null;
        public const int PORT = 23614;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    webServer.Dispose();
                }
            }

            //dispose unmanaged resources
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public WebServer(string @directory)
        {

            var url = "http://*:" + PORT;
            var root = directory;
            var fileSystem = new PhysicalFileSystem(root);
            var options = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = fileSystem
            };
            options.StaticFileOptions.ContentTypeProvider = new CustomContentTypeProvider();

            webServer = WebApp.Start(url, builder => builder.UseFileServer(options));

            Debug.WriteLine("Listening at " + url);

        }

        public class CustomContentTypeProvider : FileExtensionContentTypeProvider
        {
            public CustomContentTypeProvider()
            {
                Mappings.Add(".flac", "audio/flac");
            }
        }


        //public void GetAvailablePort()
        //{
        //    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        //    {
        //        socket.Bind(DefaultLoopbackEndpoint);
        //        PORT = ((IPEndPoint)socket.LocalEndPoint).Port;
        //    }
        //}

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }




    }
}
