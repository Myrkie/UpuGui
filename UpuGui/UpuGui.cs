using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using UpuGui.UpuCore;

// ReSharper disable once CheckNamespace
namespace UpuGui
{
    public sealed class UpuGui : Form
    {
        private Button _btnDeselectAll;
        private Button _btnExit;
        private Button _btnRegisterUnregister;
        private Button _btnSelectAll;
        private Button _btnSelectInputFile;
        private Button _btnUnpack;
        private Button _btnAbout;
#pragma warning disable CS0649
        private IContainer components;
#pragma warning restore CS0649
        private GroupBox _groupBox;
        private readonly KissUnpacker _mKu;
        private Dictionary<string, string> _mRemapInfo;
        private string? _mTmpUnpackedOutputPathForUi;
        private readonly List<string> _unpacks = new();
        private readonly UpuConsole.UpuConsole _mUpu;
        private OpenFileDialog _openFileDialog;
        private ProgressBar _progressBar;
        private FolderBrowserDialog _saveToFolderDialog;
        private StatusStrip _statusStrip1;
        private TreeView _treeViewContents;

        private UpuGui()
        {
            InitializeComponent();
            _btnUnpack!.Enabled = false;
            _btnAbout!.Enabled = true;
            _btnSelectAll!.Enabled = false;
            _btnDeselectAll!.Enabled = false;
            _progressBar!.Visible = false;
            _treeViewContents!.CheckBoxes = true;
            AllowDrop = true;
#pragma warning disable CS8622
            AppDomain.CurrentDomain.ProcessExit += Cleanup;
            FormClosed += UpuGui_FormClosed;
            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;
#pragma warning restore CS8622
            var upuGui = this;
            var str = upuGui.Text + $" {Assembly.GetExecutingAssembly().GetName().Version}";
            upuGui.Text = str;
        }

        public UpuGui(UpuConsole.UpuConsole upu) : this()
        {
            _mUpu = upu;
            if (upu.IsContextMenuHandlerRegistered())
                _btnRegisterUnregister.Text = @"Unregister Explorer Context Menu Handler";
            _mKu = new KissUnpacker();
            var mShellHandlerCheckTimer = new Timer();
            mShellHandlerCheckTimer.Interval = 5000;
#pragma warning disable CS8622
            mShellHandlerCheckTimer.Tick += ShellHandlerCheckTimer_Tick;
#pragma warning restore CS8622
            mShellHandlerCheckTimer.Enabled = true;
            mShellHandlerCheckTimer.Start();
            ShellHandlerCheckTimer_Tick(null!, EventArgs.Empty);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data!.GetDataPresent(DataFormats.FileDrop))
                return;
            e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var strArray = (string[]) e.Data!.GetData(DataFormats.FileDrop)!;
            if (strArray.Length <= 0)
                return;
            OpenFile(strArray[0]);
        }

        private void ShellHandlerCheckTimer_Tick(object sender, EventArgs e)
        {
            _btnRegisterUnregister.Text = _mUpu.IsContextMenuHandlerRegistered() ? @"Unregister Explorer Context Menu Handler" : @"Register Explorer Context Menu Handler";
            _btnRegisterUnregister.Enabled = true;
        }

        private void OpenFile(string filePathName)
        {
            _groupBox.Text = new FileInfo(filePathName).Name;
            _btnExit.Enabled = false;
            _btnSelectInputFile.Enabled = false;
            _progressBar.Visible = true;
            _treeViewContents.Nodes.Clear();
            var backgroundWorker = new BackgroundWorker();
#pragma warning disable CS8622
            backgroundWorker.DoWork += ReadInputFileWorker;
            backgroundWorker.RunWorkerCompleted += ReadInputFileWorkerCompleted;
#pragma warning restore CS8622
            backgroundWorker.RunWorkerAsync(filePathName);
        }

