namespace MusicBeePlugin
{
    partial class ChromecastPanel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.closeText = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.devicesText = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // closeText
            // 
            this.closeText.AutoSize = true;
            this.closeText.Location = new System.Drawing.Point(159, 279);
            this.closeText.Name = "closeText";
            this.closeText.Size = new System.Drawing.Size(33, 13);
            this.closeText.TabIndex = 0;
            this.closeText.Text = "Close";
            this.closeText.Click += new System.EventHandler(this.label1_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Location = new System.Drawing.Point(1, 36);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(204, 240);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // devicesText
            // 
            this.devicesText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.devicesText.Location = new System.Drawing.Point(12, 9);
            this.devicesText.Name = "devicesText";
            this.devicesText.Size = new System.Drawing.Size(77, 24);
            this.devicesText.TabIndex = 3;
            this.devicesText.Text = "Devices";
            this.devicesText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ChromecastPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(206, 301);
            this.Controls.Add(this.devicesText);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.closeText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ChromecastPanel";
            this.Text = "ChromecastPanel";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ChromecastSelection_FormClosing);
            this.Load += new System.EventHandler(this.ChromecastPanel_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label closeText;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label devicesText;
    }
}