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

        public WebServer(int? port_number, string @imageDirectory = null)
        {

            try
            {
                MEDIA_PORT = port_number ?? throw new Exception("Port Number Undefined");

                var mediaURL = "http://*:" + MEDIA_PORT;
                string path = @System.IO.Path.GetTempPath() + @"\\MusicBeeChromecast";
                System.IO.Directory.CreateDirectory(path);
                var mediaFileSystem = new PhysicalFileSystem(path);
                
                var mediaServerOptions = new FileServerOptions
                {
                    EnableDirectoryBrowsing = true,
                    FileSystem = mediaFileSystem
                };
                mediaServerOptions.StaticFileOptions.ContentTypeProvider = new CustomContentTypeProvider();

                MediaWebServer = WebApp.Start(mediaURL, builder => builder.UseFileServer(mediaServerOptions));
                //Debug.WriteLine(mediaURL);
                
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
                //The webserver can understand to treat the .tmp image files music bee produces as image files,
                //however the chromecast needs a proper image format sent to it. 
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
