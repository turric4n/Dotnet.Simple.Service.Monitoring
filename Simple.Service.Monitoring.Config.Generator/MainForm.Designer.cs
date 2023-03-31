namespace Simple.Service.Monitoring.Config.Generator
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.lostButton1 = new ReaLTaiizor.Controls.LostButton();
            this.lostButton2 = new ReaLTaiizor.Controls.LostButton();
            this.lostButton3 = new ReaLTaiizor.Controls.LostButton();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.BackColor = System.Drawing.Color.Gray;
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile});
            this.mainMenu.Location = new System.Drawing.Point(2, 36);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(706, 24);
            this.mainMenu.TabIndex = 1;
            this.mainMenu.Text = "menuStrip1";
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new System.Drawing.Size(37, 20);
            this.menuFile.Text = "File";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.CheckOnClick = true;
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem1.Text = "Open Config File";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // listView1
            // 
            this.listView1.BackColor = System.Drawing.SystemColors.GrayText;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader4,
            this.columnHeader2,
            this.columnHeader3});
            this.listView1.Dock = System.Windows.Forms.DockStyle.Top;
            this.listView1.Location = new System.Drawing.Point(2, 60);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(706, 377);
            this.listView1.TabIndex = 2;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
            this.listView1.Click += new System.EventHandler(this.listView1_Click);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Health Check Name";
            this.columnHeader1.Width = 200;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Host or Something";
            this.columnHeader4.Width = 200;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Type";
            this.columnHeader2.Width = 200;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Enabled";
            this.columnHeader3.Width = 100;
            // 
            // lostButton1
            // 
            this.lostButton1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostButton1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lostButton1.ForeColor = System.Drawing.Color.White;
            this.lostButton1.HoverColor = System.Drawing.Color.DodgerBlue;
            this.lostButton1.Image = null;
            this.lostButton1.Location = new System.Drawing.Point(2, 466);
            this.lostButton1.Name = "lostButton1";
            this.lostButton1.Size = new System.Drawing.Size(110, 40);
            this.lostButton1.TabIndex = 3;
            this.lostButton1.Text = "Add HealthCheck";
            this.lostButton1.Click += new System.EventHandler(this.lostButton1_Click);
            // 
            // lostButton2
            // 
            this.lostButton2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostButton2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lostButton2.ForeColor = System.Drawing.Color.White;
            this.lostButton2.HoverColor = System.Drawing.Color.DodgerBlue;
            this.lostButton2.Image = null;
            this.lostButton2.Location = new System.Drawing.Point(121, 465);
            this.lostButton2.Name = "lostButton2";
            this.lostButton2.Size = new System.Drawing.Size(126, 41);
            this.lostButton2.TabIndex = 4;
            this.lostButton2.Text = "Remove HealthCheck";
            this.lostButton2.Click += new System.EventHandler(this.lostButton2_Click);
            // 
            // lostButton3
            // 
            this.lostButton3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lostButton3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lostButton3.ForeColor = System.Drawing.Color.White;
            this.lostButton3.HoverColor = System.Drawing.Color.DodgerBlue;
            this.lostButton3.Image = null;
            this.lostButton3.Location = new System.Drawing.Point(595, 466);
            this.lostButton3.Name = "lostButton3";
            this.lostButton3.Size = new System.Drawing.Size(110, 40);
            this.lostButton3.TabIndex = 5;
            this.lostButton3.Text = "Alert Providers";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.ClientSize = new System.Drawing.Size(710, 556);
            this.Controls.Add(this.lostButton3);
            this.Controls.Add(this.lostButton2);
            this.Controls.Add(this.lostButton1);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.mainMenu);
            this.MainMenuStrip = this.mainMenu;
            this.Name = "MainForm";
            this.Text = "Config Editor";
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private MenuStrip mainMenu;
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem toolStripMenuItem1;
        private ListView listView1;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ReaLTaiizor.Controls.LostButton lostButton1;
        private ReaLTaiizor.Controls.LostButton lostButton2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
        private ReaLTaiizor.Controls.LostButton lostButton3;
        private ToolStripMenuItem exitToolStripMenuItem;
    }
}