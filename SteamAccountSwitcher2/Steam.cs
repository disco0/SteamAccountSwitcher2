using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using WindowsInput;
using WindowsInput.Native;

namespace SteamAccountSwitcher2
{
    /// <summary>
    /// Steam Class
    /// </summary>
    /// 
    public class Steam
    {

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        const int SW_RESTORE = 9;

        // Before I put displayRegistryInfo() back in here
        // private RegistryKey SteamRegKey = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam", true);
        
        string installDir;

        public string InstallDir
        {
            get { return installDir; }
            set { installDir = value; }
        }

        public Steam(string installDir)
        {
            this.installDir = installDir;
        }

        public bool IsSteamRunning()
        {
            Process[] pname = Process.GetProcessesByName("steam");
            if (pname.Length == 0)
                return false;
            else
                return true;
        }

        public void KillSteam()
        {
            Process[] proc = Process.GetProcessesByName("steam");
            proc[0].Kill();
        }

        public void CleanKillSteam()
        {
            Process[] proc = Process.GetProcessesByName("steam");
            proc[0].CloseMainWindow();
            proc[0].Close();
        }

        public bool StartSteamAccountAutoAuth(SteamAccount acc)
        {
            bool finished = false;

            if (IsSteamRunning())
            {
                CleanKillSteam();
            }
            int waitTimer = 30;

            // Set AutoLoginUser value in registry

            setRegistryInfo(acc.Username);

            checkRegistryInfo(acc.Username);

            // Then open Steam

            while (finished == false)
            {

                if (waitTimer == 0)
                {
                    KillSteam();
                    Debug.WriteLine("Hard killed steam.");
                }
                if (IsSteamRunning() == false)
                {
                    Process p = new Process();
                    if (File.Exists(installDir))
                    {
                        p.StartInfo = new ProcessStartInfo(installDir);
                        p.Start();
                        finished = true;

                        return true;
                    }
                }
                Thread.Sleep(100);
                waitTimer--;
            }
            return false;
        }
        
        private void setRegistryInfo(string usr)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam", true);

            // Creates Reg key for Steam if not found
            // It should already be there anyway I'm considering throwing an error if its not 
            // (can it even be somewhere else?)
            if (key == null)
            {
                Debug.Print("Unable to locate Steam Registry Key, attempting to create the key.");
                Registry.CurrentUser.CreateSubKey("Software\\Valve\\Steam");
                key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam", true);           
                
                // Checks if Reg key exists after trying to manually create one
                if (key == null)
                {
                    Debug.Print("Key creation unsuccessful.");
                    return;
                }
            }

            key.SetValue("AutoLoginUser", usr);
            key.SetValue("RememberPassword", 1);
            return;
        }

        private bool checkRegistryInfo(string usr)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam", true);

