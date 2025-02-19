﻿using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using static HuaweiUnlocker.FlashTool.FlashToolQClegacy;
using static HuaweiUnlocker.LangProc;
using System.ComponentModel;
using System.Net;
using Ionic.Zip;
using HuaweiUnlocker.DIAGNOS;
using System.Linq;
using HuaweiUnlocker.FlashTool;
using HuaweiUnlocker.TOOLS;

namespace HuaweiUnlocker
{

    public partial class Window : Form
    {
        private static string device;
        private static string loader;
        public static string Path;
        public static DIAG diag = new DIAG();
        public static HISI HISI = new HISI();
        private Dictionary<string, string> source = new Dictionary<string, string>();
        private Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\4PDA_HUAWEI_UNLOCK", true);
        private static string tempsel;
        public Window()
        {
            InitializeComponent();
            foreach (var process in Process.GetProcessesByName("emmcdl.exe")) { process.Kill(); }
            foreach (var process in Process.GetProcessesByName("fh_loader.exe")) { process.Kill(); }
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            if (!Directory.Exists("UnlockFiles")) Directory.CreateDirectory("UnlockFiles");
            if (!Directory.Exists("LOGS")) Directory.CreateDirectory("LOGS");

            foreach (string i in Directory.GetFiles("Languages", "*.ini"))
                LBOX.Items.Add(i.Split('\\').Last().Replace(".ini", ""));

            if (key == null)
            {
                key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\4PDA_HUAWEI_UNLOCK");
                key.SetValue("LANGUAGE", "English");
                key.SetValue("DEBUG", true);
                LBOX.SelectedItem = "English";
            }

            LBOX.SelectedItem = CURRENTlanguage = key.GetValue("LANGUAGE").ToString();
            DBB.Checked = debug = bool.Parse(key.GetValue("DEBUG").ToString());

            LOGGBOX = LOGGER;
            progr = PGG;

            Lang();
            //DEVICE LIST FROM WEB
            try
            {
                WebClient client = new WebClient();
                StreamReader readerD = new StreamReader(client.OpenRead("http://igriastranomier.ucoz.ru/hwlock/devices.txt"));
                string line = readerD.ReadLine();
                while ((line = readerD.ReadLine()) != null)
                {
                    if (!line.StartsWith("[") && !String.IsNullOrEmpty(line) && !line.StartsWith("//") && !line.StartsWith("#"))
                    {
                        string[] a = line.Split(' ');
                        if (!a[0].StartsWith("KIRIN"))
                            DEVICER.Items.Add(a[0]);
                        else
                            HISIbootloaders.Items.Add(a[0]);
                        source.Add(a[0], a[1]);
                    }
                }
                client.Dispose();
            }
            catch
            {
                if (Directory.Exists("Languages"))
                    LOG(E("WebCon"));
                else
                    throw new Exception("NO LANGUAGE FILE!!!");
            }
            foreach (var a in Directory.GetDirectories("UnlockFiles"))
            {
                string folderDEV = a.Split('\\').Last();
                if (!folderDEV.StartsWith("KIRIN"))
                {
                    if (!DEVICER.Items.Contains(folderDEV))
                        DEVICER.Items.Add(folderDEV);
                }
                else
                {
                    if (!HISIbootloaders.Items.Contains(folderDEV))
                        HISIbootloaders.Items.Add(folderDEV);
                }
            }
            Path = "UnlockFiles\\" + DEVICER.Text.ToUpper();
            if (!Directory.Exists(Path)) BoardU.Text = L("DdBtn"); else BoardU.Text = L("DdBtnE");
            foreach (var a in Directory.GetDirectories(Directory.GetCurrentDirectory() + "\\qc_boot"))
            {
                String[] es = a.Split('\\');
                LoaderBox.Items.Add(es[es.Length - 1]);
            }
            LangProc.Tab = Tab;
        }
        public void Lang()
        {
            ReadLngFile();
            //QUALCOMM AND BASIC
            nButton2.Text = SelectLOADER.Text = Selecty2.Text = Selecty3.Text = L("SelBtn");
            EraseMeBtn.Text = L("ErasePM");
            AutoXml.Text = AutoLdr.Text = L("AutoLBL"); ;
            Flash.Text = L("FlBtn");
            DUMPALL.Text = L("DuBtn");
            GPfir.Text = L("SelPathToFGB");
            RdGPT.Text = L("RdGPTBtn");
            ReadPA.Text = L("ReadPA");
            WritePA.Text = L("WritePA");
            ErasePA.Text = L("ErasePA");
            EraseDA.Text = L("EraseDA");
            UnlockFrp.Text = L("UnlockBTN");
            HomeTag.Text = L("HomeTag");
            BackupRestoreTag.Text = L("BackupRestoreTag");
            UnlockTag.Text = L("UnlockTag");
            GPTtag.Text = L("GPTtag");
            HISItag.Text = L("UnlockTagHISI");
            GLOADER.Text = L("LoaderHeader");
            PTOFIRM.Text = L("PathToFirmLBL");
            RAW.Text = L("RWIMGlbl");
            SLDEV.Text = L("SELDEVlbl");

            Tab.TabPages[0].Text = L("HomeTag");
            Tab.TabPages[1].Text = L("BackupRestoreTagSimpl");
            Tab.TabPages[2].Text = L("UnlockSimpl");
            Tab.TabPages[3].Text = L("GPTtagSimpl");
            Tab.TabPages[4].Text = L("DiagTagSimpl");
            Tab.TabPages[5].Text = L("UnlockSimplHISI");

            PARTLIST.Columns[0].HeaderText = L("NameTABLE0");
            PARTLIST.Columns[1].HeaderText = L("NameTABLE1");
            PARTLIST.Columns[2].HeaderText = L("NameTABLE2");

            groupBox2.Text = DevInfoQCBox.Text = L("DeviceInfoTag");
            TUTR2.Text = L("Tutr2");
            ACTBOX.Text = L("Action");
            RDinf.Text = L("DiagTagRead");
            UpgradMDbtn.Text = L("DiagTagUpgradeMode");
            ReBbtn.Text = L("DiagTagReboot");
            FrBTN.Text = L("DiagTagFactoryReset");
            //HISI TEXT
            CpuHISIBox.Text = L("HISISelectCpu");
            RdHISIinfo.Text = L("HISIReadFB");
            HISI_board_FB.Text = L("HISIWriteKirinFB");
            UNLOCKHISI.Text = L("HISIWriteKirinBLD");
            FBLstHISI.Text = L("HISIWriteKirinFBL");

            Path = "UnlockFiles\\" + DEVICER.Text.ToUpper();
            if (!Directory.Exists(Path)) BoardU.Text = L("DdBtn"); else BoardU.Text = L("DdBtnE");
            DBB.Text = LangProc.L("DebugLbl");
            LOGGBOX.Text = "Version 12.1F BETA/n(C) MOONGAMER (QUALCOMM UNLOCKER)/n(C) MASHED-POTATOES (KIRIN UNLOCKER)".Replace("/n", Environment.NewLine);
            LOG(I("SMAIN1"));
            LOG(I("SMAIN2"));
            LOG(I("SMAIN3"));
            LOG(I("Tutr"));

            LOG(I("MAIN1"));
            LOG(I("MAIN2"));
            LOG(I("MAIN3"));
            LOG(I("Tutr"));
        }
        private void LOADER_PATH(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = PrevFolder;
            openFileDialog.Filter = "Programmer files (*.mbn;*.elf;*.hex)|*.mbn;*.elf;*.hex|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK) PrevFolder = LoaderBox.Text = openFileDialog.FileName;
        }
        private void XML_PATH(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = PrevFolder;
            openFileDialog.Filter = "Sectors data files (*.xml;*.txt)|*.xml;*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK) PrevFolder = Xm.Text = openFileDialog.FileName;
        }

