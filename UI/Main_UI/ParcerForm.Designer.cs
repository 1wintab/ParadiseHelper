namespace UI.Main_UI
{
    partial class ParcerForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ParcerForm));
            richTextBox1 = new System.Windows.Forms.RichTextBox();
            Create_Button = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            panel2 = new System.Windows.Forms.Panel();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            SelectMaFile_Button = new System.Windows.Forms.Button();
            panel1 = new System.Windows.Forms.Panel();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new System.Drawing.Point(40, 159);
            richTextBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new System.Drawing.Size(756, 110);
            richTextBox1.TabIndex = 17;
            richTextBox1.Text = "";
            // 
            // Create_Button
            // 
            Create_Button.BackColor = System.Drawing.Color.YellowGreen;
            Create_Button.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            Create_Button.Font = new System.Drawing.Font("VAG World", 20F);
            Create_Button.Location = new System.Drawing.Point(699, 348);
            Create_Button.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Create_Button.Name = "Create_Button";
            Create_Button.Size = new System.Drawing.Size(128, 52);
            Create_Button.TabIndex = 16;
            Create_Button.Text = "Create";
            Create_Button.UseVisualStyleBackColor = false;
            Create_Button.Click += Create_Button_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = System.Drawing.Color.Transparent;
            label2.Font = new System.Drawing.Font("VAG World", 20F);
            label2.Location = new System.Drawing.Point(-3, 1);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(556, 44);
            label2.TabIndex = 14;
            label2.Text = "1. Select maFiles which you want to process:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = System.Drawing.Color.Transparent;
            label1.Font = new System.Drawing.Font("VAG World", 24F);
            label1.Location = new System.Drawing.Point(66, 7);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(215, 51);
            label1.TabIndex = 0;
            label1.Text = "Parser MaFile";
            // 
            // panel2
            // 
            panel2.BackColor = System.Drawing.Color.YellowGreen;
            panel2.Controls.Add(label1);
            panel2.Controls.Add(pictureBox1);
            panel2.Location = new System.Drawing.Point(40, 22);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(303, 62);
            panel2.TabIndex = 18;
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = System.Drawing.Color.YellowGreen;
            pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new System.Drawing.Point(9, 8);
            pictureBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(52, 46);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 10;
            pictureBox1.TabStop = false;
            // 
            // SelectMaFile_Button
            // 
            SelectMaFile_Button.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            SelectMaFile_Button.Location = new System.Drawing.Point(740, 102);
            SelectMaFile_Button.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            SelectMaFile_Button.Name = "SelectMaFile_Button";
            SelectMaFile_Button.Size = new System.Drawing.Size(56, 44);
            SelectMaFile_Button.TabIndex = 15;
            SelectMaFile_Button.Text = "...";
            SelectMaFile_Button.UseVisualStyleBackColor = true;
            SelectMaFile_Button.Click += SelectMaFile_Button_Click;
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.Color.YellowGreen;
            panel1.Controls.Add(label2);
            panel1.Location = new System.Drawing.Point(42, 104);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(556, 44);
            panel1.TabIndex = 19;
            // 
            // ParcerForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackgroundImage = (System.Drawing.Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            ClientSize = new System.Drawing.Size(840, 412);
            Controls.Add(panel1);
            Controls.Add(richTextBox1);
            Controls.Add(Create_Button);
            Controls.Add(panel2);
            Controls.Add(SelectMaFile_Button);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimizeBox = false;
            Name = "ParcerForm";
            Opacity = 1D;
            Text = "ParcerMaFiles";
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button Create_Button;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button SelectMaFile_Button;
        private System.Windows.Forms.Panel panel1;
    }
}