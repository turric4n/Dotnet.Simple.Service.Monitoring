namespace Simple.Service.Monitoring.Config.Generator
{
    partial class HealthCheckForm
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
            this.lostButton1 = new ReaLTaiizor.Controls.LostButton();
            this.lostLabel1 = new ReaLTaiizor.Controls.LostLabel();
            this.lostLabel2 = new ReaLTaiizor.Controls.LostLabel();
            this.lostLabel3 = new ReaLTaiizor.Controls.LostLabel();
            this.metroComboBox1 = new ReaLTaiizor.Controls.MetroComboBox();
            this.textBoxEdit1 = new ReaLTaiizor.Controls.TextBoxEdit();
            this.lostLabel4 = new ReaLTaiizor.Controls.LostLabel();
            this.lostLabel5 = new ReaLTaiizor.Controls.LostLabel();
            this.SuspendLayout();
            // 
            // lostButton1
            // 
            this.lostButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lostButton1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostButton1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lostButton1.ForeColor = System.Drawing.Color.White;
            this.lostButton1.HoverColor = System.Drawing.Color.DodgerBlue;
            this.lostButton1.Image = null;
            this.lostButton1.Location = new System.Drawing.Point(27, 405);
            this.lostButton1.Name = "lostButton1";
            this.lostButton1.Size = new System.Drawing.Size(120, 33);
            this.lostButton1.TabIndex = 0;
            this.lostButton1.Text = "Save";
            this.lostButton1.Click += new System.EventHandler(this.lostButton1_Click);
            // 
            // lostLabel1
            // 
            this.lostLabel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostLabel1.ForeColor = System.Drawing.Color.White;
            this.lostLabel1.Location = new System.Drawing.Point(28, 65);
            this.lostLabel1.Name = "lostLabel1";
            this.lostLabel1.Size = new System.Drawing.Size(461, 23);
            this.lostLabel1.TabIndex = 1;
            this.lostLabel1.Text = "Name :";
            // 
            // lostLabel2
            // 
            this.lostLabel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostLabel2.ForeColor = System.Drawing.Color.White;
            this.lostLabel2.Location = new System.Drawing.Point(28, 112);
            this.lostLabel2.Name = "lostLabel2";
            this.lostLabel2.Size = new System.Drawing.Size(75, 24);
            this.lostLabel2.TabIndex = 2;
            this.lostLabel2.Text = "Type :";
            // 
            // lostLabel3
            // 
            this.lostLabel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostLabel3.ForeColor = System.Drawing.Color.White;
            this.lostLabel3.Location = new System.Drawing.Point(28, 169);
            this.lostLabel3.Name = "lostLabel3";
            this.lostLabel3.Size = new System.Drawing.Size(140, 24);
            this.lostLabel3.TabIndex = 3;
            this.lostLabel3.Text = "EndpointOrHost :";
            // 
            // metroComboBox1
            // 
            this.metroComboBox1.AllowDrop = true;
            this.metroComboBox1.ArrowColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.metroComboBox1.BackColor = System.Drawing.Color.Transparent;
            this.metroComboBox1.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(238)))));
            this.metroComboBox1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.metroComboBox1.CausesValidation = false;
            this.metroComboBox1.DisabledBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.metroComboBox1.DisabledBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(155)))), ((int)(((byte)(155)))));
            this.metroComboBox1.DisabledForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(136)))), ((int)(((byte)(136)))), ((int)(((byte)(136)))));
            this.metroComboBox1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.metroComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.metroComboBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.metroComboBox1.FormattingEnabled = true;
            this.metroComboBox1.IsDerivedStyle = true;
            this.metroComboBox1.ItemHeight = 20;
            this.metroComboBox1.Items.AddRange(new object[] {
            "Http Endpoint",
            "RMQ Server",
            "SQLServer",
            "Hangfire",
            ""});
            this.metroComboBox1.Location = new System.Drawing.Point(98, 110);
            this.metroComboBox1.Name = "metroComboBox1";
            this.metroComboBox1.SelectedItemBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(65)))), ((int)(((byte)(177)))), ((int)(((byte)(225)))));
            this.metroComboBox1.SelectedItemForeColor = System.Drawing.Color.White;
            this.metroComboBox1.Size = new System.Drawing.Size(213, 26);
            this.metroComboBox1.Style = ReaLTaiizor.Enum.Metro.Style.Light;
            this.metroComboBox1.StyleManager = null;
            this.metroComboBox1.TabIndex = 4;
            this.metroComboBox1.ThemeAuthor = "Taiizor";
            this.metroComboBox1.ThemeName = "MetroLight";
            // 
            // textBoxEdit1
            // 
            this.textBoxEdit1.BackColor = System.Drawing.Color.Transparent;
            this.textBoxEdit1.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.textBoxEdit1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(183)))), ((int)(((byte)(191)))));
            this.textBoxEdit1.Image = null;
            this.textBoxEdit1.Location = new System.Drawing.Point(173, 152);
            this.textBoxEdit1.MaxLength = 32767;
            this.textBoxEdit1.Multiline = false;
            this.textBoxEdit1.Name = "textBoxEdit1";
            this.textBoxEdit1.ReadOnly = false;
            this.textBoxEdit1.Size = new System.Drawing.Size(316, 41);
            this.textBoxEdit1.TabIndex = 5;
            this.textBoxEdit1.TextAlignment = System.Windows.Forms.HorizontalAlignment.Left;
            this.textBoxEdit1.UseSystemPasswordChar = false;
            // 
            // lostLabel4
            // 
            this.lostLabel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostLabel4.ForeColor = System.Drawing.Color.White;
            this.lostLabel4.Location = new System.Drawing.Point(28, 281);
            this.lostLabel4.Name = "lostLabel4";
            this.lostLabel4.Size = new System.Drawing.Size(461, 23);
            this.lostLabel4.TabIndex = 6;
            this.lostLabel4.Text = "TrasportMethods :";
            // 
            // lostLabel5
            // 
            this.lostLabel5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostLabel5.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lostLabel5.ForeColor = System.Drawing.Color.White;
            this.lostLabel5.Location = new System.Drawing.Point(28, 224);
            this.lostLabel5.Name = "lostLabel5";
            this.lostLabel5.Size = new System.Drawing.Size(461, 23);
            this.lostLabel5.TabIndex = 7;
            this.lostLabel5.Text = "Alert Behaviour";
            // 
            // HealthCheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(528, 456);
            this.Controls.Add(this.lostLabel5);
            this.Controls.Add(this.lostLabel4);
            this.Controls.Add(this.textBoxEdit1);
            this.Controls.Add(this.metroComboBox1);
            this.Controls.Add(this.lostLabel3);
            this.Controls.Add(this.lostLabel2);
            this.Controls.Add(this.lostLabel1);
            this.Controls.Add(this.lostButton1);
            this.Name = "HealthCheckForm";
            this.Text = "HealthCheckForm";
            this.ResumeLayout(false);

        }

        #endregion

        private ReaLTaiizor.Controls.LostButton lostButton1;
        private ReaLTaiizor.Controls.LostLabel lostLabel1;
        private ReaLTaiizor.Controls.LostLabel lostLabel2;
        private ReaLTaiizor.Controls.LostLabel lostLabel3;
        private ReaLTaiizor.Controls.MetroComboBox metroComboBox1;
        private ReaLTaiizor.Controls.TextBoxEdit textBoxEdit1;
        private ReaLTaiizor.Controls.LostLabel lostLabel4;
        private ReaLTaiizor.Controls.LostLabel lostLabel5;
    }
}