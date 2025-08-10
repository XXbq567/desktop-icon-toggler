using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DesktopIconToggler
{
    public partial class HiddenForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        public HiddenForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            
            // 处理 Ctrl+C 退出
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                Application.Exit();
            };
        }
        
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleDesktopIcons();
            }
            base.WndProc(ref m);
        }
        
        private void ToggleDesktopIcons()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        object currentValue = key.GetValue("HideIcons");
                        int newValue = (currentValue != null && (int)currentValue == 1) ? 0 : 1;
                        
                        key.SetValue("HideIcons", newValue, RegistryValueKind.DWord);
                        
                        // 刷新桌面
                        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                        
                        string status = newValue == 1 ? "隐藏" : "显示";
                        Console.WriteLine($"桌面图标已{status}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"切换桌面图标失败: {ex.Message}");
            }
        }
    }
}
