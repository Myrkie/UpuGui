using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using UpuCore;

namespace UpuGui
{
    public class UpuGui : Form
    {
        private Button btnDeselectAll;
        private Button btnExit;
        private Button btnRegisterUnregister;
        private Button btnSelectAll;
        private Button btnSelectInputFile;
        private Button btnUnpack;
        private IContainer components;
        private GroupBox groupBox;
        private readonly KISSUnpacker m_ku;
        private Dictionary<string, string> m_remapInfo;
        private readonly Timer m_shellHandlerCheckTimer;
        private string m_tmpUnpackedOutputPathForUi;
        private readonly UpuConsole.UpuConsole m_upu;
        private OpenFileDialog openFileDialog;
        private ProgressBar progressBar;
        private FolderBrowserDialog saveToFolderDialog;
        private StatusStrip statusStrip1;
        private TreeView treeViewContents;

        public UpuGui()
        {
            InitializeComponent();
            btnUnpack.Enabled = false;
            btnSelectAll.Enabled = false;
            btnDeselectAll.Enabled = false;
            progressBar.Visible = false;
            treeViewContents.CheckBoxes = true;
            AllowDrop = true;
            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;
            var upuGui = this;
            var str = upuGui.Text + $" {Assembly.GetExecutingAssembly().GetName().Version}";
            upuGui.Text = str;
        }

        public UpuGui(UpuConsole.UpuConsole upu)
            : this()
        {
            m_upu = upu;
            if (upu.IsContextMenuHandlerRegistered())
                btnRegisterUnregister.Text = "Unregister Explorer Context Menu Handler";
            m_ku = new KISSUnpacker();
            m_shellHandlerCheckTimer = new Timer();
            m_shellHandlerCheckTimer.Interval = 5000;
            m_shellHandlerCheckTimer.Tick += ShellHandlerCheckTimer_Tick;
            m_shellHandlerCheckTimer.Enabled = true;
            m_shellHandlerCheckTimer.Start();
            ShellHandlerCheckTimer_Tick(null, EventArgs.Empty);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var strArray = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (strArray.Length <= 0)
                return;
            OpenFile(strArray[0]);
        }

        private void ShellHandlerCheckTimer_Tick(object sender, EventArgs e)
        {
            if (m_upu.IsContextMenuHandlerRegistered())
                btnRegisterUnregister.Text = "Unregister Explorer Context Menu Handler";
            else
                btnRegisterUnregister.Text = "Register Explorer Context Menu Handler";
            btnRegisterUnregister.Enabled = true;
        }

        private void btnSelectInputFile_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Unitypackage Files|*.unitypackage";
            var num = (int) openFileDialog.ShowDialog();
            if (string.IsNullOrEmpty(openFileDialog.FileName))
                return;
            OpenFile(openFileDialog.FileName);
        }

        private void OpenFile(string filePathName)
        {
            groupBox.Text = new FileInfo(filePathName).Name;
            btnExit.Enabled = false;
            btnSelectInputFile.Enabled = false;
            progressBar.Visible = true;
            treeViewContents.Nodes.Clear();
            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += ReadInputFileWorker;
            backgroundWorker.RunWorkerCompleted += ReadInputFileWorkerCompleted;
            backgroundWorker.RunWorkerAsync(filePathName);
        }

