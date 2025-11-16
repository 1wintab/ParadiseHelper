namespace UI.Main_UI
{
    partial class NavmeshDataForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NavmeshDataForm));
            exit_pictureBox = new System.Windows.Forms.PictureBox();
            pnl_Navmesh = new System.Windows.Forms.Panel();
            pb_Navmesh = new System.Windows.Forms.PictureBox();
            label_Navmesh = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)exit_pictureBox).BeginInit();
            pnl_Navmesh.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pb_Navmesh).BeginInit();
            SuspendLayout();
            // 
            // exit_pictureBox
            // 
            exit_pictureBox.BackColor = System.Drawing.Color.FromArgb(227, 184, 56);
            exit_pictureBox.Image = (System.Drawing.Image)resources.GetObject("exit_pictureBox.Image");
            exit_pictureBox.Location = new System.Drawing.Point(694, 8);
            exit_pictureBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            exit_pictureBox.Name = "exit_pictureBox";
            exit_pictureBox.Size = new System.Drawing.Size(47, 43);
            exit_pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            exit_pictureBox.TabIndex = 16;
            exit_pictureBox.TabStop = false;
            exit_pictureBox.Click += exit_pictureBox_Click;
            // 
            // pnl_Navmesh
            // 
            pnl_Navmesh.BackColor = System.Drawing.Color.FromArgb(254, 253, 252);
            pnl_Navmesh.Controls.Add(pb_Navmesh);
            pnl_Navmesh.Controls.Add(label_Navmesh);
            pnl_Navmesh.Location = new System.Drawing.Point(239, 20);
            pnl_Navmesh.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pnl_Navmesh.Name = "pnl_Navmesh";
            pnl_Navmesh.Size = new System.Drawing.Size(277, 54);
            pnl_Navmesh.TabIndex = 20;
            // 
            // pb_Navmesh
            // 
            pb_Navmesh.BackColor = System.Drawing.Color.Transparent;
            pb_Navmesh.Image = (System.Drawing.Image)resources.GetObject("pb_Navmesh.Image");
            pb_Navmesh.Location = new System.Drawing.Point(12, 6);
            pb_Navmesh.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pb_Navmesh.Name = "pb_Navmesh";
            pb_Navmesh.Size = new System.Drawing.Size(45, 40);
            pb_Navmesh.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pb_Navmesh.TabIndex = 3;
            pb_Navmesh.TabStop = false;
            // 
            // label_Navmesh
            // 
            label_Navmesh.AutoSize = true;
            label_Navmesh.BackColor = System.Drawing.Color.Transparent;
            label_Navmesh.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            label_Navmesh.Font = new System.Drawing.Font("VAG World", 20F);
            label_Navmesh.Location = new System.Drawing.Point(59, 6);
            label_Navmesh.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label_Navmesh.Name = "label_Navmesh";
            label_Navmesh.Size = new System.Drawing.Size(195, 44);
            label_Navmesh.TabIndex = 2;
            label_Navmesh.Text = "Navmesh data";
            // 
            // NavmeshDataForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackgroundImage = (System.Drawing.Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            ClientSize = new System.Drawing.Size(748, 430);
            Controls.Add(pnl_Navmesh);
            Controls.Add(exit_pictureBox);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Name = "NavmeshDataForm";
            ((System.ComponentModel.ISupportInitialize)exit_pictureBox).EndInit();
            pnl_Navmesh.ResumeLayout(false);
            pnl_Navmesh.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pb_Navmesh).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PictureBox exit_pictureBox;
        private System.Windows.Forms.Panel pnl_Navmesh;
        private System.Windows.Forms.PictureBox pb_Navmesh;
        private System.Windows.Forms.Label label_Navmesh;
    }
}