        private void ReadInputFileWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is Exception)
            {
                var num =
                    (int)
                    // ReSharper disable once StringLiteralTypo
                    MessageBox.Show(@"An exception happened: \n" + e.Result, @"Ooops...", MessageBoxButtons.OK,
                        MessageBoxIcon.Hand);
            }
            else
            {
                foreach (var node in (List<TreeNode>) e.Result!)
                    _treeViewContents.Nodes.Add(node);
            }
            _progressBar.Visible = false;
            _btnSelectInputFile.Enabled = true;
            _btnUnpack.Enabled = true;
            _btnSelectAll.Enabled = true;
            _btnDeselectAll.Enabled = true;
            _btnExit.Enabled = true;
        }

        private void ReadInputFileWorker(object sender, DoWorkEventArgs e)
        {
            var list = new List<TreeNode>();
            try
            {
                _mTmpUnpackedOutputPathForUi = _mKu.GetTempPath();
                _mRemapInfo = _mKu.Unpack(e.Argument!.ToString(), _mTmpUnpackedOutputPathForUi);
                foreach (var keyValuePair in _mRemapInfo)
                    if (File.Exists(keyValuePair.Key))
                    {
                        var text = keyValuePair.Value.Replace(_mTmpUnpackedOutputPathForUi, "");
                        if (text.StartsWith(Path.DirectorySeparatorChar.ToString()))
                            text = text.Substring(1);
                        list.Add(new TreeNode(text)
                        {
                            Checked = true,
                            Tag = keyValuePair
                        });
                    }
                list.Sort((t1, t2) => string.Compare(t1.Text, t2.Text, StringComparison.Ordinal));
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
            _btnAbout.Enabled = true;
            _btnSelectInputFile.Enabled = true;
            _btnUnpack.Enabled = false;
            _btnExit.Enabled = true;
            _progressBar.Visible = false;
        }

        private void UnpackInputFileWorker(object sender, DoWorkEventArgs e)
        {
            if (!Directory.Exists(_saveToFolderDialog.SelectedPath))
                Directory.CreateDirectory(_saveToFolderDialog.SelectedPath);
            var map = new Dictionary<string, string>();
            var dictionary = new Dictionary<string, string>();
            foreach (TreeNode treeNode in _treeViewContents.Nodes)
                if (treeNode.Checked)
                    dictionary.Add(((KeyValuePair<string, string>) treeNode.Tag).Key,
                        ((KeyValuePair<string, string>) treeNode.Tag).Value);
            foreach (var keyValuePair in dictionary)
                map[keyValuePair.Key] = keyValuePair.Value.Replace(_mTmpUnpackedOutputPathForUi!,
                    _saveToFolderDialog.SelectedPath);
            _mKu.RemapFiles(map);
        }

        private void Cleanup()
        {
            foreach (var unpack in _unpacks)
            {
                if (unpack == null || !Directory.Exists(unpack))
                    return;
                Directory.Delete(unpack, true);
            }
        }
        private void treeViewContents_AfterSelect(object sender, TreeViewEventArgs e)
        {
        }

        private void UpuGui_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cleanup(sender, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null!)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(UpuGui));
            _saveToFolderDialog = new FolderBrowserDialog();
            _btnAbout = new Button();
            _btnUnpack = new Button();
            _btnSelectInputFile = new Button();
            _groupBox = new GroupBox();
            _btnDeselectAll = new Button();
            _btnSelectAll = new Button();
            _treeViewContents = new TreeView();
            _openFileDialog = new OpenFileDialog();
            _btnExit = new Button();
            _statusStrip1 = new StatusStrip();
            _progressBar = new ProgressBar();
            _btnRegisterUnregister = new Button();
            _groupBox.SuspendLayout();
            SuspendLayout();
            // 
            // _btnUnpack
            // 
            _btnUnpack.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnUnpack.Image = (Image)resources.GetObject("_btnUnpack.Image");
            _btnUnpack.ImageAlign = ContentAlignment.MiddleLeft;
            _btnUnpack.Location = new Point(170, 429);
            _btnUnpack.Margin = new Padding(4, 4, 4, 4);
            _btnUnpack.Name = "_btnUnpack";
            _btnUnpack.Size = new Size(149, 38);
            _btnUnpack.TabIndex = 3;
            _btnUnpack.Text = @"Unpack now";
            _btnUnpack.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnUnpack.Click += btnUnpack_Click_1;
#pragma warning restore CS8622
            // 
            // _btnSelectInputFile
            // 
            _btnSelectInputFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnSelectInputFile.Image = (Image)resources.GetObject("_btnSelectInputFile.Image");
            _btnSelectInputFile.ImageAlign = ContentAlignment.MiddleLeft;
            _btnSelectInputFile.Location = new Point(14, 429);
            _btnSelectInputFile.Margin = new Padding(4, 4, 4, 4);
            _btnSelectInputFile.Name = "_btnSelectInputFile";
            _btnSelectInputFile.Size = new Size(149, 38);
            _btnSelectInputFile.TabIndex = 6;
            _btnSelectInputFile.Text = @"Select Input File";
            _btnSelectInputFile.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnSelectInputFile.Click += btnSelectInputFile_Click_1;
