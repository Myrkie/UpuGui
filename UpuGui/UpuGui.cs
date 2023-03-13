using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using UpuGui.UpuCore;

// ReSharper disable once CheckNamespace
namespace UpuGui
{
    public sealed class UpuGui : Form
    {
        // ReSharper disable once IdentifierTypo
        private static bool _exportmeta;
        private Button _btnExit;
        private Button _btnRegisterUnregister;
        private Button _btnSelectInputFile;
        private Button _btnUnpack;
        private Button _btnAbout;
        private Button _btnExpand;
        private Button _btnCollapse;
        private CheckBox _chkBoxSelectAll;
        private CheckBox _chkBoxMeta;
#pragma warning disable CS0649
        private IContainer components;
#pragma warning restore CS0649
        private GroupBox _groupBox;
        private Dictionary<string, string> _mRemapInfo;
        private string? _mTmpUnpackedOutputPathForUi;
        private readonly List<string> _unpacks = new();
        private readonly UpuConsole.UpuConsole _mUpu;
        private OpenFileDialog _openFileDialog;
        private ProgressBar _progressBar;
        private FolderBrowserDialog _saveToFolderDialog;
        private StatusStrip _statusStrip1;
        private TreeView _treeViewContents;
        private ImageList imageList;
        private RichTextBox _ConsoleTextBox;

