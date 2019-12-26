using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    class ChromecastPanel : Form
    {
        public ChromecastPanel()
        {

            System.Windows.Forms.Control chromecastPanel = new System.Windows.Forms.Control();

            //Previous button
            chromecastPanel.Controls.Add(new PictureBox
            {
                Location = new Point(2, 0),
                SizeMode = PictureBoxSizeMode.StretchImage,
                ClientSize = new Size(25, 25),
                Image = Properties.Resources.back

            });

            //Play button
            chromecastPanel.Controls.Add(new PictureBox
            {
                Location = new Point(29, 0),
                SizeMode = PictureBoxSizeMode.StretchImage,
                ClientSize = new Size(25, 25),
                Image = Properties.Resources.play

            });

            //Next song button
            chromecastPanel.Controls.Add(new PictureBox
            {
                Location = new Point(59, 0),
                SizeMode = PictureBoxSizeMode.StretchImage,
                ClientSize = new Size(25, 25),
                Image = Properties.Resources.forward

            });

            //Chromecast icon
            chromecastPanel.Controls.Add(new PictureBox
            {
                Location = new Point(200, 0),
                SizeMode = PictureBoxSizeMode.StretchImage,
                ClientSize = new Size(25, 21),
                Image = Properties.Resources.chromecast_icon_connect

            });

            TrackbarEx trackbar = new TrackbarEx
            {
                AutoSize = false,
                Width = 100,
                Minimum = 0,
                Maximum = 100,
                TickStyle = TickStyle.None,
                Location = new Point(86, 0)
            };

            chromecastPanel.Controls.Add(trackbar);

            this.Show();
        }
    }
}