#pragma warning restore CS8622
            // 
            // _groupBox
            // 
            _groupBox.Anchor = ((AnchorStyles.Top | AnchorStyles.Bottom) 
                               | AnchorStyles.Left) 
                              | AnchorStyles.Right;
            _groupBox.Controls.Add(_btnDeselectAll);
            _groupBox.Controls.Add(_btnSelectAll);
            _groupBox.Controls.Add(_treeViewContents);
            _groupBox.Location = new Point(14, 14);
            _groupBox.Margin = new Padding(4, 4, 4, 4);
            _groupBox.Name = "_groupBox";
            _groupBox.Padding = new Padding(4, 4, 4, 4);
            _groupBox.Size = new Size(638, 375);
            _groupBox.TabIndex = 7;
            _groupBox.TabStop = false;
            // ReSharper disable once StringLiteralTypo
            _groupBox.Text = @"Unitypackage File";
            // 
            // _btnDeselectAll
            // 
            _btnDeselectAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnDeselectAll.Image = (Image)resources.GetObject("_btnDeselectAll.Image");
            _btnDeselectAll.ImageAlign = ContentAlignment.MiddleLeft;
            _btnDeselectAll.Location = new Point(561, 341);
            _btnDeselectAll.Margin = new Padding(4, 4, 4, 4);
            _btnDeselectAll.Name = "_btnDeselectAll";
            _btnDeselectAll.Size = new Size(70, 26);
            _btnDeselectAll.TabIndex = 7;
            _btnDeselectAll.Text = @"None";
            _btnDeselectAll.TextAlign = ContentAlignment.MiddleRight;
            _btnDeselectAll.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnDeselectAll.Click += btnDeselectAll_Click_1;
#pragma warning restore CS8622
            // 
            // _btnSelectAll
            // 
            _btnSelectAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnSelectAll.Image = (Image)resources.GetObject("_btnSelectAll.Image");
            _btnSelectAll.ImageAlign = ContentAlignment.MiddleLeft;
            _btnSelectAll.Location = new Point(484, 341);
            _btnSelectAll.Margin = new Padding(4, 4, 4, 4);
            _btnSelectAll.Name = "_btnSelectAll";
            _btnSelectAll.Size = new Size(70, 26);
            _btnSelectAll.TabIndex = 6;
            _btnSelectAll.Text = @"All";
            _btnSelectAll.TextAlign = ContentAlignment.MiddleRight;
            _btnSelectAll.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnSelectAll.Click += btnSelectAll_Click_1;
#pragma warning restore CS8622
            // 
            // _treeViewContents
            // 
            _treeViewContents.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom 
                                                         | AnchorStyles.Left) 
                                      | AnchorStyles.Right;
            _treeViewContents.CheckBoxes = true;
            _treeViewContents.HotTracking = true;
            _treeViewContents.Location = new Point(7, 22);
            _treeViewContents.Margin = new Padding(4, 4, 4, 4);
            _treeViewContents.Name = "_treeViewContents";
            _treeViewContents.Size = new Size(624, 312);
            _treeViewContents.TabIndex = 5;
            // 
            // _btnExit
            // 
            _btnExit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnExit.Image = (Image)resources.GetObject("_btnExit.Image");
            _btnExit.ImageAlign = ContentAlignment.MiddleLeft;
            _btnExit.Location = new Point(503, 429);
            _btnExit.Margin = new Padding(4, 4, 4, 4);
            _btnExit.Name = "_btnExit";
            _btnExit.Size = new Size(149, 38);
            _btnExit.TabIndex = 8;
            _btnExit.Text = @"Exit";
            _btnExit.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnExit.Click += btnExit_Click_1;