            if (key.GetValue("AutoLoginUser", true).Equals(usr) && key.GetValue("RememberPassword", true).Equals(1))
            {
                Debug.Print("AutoLoginUser and RememberPassword values succesfully updated.");
                return true;
            }
            Debug.Print("AutoLoginUser and RememberPassword values did not match after assigning?????");
            Debug.Print("Returning true to see if login still succeeds.");
            return false;
        }

        public bool StartSteamAccount(SteamAccount acc)
        {
            bool finished = false;

            if (IsSteamRunning())
            {
                CleanKillSteam();
            }

            int waitTimer = 30;
            while (finished == false)
            {

                if(waitTimer == 0)
                {
                    KillSteam();
                    Debug.WriteLine("Hard killed steam.");
                }
                if (IsSteamRunning() == false)
                {
                    Process p = new Process();
                    if (File.Exists(installDir))
                    {
                        p.StartInfo = new ProcessStartInfo(installDir, acc.getStartParameters());
                        p.Start();
                        finished = true;

                        return true;
                    }
                }
                Thread.Sleep(100);
                waitTimer--;
            }
            return false;
        }

        public bool StartSteamAccountSafe(SteamAccount acc)
        {
            Process p;
            bool finished = false;
            string loginString = "-login " + acc.Username + " SAS-SAFEMODE";

            p = new Process();
            p.StartInfo = new ProcessStartInfo(installDir, "-fs_log " + loginString);

            if (IsSteamRunning())
            {
                CleanKillSteam();
            }

            int waitTimer = 30;
            while (finished == false)
            {
                Debug.WriteLine("Waiting for steam to exit...");
                if (waitTimer == 0)
                {
                    KillSteam();
                    Debug.WriteLine("Hard killed steam.");
                }

                if (IsSteamRunning() == false)
                {
                    p.Start();
                    finished = true;

                    System.Threading.Thread.Sleep(5000);
                    bool steamNotUpdating = false;
                    while (steamNotUpdating == false)
                    {
                        steamNotUpdating = IsSteamReady();
                    }

                    if (steamNotUpdating)
                    {
                        try
                        {
                            Debug.WriteLine("Starting input manager!");
                            System.Threading.Thread.Sleep(1500);
                            Debug.WriteLine("Done waiting.");

                            IntPtr handle = p.MainWindowHandle;
                            if (IsIconic(handle))
                            {
                                ShowWindow(handle, SW_RESTORE);
                            }
                            //SetForegroundWindow(handle);
                            //Clipboard.SetText(acc.Username);
                            InputSimulator s = new InputSimulator();
                            //s.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);

                            Debug.WriteLine("Focused window");
                            //s.Keyboard.TextEntry(acc.Username);
                            //System.Threading.Thread.Sleep(100);
                            //s.Keyboard.KeyDown(VirtualKeyCode.TAB);
                            //s.Keyboard.KeyUp(VirtualKeyCode.TAB);
                            //System.Threading.Thread.Sleep(100);
                            System.Threading.Thread.Sleep(500);
                            Debug.WriteLine("ENTERING PW NOW");
                            s.Keyboard.TextEntry(acc.Password);
                            System.Threading.Thread.Sleep(100);
                            s.Keyboard.KeyDown(VirtualKeyCode.RETURN);

                            return true;
                        }
                        catch
                        {
                            MessageBox.Show("Error logging in. Steam not in foreground.");
                        }
                        //MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                    Thread.Sleep(100);
                    waitTimer--;
                }
            }
            return false;
        }

        // Created this function to check the 'Remember Password' box when logging in, now vestigial
        public bool StartSteamAccountSafeNew(SteamAccount acc)
        {

            Process p;
            bool finished = false;
            string loginString = "-login ";// + acc.Username + " SAS-SAFEMODE";

            p = new Process();
            p.StartInfo = new ProcessStartInfo(installDir, "-fs_log " );

            if (IsSteamRunning())
            {
                CleanKillSteam();
            }

            int waitTimer = 30;
            while (finished == false)
            {
                Debug.WriteLine("Waiting for steam to exit...");
                if (waitTimer == 0)
                {
                    KillSteam();
                    Debug.WriteLine("Hard killed steam.");
                }

                if (IsSteamRunning() == false)
                {
                    p.Start();
                    finished = true;

                    System.Threading.Thread.Sleep(1000);
                    bool steamNotUpdating = false;
                    while(steamNotUpdating == false)
                    {
                        steamNotUpdating = IsSteamReady();
                    }

                    if (steamNotUpdating)
                    {
                        try
                        {
                            Debug.WriteLine("Starting input manager!");
                            System.Threading.Thread.Sleep(100);
                            Debug.WriteLine("Done waiting.");

                            IntPtr handle = p.MainWindowHandle;
                            if (IsIconic(handle))
                            {
                                ShowWindow(handle, SW_RESTORE);
                            }

                            SetForegroundWindow(handle);
                            Debug.WriteLine("Focused window");

                            Clipboard.SetText(acc.Username);
                            InputSimulator s = new InputSimulator();

                            // Shift-Tab to return up to Username field
                            s.Keyboard.KeyDown(VirtualKeyCode.LSHIFT);
                            s.Keyboard.KeyDown(VirtualKeyCode.TAB);
                            System.Threading.Thread.Sleep(100); 
                            s.Keyboard.KeyUp(VirtualKeyCode.LSHIFT);
                            s.Keyboard.KeyUp(VirtualKeyCode.TAB);
                            //System.Threading.Thread.Sleep(100);

                            // Paste Username
                            s.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                            System.Threading.Thread.Sleep(100);
                            s.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                            System.Threading.Thread.Sleep(100);

                            // Tab down to Password field
                            s.Keyboard.KeyPress(VirtualKeyCode.TAB);
                            System.Threading.Thread.Sleep(100);

                            // Paste Password
                            Debug.WriteLine("ENTERING PW NOW");
                            s.Keyboard.TextEntry(acc.Password);

                            // Tab down to "Remember Me" Field
                            System.Threading.Thread.Sleep(100);
                            s.Keyboard.KeyDown(VirtualKeyCode.TAB);
                            System.Threading.Thread.Sleep(100);
                            s.Keyboard.KeyUp(VirtualKeyCode.TAB);
                            Debug.WriteLine("TAB1");

                            System.Threading.Thread.Sleep(100);
                            s.Keyboard.KeyDown(VirtualKeyCode.SPACE);
                            s.Keyboard.KeyUp (VirtualKeyCode.SPACE);
                            Debug.WriteLine("SPACE 1");

                            System.Threading.Thread.Sleep(100);
                            s.Keyboard.KeyDown(VirtualKeyCode.RETURN);
                            s.Keyboard.KeyUp(VirtualKeyCode.RETURN);
                            Debug.WriteLine("RETURN");

                            return true;
                        }
                        catch
                        {
                            MessageBox.Show("Error logging in. Steam not in foreground.");
                        }
                        //MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                    Thread.Sleep(100);
                    waitTimer--;                    
                }
            }
            return false;
        }

        public void displayRegistryInfo()
        {
            RegistryKey key =  Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam", true);

            if (key == null)
            {
                Debug.Print("Unable to locate Steam Registry Key.");
                MessageBox.Show("Unable to locate Steam Registry Key.");
                return;
            }

            switch (key.GetValue("AutoLoginUser"))
            {
                case null:
                    Debug.Print("AutoLoginUser value does not exist in Steam registry key.");
                    MessageBox.Show("AutoLoginUser value does not exist in Steam registry key.");
                    break;
                case "":
                    Debug.Print("AutoLoginUser value currently not set.");
                    MessageBox.Show("AutoLoginUser value currently not set.");
                    break;
                default:
                    Debug.Print("AutoLoginUser = " + key.GetValue("AutoLoginUser"));
                    MessageBox.Show("AutoLoginUser = " + key.GetValue("AutoLoginUser"));
                    break;
                
            }
            return;
        }
        
        private bool IsSteamReady()
        {
            string logDir = installDir.Replace("Steam.exe", "logs\\");
            string filename = logDir + "bootstrap_log.txt";

            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Seek 1024 bytes from the end of the file
                fs.Seek(-512, SeekOrigin.End);
                // read 1024 bytes
                byte[] bytes = new byte[512];
                fs.Read(bytes, 0, 512);
                // Convert bytes to string
                string s = Encoding.Default.GetString(bytes);
                // or string s = Encoding.UTF8.GetString(bytes);
                // and output to console
                //Debug.WriteLine(s);
                string[] splitter = new string[1];
                splitter[0] = "Startup - updater";
                string[] parts = s.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

                bool steamDone = parts[parts.Length - 1].Contains("Background update loop checking for update.");
                Debug.WriteLineIf(steamDone, "steam is Done.");
                return steamDone;
            }
        }

        public bool LogoutSteam()
        {
            Process p = new Process();
            if (File.Exists(installDir))
            {
                p.StartInfo = new ProcessStartInfo(installDir, "-shutdown");
                p.Start();
                return true;
            }
            return false;

        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
