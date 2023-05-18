using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace KWTroubleshooter
{
    public partial class MainWindow : Window
    {
        private static string[] twFiles = new string[3]
        {
      "\\Core\\1.0\\Shaders.big",
      "\\RetailExe\\1.0\\cnc3game.dat",
      "\\CNC3.exe"
        };
        private static string[] twFiles1 = new string[3]
        {
      "\\Core\\1.0\\Shaders.big",
      "\\RetailExe\\1.9\\cnc3game.dat",
      "\\CNC3.exe"
        };
        private static string[] kwFiles = new string[3]
        {
      "\\Core\\1.0\\StaticStream.big",
      "\\RetailExe\\1.0\\cnc3ep1.dat",
      "\\cnc3ep1.exe"
        };
        private static string[] kwFiles1 = new string[3]
        {
      "\\Core\\1.0\\StaticStream.big",
      "\\RetailExe\\1.2\\cnc3ep1.dat",
      "\\cnc3ep1.exe"
        };

        private const double HEIGHT_ROW_INPUT = 90f;

        private readonly List<string> LANGS = new List<string>()
        {
            "english",
            "chinese_s",
            "chinese_t",
            "czech",
            "dutch",
            "french",
            "german",
            "german16",
            "hungarian",
            "italian",
            "korean",
            "polish",
            "spanish",
            "russian",
            "thai",
        };

        private static string DOCS_PATH = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        private const string strSelectKW = "Please select KW folder.";
        private const string strInvalidDir = "Please select a valid directory.";
        private const string strInvalidKWDir = "Invalid KW directory. Please select the correct folder.";
        private const string strInvalidTWDir = "Invalid TW directory. Either select the correct TW directory, or create a new directory to use as your dummy TW directory.";
        private const string strPathAccessDenied = "Failed to access path! Please select a different directory.";
        private const string strSelectTW = "Please select path for installing dummy TW.";
        private static Random random = new Random();

        public int bottomHt { get; set; }

        private static string inputTxt;
        private bool isKaneEnabled;
        private string curLang;

        public MainWindow()
        {
            InitializeComponent();
            this.tb_input.Text = "";
            this.tb_msg.Text = "";
            this.comboBox_lang.ItemsSource = LANGS;
            this.bottomHt = 90;
        }

        private void Window_ContentRendered(object sender, EventArgs e) => this.StartTroubleshooterTask();

        private async void btn_enable_kane_Click(object sender, RoutedEventArgs e)
        {
            this.btn_enable_kane.IsEnabled = false;
            bool newStatus = !this.isKaneEnabled;
            if (await Task.Run(() => this.UpdateKaneSkinsReg(newStatus)))
                this.ToggleKaneSkins(newStatus);
            this.btn_enable_kane.IsEnabled = true;
        }

        private void btn_run_troubleshooter_Click(object sender, RoutedEventArgs e) => this.StartTroubleshooterTask();

        private async void StartTroubleshooterTask()
        {
            MainWindow mainWindow = this;
            rowInput.Height = new GridLength(0);

            mainWindow.btn_enable_kane.IsEnabled = false;
            mainWindow.ToggleKaneSkins(false);

            mainWindow.comboBox_lang.IsEnabled = false;

            await Task.Run(() => RunAllTasks());

            mainWindow.btn_enable_kane.IsEnabled = true;
            mainWindow.ToggleKaneSkins(mainWindow.isKaneEnabled);
            mainWindow.comboBox_lang.IsEnabled = true;
            mainWindow.SelectLang(mainWindow.curLang);
        }

        private void RunAllTasks()
        {
            CheckTWGamePath();
            CheckGamePath();
            CheckCPFolder();
            WriteOutput("======= FINISHED =======");
        }

        private async void comboBox_lang_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.comboBox_lang.IsEnabled = false;
            string newLang = comboBox_lang.SelectedItem as string;
            if (newLang != curLang)
            {
                if (!await Task.Run(() => UpdateLang(newLang)))
                {
                    // Show error?
                }
            }
            this.comboBox_lang.IsEnabled = true;
        }

        private void SelectLang(string curLang)
        {            
            this.comboBox_lang.SelectedIndex = LANGS.FindIndex((s) => s == curLang);
        }

        private bool UpdateLang(string newLang)
        {
            string msg = "Updating Language to {0}... {1}";
            string name = "Software\\Electronic Arts\\Electronic Arts\\Command and Conquer 3 Kanes Wrath";
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(name, true))
                {
                    if (key != null)
                    {
                        if (this.WriteToReg(key, new MainWindow.RegEntry("Language", newLang, RegistryValueKind.String)))
                        {
                            this.curLang = newLang;
                            this.WriteOutput(string.Format(msg, newLang, "DONE"));
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.WriteOutput(string.Format(msg, newLang, "FAILED => " + ex.Message));
                return false;
            }
            this.WriteOutput(string.Format(msg, newLang, "FAILED"));
            return false;
        }

        private void ToggleKaneSkins(bool isEnabled)
        {
            this.btn_enable_kane.Foreground = !isEnabled ? (Brush)Brushes.Red : (Brush)Brushes.Green;
            this.btn_enable_kane.Content = !isEnabled ? (object)"Disabled" : (object)"Enabled";
        }

        private bool UpdateKaneSkinsReg(bool enable)
        {
            string msg = "Writing... {0}";
            string name = "Software\\Wow6432Node\\Electronic Arts\\Electronic Arts\\Command and Conquer 3 Kanes Wrath";
            if (!Environment.Is64BitOperatingSystem)
                name = "Software\\Electronic Arts\\Electronic Arts\\Command and Conquer 3 Kanes Wrath";
            try
            {
                using (RegistryKey key = this.GetLocalMachineKey().OpenSubKey(name, true))
                {
                    if (key != null)
                    {
                        this.WriteOutput((enable ? "Enabling" : "Disabling") + " Kane Edition Skins...");
                        string str = this.ReadOrWriteErgc() ?? "";
                        this.WriteOutput("ergc=" + str);
                        int num = (int)MainWindow.HashString(enable ? "KANE#" + str : "STANDARD#" + str);
                        this.WriteOutput(string.Format(msg, "DONE => " + num.ToString()));
                        if (this.WriteToReg(key, new MainWindow.RegEntry("Hash", (object)num, RegistryValueKind.DWord)))
                        {
                            this.isKaneEnabled = enable;
                            return true;
                        }
                    }
                }
            } catch (Exception ex)
            {
                this.WriteOutput(string.Format(msg, "FAILED => " + ex.Message));
                return false;
            }
            this.WriteOutput(string.Format(msg, "FAILED"));
            return false;
        }

        private string ReadOrWriteErgc(string valueToWrite = null)
        {
            string str = "Software\\Wow6432Node\\Electronic Arts\\Electronic Arts\\Command and Conquer 3 Kanes Wrath\\ergc";
            if (!Environment.Is64BitOperatingSystem)
                str = "Software\\Electronic Arts\\Electronic Arts\\Command and Conquer 3 Kanes Wrath\\ergc";
            if (valueToWrite != null)
            {
                if (this.WriteToReg(str, this.GetLocalMachineKey(), new MainWindow.RegEntry("", (object)valueToWrite, RegistryValueKind.String)))
                    return valueToWrite;
            }
            else
            {
                using (RegistryKey registryKey = this.GetLocalMachineKey().OpenSubKey(str))
                {
                    if (registryKey != null)
                        return (string)registryKey.GetValue("");
                }
            }
            return (string)null;
        }

        public static uint HashString(string value)
        {
            uint num1 = 0;
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length == 0)
                return num1;
            int num2 = bytes.Length / 4;
            int num3 = bytes.Length % 4;
            uint num4 = (uint)bytes.Length;
            int index1 = 0;
            for (int index2 = num2; index2 != 0; --index2)
            {
                uint num5 = num4 + ((uint)bytes[index1 + 1] << 8 | (uint)bytes[index1]);
                uint num6 = num5 ^ (uint)((((int)bytes[index1 + 3] << 8 | (int)bytes[index1 + 2]) ^ (int)num5 << 5) << 11);
                num4 = num6 + (num6 >> 11);
                index1 += 4;
            }
            switch (num3)
            {
                case 1:
                    uint num7 = num4 + (uint)bytes[index1];
                    uint num8 = num7 << 10 ^ num7;
                    num4 = num8 + (num8 >> 1);
                    break;
                case 2:
                    uint num9 = num4 + ((uint)bytes[index1 + 1] << 8 | (uint)bytes[index1]);
                    uint num10 = num9 ^ num9 << 11;
                    num4 = num10 + (num10 >> 17);
                    break;
                case 3:
                    uint num11 = num4 + ((uint)bytes[index1 + 1] << 8 | (uint)bytes[index1]);
                    uint num12 = num11 ^ (uint)(((int)num11 ^ (int)bytes[index1 + 2] << 2) << 16);
                    num4 = num12 + (num12 >> 11);
                    break;
            }
            uint num13 = num4 ^ num4 << 3;
            uint num14 = num13 + (num13 >> 5);
            uint num15 = num14 ^ num14 << 2;
            uint num16 = num15 + (num15 >> 15);
            return num16 ^ num16 << 10;
        }

        private void CheckGamePath(string gamePath = null)
        {
            string format = "Checking KW Path... {0}";
            string str1 = "Software\\Wow6432Node\\Electronic Arts\\Electronic Arts\\Command and Conquer 3 Kanes Wrath";
            if (!Environment.Is64BitOperatingSystem)
                str1 = "Software\\Electronic Arts\\Electronic Arts\\Command and Conquer 3 Kanes Wrath";
            try
            {
                using (RegistryKey key = this.GetLocalMachineKey().OpenSubKey(str1, true))
                {
                    if (key != null)
                    {
                        this.WriteOutput("Reading Reg Path...\n" + key.Name);
                        if (gamePath == null)
                        {
                            try
                            {
                                object obj = key.GetValue("InstallPath");
                                if (!string.IsNullOrWhiteSpace((string)obj))
                                    gamePath = obj as string;
                            }
                            catch (Exception ex)
                            {
                                this.WriteOutput(ex.Message);
                            }
                        }
                        if (string.IsNullOrWhiteSpace(gamePath))
                        {
                            this.WriteOutput(string.Format(format, (object)"FAILED => REG KEY NOT FOUND"));
                            string gamePath1 = this.OpenInputPanel(strSelectKW, (string)null, new Func<bool>(this.ValidateInputForKWFolderSelection));
                            if (!this.WriteToReg(key, new MainWindow.RegEntry("InstallPath", (object)gamePath1, RegistryValueKind.String)))
                                return;
                            this.CheckGamePath(gamePath1);
                        }
                        else if (Directory.Exists(gamePath))
                        {
                            if (this.CheckKWDir(gamePath))
                            {
                                if (!gamePath.EndsWith("\\"))
                                {
                                    this.WriteOutput(string.Format(format, (object)"FAILED => Missing Trailing Slash"));
                                    string gamePath2 = gamePath + "\\";
                                    if (!this.WriteToReg(key, new MainWindow.RegEntry("InstallPath", (object)gamePath2, RegistryValueKind.String)))
                                        return;
                                    this.CheckGamePath(gamePath2);
                                }
                                else
                                {
                                    this.WriteOutput(string.Format(format, (object)"OK"));
                                    this.GetDocsPath();
                                    this.CheckLeaf(key);
                                    this.CheckClassRoot(gamePath);
                                }
                            }
                            else
                            {
                                this.WriteOutput(string.Format(format, (object)"FAILED => Missing Files"));
                                string gamePath3 = this.OpenInputPanel(strSelectKW, gamePath, new Func<bool>(this.ValidateInputForKWFolderSelection));
                                if (!this.WriteToReg(key, new MainWindow.RegEntry("InstallPath", (object)gamePath3, RegistryValueKind.String)))
                                    return;
                                this.CheckGamePath(gamePath3);
                            }
                        }
                        else
                        {
                            this.WriteOutput(string.Format(format, (object)"FAILED => Directory not found!"));
                            string gamePath4 = this.OpenInputPanel(strSelectKW, gamePath, new Func<bool>(this.ValidateInputForKWFolderSelection));
                            if (!this.WriteToReg(key, new MainWindow.RegEntry("InstallPath", (object)gamePath4, RegistryValueKind.String)))
                                return;
                            this.CheckGamePath(gamePath4);
                        }
                    }
                    else
                    {
                        this.WriteOutput(string.Format(format, (object)"FAILED => Key not found. Please verify your KW installation!"));
                        string str2 = this.OpenInputPanel(strSelectKW, (string)null, new Func<bool>(this.ValidateInputForKWFolderSelection));
                        if (!this.WriteToReg(str1, this.GetLocalMachineKey(), new MainWindow.RegEntry("InstallPath", (object)str2, RegistryValueKind.String)))
                            return;
                        this.CheckGamePath();
                    }
                }
            }
            catch (Exception ex)
            {
                this.WriteOutput(string.Format(format, (object)("FAILED => Please check HKLM Registry permissions! " + ex.Message)));
            }
        }

        private void GetDocsPath()
        {
            string name = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
            bool flag = false;
            string format = "Retrieving Docs Root... {0}";
            try
            {
                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
                {
                    if (registryKey != null)
                    {
                        this.WriteOutput("Reading Reg Path...\n" + registryKey.Name);
                        string str = (string)registryKey.GetValue("Personal");
                        Console.WriteLine(str);
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            MainWindow.DOCS_PATH = str;
                            flag = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            if (flag)
                this.WriteOutput(string.Format(format, (object)"OK"));
            else
                this.WriteOutput(string.Format(format, (object)"Not found => Reverting to default path"));
        }

        private bool CheckLeaf(RegistryKey key, string userDataLeafName = null)
        {
            string format = "Checking Game Docs... {0}";
            if (userDataLeafName == null)
            {
                try
                {
                    object obj = key.GetValue("UserDataLeafName");
                    if (!string.IsNullOrWhiteSpace((string)obj))
                        userDataLeafName = obj as string;
                }
                catch (Exception ex)
                {
                    this.WriteOutput(ex.Message);
                }
            }
            if (string.IsNullOrWhiteSpace(userDataLeafName))
            {
                this.WriteOutput(string.Format(format, (object)"FAILED => Key not found!"));
                string[] strArray = new string[2]
                {
          "Command & Conquer 3 Kane's Wrath",
          "Command and Conquer 3 Kanes Wrath"
                };
                int index1 = 0;
                for (int index2 = 0; index2 < strArray.Length; ++index2)
                {
                    if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + strArray[index2]))
                    {
                        index1 = index2;
                        break;
                    }
                }
                if (this.WriteToReg(key, new MainWindow.RegEntry("UserDataLeafName", (object)strArray[index1], RegistryValueKind.String)))
                    this.CheckLeaf(key, strArray[index1]);
                return false;
            }
            this.WriteOutput(string.Format(format, (object)"OK"));
            string leafPath1 = MainWindow.DOCS_PATH + "\\" + userDataLeafName;
            string leafPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + userDataLeafName;
            this.CheckReplays(key, leafPath1);
            this.CheckProfile(key, leafPath2);
            this.CheckMaps(key, leafPath2);
            this.CheckMisc(key);
            this.CheckLang();
            return true;
        }

        private void CheckReplays(RegistryKey key, string leafPath, string userReplayFolderName = null)
        {
            string format = "Checking Replays... {0}";
            if (userReplayFolderName == null)
            {
                try
                {
                    object obj = key.GetValue("ReplayFolderName");
                    if (!string.IsNullOrWhiteSpace((string)obj))
                        userReplayFolderName = obj as string;
                }
                catch (Exception ex)
                {
                    this.WriteOutput(ex.Message);
                }
            }
            if (string.IsNullOrWhiteSpace(userReplayFolderName))
            {
                this.WriteOutput(string.Format(format, (object)"FAILED => Key not found!"));
                if (!this.WriteToReg(key, new MainWindow.RegEntry("ReplayFolderName", (object)"Replays", RegistryValueKind.String)))
                    return;
                this.CheckReplays(key, leafPath, "Replays");
            }
            else
            {
                string path = leafPath + "\\" + userReplayFolderName;
                if (Directory.Exists(path))
                {
                    this.WriteOutput(string.Format(format, (object)"OK"));
                }
                else
                {
                    this.WriteOutput(string.Format(format, (object)("FAILED => Replays Directory not found! " + path)));
                    if (Directory.CreateDirectory(path) != null)
                        this.WriteOutput(string.Format(format, (object)"OK => Replays Directory created."));
                    else
                        this.WriteOutput(string.Format(format, (object)"FAILED => Replays Directory not created!"));
                }
            }
        }

        private bool CheckKWDir(string gamePath)
        {
            bool flag = true;
            foreach (string kwFile in MainWindow.kwFiles)
            {
                if (!File.Exists(gamePath + kwFile))
                {
                    flag = false;
                    break;
                }
            }
            if (!flag)
            {
                flag = true;
                foreach (string str in MainWindow.kwFiles1)
                {
                    if (!File.Exists(gamePath + str))
                        return false;
                }
            }
            return flag;
        }

        private void CheckMaps(RegistryKey key, string leafPath, string userMapsFolderName = null)
        {
            string format = "Checking Maps... {0}";
            if (userMapsFolderName == null)
            {
                try
                {
                    object obj = key.GetValue("UseLocalUserMaps");
                    if (obj != null)
                    {
                        if ((int)obj == 0)
                            userMapsFolderName = "Maps";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine((object)ex);
                }
            }
            if (string.IsNullOrWhiteSpace(userMapsFolderName))
            {
                this.WriteOutput(string.Format(format, (object)"FAILED => Key not found!"));
                if (!this.WriteToReg(key, new MainWindow.RegEntry("UseLocalUserMaps", (object)0, RegistryValueKind.DWord)))
                    return;
                this.CheckMaps(key, leafPath, "Maps");
            }
            else
            {
                string path = System.IO.Path.Combine(leafPath, userMapsFolderName);
                if (Directory.Exists(path))
                {
                    this.WriteOutput(string.Format(format, (object)"OK"));
                }
                else
                {
                    this.WriteOutput(string.Format(format, (object)("FAILED => Maps Directory not found! Creating in... " + path)));
                    if (Directory.CreateDirectory(path) != null)
                        this.WriteOutput(string.Format(format, (object)"OK => Maps Directory created."));
                    else
                        this.WriteOutput(string.Format(format, (object)"FAILED => Maps Directory not created!"));
                }
            }
        }

        private void CheckProfile(RegistryKey key, string leafPath, string userProfileFolderName = null)
        {
            string format = "Checking Profiles... {0}";
            if (userProfileFolderName == null)
            {
                try
                {
                    object obj = key.GetValue("ProfileFolderName");
                    if (!string.IsNullOrWhiteSpace((string)obj))
                        userProfileFolderName = obj as string;
                }
                catch (Exception ex)
                {
                    this.WriteOutput(ex.Message);
                }
            }
            if (string.IsNullOrWhiteSpace(userProfileFolderName))
            {
                this.WriteOutput(string.Format(format, (object)"FAILED => Key not found!"));
                if (!this.WriteToReg(key, new MainWindow.RegEntry("ProfileFolderName", (object)"Profiles", RegistryValueKind.String)))
                    return;
                this.CheckProfile(key, leafPath, "Profiles");
            }
            else
            {
                string path = leafPath + "\\" + userProfileFolderName;
                if (Directory.Exists(path))
                    this.WriteOutput(string.Format(format, (object)"OK"));
                else
                    this.WriteOutput(string.Format(format, (object)("FAILED => Directory not found! " + path)));
            }
        }

        private void CheckMisc(RegistryKey key)
        {
            string format = "Checking other Registry values... {0}";
            int num1 = 0;
            try
            {
                string str = this.ReadOrWriteErgc();
                if (string.IsNullOrWhiteSpace(str))
                    str = this.ReadOrWriteErgc(MainWindow.RandomString(20));
                object obj = key.GetValue("Hash");
                if (obj != null && (int)obj == (int)MainWindow.HashString("KANE#" + str))
                    this.isKaneEnabled = true;
                num1 = (int)MainWindow.HashString("STANDARD#" + str);
            }
            catch (Exception ex)
            {
                this.WriteOutput(ex.Message);
            }
            List<MainWindow.RegEntry> list = new List<MainWindow.RegEntry>();
            list.Add(new MainWindow.RegEntry("Hash", (object)num1, RegistryValueKind.DWord));
            list.Add(new MainWindow.RegEntry("MapPackVersion", (object)65536U, RegistryValueKind.DWord));
            list.Add(new MainWindow.RegEntry("Language", (object)"english", RegistryValueKind.String));
            list.Add(new MainWindow.RegEntry("Package", (object)"{CC2422C9-F7B5-4175-B295-5EC2283AA674}", RegistryValueKind.String));
            list.Add(new MainWindow.RegEntry("SaveFolderName", (object)"SaveGames", RegistryValueKind.String));
            list.Add(new MainWindow.RegEntry("ScreenshotsFolderName", (object)"Screenshots", RegistryValueKind.String));
            list.Add(new MainWindow.RegEntry("Version", (object)65536U, RegistryValueKind.DWord));
            int num2 = 0;
            foreach (MainWindow.RegEntry regEntry in list)
            {
                try
                {
                    if (key.GetValue(regEntry.key) == null)
                        ++num2;
                }
                catch (Exception ex)
                {
                    ++num2;
                }
            }
            if (num2 == 0)
                this.WriteOutput(string.Format(format, (object)"OK"));
            else
                this.WriteOutput(string.Format(format, (object)(num2.ToString() + " entries missing!")));
            this.WriteToReg(key, list, false);
        }

        private void CheckTWGamePath(string kwPath = null)
        {
            string str1 = "Software\\Wow6432Node\\Electronic Arts\\Electronic Arts\\Command and Conquer 3";
            if (!Environment.Is64BitOperatingSystem)
                str1 = "Software\\Electronic Arts\\Electronic Arts\\Command and Conquer 3";
            string format = "Checking TW Path... {0}";
            try
            {
                using (RegistryKey registryKey = this.GetLocalMachineKey().OpenSubKey(str1, true))
                {
                    if (registryKey != null)
                    {
                        this.WriteOutput("Reading Reg Path...\n" + registryKey.Name);
                        string str2 = (string)null;
                        try
                        {
                            object obj = registryKey.GetValue("InstallPath");
                            if (!string.IsNullOrWhiteSpace((string)obj))
                                str2 = obj as string;
                        }
                        catch (Exception ex)
                        {
                            this.WriteOutput(ex.Message);
                        }
                        if (string.IsNullOrWhiteSpace(str2))
                        {
                            this.WriteOutput(string.Format(format, (object)"FAILED => Value not found!"));
                            string twGamePath = this.OpenInputPanel(strSelectTW, (string)null, new Func<bool>(this.ValidateInputForTWFolderSelection));
                            this.CreateDummyTWKey(str1, twGamePath);
                        }
                        else if (Directory.Exists(str2))
                        {
                            foreach (string twFile in MainWindow.twFiles)
                            {
                                if (!File.Exists(str2 + twFile))
                                {
                                    this.WriteOutput(string.Format(format, (object)"FAILED => Missing files!"));
                                    this.CreateDummyTWFiles(str2);
                                    return;
                                }
                            }
                            this.WriteOutput(string.Format(format, (object)"OK"));
                        }
                        else
                        {
                            this.WriteOutput(string.Format(format, (object)"FAILED => Directory not found!"));
                            this.CreateDummyTWFiles(this.OpenInputPanel(strSelectTW, str2, new Func<bool>(this.ValidateInputForTWFolderSelection)));
                        }
                    }
                    else
                    {
                        this.WriteOutput(string.Format(format, (object)"FAILED => Key does not exist!"));
                        string twGamePath = this.OpenInputPanel(strSelectTW, this.GetParentDir(kwPath), new Func<bool>(this.ValidateInputForTWFolderSelection));
                        this.CreateDummyTWKey(str1, twGamePath);
                    }
                }
            }
            catch (Exception ex)
            {
                this.WriteOutput(string.Format(format, (object)("FAILED => Please check HKLM Registry permissions! " + ex.Message)));
            }
        }

        private void CreateDummyTWKey(string regPath, string twGamePath)
        {
            string format = "Writing Registry entries for TW Dummy... {0}";
            try
            {
                using (RegistryKey subKey = this.GetLocalMachineKey().CreateSubKey(regPath))
                {
                    if (subKey == null)
                        return;
                    this.WriteOutput("Reading Reg Path...\n" + subKey.Name);
                    if (this.WriteToReg(subKey, new List<MainWindow.RegEntry>()
          {
            new MainWindow.RegEntry("InstallPath", (object) twGamePath, RegistryValueKind.String)
          }))
                    {
                        this.WriteOutput(string.Format(format, (object)"DONE"));
                        this.CreateDummyTWFiles(twGamePath);
                    }
                    else
                        this.WriteOutput(string.Format(format, (object)"FAILED => Please check permissions"));
                }
            }
            catch (Exception ex)
            {
                this.WriteOutput(string.Format(format, (object)("FAILED => " + ex.Message)));
            }
        }

        private void CreateDummyTWFiles(string gamePath)
        {
            string format = "Creating dummy TW files... {0}";
            try
            {
                foreach (string twFile in MainWindow.twFiles)
                {
                    if (!File.Exists(gamePath + twFile))
                    {
                        Directory.CreateDirectory(Directory.GetParent(gamePath + twFile).FullName);
                        this.CreateDummyFile(gamePath + twFile);
                    }
                }
                this.WriteOutput(string.Format(format, (object)("DONE => Created in: " + gamePath)));
            }
            catch (Exception ex)
            {
                this.WriteOutput(string.Format(format, (object)("FAILED => " + ex.Message)));
            }
        }

        private void CheckLang()
        {
            string format = "Checking Language... {0}";
            string name = "Software\\Electronic Arts\\Electronic Arts\\Command and Conquer 3 Kanes Wrath";
            try
            {
                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
                {
                    if (registryKey != null)
                    {
                        this.WriteOutput("Reading Reg Path...\n" + registryKey.Name);
                        try
                        {
                            curLang = (string)registryKey.GetValue("Language");
                            if (!string.IsNullOrWhiteSpace(curLang))
                            {
                                this.WriteOutput(string.Format(format, (object)$"{curLang}, OK"));
                            }
                            else
                                this.WriteOutput(string.Format(format, (object)"FAILED => Incorrect value!"));
                        }
                        catch (Exception ex)
                        {
                            this.WriteOutput(string.Format(format, (object)"FAILED => Invalid value!"));
                        }
                    }
                    else
                        this.WriteOutput(string.Format(format, (object)"FAILED => Key not found, please re-install game!"));
                }
            }
            catch (Exception ex)
            {
                this.WriteOutput(string.Format(format, (object)("FAILED => Please check HKCU Registry permissions! " + ex.Message)));
            }
        }

        private void CheckClassRoot(string kwPath)
        {
            bool flag1 = false;
            string str1 = ".kwreplay\\DefaultIcon";
            string str2 = kwPath + "cnc3ep1.exe,0";
            string format1 = "Checking DefaultIcon... {0}";
            try
            {
                using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(str1, true))
                {
                    if (registryKey != null)
                    {
                        this.WriteOutput("Reading Reg Path...\n" + registryKey.Name);
                        try
                        {
                            if (((string)registryKey.GetValue("")).ToLower() == str2.ToLower())
                            {
                                this.WriteOutput(string.Format(format1, (object)"OK"));
                            }
                            else
                            {
                                this.WriteOutput(string.Format(format1, (object)"FAILED => Invalid path"));
                                flag1 = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            this.WriteOutput(string.Format(format1, (object)"FAILED => Value does not exist!"));
                            flag1 = true;
                        }
                    }
                    else
                    {
                        this.WriteOutput(string.Format(format1, (object)"FAILED => Key does not exist!"));
                        flag1 = true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.WriteOutput(ex.Message);
                this.WriteOutput(string.Format(format1, (object)"FAILED => Please check HKCR Registry permissions!"));
            }
            if (flag1)
                this.WriteToReg(str1, Registry.ClassesRoot, new MainWindow.RegEntry("", (object)str2, RegistryValueKind.String));
            bool flag2 = false;
            string str3 = ".kwreplay\\shell\\open\\command";
            string str4 = kwPath + "cnc3ep1.exe -replayGame \"%1\"";
            string format2 = "Checking Replay Autoplay... {0}";
            try
            {
                using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(str3, true))
                {
                    if (registryKey != null)
                    {
                        this.WriteOutput("Reading Reg Path...\n" + registryKey.Name);
                        try
                        {
                            if (((string)registryKey.GetValue("")).ToLower() == str4.ToLower())
                            {
                                this.WriteOutput(string.Format(format2, (object)"OK"));
                            }
                            else
                            {
                                this.WriteOutput(string.Format(format2, (object)"FAILED => Incorrect path!"));
                                flag2 = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            this.WriteOutput(string.Format(format2, (object)"FAILED => Invalid value!"));
                            flag2 = true;
                        }
                    }
                    else
                    {
                        this.WriteOutput(string.Format(format2, (object)"FAILED => Key does not exist!"));
                        flag2 = true;
                    }
                    if (!flag2)
                        return;
                    this.WriteToReg(str3, Registry.ClassesRoot, new MainWindow.RegEntry("", (object)str4, RegistryValueKind.String));
                }
            }
            catch (Exception ex)
            {
                this.WriteOutput(string.Format(format2, (object)("FAILED => Please check HKCR Registry permissions! " + ex.Message)));
            }
        }

        private void CheckCPFolder()
        {
            string format = "Checking Command Post folder permissions... {0}";
            string path = MainWindow.DOCS_PATH + "\\CommandPost\\";
            try
            {
                if (Directory.Exists(path))
                {
                    this.SetFullControlPermissionsToEveryone(path);
                    this.WriteOutput(string.Format(format, (object)"OK"));
                }
                else
                    this.WriteOutput(string.Format(format, (object)"FAILED => Could not find directory"));
            }
            catch (Exception ex)
            {
                this.WriteOutput(string.Format(format, (object)("FAILED => Could not set directory permissions. Try re-installing Command Post. " + ex.Message)));
            }
        }

        private void SetFullControlPermissionsToEveryone(string path)
        {
            SecurityIdentifier identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, (SecurityIdentifier)null);
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            DirectorySecurity accessControl = directoryInfo.GetAccessControl(AccessControlSections.Access);
            foreach (AccessRule accessRule in (ReadOnlyCollectionBase)accessControl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                if (accessRule is FileSystemAccessRule)
                {
                    FileSystemAccessRule systemAccessRule = (FileSystemAccessRule)accessRule;
                    this.WriteOutput(systemAccessRule.IdentityReference.Translate(typeof(SecurityIdentifier))?.ToString() + " = " + systemAccessRule.AccessControlType.ToString());
                }
            }
            FileSystemAccessRule systemAccessRule1 = new FileSystemAccessRule((IdentityReference)identity, FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
            FileSystemAccessRule systemAccessRule2 = new FileSystemAccessRule((IdentityReference)identity, FileSystemRights.Write, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Deny);
            directoryInfo.SetAccessControl(accessControl);
        }

        private RegistryKey GetLocalMachineKey()
        {
            try
            {
                return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            catch (Exception ex)
            {
                return Registry.LocalMachine;
            }
        }

        private bool WriteToReg(
          string regPath,
          RegistryKey baseRegKey,
          MainWindow.RegEntry entry,
          bool canOverwrite = true)
        {
            try
            {
                using (RegistryKey subKey = baseRegKey.CreateSubKey(regPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (subKey != null)
                        return this.WriteToReg(subKey, entry, canOverwrite);
                }
            }
            catch (Exception ex)
            {
                this.WriteOutput("Failed to create subkey! " + regPath + "," + ex?.ToString());
            }
            return false;
        }

        private bool WriteToReg(RegistryKey key, MainWindow.RegEntry entry, bool canOverwrite = true) => this.WriteToReg(key, new List<MainWindow.RegEntry>()
    {
      entry
    }, canOverwrite);

        private bool WriteToReg(RegistryKey key, List<MainWindow.RegEntry> list, bool canOverwrite = true)
        {
            int count = list.Count;
            int num1 = 0;
            int num2 = 0;
            List<MainWindow.RegEntry>.Enumerator enumerator = list.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    MainWindow.RegEntry current = enumerator.Current;
                    try
                    {
                        if (key.GetValue(current.key) == null)
                        {
                            key.SetValue(current.key, current.value, current.type);
                            ++num1;
                        }
                        else if (canOverwrite)
                        {
                            key.SetValue(current.key, current.value, current.type);
                            ++num2;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.WriteOutput(ex.Message + $" Key={key.Name}, Value={current.value?.ToString()}, Type={current.type}");
                        throw ex;
                    }
                }
                if (num2 + num1 > 0)
                    this.WriteOutput(string.Format(">>> Registry write completed. {0} keys created, {1} updated.", (object)num1.ToString(), (object)num2.ToString()));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine((object)ex);
                this.WriteOutput(">>> Failed to write to Registry! " + ex.Message);
            }
            return false;
        }

        private void WriteOutput(string s) => System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
        {
            System.Windows.Controls.TextBox tbOutput = this.tb_output;
            tbOutput.Text = tbOutput.Text + s + "\n";
            this.tb_output.ScrollToEnd();
        }));

        private void UpdateOutput(string s) => System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
        {
            string[] strArray = Regex.Split(MainWindow.inputTxt, "\n");
            int num = 0;
            int index = 0;
            StringBuilder stringBuilder = new StringBuilder(MainWindow.inputTxt);
            for (; index != strArray.Length - 2; ++index)
                num += strArray[index].Length + 1;
            stringBuilder.Remove(num, strArray[index].Length);
            stringBuilder.Insert(num, s);
        }));

        private string GetParentDir(string path)
        {
            try
            {
                path = path.TrimEnd("\\".ToCharArray());
                return Directory.GetParent(path).FullName;
            }
            catch (Exception ex)
            {
            }
            return (string)null;
        }

        private string GetLastValidParent(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return (string)null;
            return Directory.Exists(path) ? path : this.GetLastValidParent(this.GetParentDir(path));
        }

        private string OpenInputPanel(string msg, string placeholderTxt, Func<bool> validateAction)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                this.bottomHt = 90;
                rowInput.Height = new GridLength(HEIGHT_ROW_INPUT);
                this.tb_msg.Text = msg;
                this.tb_input.Text = this.GetLastValidParent(placeholderTxt);
            }));
            do
            {
                MainWindow.inputTxt = (string)null;
                while (MainWindow.inputTxt == null)
                    Thread.Sleep(100);
            }
            while ((validateAction != null ? (validateAction() ? 1 : 0) : 1) == 0);
            this.HideInputPanel();
            return MainWindow.inputTxt;
        }

        private void HideInputPanel() => System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
        {
            this.bottomHt = 0;
            rowInput.Height = new GridLength(0);
            this.UpdateLayout();
        }));

        private bool ValidateInputForTWFolderSelection()
        {
            if (!string.IsNullOrWhiteSpace(MainWindow.inputTxt))
            {
                if (Directory.Exists(MainWindow.inputTxt))
                {
                    try
                    {
                        string[] files = Directory.GetFiles(MainWindow.inputTxt);
                        string[] directories = Directory.GetDirectories(MainWindow.inputTxt);
                        if (files.Length == 0)
                        {
                            if (directories.Length == 0)
                                goto label_18;
                        }
                        bool flag = true;
                        foreach (string[] strArray in new List<string[]>()
            {
              MainWindow.twFiles,
              MainWindow.twFiles1
            })
                        {
                            flag = true;
                            foreach (string str in strArray)
                            {
                                if (!File.Exists(MainWindow.inputTxt + str))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag)
                                break;
                        }
                        if (!flag)
                        {
                            int num = (int)System.Windows.MessageBox.Show(strInvalidTWDir);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        int num = (int)System.Windows.MessageBox.Show(strPathAccessDenied);
                        return false;
                    }
                label_18:
                    if (!MainWindow.inputTxt.EndsWith("\\"))
                        MainWindow.inputTxt += "\\";
                    return true;
                }
            }
            int num1 = (int)System.Windows.MessageBox.Show(strInvalidDir);
            return false;
        }

        private bool ValidateInputForKWFolderSelection()
        {
            if (string.IsNullOrWhiteSpace(MainWindow.inputTxt) || !Directory.Exists(MainWindow.inputTxt))
            {
                int num = (int)System.Windows.MessageBox.Show(strInvalidDir);
                return false;
            }
            if (!this.CheckKWDir(MainWindow.inputTxt))
            {
                int num = (int)System.Windows.MessageBox.Show(strInvalidKWDir);
                return false;
            }
            if (!MainWindow.inputTxt.EndsWith("\\"))
                MainWindow.inputTxt += "\\";
            return true;
        }

        private void CreateDummyFile(string file)
        {
            try
            {
                File.Create(file).Dispose();
            }
            catch (Exception ex)
            {
                this.WriteOutput(ex.Message);
            }
        }

        public static string RandomString(int length) => new string(Enumerable.Repeat<string>("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length).Select<string, char>((Func<string, char>)(s => s[MainWindow.random
            .Next(s.Length)])).ToArray<char>());

        private void btn_ok_Click(object sender, RoutedEventArgs e) => MainWindow.inputTxt = this.tb_input.Text;

        private void btn_browse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.SelectedPath = this.tb_input.Text;
            int num = (int)folderBrowserDialog.ShowDialog();
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
                return;
            this.tb_input.Text = folderBrowserDialog.SelectedPath;
        }

        /*
        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent()
        {
            if (this._contentLoaded)
                return;
            this._contentLoaded = true;
            System.Windows.Application.LoadComponent((object)this, new Uri("/KWTroubleshooter;component/mainwindow.xaml", UriKind.Relative));
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        void IComponentConnector.Connect(int connectionId, object target)
        {
            switch (connectionId)
            {
                case 1:
                    ((Window)target).ContentRendered += new EventHandler(this.Window_ContentRendered);
                    break;
                case 2:
                    this.btn_enable_kane = (System.Windows.Controls.Button)target;
                    this.btn_enable_kane.Click += new RoutedEventHandler(this.btn_enable_kane_Click);
                    break;
                case 3:
                    this.tb_output = (System.Windows.Controls.TextBox)target;
                    break;
                case 4:
                    this.panel_input = (StackPanel)target;
                    break;
                case 5:
                    this.tb_msg = (TextBlock)target;
                    break;
                case 6:
                    this.tb_input = (System.Windows.Controls.TextBox)target;
                    break;
                case 7:
                    this.btn_browse = (System.Windows.Controls.Button)target;
                    this.btn_browse.Click += new RoutedEventHandler(this.btn_browse_Click);
                    break;
                case 8:
                    this.btn_ok = (System.Windows.Controls.Button)target;
                    this.btn_ok.Click += new RoutedEventHandler(this.btn_ok_Click);
                    break;
                default:
                    this._contentLoaded = true;
                    break;
            }
        }
        */

        private class RegEntry
        {
            public string key { get; private set; }

            public object value { get; private set; }

            public RegistryValueKind type { get; private set; }

            public RegEntry(string key, object value, RegistryValueKind type)
            {
                this.key = key;
                this.value = value;
                this.type = type;
            }
        }
    }
}

