namespace Newton
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            comboDevices = new ComboBox();
            btnStart = new Button();
            btnStop = new Button();
            panelPlot = new Panel();
            btnOpenA = new Button();
            btnOpenB = new Button();
            lblStatusA = new Label();
            lblStatusB = new Label();
            btnTareA = new Button();
            btnTareB = new Button();
            groupBoxControls = new GroupBox();
            groupBox2 = new GroupBox();
            btnDisconnectAll = new Button();
            txtFactorB = new TextBox();
            label2 = new Label();
            label1 = new Label();
            txtFactorA = new TextBox();
            groupBox1 = new GroupBox();
            chkPhoneMode = new CheckBox();
            comboTimeWindow = new ComboBox();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            clearGraphToolStripMenuItem = new ToolStripMenuItem();
            saveAsCSVToolStripMenuItem = new ToolStripMenuItem();
            refresDevicesToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            groupBoxControls.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // comboDevices
            // 
            comboDevices.Font = new Font("Calibri", 7.8F, FontStyle.Italic, GraphicsUnit.Point, 238);
            comboDevices.FormattingEnabled = true;
            comboDevices.Location = new Point(6, 18);
            comboDevices.Name = "comboDevices";
            comboDevices.Size = new Size(213, 23);
            comboDevices.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(349, 14);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(48, 28);
            btnStart.TabIndex = 6;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(349, 44);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(48, 28);
            btnStop.TabIndex = 7;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // panelPlot
            // 
            panelPlot.Location = new Point(12, 143);
            panelPlot.Name = "panelPlot";
            panelPlot.Size = new Size(1010, 421);
            panelPlot.TabIndex = 8;
            // 
            // btnOpenA
            // 
            btnOpenA.Location = new Point(478, 14);
            btnOpenA.Name = "btnOpenA";
            btnOpenA.Size = new Size(67, 28);
            btnOpenA.TabIndex = 0;
            btnOpenA.Text = "Open A";
            btnOpenA.UseVisualStyleBackColor = true;
            btnOpenA.Click += btnOpenA_Click;
            // 
            // btnOpenB
            // 
            btnOpenB.Location = new Point(478, 45);
            btnOpenB.Name = "btnOpenB";
            btnOpenB.Size = new Size(67, 28);
            btnOpenB.TabIndex = 9;
            btnOpenB.Text = "Open B";
            btnOpenB.UseVisualStyleBackColor = true;
            btnOpenB.Click += btnOpenB_Click;
            // 
            // lblStatusA
            // 
            lblStatusA.AutoSize = true;
            lblStatusA.Location = new Point(551, 17);
            lblStatusA.Name = "lblStatusA";
            lblStatusA.Size = new Size(15, 20);
            lblStatusA.TabIndex = 10;
            lblStatusA.Text = "-";
            // 
            // lblStatusB
            // 
            lblStatusB.AutoSize = true;
            lblStatusB.Location = new Point(551, 44);
            lblStatusB.Name = "lblStatusB";
            lblStatusB.Size = new Size(15, 20);
            lblStatusB.TabIndex = 11;
            lblStatusB.Text = "-";
            // 
            // btnTareA
            // 
            btnTareA.Location = new Point(402, 14);
            btnTareA.Name = "btnTareA";
            btnTareA.Size = new Size(70, 28);
            btnTareA.TabIndex = 12;
            btnTareA.Text = "Tare A";
            btnTareA.UseVisualStyleBackColor = true;
            btnTareA.Click += btnTareA_Click;
            // 
            // btnTareB
            // 
            btnTareB.Location = new Point(402, 44);
            btnTareB.Name = "btnTareB";
            btnTareB.Size = new Size(70, 28);
            btnTareB.TabIndex = 13;
            btnTareB.Text = "Tare B";
            btnTareB.UseVisualStyleBackColor = true;
            btnTareB.Click += btnTareB_Click;
            // 
            // groupBoxControls
            // 
            groupBoxControls.Controls.Add(groupBox2);
            groupBoxControls.Controls.Add(groupBox1);
            groupBoxControls.Location = new Point(12, 31);
            groupBoxControls.Name = "groupBoxControls";
            groupBoxControls.Size = new Size(1016, 115);
            groupBoxControls.TabIndex = 14;
            groupBoxControls.TabStop = false;
            groupBoxControls.Text = "-";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(btnDisconnectAll);
            groupBox2.Controls.Add(txtFactorB);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(txtFactorA);
            groupBox2.Controls.Add(btnTareB);
            groupBox2.Controls.Add(btnTareA);
            groupBox2.Controls.Add(lblStatusB);
            groupBox2.Controls.Add(btnOpenA);
            groupBox2.Controls.Add(btnStop);
            groupBox2.Controls.Add(comboDevices);
            groupBox2.Controls.Add(btnStart);
            groupBox2.Controls.Add(btnOpenB);
            groupBox2.Controls.Add(lblStatusA);
            groupBox2.Location = new Point(177, 14);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(799, 78);
            groupBox2.TabIndex = 16;
            groupBox2.TabStop = false;
            groupBox2.Text = "Sensors";
            // 
            // btnDisconnectAll
            // 
            btnDisconnectAll.Location = new Point(5, 44);
            btnDisconnectAll.Name = "btnDisconnectAll";
            btnDisconnectAll.Size = new Size(214, 30);
            btnDisconnectAll.TabIndex = 18;
            btnDisconnectAll.Text = "DisconnectAllDevices";
            btnDisconnectAll.UseVisualStyleBackColor = true;
            btnDisconnectAll.Click += btnDisconnectAll_Click;
            // 
            // txtFactorB
            // 
            txtFactorB.Location = new Point(293, 45);
            txtFactorB.Name = "txtFactorB";
            txtFactorB.Size = new Size(50, 27);
            txtFactorB.TabIndex = 17;
            txtFactorB.Text = "1,00";
            txtFactorB.TextChanged += txtFactorB_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(224, 14);
            label2.Name = "label2";
            label2.Size = new Size(68, 20);
            label2.TabIndex = 16;
            label2.Text = "Faktror A";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(224, 52);
            label1.Name = "label1";
            label1.Size = new Size(67, 20);
            label1.TabIndex = 15;
            label1.Text = "Faktror B";
            // 
            // txtFactorA
            // 
            txtFactorA.Location = new Point(293, 15);
            txtFactorA.Name = "txtFactorA";
            txtFactorA.Size = new Size(50, 27);
            txtFactorA.TabIndex = 14;
            txtFactorA.Text = "1,00";
            txtFactorA.TextChanged += txtFactorA_TextChanged;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(chkPhoneMode);
            groupBox1.Controls.Add(comboTimeWindow);
            groupBox1.Location = new Point(6, 14);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(149, 92);
            groupBox1.TabIndex = 15;
            groupBox1.TabStop = false;
            groupBox1.Text = "Time Base [s]";
            // 
            // chkPhoneMode
            // 
            chkPhoneMode.AutoSize = true;
            chkPhoneMode.Location = new Point(6, 60);
            chkPhoneMode.Name = "chkPhoneMode";
            chkPhoneMode.Size = new Size(78, 24);
            chkPhoneMode.TabIndex = 18;
            chkPhoneMode.Text = "MobilT";
            chkPhoneMode.UseVisualStyleBackColor = true;
            chkPhoneMode.CheckedChanged += chkPhoneMode_CheckedChanged;
            // 
            // comboTimeWindow
            // 
            comboTimeWindow.FormattingEnabled = true;
            comboTimeWindow.Location = new Point(6, 26);
            comboTimeWindow.Name = "comboTimeWindow";
            comboTimeWindow.Size = new Size(105, 28);
            comboTimeWindow.TabIndex = 14;
            comboTimeWindow.SelectedIndexChanged += comboTimeWindow_SelectedIndexChanged;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1058, 28);
            menuStrip1.TabIndex = 15;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { clearGraphToolStripMenuItem, saveAsCSVToolStripMenuItem, refresDevicesToolStripMenuItem, exitToolStripMenuItem, helpToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // clearGraphToolStripMenuItem
            // 
            clearGraphToolStripMenuItem.Name = "clearGraphToolStripMenuItem";
            clearGraphToolStripMenuItem.Size = new Size(224, 26);
            clearGraphToolStripMenuItem.Text = "Clear Graph";
            clearGraphToolStripMenuItem.Click += clearGraphToolStripMenuItem_Click;
            // 
            // saveAsCSVToolStripMenuItem
            // 
            saveAsCSVToolStripMenuItem.Name = "saveAsCSVToolStripMenuItem";
            saveAsCSVToolStripMenuItem.Size = new Size(224, 26);
            saveAsCSVToolStripMenuItem.Text = "Save As CSV";
            saveAsCSVToolStripMenuItem.Click += saveAsCSVToolStripMenuItem_Click;
            // 
            // refresDevicesToolStripMenuItem
            // 
            refresDevicesToolStripMenuItem.Name = "refresDevicesToolStripMenuItem";
            refresDevicesToolStripMenuItem.Size = new Size(224, 26);
            refresDevicesToolStripMenuItem.Text = "Refres Devices";
            refresDevicesToolStripMenuItem.Click += refresDevicesToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(224, 26);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(224, 26);
            helpToolStripMenuItem.Text = "Help";
            helpToolStripMenuItem.Click += helpToolStripMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1058, 634);
            Controls.Add(groupBoxControls);
            Controls.Add(panelPlot);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Text = "Live Force Display";
            Load += MainForm_Load;
            groupBoxControls.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox comboDevices;
        private Button btnStart;
        private Button btnStop;
        private Panel panelPlot;
        private Button btnOpenA;
        private Button btnOpenB;
        private Label lblStatusA;
        private Label lblStatusB;
        private Button btnTareA;
        private Button btnTareB;
        private GroupBox groupBoxControls;
        private ComboBox comboTimeWindow;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem clearGraphToolStripMenuItem;
        private ToolStripMenuItem saveAsCSVToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem refresDevicesToolStripMenuItem;
        private Label label1;
        private TextBox txtFactorA;
        private TextBox txtFactorB;
        private Label label2;
        private ToolStripMenuItem helpToolStripMenuItem;
        private CheckBox chkPhoneMode;
        private Button btnDisconnectAll;
    }
}
