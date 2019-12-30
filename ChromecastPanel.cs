using GoogleCast;
using GoogleCast.Channels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class ChromecastPanel : Form
    {
        private Color backgroundColor { get; set; }
        public IMediaChannel ChromecastMediaChannel { get; set; } = null;
        public Sender ChromecastSender { get; set; } = null;

        public ChromecastPanel(Color color)
        {
            backgroundColor = color;
            InitializeComponent();
        }

        private async void ChromecastPanel_Load(object sender, EventArgs e)
        {


            var contrastColor = ContrastColor(backgroundColor);
            this.BackColor = backgroundColor;
            this.closeText.ForeColor = contrastColor;
            this.devicesText.ForeColor = contrastColor;

            IEnumerable<IReceiver> receiver = await new DeviceLocator().FindReceiversAsync();

            foreach (var x in receiver)
            {
                Button b = new Button
                {
                    BackColor = Color.Transparent,
                    ForeColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Text = x.FriendlyName,
                    AutoSize = false,
                    Width = 200,

                };

                b.Click +=
                    new EventHandler((s, e2) => MyButtonHandler(s, e, receiver));

                flowLayoutPanel1.Controls.Add(b);
            }


        }

        async void MyButtonHandler(object sender, EventArgs e, IEnumerable<IReceiver> devices)
        {

            var sender2 = new Sender();

            IReceiver device = null;
            foreach (var x in devices)
            {
                if (x.FriendlyName == (sender as Button).Text)
                {
                    device = x;
                }
            }

            if (device != null)
            {
                //Connect to the device
                await sender2.ConnectAsync(device);
                //Launch the media reciever app
                var mediaChannel = sender2.GetChannel<IMediaChannel>();
                await sender2.LaunchAsync(mediaChannel);

                ChromecastSender = sender2;
                ChromecastMediaChannel = mediaChannel;

                this.FormClosing -= ChromecastSelection_FormClosing;
                this.Close();
            }

        }


        private void label1_Click(object sender, EventArgs e)
        {
            this.FormClosing -= ChromecastSelection_FormClosing;
            this.Close();
        }

        Color ContrastColor(Color color)
        {
            int d = 0;

            // Counting the perceptive luminance - human eye favors green color... 
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

            if (luminance > 0.5)
                d = 0; // bright colors - black font
            else
                d = 255; // dark colors - white font

            return Color.FromArgb(d, d, d);
        }


        private void ChromecastSelection_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.FormClosing -= ChromecastSelection_FormClosing;
            this.Close();
        }
    }
}