        private void ReadInputFileWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is Exception)
            {
                var num =
                    (int)
                    MessageBox.Show("An exception happened: \n" + e.Result, "Ooops...", MessageBoxButtons.OK,
                        MessageBoxIcon.Hand);
            }
            else
            {
                foreach (var node in (List<TreeNode>) e.Result)
                    treeViewContents.Nodes.Add(node);
            }
            progressBar.Visible = false;
            btnSelectInputFile.Enabled = true;
            btnUnpack.Enabled = true;
            btnSelectAll.Enabled = true;
            btnDeselectAll.Enabled = true;
            btnExit.Enabled = true;
        }

        private void ReadInputFileWorker(object sender, DoWorkEventArgs e)
        {
            var list = new List<TreeNode>();
            try
            {
                m_tmpUnpackedOutputPathForUi = m_ku.GetTempPath();
                m_remapInfo = m_ku.Unpack(e.Argument.ToString(), m_tmpUnpackedOutputPathForUi);
                foreach (var keyValuePair in m_remapInfo)
                    if (File.Exists(keyValuePair.Key))
                    {
                        var text = keyValuePair.Value.Replace(m_tmpUnpackedOutputPathForUi, "");
                        if (text.StartsWith(Path.DirectorySeparatorChar.ToString()))
                            text = text.Substring(1);
                        list.Add(new TreeNode(text)
                        {
                            Checked = true,
                            Tag = keyValuePair
                        });
                    }
                list.Sort((t1, t2) => t1.Text.CompareTo(t2.Text));
            }
            catch (Exception ex)
            {
                e.Result = ex;
                return;
            }
            e.Result = list;
        }

       

        private void UnpackInputFileWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnSelectInputFile.Enabled = true;
            btnUnpack.Enabled = true;
            btnExit.Enabled = true;
            progressBar.Visible = false;
        }

        private void UnpackInputFileWorker(object sender, DoWorkEventArgs e)
        {
            if (!Directory.Exists(saveToFolderDialog.SelectedPath))
                Directory.CreateDirectory(saveToFolderDialog.SelectedPath);
            var map = new Dictionary<string, string>();
            var dictionary = new Dictionary<string, string>();
            foreach (TreeNode treeNode in treeViewContents.Nodes)
                if (treeNode.Checked)
                    dictionary.Add(((KeyValuePair<string, string>) treeNode.Tag).Key,
                        ((KeyValuePair<string, string>) treeNode.Tag).Value);
            foreach (var keyValuePair in dictionary)
                map[keyValuePair.Key] = keyValuePair.Value.Replace(m_tmpUnpackedOutputPathForUi,
                    saveToFolderDialog.SelectedPath);
            m_ku.RemapFiles(map);
        }

        private void saveToFolderDialog_HelpRequest(object sender, EventArgs e)
        {
        }

        private void Cleanup()
        {
            if ((m_tmpUnpackedOutputPathForUi == null) || !Directory.Exists(m_tmpUnpackedOutputPathForUi))
                return;
            Directory.Delete(m_tmpUnpackedOutputPathForUi, true);
        }

     

   
     

        private void treeViewContents_AfterSelect(object sender, TreeViewEventArgs e)
        {
        }

        private void UpuGui_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cleanup();
        }

     

     
        private void picDonate_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=SUWNSCDY6SDFN");
        }

        private void treeViewContents_Click(object sender, EventArgs e)
        {
        }

        private void treeViewContents_AfterSelect_1(object sender, TreeViewEventArgs e)
        {
        }

        private void treeViewContents_MouseUp(object sender, MouseEventArgs e)
        {
            var treeViewHitTestInfo = treeViewContents.HitTest(e.Location);
            if ((treeViewHitTestInfo.Node == null) || (treeViewHitTestInfo.Location != TreeViewHitTestLocations.Label))
                return;
            if (treeViewContents.SelectedNode == treeViewHitTestInfo.Node)
                treeViewContents.SelectedNode.Checked = !treeViewContents.SelectedNode.Checked;
            else
                treeViewHitTestInfo.Node.Checked = !treeViewHitTestInfo.Node.Checked;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpuGui));
            this.saveToFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.btnUnpack = new System.Windows.Forms.Button();
            this.btnSelectInputFile = new System.Windows.Forms.Button();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.btnDeselectAll = new System.Windows.Forms.Button();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.treeViewContents = new System.Windows.Forms.TreeView();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.btnExit = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.btnRegisterUnregister = new System.Windows.Forms.Button();
            this.groupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnUnpack
            // 
            this.btnUnpack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUnpack.Image = ((System.Drawing.Image)(resources.GetObject("btnUnpack.Image")));
            this.btnUnpack.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnUnpack.Location = new System.Drawing.Point(170, 429);
            this.btnUnpack.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnUnpack.Name = "btnUnpack";
            this.btnUnpack.Size = new System.Drawing.Size(149, 38);
            this.btnUnpack.TabIndex = 3;
            this.btnUnpack.Text = "Unpack now";
            this.btnUnpack.UseVisualStyleBackColor = true;
            this.btnUnpack.Click += new System.EventHandler(this.btnUnpack_Click_1);
            // 
            // btnSelectInputFile
            // 
            this.btnSelectInputFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSelectInputFile.Image = ((System.Drawing.Image)(resources.GetObject("btnSelectInputFile.Image")));
            this.btnSelectInputFile.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSelectInputFile.Location = new System.Drawing.Point(14, 429);
            this.btnSelectInputFile.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSelectInputFile.Name = "btnSelectInputFile";
            this.btnSelectInputFile.Size = new System.Drawing.Size(149, 38);
            this.btnSelectInputFile.TabIndex = 6;
            this.btnSelectInputFile.Text = "Select Input File";
            this.btnSelectInputFile.UseVisualStyleBackColor = true;
            this.btnSelectInputFile.Click += new System.EventHandler(this.btnSelectInputFile_Click_1);
            // 
            // groupBox
            // 
            this.groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox.Controls.Add(this.btnDeselectAll);
            this.groupBox.Controls.Add(this.btnSelectAll);
            this.groupBox.Controls.Add(this.treeViewContents);
            this.groupBox.Location = new System.Drawing.Point(14, 14);
            this.groupBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox.Name = "groupBox";
            this.groupBox.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox.Size = new System.Drawing.Size(638, 375);
            this.groupBox.TabIndex = 7;
            this.groupBox.TabStop = false;
            this.groupBox.Text = "Unitypackage File";
            // 
            // btnDeselectAll
            // 
            this.btnDeselectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDeselectAll.Image = ((System.Drawing.Image)(resources.GetObject("btnDeselectAll.Image")));
            this.btnDeselectAll.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDeselectAll.Location = new System.Drawing.Point(561, 341);
            this.btnDeselectAll.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnDeselectAll.Name = "btnDeselectAll";
            this.btnDeselectAll.Size = new System.Drawing.Size(70, 26);
            this.btnDeselectAll.TabIndex = 7;
            this.btnDeselectAll.Text = "None";
            this.btnDeselectAll.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnDeselectAll.UseVisualStyleBackColor = true;
            this.btnDeselectAll.Click += new System.EventHandler(this.btnDeselectAll_Click_1);
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectAll.Image = ((System.Drawing.Image)(resources.GetObject("btnSelectAll.Image")));
            this.btnSelectAll.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSelectAll.Location = new System.Drawing.Point(484, 341);
            this.btnSelectAll.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(70, 26);
            this.btnSelectAll.TabIndex = 6;
            this.btnSelectAll.Text = "All";
            this.btnSelectAll.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnSelectAll.UseVisualStyleBackColor = true;
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click_1);
            // 
            // treeViewContents
            // 
            this.treeViewContents.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeViewContents.CheckBoxes = true;
            this.treeViewContents.HotTracking = true;
            this.treeViewContents.Location = new System.Drawing.Point(7, 22);
            this.treeViewContents.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.treeViewContents.Name = "treeViewContents";
            this.treeViewContents.Size = new System.Drawing.Size(624, 312);
            this.treeViewContents.TabIndex = 5;
            // 
            // btnExit
            // 
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.Image = ((System.Drawing.Image)(resources.GetObject("btnExit.Image")));
            this.btnExit.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnExit.Location = new System.Drawing.Point(503, 429);
            this.btnExit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(149, 38);
            this.btnExit.TabIndex = 8;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click_1);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 473);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            this.statusStrip1.Size = new System.Drawing.Size(666, 22);
            this.statusStrip1.TabIndex = 9;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(14, 472);
            this.progressBar.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.progressBar.MarqueeAnimationSpeed = 200;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(631, 16);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 10;
            // 
            // btnRegisterUnregister
            // 
            this.btnRegisterUnregister.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRegisterUnregister.Location = new System.Drawing.Point(14, 396);
            this.btnRegisterUnregister.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnRegisterUnregister.Name = "btnRegisterUnregister";
            this.btnRegisterUnregister.Size = new System.Drawing.Size(306, 26);
            this.btnRegisterUnregister.TabIndex = 8;
            this.btnRegisterUnregister.Text = "Register Context Menu Handler";
            this.btnRegisterUnregister.UseVisualStyleBackColor = true;
            this.btnRegisterUnregister.Click += new System.EventHandler(this.btnRegisterUnregister_Click_1);
            // 
            // UpuGui
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(666, 495);
            this.Controls.Add(this.btnRegisterUnregister);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.groupBox);
            this.Controls.Add(this.btnSelectInputFile);
            this.Controls.Add(this.btnUnpack);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimumSize = new System.Drawing.Size(522, 294);
            this.Name = "UpuGui";
            this.Text = "Unitypackage Unpacker for Unity®";
            this.groupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void groupBox_Enter(object sender, EventArgs e)
        {
        }

        private void btnSelectInputFile_Click_1(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Unitypackage Files|*.unitypackage";
            var num = (int)openFileDialog.ShowDialog();
            if (string.IsNullOrEmpty(openFileDialog.FileName))
                return;
            OpenFile(openFileDialog.FileName);
        }

        private void btnUnpack_Click_1(object sender, EventArgs e)
        {
            if (m_remapInfo == null)
                return;
            var num = (int)saveToFolderDialog.ShowDialog();
            if (string.IsNullOrEmpty(saveToFolderDialog.SelectedPath))
                return;
            btnSelectInputFile.Enabled = false;
            btnUnpack.Enabled = false;
            btnExit.Enabled = false;
            progressBar.Visible = true;
            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += UnpackInputFileWorker;
            backgroundWorker.RunWorkerCompleted += UnpackInputFileWorkerCompleted;
            backgroundWorker.RunWorkerAsync();
        }

        private void btnRegisterUnregister_Click_1(object sender, EventArgs e)
        {
            m_upu.RegisterUnregisterShellHandler(!m_upu.IsContextMenuHandlerRegistered());
            btnRegisterUnregister.Enabled = false;
        }

        private void btnSelectAll_Click_1(object sender, EventArgs e)
        {
            foreach (TreeNode treeNode in treeViewContents.Nodes)
                treeNode.Checked = true;
        }

        private void btnDeselectAll_Click_1(object sender, EventArgs e)
        {
            foreach (TreeNode treeNode in treeViewContents.Nodes)
                treeNode.Checked = false;
        }

        private void btnExit_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}