#pragma warning restore CS8622
            // 
            // _statusStrip1
            // 
            _statusStrip1.Location = new Point(0, 473);
            _statusStrip1.Name = "_statusStrip1";
            _statusStrip1.Padding = new Padding(1, 0, 16, 0);
            _statusStrip1.Size = new Size(666, 22);
            _statusStrip1.TabIndex = 9;
            _statusStrip1.Text = @"statusStrip1";
            // 
            // _progressBar
            // 
            _progressBar.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left) 
                                 | AnchorStyles.Right;
            _progressBar.Location = new Point(14, 472);
            _progressBar.Margin = new Padding(4, 4, 4, 4);
            _progressBar.MarqueeAnimationSpeed = 200;
            _progressBar.Name = "_progressBar";
            _progressBar.Size = new Size(631, 16);
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.TabIndex = 10;
            // 
            // _btnRegisterUnregister
            // 
            _btnRegisterUnregister.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnRegisterUnregister.Location = new Point(14, 396);
            _btnRegisterUnregister.Margin = new Padding(4, 4, 4, 4);
            _btnRegisterUnregister.Name = "_btnRegisterUnregister";
            _btnRegisterUnregister.Size = new Size(306, 26);
            _btnRegisterUnregister.TabIndex = 8;
            _btnRegisterUnregister.Text = @"Register Context Menu Handler";
            _btnRegisterUnregister.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnRegisterUnregister.Click += btnRegisterUnregister_Click_1;
#pragma warning restore CS8622
            //
            // _btnAbout
            // 
            _btnAbout.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnAbout.Image = (Image)resources.GetObject("_btnAbout.Image");
            _btnAbout.ImageAlign = ContentAlignment.MiddleLeft;
            _btnAbout.Location = new Point(526, 394);
            _btnAbout.Margin = new Padding(4);
            _btnAbout.Name = "_btnAbout";
            _btnAbout.Size = new Size(100, 31);
            _btnAbout.TabIndex = 11;
            _btnAbout.Text = @"About";
            _btnAbout.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnAbout.Click += btnAbout_Click_1;
#pragma warning restore CS8622
            // 
            // UpuGui
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(666, 495);
            Controls.Add(_btnRegisterUnregister);
            Controls.Add(_progressBar);
            Controls.Add(_statusStrip1);
            Controls.Add(_btnExit);
            Controls.Add(_groupBox);
            Controls.Add(_btnSelectInputFile);
            Controls.Add(_btnUnpack);
            Controls.Add(_btnAbout);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 4, 4, 4);
            MinimumSize = new Size(522, 294);
            Name = "UpuGui";
            // ReSharper disable once StringLiteralTypo
            Text = @"Unitypackage Unpacker for Unity®";
            _groupBox.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        private void btnSelectInputFile_Click_1(object sender, EventArgs e)
        {
            // ReSharper disable once StringLiteralTypo
            _openFileDialog.Filter = @"Unitypackage Files|*.unitypackage";
            var num = (int)_openFileDialog.ShowDialog();
            if (string.IsNullOrEmpty(_openFileDialog.FileName))
                return;
            OpenFile(_openFileDialog.FileName);
        }

        private void btnUnpack_Click_1(object sender, EventArgs e)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_mRemapInfo == null)
                return;
            var num = (int)_saveToFolderDialog.ShowDialog();
            if (string.IsNullOrEmpty(_saveToFolderDialog.SelectedPath))
                return;
            _btnAbout.Enabled = false;
            _btnSelectInputFile.Enabled = false;
            _btnUnpack.Enabled = false;
            _btnExit.Enabled = false;
            _progressBar.Visible = true;
            var backgroundWorker = new BackgroundWorker();
#pragma warning disable CS8622
            backgroundWorker.DoWork += UnpackInputFileWorker;
            backgroundWorker.RunWorkerCompleted += UnpackInputFileWorkerCompleted;
#pragma warning restore CS8622
            backgroundWorker.RunWorkerAsync();
        }

        private void btnRegisterUnregister_Click_1(object sender, EventArgs e)
        {
            _mUpu.RegisterUnregisterShellHandler(!_mUpu.IsContextMenuHandlerRegistered());
            _btnRegisterUnregister.Enabled = false;
        }

        private void btnSelectAll_Click_1(object sender, EventArgs e)
        {
            foreach (TreeNode treeNode in _treeViewContents.Nodes)
                treeNode.Checked = true;
        }

        private void btnDeselectAll_Click_1(object sender, EventArgs e)
        {
            foreach (TreeNode treeNode in _treeViewContents.Nodes)
                treeNode.Checked = false;
        }

        private void btnExit_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void btnAbout_Click_1(object sender, EventArgs e)
        {
            Process.Start("cmd","/c start https://github.com/Myrkie/UpuGui");
        }
    }
}