using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace MusicBeePlugin
{
    public partial class Settings : Form
    {
        private static string SettingsPath { get; set; }

        public Settings(string @path)
        {
            SettingsPath = path;
            InitializeComponent();
            ReadSettings();

        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }



        public void CreateSettings()
        {
            using (var writer = new XmlTextWriter(SettingsPath + @"\MB_Chromecast_Settings.xml", Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                //Write the root element
                writer.WriteStartElement("settings");

                //Write sub-elements
                writer.WriteElementString("server_port", ((int)serverPortSelect.Value).ToString());

                // end the root element
                writer.WriteEndElement();
            }

        }

        private void ReadSettings()
        {
            if (File.Exists(SettingsPath + @"\MB_Chromecast_Settings.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(SettingsPath + @"\MB_Chromecast_Settings.xml");
                var temp = doc.GetElementsByTagName("server_port")[0].InnerText;
                if (!string.IsNullOrEmpty(temp))
                {
                    serverPortSelect.Value = Convert.ToDecimal(temp);
                }
            }
        }

        private void closeText_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveText_Click(object sender, EventArgs e)
        {
            CreateSettings();
            this.Close();
        }


    }

}