        private void Flash_Click(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            if (String.IsNullOrEmpty(pather.Text) || !Directory.Exists(pather.Text) && !File.Exists(pather.Text))
            {
                LOG(E("NoFirmPath"));
                return;
            }
            if (!CheckDevice(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text)) return;
            progr.Value = 0;
            if (Xm.Text.Length < 5 && !RAW.Checked)
            {
                LOG(E("ErrXML"));
                return;
            }
            if (pather.Text.Length < 5 && RAW.Checked)
            {
                LOG(E("ErrBin"));
                return;
            }
            if (!RAW.Checked)
            {
                if (!FlashPartsXml(Xm.Text, PatXm.Text, AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text, pather.Text))
                    LOG(E("ErrXML2"));
                else
                    LOG(I("Flashing") + " " + pather.Text);
            }
            else
            {
                if (!FlashPartsRaw(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text, pather.Text))
                    LOG(E("ErrBin2"));
                else
                    LOG(I("Flashing2") + pather.Text);
            }

            progr.Value = 100;
        }

        private void PATHTOFIRMWARE_Clck(object sender, EventArgs e)
        {
            if (!RAW.Checked)
            {
                FolderBrowserDialog openFileDialog = new FolderBrowserDialog();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pather.Text = openFileDialog.SelectedPath;
                    if (AutoXml.Checked)
                    {
                        foreach (var a in Directory.GetFiles(openFileDialog.SelectedPath))
                        {
                            if (AutoXml.Checked && a.EndsWith(".xml"))
                            {
                                if (a.Contains("rawprogram"))
                                    Xm.Text = a;
                                if (a.Contains("patch"))
                                    PatXm.Text = a;
                            }
                            if (AutoLdr.Checked && a.EndsWith(".mbn") || a.EndsWith(".elf") || a.EndsWith(".hex")) LoaderBox.Text = a;
                        }
                    }
                }
            }
            else
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = PrevFolder;
                openFileDialog.Filter = "Sector DUMP files (*.img;*.bin)|*.img;*.bin;*.emmc|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK) PrevFolder = pather.Text = openFileDialog.FileName;
            }
        }

        private void DumpALL_CLK(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            if (!CheckDevice(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text)) return;
            progr.Value = 0;

            FolderBrowserDialog openFileDialog = new FolderBrowserDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                pather.Text = openFileDialog.SelectedPath + "\\DUMP.APP";
                if (!Dump(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text, pather.Text))
                    LOG("ERROR: Failed Dump All!");
                else
                    LOG(I("Dumping") + pather.Text);
            }

            progr.Value = 100;
        }

        private void AutoLdr_CheckedChanged(object sender, EventArgs e)
        {
            SelectLOADER.Enabled = AutoLdr.Checked;
        }

        private void RdGPT_Click(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            if (!CheckDevice(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text)) return;
            LOG(I("ReadGPT"));
            GPTTABLE = new Dictionary<string, int[]>();
            bool gpt = ReadGPT(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text);
            if (gpt)
            {
                foreach (var obj in GPTTABLE) PARTLIST.Rows.Add(obj.Key, obj.Value[0], obj.Value[1]);
                PARTLIST.AutoResizeRows();
                LOG(I("SUCC_ReadGPT"));
                RdGPT.Visible = false;
                RdGPT.Enabled = false;
                progr.Value = 100;
                return;
            }
            else
                LOG(I("ERR_ReadGPT"));
            RdGPT.Visible = true;
            RdGPT.Enabled = true;
        }

        private void PARTLIST_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (PARTLIST.Rows.Count > 0)
            {
                PARTLIST.Enabled = false;
                WHAT.Enabled = true;
                WHAT.Visible = true;
                string partition = PARTLIST.CurrentRow.Cells[0].Value.ToString();
                WHAT.Text = L("Action") + partition;
                tempsel = partition;
                LOG(I("sl") + partition);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            WHAT.Enabled = false;
            PARTLIST.Enabled = true;
            WHAT.Visible = false;
        }

        private void ERASEevent_Click(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            if (!CheckDevice(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text)) return;
            DialogResult dialogResult = System.Windows.Forms.MessageBox.Show(L("AreY") + tempsel, L("CZdmg2"), MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (Erase(tempsel, AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text))
                    LOG(I("ErPS") + tempsel);
                else
                    LOG(E("ErPE") + tempsel);
                progr.Value = 100;
            }
        }

        private void WRITEevent_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            if (file.ShowDialog() == DialogResult.OK)
            {
                if (Write(tempsel, AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text, file.FileName))
                    LOG(I("EwPS") + tempsel + newline);
                else
                    LOG(E("EwPE") + tempsel);
                progr.Value = 100;
            }
        }

        private void READevent_Click_1(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            if (folder.ShowDialog() == DialogResult.OK)
            {
                int i = GPTTABLE[tempsel][0];
                int j = GPTTABLE[tempsel][1];
                if (Dump(i, j, tempsel, AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text, folder.SelectedPath))
                    LOG(I("EdPS") + tempsel + newline);
                else
                    LOG(E("EdPE") + tempsel);
                progr.Value = 100;
            }
        }

        private void DEVICER_SelectedIndexChanged(object sender, EventArgs e)
        {
            Path = "UnlockFiles\\" + DEVICER.Text.ToUpper();
            if (!Directory.Exists(Path)) BoardU.Text = L("DdBtn"); else BoardU.Text = L("DdBtnE");
        }

        private void ISAS2(object sender, EventArgs e)
        {
            if (AutoLdr.Checked)
            {
                foreach (var a in Directory.GetDirectories(Directory.GetCurrentDirectory() + "\\qc_boot"))
                {
                    String[] es = a.Split('\\');
                    LoaderBox.Items.Add(es[es.Length - 1]);
                }
                LoaderBox.DropDownStyle = ComboBoxStyle.DropDownList;
            }
            else
            {
                LoaderBox.Text = "";
                LoaderBox.Items.Clear();
                LoaderBox.DropDownStyle = ComboBoxStyle.DropDown;
            }
            SelectLOADER.Enabled = !AutoLdr.Checked;
        }
        private void UnZip(string zipFile, string folderPath)
        {
            ZipFile.Read(zipFile).ExtractAll(folderPath, ExtractExistingFileAction.OverwriteSilently);
        }
        private void QCDownloaded(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LOG(E("FailCon") + Environment.NewLine + L("Error") + e.Error);
                return;
            }
            LOG(I("Downloaded") + DEVICER.Text.ToUpper() + ".zip");
            UnZip(DEVICER.Text.ToUpper() + ".zip", "UnlockFiles\\" + device);
            UNLBTN_Click(sender, e);
        }
        private void HISIDownloaded(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LOG(E("FailCon") + Environment.NewLine + L("Error") + e.Error);
                return;
            }
            LOG(I("Downloaded") + HISIbootloaders.Text.ToUpper() + ".zip");
            UnZip(HISIbootloaders.Text.ToUpper() + ".zip", "UnlockFiles\\" + device);
            UNLOCKHISI_Click(sender, e);
        }
        private void Erasda_Click(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            if (!CheckDevice(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text)) return;
            progr.Value = 0;

            LOG(I("CheckCon"));
            loader = PickLoader(DEVICER.Text.ToUpper().Split('-')[0]);
            LOG(I("EraserD"));
            if (!Erase("userdata", loader))
                LOG(E("FailUsrData"));

            else LOG(I("Success"));

            progr.Value = 100;
        }
        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = e.BytesReceived;
            double totalBytes = e.TotalBytesToReceive;
            double percentage = bytesIn / totalBytes * 100;
            progr.Value = (int)percentage;
        }
        private void UNLBTN_Click(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            progr.Value = 0;
            device = DEVICER.Text.ToUpper();
            Path = "UnlockFiles\\" + device;
            if (!DEVICER.Text.Contains("-")) { LOG(I("SelDev")); return; }
            loader = AutoLdr.Checked ? PickLoader(DEVICER.Text.Split('-')[0]) : LoaderBox.Text;

            if (!Directory.Exists(Path))
            {
                progr.Value = 1;
                LOG(I("DownloadFor") + device);
                LOG("URL: " + source[device]);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(QCDownloaded);
                client.DownloadFileAsync(new Uri(source[device]), device + ".zip");
                BoardU.Text = L("DdBtnE");
                Tab.Enabled = false;
                return;
            }

            if (!CheckDevice(loader)) { Tab.Enabled = true; return; }

            LOG(I("SendingCmd"));
            if (!Unlock(device, loader, Path))
                LOG(E("FailUnl"));
            else
                LOG(I("PrcsUnl"));

            progr.Value = 100;

        }

        private void UnlockFrp_Click(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            if (!CheckDevice(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text)) return;
            progr.Value = 0;
            device = DEVICER.Text.ToUpper();
            LOG(I("CheckCon"));
            loader = PickLoader(device.Split('-')[0]);

            LOG(I("SendingCmd"));
            if (!UnlockFrp(loader))
                LOG(E("FailFrp"));
            else
                LOG(I("Success"));

            progr.Value = 100;
        }
        private bool Find()
        {
            Port_D data = GETPORT("android adapter pcui");
            if (diag.PCUI != data.ComName)
            {
                diag.PCUI = data.ComName;
                LOG(data.ComName != "NaN" ? L("Info") + "PCUI PORT: " + data.DeviceName : L("Error") + "PCUI PORT not found");
            }

            data = GETPORT("dbadapter reserved interface");
            if (diag.DBDA != data.ComName)
            {
                diag.DBDA = data.ComName;
                LOG(data.ComName != "NaN" ? L("Info") + "DBADAPTER PORT: " + data.DeviceName : L("Error") + "DBADAPTER PORT not found");
            }

            return diag.DBDA != "NaN" && diag.PCUI != "NaN";
        }
        private void ReadINFOdiag_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Find()) return;
                diag.HACKdbPort();
                LOG(I("TrDaI"));
                if (diag.DBDA != "")
                {
                    IMEIbox.Text = diag.GET_IMEI1();

                    string[] DATA = diag.GET_FIRMWAREINFO();
                    SNbox.Text = DATA[0];
                    BIDbox.Text = DATA[1];

                    DATA = diag.GET_BOARDINFO();
                    CHIPbox.Text = DATA[0];
                    VERbox.Text = DATA[1];
                }
            }
            catch { }
        }

        private void RB_Click(object sender, EventArgs e)
        {
            if (!Find()) return;
            LOG(I("TrRb") + "system");
            diag.REBOOT();
        }

        private void RecoveryBTN_Click(object sender, EventArgs e)
        {
            if (!Find()) return;
            LOG(I("TrRb") + "UpgradeMode (Recovery 3 point)");
            OpenFileDialog f = new OpenFileDialog();

            if (f.ShowDialog() == DialogResult.OK)
                diag.To_Three_Recovery(f.FileName, CPUbox.Text);
            else
                diag.To_Three_Recovery("", "");
        }

        private void TryAUTH_CLCK(object sender, EventArgs e)
        {
            if (!Find()) return;
            LOG("TRYING TO AUTH PHONE FUCK THE HW: !!!BETA TEST NOT WORKING!!!");

        }
        private void FlashF_Click(object sender, EventArgs e)
        {
            if (!Find()) return;
            byte[] msg = diag.DIAG_SEND(CMD.Text, "", 0, true, true);
            CMD.Text = CRC.HexDump(msg) + Environment.NewLine + (CMDS.GetStatus(msg) ? "ACTION: SUCCESSFULL" : "ACTION: UNKNOWN CMD OR ACCESS DANIED");
        }
        private void BURGBTN_Click(object sender, EventArgs e)
        {
            if (BURG.MaximumSize.Width > BURG.Size.Width)
                while (BURG.MaximumSize.Width > BURG.Size.Width)
                {
                    BURG.Width += 1;
                    BURGBTN.Width += 1;
                }
            else if (BURG.MinimumSize.Width < BURG.Size.Width)
                while (BURG.MinimumSize.Width < BURG.Size.Width)
                {
                    BURG.Width -= 3;
                    BURGBTN.Width -= 3;
                }
        }

        private void RAW_CheckedChanged(object sender, EventArgs e)
        {
            Selecty2.Visible = !RAW.Checked;
            AutoXml.Enabled = !RAW.Checked;
            Selecty2.Enabled = !RAW.Checked;
            DETECTED.Enabled = !RAW.Checked;
            Xm.Enabled = !RAW.Checked;
        }

        private void AutoXml_CheckedChanged(object sender, EventArgs e)
        {
            Selecty2.Enabled = !AutoXml.Checked;
        }
        private void OnApplicationExit(object sender, EventArgs e)
        {
            if (!File.Exists("log.txt")) return;
            File.Copy("log.txt", "LOGS\\" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "=LOG.txt", true);
        }
        private void button6_Click(object sender, EventArgs e)
        {
            Tab.SelectTab(0);
        }
        private void button5_Click(object sender, EventArgs e)
        {
            Tab.SelectTab(1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Tab.SelectTab(2);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Tab.SelectTab(3);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Tab.SelectTab(4);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Tab.SelectTab(5);
        }

        private void DebugE_ch(object sender, EventArgs e)
        {
            debug = DBB.Checked;
        }

        private void CLEARDD_Click(object sender, EventArgs e)
        {
            CMD.Text = "";
        }

        private void SelLanguage_Click(object sender, EventArgs e)
        {
            CURRENTlanguage = LBOX.Text;
            Lang();
            key.SetValue("LANGUAGE", LBOX.Text);
            key.SetValue("DEBUG", DBB.Checked);
        }

        private void Tab_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loadedhose)
            {
                RdGPT.Visible = true;
                RdGPT.Enabled = true;
            }
        }

        private void DBB_CheckedChanged(object sender, EventArgs e)
        {
            debug = DBB.Checked;
        }

        private void FrBTN_Click(object sender, EventArgs e)
        {
            if (!Find()) return;
            diag.HACKdbPort();
            diag.FACTORY_RESET();
        }

        private void EraseDA_Click(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            if (!CheckDevice(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text)) return;
            DialogResult dialogResult = System.Windows.Forms.MessageBox.Show(L("AreY") + tempsel, "WARNING: CAN CAUSE DAMAGE", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (Erase("userdata", AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text))
                    LOG(I("ErPS") + tempsel);
                else
                    LOG(E("ErPE") + tempsel);
                progr.Value = 100;
            }
        }

        private void RdHISIinfo_Click(object sender, EventArgs e)
        {
            Tab.Enabled = false;
            try
            {
                LOG("=============READ INFO (FASTBOOT)=============");
                if (HISI.ReadInfo(false))
                {
                    BSNhi.Text = HISI.BSN;
                    BUIDhi.Text = HISI.BVER;
                    AVERhi.Text = HISI.AVER;
                    BLkeyHI.Text = HISI.BLKEY;
                    ASERhisi.Text = HISI.ASerial;
                }
                else LOG(E("DeviceNotCon"));
            }
            catch (Exception esd)
            {
                LOG("ERROR: " + esd);
            }
            Tab.Enabled = true;
        }
        private void FBLstHISI_Click(object sender, EventArgs e)
        {
            LOG("=============WRITE FBLOCK (FASTBOOT)=============");
            LOG("=============> VALUE: " + (EnDisFBLOCK.Checked ? 1 : 0) + " <=============");
            try
            {
                if (HISI.ReadInfo(false))
                {
                    BSNhi.Text = HISI.BSN;
                    BUIDhi.Text = HISI.BVER;
                    AVERhi.Text = HISI.AVER;
                    BLkeyHI.Text = HISI.BLKEY;
                    ASERhisi.Text = HISI.ASerial;
                    HISI.SetFBLOCK(EnDisFBLOCK.Checked ? 1 : 0);
                }
            }
            catch (Exception se)
            {
                if (debug)
                    LOG("ERR: " + se);
            }
        }

        private void HISI_board_FB_Click(object sender, EventArgs e)
        {
            try
            {
                if (BLkeyHI.Text.Length == 16)
                {
                    LOG("=============REWRITE KEY (FASTBOOT)=============");
                    LOG("=============> KEY: " + BLkeyHI.Text + " <=============");
                    LOG("=============> LENGHT: " + BLkeyHI.Text + " <=============");
                    if (HISI.ReadInfo(false))
                    {
                        BSNhi.Text = HISI.BSN;
                        BUIDhi.Text = HISI.BVER;
                        AVERhi.Text = HISI.AVER;
                        BLkeyHI.Text = HISI.BLKEY;
                        HISI.WriteBOOTLOADERKEY(BLkeyHI.Text);
                    }
                    else LOG(E("DeviceNotCon"));
                }
                else
                    LOG(E("KeyLenghtERR"));
            }
            catch (Exception se)
            {
                if (debug)
                    LOG("ERR: " + se);
            }
        }

        private void UNLOCKHISI_Click(object sender, EventArgs e)
        {
            try
            {
                Tab.Enabled = false;
                if (BLkeyHI.Text.Length == 16)
                {
                    if (isVCOM.Checked)
                    {
                        if (String.IsNullOrEmpty(HISIbootloaders.Text))
                        {
                            LOG("Select CPU First");
                            Tab.Enabled = true;
                            return;
                        }
                        device = HISIbootloaders.Text;
                        Path = "UnlockFiles\\" + HISIbootloaders.Text + "\\manifest.xml";
                        if (!File.Exists(Path))
                        {
                            progr.Value = 1;
                            LOG(I("DownloadFor") + device);
                            LOG("URL: " + source[device]);
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                            WebClient client = new WebClient();
                            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                            client.DownloadFileCompleted += new AsyncCompletedEventHandler(HISIDownloaded);
                            client.DownloadFileAsync(new Uri(source[device]), device + ".zip");
                            UNLOCKHISI.Text = L("HISIWriteKirinBLD");
                            Tab.Enabled = false;
                            return;
                        }
                        Port_D data = GETPORT("huawei usb com");
                        if (data.ComName != "NaN")
                        {
                            FlashToolHisi.FlashBootloader(Bootloader.ParseBootloader("UnlockFiles\\" + HISIbootloaders.Text.ToUpper() + "\\manifest.xml"), data.ComName);
                            LOG("[FastBoot] Waiting for any device...");
                            if (HISI.ReadInfo(true))
                            {
                                BSNhi.Text = HISI.BSN;
                                BUIDhi.Text = HISI.BVER;
                                AVERhi.Text = HISI.AVER;
                                BLkeyHI.Text = HISI.BLKEY;
                                HISI.WriteBOOTLOADERKEY(BLkeyHI.Text);
                            }
                            else LOG(E("DeviceNotCon") + "FASTBOOT TIMED OUT");
                        }
                        else { Tab.Enabled = true; LOG(E("DeviceNotCon")); }
                    }
                    else
                    {
                        LOG("=============REWRITE KEY (FASTBOOT)=============");
                        LOG("=============> KEY: " + BLkeyHI.Text + " <=============");
                        LOG("=============> LENGHT: " + BLkeyHI.Text + " <=============");
                        if (HISI.ReadInfo(false))
                        {
                            HISI.WriteBOOTLOADERKEY(BLkeyHI.Text);
                            BSNhi.Text = HISI.BSN;
                            BUIDhi.Text = HISI.BVER;
                            AVERhi.Text = HISI.AVER;
                            BLkeyHI.Text = HISI.BLKEY;
                        }
                        else LOG(E("DeviceNotCon"));
                    }
                }
                else
                    LOG(E("KeyLenghtERR"));
            }
            catch (Exception se)
            {
                if (debug)
                    LOG("ERR: " + se);
            }
            Tab.Enabled = true;
        }

        private void HISIbootloaders_SelectedIndexChanged(object sender, EventArgs e)
        {
            Path = "UnlockFiles\\" + HISIbootloaders.Text.ToUpper() + "\\manifest.xml";
            if (!Directory.Exists(Path)) UNLOCKHISI.Text = L("HISIWriteKirinBLD"); else BoardU.Text = L("HISIWriteKirinBL");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Find()) return;
            CMD.Text = diag.TestHack();
        }

        private void nButton2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = PrevFolder;
            openFileDialog.Filter = "Patch0 Repartition data files (*.xml;*.txt)|*.xml;*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK) PrevFolder = PatXm.Text = openFileDialog.FileName;
        }

        private void EraseMeBtn_Click(object sender, EventArgs e)
        {
            TxSide = GETPORT("qdloader 9008");
            if (!CheckDevice(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text)) return;
            DialogResult dialogResult = MessageBox.Show(L("ERmINFO"), L("CZdmg"), MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
                if (EraseMemory(AutoLdr.Checked ? PickLoader(LoaderBox.Text) : LoaderBox.Text))
                    LOG(I("EraseMS"));
                else
                    LOG(E("EEraseMS"));
            progr.Value = 100;
        }
    }
}
