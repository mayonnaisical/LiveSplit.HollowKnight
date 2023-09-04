﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
namespace LiveSplit.HollowKnight {
    public partial class HollowKnightSettings : UserControl {
        public List<SplitName> Splits { get; private set; }
        private bool isLoading;
        private List<string> availableSplits = new List<string>();
        private List<string> availableSplitsAlphaSorted = new List<string>();
        private Label startTriggerSeparator = new Label();

        public HollowKnightSettings() {
            isLoading = true;
            InitializeComponent();
            string version = typeof(HollowKnightComponent).Assembly.GetName().Version.ToString();
#if DEBUG
            version += "-dev";
#endif
            this.versionLabel.Text = "Autosplitter Version: " + version;

            Splits = new List<SplitName>();
            isLoading = false;
        }

        public bool HasSplit(SplitName split) {
            return Splits.Contains(split);
        }

        private void Settings_Load(object sender, EventArgs e) {
            LoadSettings();
        }

        public void LoadSettings() {
            isLoading = true;
            this.flowMain.SuspendLayout();

            for (int i = flowMain.Controls.Count - 1; i > 0; i--) {
                flowMain.Controls.RemoveAt(i);
            }

            foreach (SplitName split in Splits) {
                MemberInfo info = typeof(SplitName).GetMember(split.ToString())[0];
                DescriptionAttribute description = (DescriptionAttribute)info.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];

                HollowKnightSplitSettings setting = new HollowKnightSplitSettings();
                setting.cboName.DataSource = new List<string>() { description.Description };
                setting.cboName.Enabled = false;
                setting.cboName.Text = description.Description;
                AddHandlers(setting);

                flowMain.Controls.Add(setting);
            }

            AddStartTriggerSeparator();

            isLoading = false;
            this.flowMain.ResumeLayout(true);
        }

        private void AddStartTriggerSeparator() {
            // horizontal rule (https://stackoverflow.com/a/3296161)
            this.startTriggerSeparator.AccessibleName = "Start trigger separator";
            this.startTriggerSeparator.Text = string.Empty;
            this.startTriggerSeparator.BorderStyle = BorderStyle.Fixed3D;
            this.startTriggerSeparator.AutoSize = false;
            this.startTriggerSeparator.Height = 2;
            this.flowMain.Controls.Add(this.startTriggerSeparator);
            PositionStartTriggerSeparator();
        }

        private void PositionStartTriggerSeparator() {
            this.flowMain.Controls.SetChildIndex(this.startTriggerSeparator, 2);
        }

