using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Host.HttpListener;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Owin;


namespace MusicBeePlugin
{
    public class WebServer : IDisposable
    {
        public IDisposable MediaWebServer { get; set; } = null;
        bool disposed;

        private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);

        public int? MEDIA_PORT { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (MediaWebServer != null)
                    {
                        MediaWebServer.Dispose();
                    }
                    Debug.WriteLine("closing webserver");
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

        public WebServer(string @musicDirectory, int? port_number, string @imageDirectory = null)
        {

            try
            {
                MEDIA_PORT = port_number ?? throw new Exception("Port Number Undefined");

                var mediaURL = "http://*:" + MEDIA_PORT;
                var mediaRoot = musicDirectory;
                var mediaFileSystem = new PhysicalFileSystem(mediaRoot);
                var mediaServerOptions = new FileServerOptions
                {
                    EnableDirectoryBrowsing = true,
                    FileSystem = mediaFileSystem
                };
                mediaServerOptions.StaticFileOptions.ContentTypeProvider = new CustomContentTypeProvider();

                MediaWebServer = WebApp.Start(mediaURL, builder => builder.UseFileServer(mediaServerOptions));

                Debug.WriteLine("Listening at " + mediaURL);

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);

                //Change this after
                throw new Exception("Webserver exception");
            }

        }


        public class CustomContentTypeProvider : FileExtensionContentTypeProvider
        {
            public CustomContentTypeProvider()
            {
                Mappings.Add(".flac", "audio/flac");
                Mappings.Add(".tmp", "image/png");
            }
        }


        public void Stop()
        {
            if (MediaWebServer != null)
            {
                Dispose();
            }
            else
            {
                throw new NullReferenceException("Webserver is null");
            }
        }


    }
}