        private UpuGui()
        {
            InitializeComponent();
            _btnUnpack!.Enabled = false;
            _btnAbout!.Enabled = true;
            _chkBoxSelectAll!.Enabled = false;
            _btnExpand!.Enabled = false;
            _btnCollapse!.Enabled = false;
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
            if (UpuConsole.UpuConsole.IsContextMenuHandlerRegistered())
                _btnRegisterUnregister.Text = @"Unregister Explorer Context Menu Handler";
            var mShellHandlerCheckTimer = new Timer();
            mShellHandlerCheckTimer.Interval = 5000;
#pragma warning disable CS8622
            mShellHandlerCheckTimer.Tick += ShellHandlerCheckTimer_Tick;
#pragma warning restore CS8622
            mShellHandlerCheckTimer.Enabled = true;
            mShellHandlerCheckTimer.Start();
            ShellHandlerCheckTimer_Tick(null!, EventArgs.Empty);
        }

        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(UpuGui));
            _saveToFolderDialog = new FolderBrowserDialog();
            _btnAbout = new Button();
            _btnUnpack = new Button();
            _btnSelectInputFile = new Button();
            _groupBox = new GroupBox();
            _btnExpand = new Button();
            _btnCollapse = new Button();
            _chkBoxSelectAll = new CheckBox();
            _chkBoxMeta = new CheckBox();
            _treeViewContents = new TreeView();
            _openFileDialog = new OpenFileDialog();
            _btnExit = new Button();
            _statusStrip1 = new StatusStrip();
            _progressBar = new ProgressBar();
            _btnRegisterUnregister = new Button();
            _ConsoleTextBox = new RichTextBox();
            imageList = new ImageList(components);
            Console.SetOut(new ControlWriter(_ConsoleTextBox));
            _groupBox.SuspendLayout();
            SuspendLayout();
            // 
            // imageList
            // 
            imageList.ImageStream = (ImageListStreamer)resources.GetObject("imageList.ImageStream");
            imageList.TransparentColor = Color.Transparent;
            imageList.Images.SetKeyName(0, "d_AnimationClip Icon.png");
            imageList.Images.SetKeyName(1, "d_Assembly Icon.png");
            imageList.Images.SetKeyName(2, "d_cs Script Icon.png");
            imageList.Images.SetKeyName(3, "d_Folder Icon.png");
            imageList.Images.SetKeyName(4, "d_Image Icon.png");
            imageList.Images.SetKeyName(5, "d_Material Icon.png");
            imageList.Images.SetKeyName(6, "d_PrefabModel Icon.png");
            imageList.Images.SetKeyName(7, "d_ScriptableObject Icon.png");
            imageList.Images.SetKeyName(8, "d_Shader Icon.png");
            imageList.Images.SetKeyName(9, "d_TextAsset Icon.png");
            imageList.Images.SetKeyName(10, "d__Help.png");
            imageList.Images.SetKeyName(11, "d_AnimatorController Icon.png");
            imageList.Images.SetKeyName(12, "d_AvatarMask Icon.png");
            imageList.Images.SetKeyName(13, "d_Mesh Icon.png");
            imageList.Images.SetKeyName(14, "d_SceneAsset Icon.png");
            imageList.Images.SetKeyName(15, "d_AudioSource Icon.png");
            // 
            // _groupBox
            // 
            _groupBox.Anchor = AnchorStyles.Top | 
                               AnchorStyles.Bottom | 
                               AnchorStyles.Left | 
                               AnchorStyles.Right;
            _groupBox.Controls.Add(_btnExpand);
            _groupBox.Controls.Add(_btnCollapse);
            _groupBox.Controls.Add(_treeViewContents);
            _groupBox.Controls.Add(_chkBoxSelectAll);
            _groupBox.Controls.Add(_ConsoleTextBox);
            _groupBox.Location = new Point(14, 14);
            _groupBox.Margin = new Padding(4, 4, 4, 4);
            _groupBox.Name = "_groupBox";
            _groupBox.Padding = new Padding(4, 4, 4, 4);
            _groupBox.Size = new Size(638, 375);
            _groupBox.TabIndex = 1;
            _groupBox.TabStop = false;
            // ReSharper disable once StringLiteralTypo
            _groupBox.Text = @"Unitypackage File";
            // 
            // _treeViewContents
            // 
            _treeViewContents.Anchor = AnchorStyles.Top |
                                       AnchorStyles.Bottom | 
                                       AnchorStyles.Left | 
                                       AnchorStyles.Right;
            _treeViewContents.CheckBoxes = true;
            _treeViewContents.HotTracking = true;
            _treeViewContents.Location = new Point(7, 22);
            _treeViewContents.Margin = new Padding(4);
            _treeViewContents.Name = "_treeViewContents";
            _treeViewContents.Size = new Size(312, 312);
            _treeViewContents.TabIndex = 2;
            _treeViewContents.ImageList = imageList;
#pragma warning disable CS8622
            _treeViewContents.AfterSelect += treeViewContents_AfterCheck;
            _treeViewContents.AfterCheck += treeViewContents_AfterCheck;
            _treeViewContents.BeforeSelect += treeViewContents_BeforeSelect;
#pragma warning restore CS8622
            // 
            // _ConsoleTextBox
            // 
            _ConsoleTextBox.Anchor = AnchorStyles.Top | 
                                     AnchorStyles.Bottom |
                                     AnchorStyles.Right;
            _ConsoleTextBox.Location = new Point(326, 22);
            _ConsoleTextBox.Margin = new Padding(4);
            _ConsoleTextBox.Name = "_ConsoleTextBox";
            _ConsoleTextBox.Size = new Size(312, 312);
            _ConsoleTextBox.ReadOnly = true;
            _ConsoleTextBox.AllowDrop = false;
            _ConsoleTextBox.TabIndex = 8;
            _ConsoleTextBox.Text = "";
#pragma warning disable CS8622
            _ConsoleTextBox.TextChanged += _ConsoleTextBox_TextChanged;
#pragma warning restore CS8622
            // 
            // _btnExpand
            // 
            _btnExpand.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnExpand.Image = (Image)resources.GetObject("_btnExpand.Image");
            _btnExpand.ImageAlign = ContentAlignment.MiddleLeft;
            _btnExpand.Location = new Point(8, 341);
            _btnExpand.Margin = new Padding(4);
            _btnExpand.Name = "_btnExpand";
            _btnExpand.Size = new Size(70, 26);
            _btnExpand.TabIndex = 3;
            _btnExpand.Text = @"Expand";
            _btnExpand.TextAlign = ContentAlignment.MiddleRight;
            _btnExpand.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnExpand.Click += BtnExpand_Click_1;
#pragma warning restore CS8622
            // 
            // _btnCollapse
            // 
            _btnCollapse.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnCollapse.Image = (Image)resources.GetObject("_btnCollapse.Image");
            _btnCollapse.ImageAlign = ContentAlignment.MiddleLeft;
            _btnCollapse.Location = new Point(80, 341);
            _btnCollapse.Margin = new Padding(4);
            _btnCollapse.Name = "_btnCollapse";
            _btnCollapse.Size = new Size(70, 26);
            _btnCollapse.TabIndex = 4;
            _btnCollapse.Text = @"Collapse";
            _btnCollapse.TextAlign = ContentAlignment.MiddleRight;
            _btnCollapse.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnCollapse.Click += _btnCollapse_Click_1;
#pragma warning restore CS8622
            // 
            // _chkBoxSelectAll
            // 
            _chkBoxSelectAll.Checked = true;
            _chkBoxSelectAll.CheckState = CheckState.Checked;
            _chkBoxSelectAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _chkBoxSelectAll.Location = new Point(527, 343);
            _chkBoxSelectAll.Margin = new Padding(4, 4, 4, 4);
            _chkBoxSelectAll.Name = "_chkBoxSelectAll";
            _chkBoxSelectAll.Size = new Size(85, 24);
            _chkBoxSelectAll.TabIndex = 5;
            _chkBoxSelectAll.Text = @"Select All";
            _chkBoxSelectAll.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _chkBoxSelectAll.CheckedChanged += _chkBoxSelectAll_CheckedChanged;
#pragma warning restore CS8622
            // 
            // _btnRegisterUnregister
            // 
            _btnRegisterUnregister.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnRegisterUnregister.Location = new Point(14, 396);
            _btnRegisterUnregister.Margin = new Padding(4, 4, 4, 4);
            _btnRegisterUnregister.Name = "_btnRegisterUnregister";
            _btnRegisterUnregister.Size = new Size(306, 26);
            _btnRegisterUnregister.TabIndex = 6;
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
            _btnAbout.TabIndex = 7;
            _btnAbout.Text = @"About";
            _btnAbout.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnAbout.Click += _btnAbout_Click_1;
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
            _btnSelectInputFile.TabIndex = 8;
            _btnSelectInputFile.Text = @"Select Input File";
            _btnSelectInputFile.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnSelectInputFile.Click += btnSelectInputFile_Click_1;
#pragma warning restore CS8622
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
            _btnUnpack.TabIndex = 9;
            _btnUnpack.Text = @"Unpack now";
            _btnUnpack.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnUnpack.Click += btnUnpack_Click_1;
#pragma warning restore CS8622
            // 
            // _chkBoxMeta
            // 
            _chkBoxMeta.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _chkBoxMeta.Location = new Point(406, 429);
            _chkBoxMeta.Margin = new Padding(4);
            _chkBoxMeta.Name = "_chkBoxMeta";
            _chkBoxMeta.Size = new Size(89, 38);
            _chkBoxMeta.TabIndex = 10;
            _chkBoxMeta.Text = @"ExportMeta";
            _chkBoxMeta.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _chkBoxMeta.CheckedChanged += _chkBoxMeta_CheckedChanged;
#pragma warning restore CS8622
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
            _btnExit.TabIndex = 11;
            _btnExit.Text = @"Exit";
            _btnExit.UseVisualStyleBackColor = true;
#pragma warning disable CS8622
            _btnExit.Click += _btnExit_Click_1;
#pragma warning restore CS8622
            // 
            // _statusStrip1
            // 
            _statusStrip1.Location = new Point(0, 473);
            _statusStrip1.Name = "_statusStrip1";
            _statusStrip1.Padding = new Padding(1, 0, 16, 0);
            _statusStrip1.Size = new Size(666, 22);
            _statusStrip1.TabIndex = 12;
            _statusStrip1.Text = @"statusStrip1";
            // 
            // _progressBar
            // 
            _progressBar.Anchor = AnchorStyles.Bottom |
                                   AnchorStyles.Left |
                                   AnchorStyles.Right;
            _progressBar.Location = new Point(14, 472);
            _progressBar.Margin = new Padding(4, 4, 4, 4);
            _progressBar.MarqueeAnimationSpeed = 200;
            _progressBar.Name = "_progressBar";
            _progressBar.Size = new Size(631, 16);
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.TabIndex = 13;
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
            Controls.Add(_chkBoxMeta);
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
        
        
        private static void Form1_DragEnter(object sender, DragEventArgs e)
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
            var outfile = strArray[0];
            _openFileDialog.FileName = outfile;

