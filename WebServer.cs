using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Owin;


namespace MusicBeePlugin
{
    public class WebServer : IDisposable
    {
        public IDisposable MediaWebServer { get; set; } = null;
        public IDisposable ImageWebServer { get; set; } = null;
        bool disposed;

        private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);

        public const int MEDIA_PORT = 23614;
        public const int IMAGE_PORT = MEDIA_PORT + 1;

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
                    if (ImageWebServer != null)
                    {
                        ImageWebServer.Dispose();
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

        public WebServer(string @musicDirectory, string @imageDirectory = null)
        {
            try
            {
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

                if (imageDirectory != null)
                {
                    var imageURL = "http://*:" + IMAGE_PORT;
                    var imageRoot = imageDirectory;
                    var imageFileSystem = new PhysicalFileSystem(imageRoot);
                    var imageServerOptions = new FileServerOptions
                    {
                        EnableDirectoryBrowsing = true,
                        FileSystem = imageFileSystem
                    };

                    imageServerOptions.StaticFileOptions.ContentTypeProvider = new CustomContentTypeProvider();
                    ImageWebServer = WebApp.Start(imageURL, builder =>
                    {
                        builder.UseFileServer(imageServerOptions);
                    });

                }


                Debug.WriteLine("Listening at " + mediaURL);


            }
            catch (Exception e)
            {
                //Change this after
                throw new Exception("Webserver exception");
            }

        }


        public void Example()
        {

        }

        public class CustomContentTypeProvider : FileExtensionContentTypeProvider
        {
            public CustomContentTypeProvider()
            {
                Mappings.Add(".flac", "audio/flac");
                Mappings.Add(".tmp", "image/png");
            }
        }


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
