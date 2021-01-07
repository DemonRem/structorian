using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;
using Structorian.Engine;
using Structorian.Properties;
using Structorian.UI;

namespace Structorian
{
    public partial class MainForm : Form
    {
        private string _structFileName;
        private StructFile _structFile;
        private DataView _dataView;
        private TextMarker _currentCellMarker;
        private Settings _settings = new Settings();
        private bool _structuresModified = false;
        private bool _settingsLoaded = false;
        
        public MainForm()
        {
            InitializeComponent();
            _dataView = new DataView(this);
            _dataView.CellSelected += _dataView_OnCellSelected;
            _dataView.Dock = DockStyle.Fill;
            splitContainer2.Panel2.Controls.Add(_dataView);
            
            Application.AddMessageFilter(new WheelMessageFilter());
            string lastStrsFile = _settings.LastStrsFile;
            if (lastStrsFile != null && lastStrsFile.Length > 0)
                LoadStructsFile(lastStrsFile);
            _structEditControl.Document.DocumentChanged += delegate { _structuresModified = true; };
            _dataView.HexDump.StatusTextChanged += (sender, e) => tslSelection.Text = e.Text;
            
            RestoreFormPosition();
            _settingsLoaded = true;
        }

        public void setFilterField(List<string> ds)
        {
            this.tsFilterField.ComboBox.DataSource = ds;
        }

        public string getFilterField()
        {
            return this.tsFilterField.Text;
        }

        public string getFilterValue()
        {
            return this.tsFilterValue.Text;
        }

        private void RestoreFormPosition()
        {
            if (_settings.MainFormMaximized)
                WindowState = FormWindowState.Maximized;
            else
            {
                if (_settings.PropertyValues ["MainFormLocation"].PropertyValue != null)
                    Location = _settings.MainFormLocation;
                if (_settings.PropertyValues["MainFormSize"].PropertyValue != null)
                    Size = _settings.MainFormSize;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CheckSaveStructures())
            {
                e.Cancel = true;
                return;
            }
            
            if (WindowState == FormWindowState.Maximized)
                _settings.MainFormMaximized = true;
            else
            {
                _settings.MainFormMaximized = false;
                _settings.MainFormLocation = Location;
                _settings.MainFormSize = Size;
            }
            _settings.Save();
        }

        private void newStructuresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckSaveStructures();
            _structEditControl.Text = "";
            SetLastStructFile(null);
        }

        private void SetLastStructFile(string file)
        {
            _structFileName = file;
            string name = (file == null) ? "<untitled>" : Path.GetFileName(file);
            Text = name + " - Structorian";
            if (_settingsLoaded)
            {
                _settings.LastStrsFile = file;
                _settings.Save();
            }
        }