            OpenFile(outfile);
        }

        private void ShellHandlerCheckTimer_Tick(object sender, EventArgs e)
        {
            _btnRegisterUnregister.Text = UpuConsole.UpuConsole.IsContextMenuHandlerRegistered() ? @"Unregister Explorer Context Menu Handler" : @"Register Explorer Context Menu Handler";
            _btnRegisterUnregister.Enabled = true;
        }

        private static string? _previousfile;

        private void OpenFile(string? filePathName)
        {
            // checks if the file was previous loaded to avoid repeated decompression
            if (filePathName == _previousfile) return;
            _previousfile = filePathName;
            _groupBox.Text = new FileInfo(filePathName).Name;
            _btnExit.Enabled = false;
            _btnSelectInputFile.Enabled = false;
            _progressBar.Visible = true;
            var backgroundWorker = new BackgroundWorker();
#pragma warning disable CS8622
            backgroundWorker.DoWork += ReadInputFileWorker;
            backgroundWorker.RunWorkerCompleted += ReadInputFileWorkerCompleted;
#pragma warning restore CS8622
            _treeViewContents.Nodes.Clear();
            backgroundWorker.RunWorkerAsync(filePathName);
        }

        private void ReadInputFileWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is Exception)
            {
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
            _chkBoxSelectAll.Enabled = true;
            _btnExpand.Enabled = true;
            _btnCollapse.Enabled = true;
            _btnExit.Enabled = true;
        }
        // iterate for the next person as a warning to not change what isn't broken
        // time wasted on this method 4 hours
        private void ReadInputFileWorker(object sender, DoWorkEventArgs e)
        {
            try
            {
                var treeNodes = new List<TreeNode>();
                _mTmpUnpackedOutputPathForUi = KissUnpacker.GetTempPath();
                _unpacks.Add(_mTmpUnpackedOutputPathForUi);
                _mRemapInfo = KissUnpacker.Unpack(e.Argument!.ToString(), _mTmpUnpackedOutputPathForUi, _mTmpUnpackedOutputPathForUi);

                // Create a dictionary to hold the parent nodes for each directory path
                var directoryNodes = new Dictionary<string, TreeNode>();

                // Iterate over each file in the remap info and create a tree node for it
                foreach (var keyValuePair in _mRemapInfo)
                {
                    if (!File.Exists(keyValuePair.Key)) continue;
                    var relativePath = keyValuePair.Value.Replace(_mTmpUnpackedOutputPathForUi, "");

                    // Create a list of directory names in the relative path
                    var directories = new List<string>(relativePath.Split(Path.DirectorySeparatorChar.ToString()));

                    // Remove any empty directory names
                    directories.RemoveAll(string.IsNullOrEmpty);

                    // Remove the file name from the list of directories
                    directories.RemoveAt(directories.Count - 1);

                    // Initialize the parent node to null
                    TreeNode? parentNode = null;

                    // Traverse the list of directory names and create nodes for them
                    foreach (var directory in directories)
                    {
                        // Check if the parent node already exists
                        if (!directoryNodes.TryGetValue(directory, out var directoryNode))
                        {
                            // Create a new directory node and add it to the parent node
                            directoryNode = new TreeNode(directory) { Checked = true};
                            directoryNodes[directory] = directoryNode;
                            directoryNode.ImageIndex = 3;
                            directoryNode.SelectedImageIndex = 3;
                            if (parentNode == null)
                            {
                                treeNodes.Add(directoryNode);
                            }
                            else
                            {
                                parentNode.Nodes.Add(directoryNode);
                            }
                        }

                        // Set the current directory node as the parent for the next directory node
                        parentNode = directoryNode;
                    }

                    var text = keyValuePair.Value.Split('\\').Last();
                    var extension = text.Split('.').Last();

                    // Create the file node and add it to the final parent node
                    var fileNode = new TreeNode(relativePath) { Checked = true, Tag = keyValuePair, Text = text };
                    switch (extension)
                    {
                        case "anim":
                            fileNode.ImageIndex = 0;
                            fileNode.SelectedImageIndex = 0;
                            break;
                        case "dll":
                            fileNode.ImageIndex = 1;
                            fileNode.SelectedImageIndex = 1;
                            break;
                        case "cs":
                            fileNode.ImageIndex = 2;
                            fileNode.SelectedImageIndex = 2;
                            break;
                        case "prefab":
                            fileNode.ImageIndex = 6;
                            fileNode.SelectedImageIndex = 6;
                            break;
                        case "mat":
                            fileNode.ImageIndex = 5;
                            fileNode.SelectedImageIndex = 5;
                            break;
                        case "png":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "psd":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "bmp":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "exr":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "gif":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "hdr":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "iff":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "pict":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "tga":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "tiff":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "jpg":
                            fileNode.ImageIndex = 4;
                            fileNode.SelectedImageIndex = 4;
                            break;
                        case "shader":
                            fileNode.ImageIndex = 8;
                            fileNode.SelectedImageIndex = 8;
                            break;
                        case "asset":
                            fileNode.ImageIndex = 9;
                            fileNode.SelectedImageIndex = 9;
                            break;
                        case "controller":
                            fileNode.ImageIndex = 11;
                            fileNode.SelectedImageIndex = 11;
                            break;
                        case "mask":
                            fileNode.ImageIndex = 12;
                            fileNode.SelectedImageIndex = 12;
                            break;
                        case "fbx":
                            fileNode.ImageIndex = 13;
                            fileNode.SelectedImageIndex = 13;
                            break;
                        case "dae":
                            fileNode.ImageIndex = 13;
                            fileNode.SelectedImageIndex = 13;
                            break;
                        case "3ds":
                            fileNode.ImageIndex = 13;
                            fileNode.SelectedImageIndex = 13;
                            break;
                        case "skp":
                            fileNode.ImageIndex = 13;
                            fileNode.SelectedImageIndex = 13;
                            break;
                        case "blend":
                            fileNode.ImageIndex = 13;
                            fileNode.SelectedImageIndex = 13;
                            break;
                        case "obj":
                            fileNode.ImageIndex = 13;
                            fileNode.SelectedImageIndex = 13;
                            break;
                        case "unity":
                            fileNode.ImageIndex = 14;
                            fileNode.SelectedImageIndex = 14;
                            break;
                        case "mp3":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        case "off":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        case "wav":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        case "aiff":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        case "aif":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        case "mod":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        case "it":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        case "s3m":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        case "xm":
                            fileNode.ImageIndex = 15;
                            fileNode.SelectedImageIndex = 15;
                            break;
                        default:
                            fileNode.ImageIndex = 10;
                            fileNode.SelectedImageIndex = 10;
                            break;
                    }

                    // Add the final file node to its parent node, if it exists
                    if (parentNode != null)
                    {
                        parentNode.Nodes.Add(fileNode);
                    }
                    else
                    {
                        treeNodes.Add(fileNode);
                    }
                }

                // Set the result to the root nodes of the tree
                e.Result = treeNodes;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
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
            foreach (var treeNode in Collect(_treeViewContents.Nodes))
            {
                if(treeNode.Tag == null) continue;
                if (!treeNode.Checked) continue;
                dictionary.Add(((KeyValuePair<string, string>)treeNode.Tag).Key, ((KeyValuePair<string, string>)treeNode.Tag).Value);
                if (_exportmeta)
                {
                    dictionary.Add(((KeyValuePair<string, string>)treeNode.Tag).Key + ".meta", ((KeyValuePair<string, string>)treeNode.Tag).Value + ".meta");
                }
            }
            foreach (var keyValuePair in dictionary)
                map[keyValuePair.Key] = keyValuePair.Value.Replace(_mTmpUnpackedOutputPathForUi!,
                    _saveToFolderDialog.SelectedPath);
            KissUnpacker.RemapFiles(map);
        }

       static IEnumerable<TreeNode> Collect(TreeNodeCollection nodes)
        {
            foreach(TreeNode node in nodes)
            {
                yield return node;

                foreach (var child in Collect(node.Nodes))
                    yield return child;
            }
        }
       private static void treeViewContents_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Check if the selected node is a parent node
            if (e.Node.Nodes.Count > 0)
            {
                // Select all the child nodes recursively
                foreach (TreeNode childNode in e.Node.Nodes)
                {
                    childNode.Checked = e.Node.Checked;
                }
            }
        }
        private static void treeViewContents_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Contains(e.Node))
            {
                e.Cancel = true;
            }
        }
        
        private void Cleanup(object sender, EventArgs e)
        {
            foreach (var unpack in _unpacks)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (unpack == null || !Directory.Exists(unpack))
                    return;
                Directory.Delete(unpack, true);
            }
        }

        private void UpuGui_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cleanup(sender, e);
        }
        

        private void btnSelectInputFile_Click_1(object sender, EventArgs e)
        {
            // ReSharper disable once StringLiteralTypo
            _openFileDialog.Filter = @"Unitypackage Files|*.unitypackage"; 
            _openFileDialog.ShowDialog();
            if (string.IsNullOrEmpty(_openFileDialog.FileName))
                return;
            OpenFile(_openFileDialog.FileName);
        }

        private void btnUnpack_Click_1(object sender, EventArgs e)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_mRemapInfo == null)
                return;
            _saveToFolderDialog.ShowDialog();
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

        private void _ConsoleTextBox_TextChanged(object sender, EventArgs e)
        {
            _ConsoleTextBox.SelectionStart = _ConsoleTextBox.Text.Length;
            _ConsoleTextBox.ScrollToCaret();
        }

        private void btnRegisterUnregister_Click_1(object sender, EventArgs e)
        {
            _mUpu.RegisterUnregisterShellHandler(!UpuConsole.UpuConsole.IsContextMenuHandlerRegistered());
            _btnRegisterUnregister.Enabled = false;
        }
        private static void _btnExit_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private static void _btnAbout_Click_1(object sender, EventArgs e)
        {
            Process.Start("cmd","/c start https://github.com/Myrkie/UpuGui");
        }
        private void BtnExpand_Click_1(object sender, EventArgs e)
        {
            foreach (var node in Collect(_treeViewContents.Nodes))
            {
                node.ExpandAll();
            }
            _treeViewContents.Nodes[0].EnsureVisible();
        }
        
                
        private void _chkBoxMeta_CheckedChanged(object sender, EventArgs e)
        {
            _exportmeta = _chkBoxMeta.Checked;
        }
        
        private void _btnCollapse_Click_1(object sender, EventArgs e)
        {
            foreach (var node in Collect(_treeViewContents.Nodes))
            {
                node.Collapse();
            }
        }
        private void _chkBoxSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            if(_chkBoxSelectAll.Checked){
                foreach (TreeNode node in _treeViewContents.Nodes)
                {
                    CheckNode(node, true);
                }
            }
            else
            {
                foreach (TreeNode node in _treeViewContents.Nodes)
                {
                    CheckNode(node, false);
                }
            }
        }
        private static void CheckNode(TreeNode node, bool isChecked)
        {
            node.Checked = isChecked;
            foreach (TreeNode childNode in node.Nodes)
            {
                CheckNode(childNode, isChecked);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null!)
                components.Dispose();
            base.Dispose(disposing);
        }
    }
    
    public class ControlWriter : TextWriter
    {
        private Control textbox;
        public ControlWriter(Control textbox)
        {
            this.textbox = textbox;
        }

        public override void Write(char value)
        {
            textbox.Text += value;
        }

        public override void Write(string value)
        {
            textbox.Text += value;
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }
}