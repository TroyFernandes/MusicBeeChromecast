using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoogleCast;
using GoogleCast.Channels;
using MaterialSkin;
using MaterialSkin.Controls;

namespace MusicBeePlugin
{
    public partial class ChromecastSelction : MaterialForm
    {

        public IMediaChannel ChromecastMediaChannel { get; set; } = null;
        public Sender ChromecastSender { get; set; } = null;

        private readonly MaterialSkinManager materialSkinManager;

        public ChromecastSelction()
        {
            InitializeComponent();

            materialSkinManager = MaterialSkinManager.Instance;

            materialSkinManager.EnforceBackcolorOnAllComponents = true;

            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Indigo500, Primary.Indigo700, Primary.Indigo100, Accent.Pink200, TextShade.WHITE);


        }

        private async void ChromecastSelection_Load(object sender, EventArgs e)
        {
            IEnumerable<IReceiver> receiver = await new DeviceLocator().FindReceiversAsync();

            foreach (var x in receiver)
            {
                MaterialButton b = new MaterialButton
                {
                    BackColor = Color.Transparent,
                    ForeColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Text = x.FriendlyName,
                    DrawShadows = false,
                    AutoSize = false,
                    Width = 200,

                };

                //b.Click += new EventHandler(this.MyButtonHandler);
                //b.Click += (sender2, e2) => MyButtonHandler(sender, e, receiver);

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
                if (x.FriendlyName == (sender as MaterialButton).Text)
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


        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void ChromecastSelection_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.FormClosing -= ChromecastSelection_FormClosing;
            this.Close();
        }
    }
}