        private void loadStructuresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_openStructsDialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadStructsFile(_openStructsDialog.FileName);
            }
        }

        private void LoadStructsFile(string name)
        {
            if (!CheckSaveStructures()) return;
            SetLastStructFile(Path.GetFullPath(name));
            using(Stream stream = new FileStream(name, FileMode.Open))
            {
                string strs = new StreamReader(stream).ReadToEnd();
                _structEditControl.Text = strs;
                _structEditControl.ShowEOLMarkers = false;
                _structEditControl.ShowInvalidLines = false;
                _structEditControl.ShowSpaces = false;
                _structuresModified = false;
                ParseStructures();
            }
        }

        private bool CheckSaveStructures()
        {
            if (!_structuresModified) 
                return true;
            if (_structFileName == null) 
                return true;
            
            DialogResult dr = MessageBox.Show(
                "The file " + _structFileName + " has been modified. Would you like to save the changes?",
                "Structorian",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);
            if (dr == DialogResult.Cancel) 
                return false;
            if (dr == DialogResult.Yes)
                SaveStructuresToDisk();
            return true;
        }

        private void ParseStructures()
        {
            StructParser parser = new StructParser();
            try
            {
                StructSourceContext context = new StructSourceContext();
                context.BaseDirectory = Path.GetDirectoryName(_structFileName);
                context.AddSourceText(_structFileName, _structEditControl.Text);
                _structFile = parser.LoadStructs(_structFileName, context);
            }
            catch(ParseException ex)
            {
                MessageBox.Show(this, "Error in " + ex.Position + ": " + ex.Message);
                List<ParseException> list = new List<ParseException>();
                list.Add(ex);
                HighlightErrors(list.AsReadOnly());
                return;
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, "Error loading structures: " + ex.Message);
            }
            HighlightErrors(parser.Errors);
            if (parser.Errors.Count > 0)
            {
                ParseException ex = parser.Errors[0];
                MessageBox.Show(this, "Error in " + ex.Position + ": " + ex.Message);
            }
        }

        private void _btnSaveStructures_Click(object sender, EventArgs e)
        {
            if (_structFileName == null)
            {
                if (_saveStructsDialog.ShowDialog(this) != DialogResult.OK)
                    return;
                SetLastStructFile(Path.GetFullPath(_saveStructsDialog.FileName));
            }
            SaveStructuresToDisk();
            ParseStructures();

            if (_structFile != null)
            {
                _dataView.ReloadData(s => FindMatchingStruct(s));
            }
        }

        private void SaveStructuresToDisk()
        {
            StreamWriter writer = new StreamWriter(_structFileName);
            try
            {
                writer.Write(_structEditControl.Text);
            }
            finally
            {
                writer.Close();
            }
            _structuresModified = false;
        }

        private void loadDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_structFile == null || _structFile.Structs.Count == 0) return;
            if (_openDataDialog.ShowDialog(this) == DialogResult.OK)
            {
                string fileName = _openDataDialog.FileName;
                _dataView.LoadData(Path.GetFullPath(fileName), FindMatchingStruct(fileName));
            }
        }

        private StructDef FindMatchingStruct(string fileName)
        {
            foreach(StructDef def in _structFile.Structs)
            {
                string fileMask = def.FileMask;
                if (fileMask == null)
                    continue;
                string rx = fileMask.Replace(".", "\\.").Replace("*", ".+").Replace("?", ".");
                if (new Regex(rx, RegexOptions.IgnoreCase).IsMatch(fileName))
                {
                    return def;
                }
            }
            return _structFile.Structs[0];
        }

        private void showLocalOffsetsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _dataView.ShowLocalOffsets = !_dataView.ShowLocalOffsets;
            ((ToolStripMenuItem) sender).Checked = _dataView.ShowLocalOffsets;
        }

        private void _dataView_OnCellSelected(object sender, CellSelectedEventArgs e)
        {
            TextPosition pos = e.Cell.GetStructDef().Position;
            TextPosition endPos = e.Cell.GetStructDef().EndPosition;
            IDocument doc = _structEditControl.Document;
            
            if (_currentCellMarker != null)
                doc.MarkerStrategy.RemoveMarker(_currentCellMarker);
            int offset = doc.PositionToOffset(new Point(pos.Col, pos.Line-1));
            int endOffset = doc.PositionToOffset(new Point(endPos.Col, endPos.Line - 1));
            if (offset != endOffset)
            {
                _currentCellMarker = new TextMarker(offset, endOffset - offset, TextMarkerType.SolidBlock,
                                                    Color.LightSkyBlue);
                doc.MarkerStrategy.AddMarker(_currentCellMarker);
            }
            _structEditControl.ActiveTextAreaControl.ScrollTo(pos.Line-1);
            _structEditControl.Refresh();
        }

        private void HighlightErrors(ReadOnlyCollection<ParseException> exceptions)
        {
            IDocument doc = _structEditControl.Document;
            doc.MarkerStrategy.RemoveAll(m => m.TextMarkerType == TextMarkerType.WaveLine);
            foreach(ParseException ex in exceptions)
            {
                int offset = doc.PositionToOffset(new Point(ex.Position.Col, ex.Position.Line - 1));
                TextMarker marker = new TextMarker(offset, ex.Length, TextMarkerType.WaveLine, Color.Red);
                marker.ToolTip = ex.Message;
                doc.MarkerStrategy.AddMarker(marker);
            }
            if (exceptions.Count > 0)
            {
                TextPosition pos = exceptions [0].Position;
                _structEditControl.ActiveTextAreaControl.Caret.Position = new Point(pos.Col, pos.Line-1);
            }
            _structEditControl.Refresh();
        }

        private void miSaveAllBlobs_Click(object sender, EventArgs e)
        {
            if (_targetDialog.ShowDialog(this) != DialogResult.OK) return;
            var action = new SaveAllBlobsAction(_dataView.ActiveInstanceTree, _targetDialog.SelectedPath);
            RunBackgroundAction(action, null);
        }

        private void RunBackgroundAction(StructorianAction action, Action callback)
        {
            tslProgress.Text = action.Text;
            backgroundWorker1.RunWorkerAsync(new BackgroundTask(action, callback));
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            ((BackgroundTask) e.Argument).Action.Run();
            e.Result = ((BackgroundTask) e.Argument).Callback;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            _dataView.FlushPendingNodes();
            if (e.Result != null)
                ((Action) e.Result).Invoke();
            tslProgress.Text = "";
        }

        private void miFindStructure_Click(object sender, EventArgs e)
        {
            var dlg = new FindStructureDialog(_structFile);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;
            var action = new FindStructuresAction(_dataView.InstanceTrees, dlg.SelectedDef, dlg.Expression);
            RunBackgroundAction(action, () => _dataView.ShowSearchResults(action.Results));
        }

        private class WheelMessageFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                return false;
            }
        }

        private class BackgroundTask
        {
            public StructorianAction Action { get; set; }
            public Action Callback { get; set; }

            public BackgroundTask(StructorianAction action, Action callback)
            {
                Action = action;
                Callback = callback;
            }
        }

        private void saveDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this._dataView._listPatchData == null || this._dataView._listPatchData.Count <= 0)
            {
                MessageBox.Show("Nothing to patch");
                return;
            }
            // Backup
            File.Copy(this._dataView.GetFullPath(), this._dataView.GetFullPath() + ".BAK", true);
            Stream stream = _dataView.GetStream();
            using (BinaryWriter binWriter = new BinaryWriter(File.Open(this._dataView.GetFullPath() + ".patched", FileMode.Create)))
            { 
                byte[] byteArray = this.ReadToEnd(stream);
                byte[] buffer;
                foreach (PatchData patchData in this._dataView._listPatchData.Values)
                {
                    buffer = Utils.ToBytes(patchData._dataValue);
                    int j = 0;
                    for (int i = (int) patchData._offset; i < patchData._offset + buffer.Length; i++)
                    {
                        byteArray[i] = buffer[j++];
                    }
                }
                binWriter.Write(byteArray);
            }
            MessageBox.Show("Saved");
        }

        private byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(((StructInstance)this._dataView.GetInstanceTree().Children[0])._checksum.ToString());
        }
    }
}