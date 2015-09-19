using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace yuicompressor_gui
{
    public partial class FormMain : Form
    {

        private static readonly double MinVersion = 1.4;
        private static readonly string BuildsFolder = "builds";
        private static readonly string JsFilePattern = "\\.js$";
        private static readonly string CssFilePattern = "\\.css$";

        public FormMain()
        {
            InitializeComponent();
            this.CtrlArgLineBreak.Maximum = Decimal.MaxValue;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        private static string[] RunJarFile(string java, string jar, string args)
        {
            string[] output = new string[2];
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = java;
                process.StartInfo.Arguments = " -jar \"" + jar + "\" " + string.Concat(args).Trim(' ');
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.EnableRaisingEvents = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();

                output[0] = string.Empty;
                process.BeginOutputReadLine();
                process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) 
                {
                    output[0] += e.Data;
                };

                output[1] = string.Empty;
                process.BeginErrorReadLine();
                process.ErrorDataReceived+= delegate (object sender, DataReceivedEventArgs e)
                {
                    output[1] += e.Data;
                };

                process.WaitForExit();
                process.Close();

            }
            catch (Exception ex)
            {
                output[1] = ex.Message;
            }

            return output;
        }
 

        private static bool ValidateJavaExeFile(string java, out string error)
        {
            error = string.Empty;
            try
            {
                Process process = new Process();
                string output = string.Empty;
                Match version = null;

                process.StartInfo.FileName = java;
                process.StartInfo.Arguments = "-version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();

                output = process.StandardOutput.ReadToEnd();

                if (string.IsNullOrEmpty(output))
                {
                    output = process.StandardError.ReadToEnd();
                }


                version = Regex.Match(output, @"^(?:java version ""(\d+(?:\.?\d+))[^""]*"")+", RegexOptions.IgnoreCase);


                if (version != null && version.Success)
                {
                    process.Close();
                    if (double.Parse(version.Groups[1].Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture) >= MinVersion)
                    {
                        return true;
                    }

                    error = "The YUI Compressor is written in Java (requires Java >= " + MinVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                    return false;
                }


                process.WaitForExit();
                process.Close();

                error = "\"" + java + "\" file is invalid!\nPlease change \"java.exe Path\" property and try again...";
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return false;
        }



        class YUICompressor
        {
            public const string FileNamePattern = "yuicompressor-(\\d+(?:\\.\\d+(?:\\.\\d+)?)?)\\.jar$";

            public static bool ValidateFileName(string filePath)
            {
                return Regex.IsMatch(filePath, FileNamePattern);
            }

            public YUICompressor(string filePath)
            {
                this.FilePath = filePath;
            }

            private string _version;
            public string Version
            {
                get { return this._version; }
            }

            private string _filePath;
            public string FilePath
            {
                get { return this._filePath; }
                set
                {
                    if (value != this._filePath)
                    {
                        this._filePath = value;
                        this._fileInfo = new FileInfo(this._filePath);
                        Match m = Regex.Match(this._filePath, FileNamePattern);
                        this._version = (m != null && m.Success) ? m.Groups[1].Value : "Unknown";
                    }
                }
            }

            private FileInfo _fileInfo;
            public FileInfo FileInfo
            {
                get { return this._fileInfo; }
            }
        }

        private void SetupIcons()
        {
            this.CtrlArgTypeInfo.Image =
            this.CtrlArgCharsetInfo.Image =
            this.CtrlArgLineBreakInfo.Image =
            this.CtrlArgJsPreserveSemicolonInfo.Image =
            this.CtrlArgJsNoMungeInfo.Image =
            this.CtrlArgJsPreserveSemicolonInfo.Image =
            this.CtrlArgJsDisableOptimizationsInfo.Image = SystemIcons.Information.ToBitmap();
        }

        List<YUICompressor> CompressorBuilds = new List<YUICompressor>();
        private void PopulateCompressorBuilds()
        {
            this.CtrlCompressor.SelectedIndexChanged -= this.CompressorVersionChanged;
            this.CtrlCompressor.DataSource = null;
            this.CompressorBuilds.Clear();
            if (!Directory.Exists(BuildsFolder))
            {
                Directory.CreateDirectory(BuildsFolder);
            }

            string[] buildsFolderFiles = Directory.GetFiles(BuildsFolder);

            foreach (string compressorPath in buildsFolderFiles)
            {
                if (YUICompressor.ValidateFileName(compressorPath))
                {
                    this.CompressorBuilds.Add(new YUICompressor(compressorPath));
                }
            }

            this.CtrlCompressor.DataSource = this.CompressorBuilds;
            this.CtrlCompressor.DisplayMember = "Version";
            this.CtrlCompressor.ValueMember = "FilePath";

            this.CtrlCompressor.SelectedIndexChanged += this.CompressorVersionChanged;
        }

        private void Init()
        {
            this.SetupIcons();
            this.PopulateCompressorBuilds();


            this.CtrlJavaPath.DataBindings.Add(new Binding("Text", Properties.Settings.Default, "JavaPath", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlCompressor.DataBindings.Add(new Binding("SelectedValue", Properties.Settings.Default, "CompressorVersion", true, DataSourceUpdateMode.OnPropertyChanged));


            this.CtrlArgType.DataBindings.Add(new Binding("SelectedIndex", Properties.Settings.Default, "ArgType", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlArgCharset.DataBindings.Add(new Binding("Text", Properties.Settings.Default, "ArgCharset", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlArgLineBreak.DataBindings.Add(new Binding("Value", Properties.Settings.Default, "ArgLineBreak", true, DataSourceUpdateMode.OnPropertyChanged));

            this.CtrlArgJsNoMunge.DataBindings.Add(new Binding("Checked", Properties.Settings.Default, "ArgJsNoMunge", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlArgJsPreserveSemicolon.DataBindings.Add(new Binding("Checked", Properties.Settings.Default, "ArgJsPreserveSemicolon", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlArgJsDisableOptimizations.DataBindings.Add(new Binding("Checked", Properties.Settings.Default, "ArgJsDisableOptimizations", true, DataSourceUpdateMode.OnPropertyChanged));

            this.CtrlOutputSeparatedFiles.DataBindings.Add(new Binding("Checked", Properties.Settings.Default, "OutputSepearatedFiles", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlOutputSepearatedFilePrefix.DataBindings.Add(new Binding("Text", Properties.Settings.Default, "OutputSepearatedFilePrefix", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlOutputSepearatedFileSuffix.DataBindings.Add(new Binding("Text", Properties.Settings.Default, "OutputSepearatedFileSuffix", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlOutputSepearatedFileOverwrite.DataBindings.Add(new Binding("Checked", Properties.Settings.Default, "OutputSepearatedFileOverwrite", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlOutputCombineFiles.DataBindings.Add(new Binding("Checked", Properties.Settings.Default, "OutputCombinedFiles", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlOutputShowAsText.DataBindings.Add(new Binding("Checked", Properties.Settings.Default, "OutputShowAsText", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlOutputShowAsTextPrettyPrint.DataBindings.Add(new Binding("Checked", Properties.Settings.Default, "OutputShowAsTextPrettyPrint", true, DataSourceUpdateMode.OnPropertyChanged));
            this.CtrlOutputEncoding.DataBindings.Add(new Binding("SelectedIndex", Properties.Settings.Default, "OutputEncoding", true, DataSourceUpdateMode.OnPropertyChanged));


            this.Log("Previous settings are loaded ...");

            if (this.CtrlCompressor.SelectedIndex == -1) this.CtrlCompressor.SelectedIndex = this.CtrlCompressor.Items.Count - 1;


            string javaPath = this.CtrlJavaPath.Text == "[Auto Detect]" ? "java" : this.CtrlJavaPath.Text;
            string error = string.Empty;
            if (!ValidateJavaExeFile(javaPath, out error))
            {
                this.UpdateCtrlGroups(false);
                MessageBox.Show(error, "Error!");
                return;
            }

            if (this.CtrlCompressor.SelectedIndex == -1)
            {
                this.UpdateCtrlGroups(false);
                MessageBox.Show("Compressor not found! Please add a compressor...", "Error!");
                return;
            }

            this.UpdateCompressionType();

            ///
            /// bind events
            ///
            this.CtrlArgType.SelectedIndexChanged += UpdateCompressionType;

            // CtrlGroupOutputOptions controls events
            this.CtrlOutputSeparatedFiles.CheckedChanged += UpdateOutputFilesControls;
            this.CtrlOutputCombineFiles.CheckedChanged += UpdateOutputFilesControls;
            this.CtrlOutputShowAsText.CheckedChanged += UpdateOutputFilesControls;
            this.CtrlOutputCombinedFilePathJs.Click += SetCombinedOutputFilePath;
            this.CtrlOutputCombinedFilePathClearJs.Click += UnsetCombinedOutputFilePath;
            this.CtrlOutputCombinedFilePathCss.Click += SetCombinedOutputFilePath;
            this.CtrlOutputCombinedFilePathClearCss.Click += UnsetCombinedOutputFilePath;

            // CtrlGroupInputFiles controls events
            this.CtrlInputFiles.SelectedIndexChanged += UpdateInputFilesControls;
            this.CtrlAddInputFiles.Click += CtrlAddInputFiles_Click;
            this.CtrlRemoveInputFiles.Click += CtrlRemoveInputFiles_Click;
            this.CtrlClearInputFiles.Click += CtrlClearInputFiles_Click;

        }


        private void Log(string text)
        {
            this.CtrlOutputConsole.AppendText(text + Environment.NewLine);
            this.CtrlOutputConsole.ScrollToCaret();
        }

        private bool AllowJsFiles()
        {
            return this.CtrlArgType.SelectedIndex == 0 || this.CtrlArgType.SelectedIndex == 1;
        }
        private bool AllowCssFiles()
        {
            return this.CtrlArgType.SelectedIndex == 0 || this.CtrlArgType.SelectedIndex == 2;
        }

        private void UpdateCtrlGroups(bool enabled)
        {
            this.CtrlGroupCompressorOptions.Enabled =
            this.CtrlGroupJavascriptOptions.Enabled =
            this.CtrlGroupOutputOptions.Enabled =
            this.CtrlGroupInputFiles.Enabled =
            this.ButtonCompress.Enabled = enabled;
        }

        private void SaveOutputFile(string file, string data)
        {
            Encoding encoding = new UTF8Encoding(false);

            if (this.CtrlOutputEncoding.SelectedIndex == 1)
            {
                encoding = new ASCIIEncoding();
            }

            using (StreamWriter sw = new StreamWriter(file, false, encoding))
            {
                sw.Write(data);
                sw.Close();
                sw.Dispose();
            }
        }

        #region Main Controls Section
        private void FormMain_Load(object sender, EventArgs e)
        {
            Init();
        }

        private void ButtonCompress_Click(object sender, EventArgs e)
        {
            this.UpdateCtrlGroups(false);
            this.FormClosing += FormMain_FormClosing;
            Properties.Settings.Default.Save();
            this.Log("Settings saved...");

            YUICompressor compressor = this.CtrlCompressor.SelectedItem as YUICompressor;
            string javaPath = this.CtrlJavaPath.Text;
            string sepearator = "".PadRight(120, '*');

            if (javaPath == "[Auto Detect]") javaPath = "java";


            string generalArgs = " --verbose";

            if (this.CtrlArgLineBreak.Value >= 0) generalArgs += " --line-break " + this.CtrlArgLineBreak.Value.ToString("G29");
            if (!string.IsNullOrEmpty(this.CtrlArgCharset.Text)) generalArgs += " --charset " + this.CtrlArgCharset.Text;

            string jsArgs = string.Empty;
            if (this.AllowJsFiles())
            {
                jsArgs += " --type js";
                if (this.CtrlArgJsNoMunge.Checked) jsArgs += " --nomunge";
                if (this.CtrlArgJsPreserveSemicolon.Checked) jsArgs += " --preserve-semi";
                if (this.CtrlArgJsDisableOptimizations.Checked) jsArgs += " --disable-optimizations";
            }

            string cssArgs = string.Empty;
            if (this.AllowCssFiles())
            {
                cssArgs += " --type css";
            }

            this.Log("Compressing started...");

            string[] result;
            string outputFile;
            FileInfo inputFile;
            List<string> jsOutputs = new List<string>();
            List<string> cssOutputs = new List<string>();
            List<string> allErrors = new List<string>();
            List<string> jsFiles = new List<string>();
            List<string> cssFiles = new List<string>();

            foreach (ListViewItem listItem in this.CtrlInputFiles.Items)
            {
                this.Log(sepearator);
                this.Log("Run: ");


                outputFile = string.Empty;
                inputFile = new FileInfo(listItem.Text);

                if (this.CtrlOutputSeparatedFiles.Checked)
                {

                    string fileNameWithoutExtension = this.CtrlOutputSepearatedFilePrefix.Text.Trim() + Path.GetFileNameWithoutExtension(inputFile.Name) + this.CtrlOutputSepearatedFileSuffix.Text.Trim();

                    outputFile =
                        inputFile.DirectoryName +
                        Path.DirectorySeparatorChar +
                        fileNameWithoutExtension +
                        inputFile.Extension;

                    if (!this.CtrlOutputSepearatedFileOverwrite.Checked && File.Exists(outputFile))
                    {
                        int outputFileIndex = 0;
                        do
                        {
                            outputFile =
                                inputFile.DirectoryName +
                                Path.DirectorySeparatorChar +
                                fileNameWithoutExtension + (++outputFileIndex) +
                                inputFile.Extension;
                        } while (File.Exists(outputFile));

                    }
                }

                if (Regex.IsMatch(inputFile.Name, JsFilePattern, RegexOptions.IgnoreCase))
                {
                    this.Log(javaPath + " -jar \"" + compressor.FileInfo.FullName + "\"" + generalArgs + jsArgs + " -o \"" + outputFile + "\"" + " \""+ inputFile.FullName + "\"");
                    result = RunJarFile(javaPath, compressor.FileInfo.FullName, generalArgs + jsArgs + " \"" + inputFile.FullName + "\"");
                    jsOutputs.Add(result[0]);
                    jsFiles.Add(inputFile.Name);
                }
                else
                {
                    this.Log(javaPath + " -jar \"" + compressor.FileInfo.FullName + "\"" + generalArgs + cssArgs + " -o \"" + outputFile + "\"" + " \"" + inputFile.FullName + "\"");
                    result = RunJarFile(javaPath, compressor.FileInfo.FullName, generalArgs + cssArgs + " \"" + inputFile.FullName + "\"");
                    cssOutputs.Add(result[0]);
                    cssFiles.Add(inputFile.Name);
                }

                

                try
                {
                    this.SaveOutputFile(outputFile, result[0]);
                }
                catch(Exception ex)
                {
                    if (string.IsNullOrEmpty(result[1]))
                    {
                        result[1] = ex.Message;
                    }
                    else
                    {
                        result[1] += ex.Message;
                    }
                }

                allErrors.Add(result[1]);
                this.Log(sepearator);

                this.Log(Environment.NewLine);
            }

            this.Log(sepearator);
            this.Log("Compiler Output: ");
            this.Log(string.Join(Environment.NewLine, allErrors.ToArray()));
            this.Log(sepearator);

            if (this.CtrlOutputCombineFiles.Checked)
            {
                if (this.CtrlOutputCombinedFilePathJs.Tag != null)
                {
                    this.SaveOutputFile(this.CtrlOutputCombinedFilePathJs.Text, string.Join(string.Empty, jsOutputs.ToArray()));
                }

                if (this.CtrlOutputCombinedFilePathCss.Tag != null)
                {
                    this.SaveOutputFile(this.CtrlOutputCombinedFilePathCss.Text, string.Join(string.Empty, cssOutputs.ToArray()));
                }
            }




            this.Log("Compressing complete...");
            this.UpdateCtrlGroups(true);
            this.FormClosing -= FormMain_FormClosing;

            if (this.CtrlOutputShowAsText.Checked)
            {
                using (FormTextOutput form = new FormTextOutput())
                {
                    string textOutput = string.Empty;
                    string textOutputHeader = string.Empty;
                    bool prettyPrint = this.CtrlOutputShowAsTextPrettyPrint.Checked;
                    if (jsFiles.Count > 0)
                    {
                        if (prettyPrint)
                        {
                            textOutputHeader = " @JavaScript files (" + string.Join(", ", jsFiles.ToArray()) + ") ";
                            textOutput += "/" + "".PadRight(textOutputHeader.Length + 2, '*') + Environment.NewLine;
                            textOutput += " *" + textOutputHeader + "*" + Environment.NewLine;
                            textOutput += " " + "".PadRight(textOutputHeader.Length + 2, '*') + "/" + Environment.NewLine;
                            textOutput += Environment.NewLine;
                        }

                        for (int i = 0; i < jsFiles.Count; i++)
                        {
                            if (prettyPrint) textOutput += "/*" + jsFiles[i] + "*/" + Environment.NewLine;
                            textOutput += jsOutputs[i] + Environment.NewLine;
                            if (prettyPrint) textOutput += Environment.NewLine;
                        }

                        form.SetJsContent(textOutput);
                    }

                    textOutput = string.Empty;
                    textOutputHeader = string.Empty;
                    if (cssFiles.Count > 0)
                    {
                        if (prettyPrint)
                        {
                            textOutputHeader = " @StyleSheet files (" + string.Join(", ", cssFiles.ToArray()) + ") ";
                            textOutput += "/" + "".PadRight(textOutputHeader.Length + 2, '*') + Environment.NewLine;
                            textOutput += " *" + textOutputHeader + "*" + Environment.NewLine;
                            textOutput += " " + "".PadRight(textOutputHeader.Length + 2, '*') + "/" + Environment.NewLine;
                            textOutput += Environment.NewLine;
                        }

                        for (int i = 0; i < cssFiles.Count; i++)
                        {
                            if (prettyPrint) textOutput += "/*" + cssFiles[i] + "*/" + Environment.NewLine;
                            textOutput += cssOutputs[i] + Environment.NewLine;
                            if (prettyPrint) textOutput += Environment.NewLine;
                        }
                        form.SetCssContent(textOutput);
                    }

                    
                    form.ShowDialog(this);
                }
            }
        }
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            MessageBox.Show("Please wait...", "Compressing...");
        }

        private void ButtonExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void CtrlCompressorOffical_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(CtrlCompressorOffical.Text);
        }
        #endregion Main Controls Section


        #region General Options Section
        private void CompressorVersionChanged(object sender, EventArgs e)
        {
            this.UpdateCtrlGroups(this.CtrlCompressor.SelectedIndex != -1);
        }
        private void CtrlCompressorAdd_Click(object sender, EventArgs e)
        {
            this.OpenFileDialog.Filter = "Executable Jar File (*.jar)|*.jar";
            this.OpenFileDialog.FileName = string.Empty;
            this.OpenFileDialog.Multiselect = false;
            if (this.OpenFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                if (!Directory.Exists(BuildsFolder))
                {
                    Directory.CreateDirectory(BuildsFolder);
                }

                FileInfo fi = new FileInfo(this.OpenFileDialog.FileName);
                string newCompressorPath = BuildsFolder + Path.DirectorySeparatorChar + fi.Name;
                File.Copy(fi.FullName, newCompressorPath, true);

                if (!this.CompressorBuilds.Exists(x => string.Compare(x.FileInfo.Name, fi.Name, true) == 0))
                {
                    this.PopulateCompressorBuilds();
                    this.CtrlCompressor.SelectedValue = newCompressorPath;
                }
            }
        }
        private void CtrlCompressorReleases_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/yui/yuicompressor/releases");
        }

        private void CtrlJavaPath_Click(object sender, EventArgs e)
        {
            this.OpenFileDialog.Filter = "Java Application (*.exe)|*.exe";
            this.OpenFileDialog.FileName = "java.exe";
            this.OpenFileDialog.Multiselect = false;
            if (this.OpenFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string error = string.Empty;
                if (!ValidateJavaExeFile(this.OpenFileDialog.FileName, out error))
                {
                    MessageBox.Show(error, "Error");
                    return;
                }

                this.CtrlJavaPath.Text = this.OpenFileDialog.FileName;
            }
        }
        private void CtrlJavaPathAutoDetect_Click(object sender, EventArgs e)
        {
            string error = string.Empty;
            if (!ValidateJavaExeFile("java", out error))
            {
                MessageBox.Show(
                    "Java is not detected!\n" +
                    "Possible Causes:\n" +
                    "1. Java is not installed on your system.\n" +
                    "2. Java is installed but version is too old. (requires Java >= 1.4)" +
                    "3. Java environment variable is not defined on your system.\n" +
                    "If java installed on your system but the path environment variable is not defined  please choose absolute path.\n" +
                    "System Error Message:\n" + error,
                    "Java is not detected!"
                );
                this.UpdateCtrlGroups(false);
                return;
            }

            this.CtrlJavaPath.Text = "[Auto Detect]";
        }
        #endregion General Options Section

        #region Compressor General Options Section
        private void UpdateCompressionType(object sender = null, EventArgs e = null)
        {
            this.CtrlGroupJavascriptOptions.Enabled = this.AllowJsFiles();
            this.UpdateInputFiles();
            this.UpdateOutputFilesControls();
        }
        #endregion Compressor General Options Section

        #region Compressor Input Files Section

        private List<string> InputFiles = new List<string>();
        private void UpdateInputFiles()
        {
            this.CtrlInputFiles.Clear();

            if (this.CtrlArgType.SelectedIndex == 0)
            {
                foreach (string item in this.InputFiles) this.CtrlInputFiles.Items.Add(item);
            }
            else
            {
                string allowedExtensionPattern = this.CtrlArgType.SelectedIndex == 1 ? JsFilePattern : CssFilePattern;

                foreach (string item in this.InputFiles)
                {
                    if (Regex.IsMatch(item, allowedExtensionPattern, RegexOptions.IgnoreCase))
                    {
                        this.CtrlInputFiles.Items.Add(item);
                    }
                }
            }

            this.UpdateInputFilesControls();
        }
        private void AddInputFile(string file, bool update = false)
        {
            if (this.InputFiles.Exists(x => string.Compare(x,file) == 0))
            {
                this.Log(file + " already exists in the list!");
            }
            else
            {
                this.InputFiles.Add(file);
                this.Log(file + " was added to the list...");
            }

            if (update) this.UpdateInputFiles();
        }
        private void AddInputFiles(string[] files)
        {
            foreach (string file in files)
            {
                this.AddInputFile(file);
            }
            this.UpdateInputFiles();
        }
        private void RemoveInputFile(string file, bool update=false)
        {
            if (this.InputFiles.Remove(file))
            {
                this.Log(file + " was removed from the list...");

                if (update) this.UpdateInputFiles();
            }
        }
        private void ClearInputFiles()
        {
            this.Log("The input file list has been cleared...");
            this.InputFiles.Clear();
            this.UpdateInputFiles();
        }


        private void UpdateInputFilesControls(object sender=null, EventArgs e=null)
        {
            int numFiles = this.CtrlInputFiles.Items.Count;
            this.CtrlRemoveInputFiles.Enabled = 
            this.CtrlClearInputFiles.Enabled = numFiles > 0;

            if (this.CtrlRemoveInputFiles.Enabled)
            {
                this.CtrlRemoveInputFiles.Enabled = this.CtrlInputFiles.SelectedItems.Count > 0; 
            }

            this.UpdateOutputFilesControls();
        }
        private void CtrlAddInputFiles_Click(object sender, EventArgs e)
        {
            this.OpenFileDialog.Filter = "All Allowed Files(*.js - *.css)|*.js;*.css|JavaScript Files (*.js)|*.js|StyleSheet Files (*.css)|*.css";
            this.OpenFileDialog.FileName = string.Empty;
            this.OpenFileDialog.Multiselect = true;
            if (this.OpenFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                foreach (string file in this.OpenFileDialog.FileNames)
                {
                    this.AddInputFile(file);
                }

                this.UpdateInputFiles();
            }
        }
        private void CtrlRemoveInputFiles_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Selected file(s) will be deleted from the list! Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (ListViewItem listViewItem in this.CtrlInputFiles.SelectedItems)
                {
                    this.RemoveInputFile(listViewItem.Text);
                }
                this.UpdateInputFiles();
            }
        }
        private void CtrlClearInputFiles_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("All files will be deleted from the list! Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this.ClearInputFiles();
            }
        }

        #endregion Compressor Input Files Section

        #region Compressor Output Files Options Section

        private void UpdateOutputFilesControls(object sender = null, EventArgs e = null)
        {
            bool hasOutputType = 
                this.CtrlOutputSeparatedFiles.Checked || 
                this.CtrlOutputShowAsText.Checked || 
                (this.CtrlOutputCombineFiles.Checked && 
                    (
                        (this.AllowJsFiles() && this.CtrlOutputCombinedFilePathJs.Tag != null ) ||
                        (this.AllowCssFiles() && this.CtrlOutputCombinedFilePathCss.Tag != null)
                    )
                );

            

            // Update separated files path options
            this.CtrlOutputSeparatedFilesOptions.Enabled = this.CtrlOutputSeparatedFiles.Checked;

            // Update combined files path options
            if (this.CtrlOutputCombineFiles.Checked)
            {
                this.CtrlOutputCombinedFilePathJs.Enabled =
                this.CtrlOutputCombinedFilePathClearJs.Enabled = this.AllowJsFiles();
                this.CtrlOutputCombinedFilePathCss.Enabled =
                this.CtrlOutputCombinedFilePathClearCss.Enabled = this.AllowCssFiles();
            }
            else
            {
                this.CtrlOutputCombinedFilePathJs.Enabled =
                this.CtrlOutputCombinedFilePathClearJs.Enabled =
                this.CtrlOutputCombinedFilePathCss.Enabled =
                this.CtrlOutputCombinedFilePathClearCss.Enabled = false;
            }

            this.CtrlOutputShowAsTextPrettyPrint.Enabled = this.CtrlOutputShowAsText.Checked;

            this.CtrlGroupInputFiles.Enabled = hasOutputType;
            this.ButtonCompress.Enabled = hasOutputType && this.CtrlInputFiles.Items.Count > 0;
        }
        private void SetCombinedOutputFilePath(object sender = null, EventArgs e = null)
        {
            Button button = sender as Button;

            if (button == this.CtrlOutputCombinedFilePathJs)
            {
                this.SaveFileDialog.Filter = "JavaScript Files (*.js)|*.js";
                this.SaveFileDialog.FileName = "all-compressed-scripts.min.js";
            }
            else if (button == this.CtrlOutputCombinedFilePathCss)
            {
                this.SaveFileDialog.Filter = "StyleSheet Files (*.css)|*.css";
                this.SaveFileDialog.FileName = "all-compressed-styles.min.css";
            }
            else
            {
                return;
            }

            if (this.SaveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                button.Tag = true;
                button.Text = this.SaveFileDialog.FileName;
                this.UpdateOutputFilesControls();
            }
        }
        private void UnsetCombinedOutputFilePath(object sender = null, EventArgs e = null)
        {
            Button button = sender as Button;
            if (sender == this.CtrlOutputCombinedFilePathClearJs)
            {
                this.CtrlOutputCombinedFilePathJs.Tag = null;
                this.CtrlOutputCombinedFilePathJs.Text = "Choose JavaScript File...";
            }
            else if (sender == this.CtrlOutputCombinedFilePathClearCss)
            {
                this.CtrlOutputCombinedFilePathCss.Tag = null;
                this.CtrlOutputCombinedFilePathCss.Text = "Choose StyleSheet File...";
            }
            else
            {
                return;
            }

            this.UpdateOutputFilesControls();
        }
        #endregion Compressor Output Files Options Section





    }
}