        private void AddHandlers(HollowKnightSplitSettings setting) {
            setting.cboName.SelectedIndexChanged += new EventHandler(ControlChanged);
            setting.btnRemove.Click += new EventHandler(btnRemove_Click);
            setting.btnEdit.Click += new EventHandler(btnEdit_Click);
            setting.btnAddAbove.Click += new EventHandler(btnAddAbove_Click);
            setting.btnAddBelow.Click += new EventHandler(btnAddBelow_Click);
        }
        private void RemoveHandlers(HollowKnightSplitSettings setting) {
            setting.cboName.SelectedIndexChanged -= ControlChanged;
            setting.btnRemove.Click -= btnRemove_Click;
            setting.btnEdit.Click -= btnEdit_Click;
            setting.btnAddAbove.Click -= btnAddAbove_Click;
            setting.btnAddBelow.Click -= btnAddBelow_Click;
        }
        public void btnRemove_Click(object sender, EventArgs e) {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--) {
                if (flowMain.Controls[i].Contains((Control)sender)) {
                    RemoveHandlers((HollowKnightSplitSettings)((Button)sender).Parent);

                    flowMain.Controls.RemoveAt(i);
                    break;
                }
            }
            UpdateSplits();
        }
        public void btnEdit_Click(object sender, EventArgs e) {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--) {
                if (flowMain.Controls[i].Contains((Control)sender)) {
                    HollowKnightSplitSettings setting = (HollowKnightSplitSettings)((Button)sender).Parent;
                    if (setting.cboName.Enabled) {
                        disableEdit(setting);
                    } else {
                        enableEdit(setting);
                    }
                    break;
                }
            }
        }
        private void btnAddAbove_Click(object sender, EventArgs e) {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--) {
                if (flowMain.Controls[i].Contains((Control)sender)) {
                    HollowKnightSplitSettings setting = (HollowKnightSplitSettings)((Button)sender).Parent;
                    int index = setting.Parent.Controls.GetChildIndex(setting);
                    addSplitAtIndex(index);
                }
            }
        }
        private void btnAddBelow_Click(object sender, EventArgs e) {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--) {
                if (flowMain.Controls[i].Contains((Control)sender)) {
                    HollowKnightSplitSettings setting = (HollowKnightSplitSettings)((Button)sender).Parent;
                    int index = setting.Parent.Controls.GetChildIndex(setting);
                    addSplitAtIndex(index + 1);
                }
            }
        }
        private void enableEdit(HollowKnightSplitSettings setting) {
            string currentText = setting.cboName.Text;
            setting.btnEdit.Text = "✔";
            setting.cboName.DataSource = GetAvailableSplits();
            setting.cboName.Text = currentText;
            setting.cboName.Enabled = true;
        }
        private void disableEdit(HollowKnightSplitSettings setting) {
            setting.btnEdit.Text = "✏";
            setting.cboName.Enabled = false;
        }
        public void ControlChanged(object sender, EventArgs e) {
            UpdateSplits();
        }
        public void UpdateSplits() {

            if (isLoading) return;

            Splits.Clear();

            foreach (Control c in flowMain.Controls) {
                if (c is HollowKnightSplitSettings) {
                    HollowKnightSplitSettings setting = (HollowKnightSplitSettings)c;
                    if (!string.IsNullOrEmpty(setting.cboName.Text)) {
                        SplitName split = HollowKnightSplitSettings.GetSplitName(setting.cboName.Text);
                        Splits.Add(split);
                    }
                }
            }
            PositionStartTriggerSeparator();
        }
        public XmlNode UpdateSettings(XmlDocument document) {

            XmlElement xmlSettings = document.CreateElement("Settings");

            // TODO: have livesplit automatically change starting and ending split to just another split

            /*
            XmlElement xmlAutosplitEndRuns = document.CreateElement("AutosplitEndRuns");
            xmlAutosplitEndRuns.InnerText = AutosplitEndRuns.ToString();
            xmlSettings.AppendChild(xmlAutosplitEndRuns);

            XmlElement xmlAutosplitStartRuns = document.CreateElement("AutosplitStartRuns");
            xmlAutosplitStartRuns.InnerText = AutosplitStartRuns.ToString();
            xmlSettings.AppendChild(xmlAutosplitStartRuns);
            */

            XmlElement xmlSplits = document.CreateElement("Splits");
            xmlSettings.AppendChild(xmlSplits);

            foreach (SplitName split in Splits) {
                XmlElement xmlSplit = document.CreateElement("Split");
                xmlSplit.InnerText = split.ToString();

                xmlSplits.AppendChild(xmlSplit);
            }

            return xmlSettings;
        }
        public void SetSettings(XmlNode settings) {

            Splits.Clear(); // reset the splits

            XmlNodeList splitNodes = settings.SelectNodes(".//Splits/Split");
            foreach (XmlNode splitNode in splitNodes) {
                SplitName split = HollowKnightSplitSettings.GetSplitName(splitNode.InnerText);
                Splits.Add(split);
            }

            // legacy auto-start and auto-end splits

            XmlNode AutosplitStartRunsNode = settings.SelectSingleNode(".//AutosplitStartRuns"); // will be null if it's the new version (or really really old version, that'll be broken i guess)
            if (AutosplitStartRunsNode != null) { // if it's the an old .lss file
                string splitDescription = AutosplitStartRunsNode.InnerText.Trim();

                // if there's an explicit auto-start split, add it to the list, if not, add legacy start
                if (!string.IsNullOrEmpty(splitDescription)) {
                    cboStartTriggerName.DataSource = GetAvailableSplits();
                    Splits.Insert(0, HollowKnightSplitSettings.GetSplitName(splitDescription));
                } else { // implicit auto-start in .lss, add it to the list
                    Splits.Insert(0, SplitName.LegacyStart); // add the legacy start split for now
                }

            }

            XmlNode AutosplitEndRunsNode = settings.SelectSingleNode(".//AutosplitEndRuns"); // will be null if it's the new version... same as above
            if (AutosplitEndRunsNode != null) { // if it's an old .lss file

                // check if autoend split
                bool isAutosplitEndRuns = false;
                bool.TryParse(AutosplitEndRunsNode.InnerText, out isAutosplitEndRuns);

                // if there's an explicit auto-end, leave it, if there's not, add legacy ending split to list
                if (!isAutosplitEndRuns) {
                    Splits.Add(SplitName.LegacyEnd);
                }

            }

            return;

        }

        private HollowKnightSplitSettings createSetting() {
            HollowKnightSplitSettings setting = new HollowKnightSplitSettings();
            List<string> splitNames = GetAvailableSplits();
            setting.cboName.DataSource = splitNames;
            setting.cboName.Text = splitNames[0];
            setting.btnEdit.Text = "✔";
            AddHandlers(setting);
            return setting;
        }
        private void btnAddSplit_Click(object sender, EventArgs e) {
            HollowKnightSplitSettings setting = createSetting();
            flowMain.Controls.Add(setting);
            UpdateSplits();
        }
        private void addSplitAtIndex(int index) {
            HollowKnightSplitSettings setting = createSetting();
            flowMain.Controls.Add(setting);
            flowMain.Controls.SetChildIndex(setting, index);
            UpdateSplits();
        }
        private List<string> GetAvailableSplits() {
            if (availableSplits.Count == 0) {
                foreach (SplitName split in Enum.GetValues(typeof(SplitName))) {
                    MemberInfo info = typeof(SplitName).GetMember(split.ToString())[0];
                    DescriptionAttribute description = (DescriptionAttribute)info.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
                    availableSplits.Add(description.Description);
                    availableSplitsAlphaSorted.Add(description.Description);
                }
                availableSplitsAlphaSorted.Sort(delegate (string one, string two) {
                    return one.CompareTo(two);
                });
            }
            return rdAlpha.Checked ? availableSplitsAlphaSorted : availableSplits;
        }
        private void radio_CheckedChanged(object sender, EventArgs e) {
            foreach (Control c in flowMain.Controls) {
                if (c is HollowKnightSplitSettings) {
                    HollowKnightSplitSettings setting = (HollowKnightSplitSettings)c;
                    if (setting.cboName.Enabled) {
                        string text = setting.cboName.Text;
                        setting.cboName.DataSource = GetAvailableSplits();
                        setting.cboName.Text = text;
                    }
                }
            }
        }
        private void flowMain_DragDrop(object sender, DragEventArgs e) {
            UpdateSplits();
        }
        private void flowMain_DragEnter(object sender, DragEventArgs e) {
            e.Effect = DragDropEffects.Move;
        }
        private void flowMain_DragOver(object sender, DragEventArgs e) {
            HollowKnightSplitSettings data = (HollowKnightSplitSettings)e.Data.GetData(typeof(HollowKnightSplitSettings));
            FlowLayoutPanel destination = (FlowLayoutPanel)sender;
            Point p = destination.PointToClient(new Point(e.X, e.Y));
            var item = destination.GetChildAtPoint(p);
            int index = destination.Controls.GetChildIndex(item, false);
            if (index == 0) {
                e.Effect = DragDropEffects.None;
            } else {
                e.Effect = DragDropEffects.Move;
                int oldIndex = destination.Controls.GetChildIndex(data);
                if (oldIndex != index) {
                    enableEdit(data);
                    destination.Controls.SetChildIndex(data, index);
                    destination.Invalidate();
                }
            }
            PositionStartTriggerSeparator();
        }

        private void cboStartTriggerName_SelectedIndexChanged(object sender, EventArgs e) {
            UpdateSplits();
        }
    }
}
