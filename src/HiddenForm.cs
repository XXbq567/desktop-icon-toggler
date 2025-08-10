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
        private const int MOD_ALT = 0x0001;
        private const int VK_Q = 0x51;
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        public HiddenForm()
        {
            // 创建完全隐藏的窗口
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(0, 0);
            
            // 注册热键
            this.Load += (s, e) => {
                bool success = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, VK_Q);
                if (!success)
                {
                    MessageBox.Show("热键注册失败，可能与其他程序冲突", "警告", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            
            // 窗口关闭时取消注册热键
            this.FormClosing += (s, e) => {
                UnregisterHotKey(this.Handle, HOTKEY_ID);
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
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        object currentValue = key.GetValue("HideIcons");
                        int newValue = (currentValue != null && (int)currentValue == 1) ? 0 : 1;
                        
                        key.SetValue("HideIcons", newValue, RegistryValueKind.DWord);
                        
                        // 立即刷新桌面
                        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                        
                        // 强制刷新资源管理器
                        RefreshDesktop();
                    }
                }
            }
            catch (Exception)
            {
                // 静默处理错误
            }
        }
        
        private void RefreshDesktop()
        {
            try
            {
                // 发送F5按键刷新桌面
                SendKeys.Send("{F5}");
            }
            catch
            {
                // 如果SendKeys失败，尝试重启资源管理器
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe");
                }
                catch { }
            }
        }
        
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false); // 永远不显示窗口
        }
    }
}
