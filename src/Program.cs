using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

namespace DesktopIconToggler
{
    class Program
    {
        // Win32 API
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        private const int MOD_ALT = 0x0001;
        private const int VK_Q = 0x51;
        private const int HOTKEY_ID = 9000;
        private const int SW_HIDE = 0;
        
        private static readonly string APP_NAME = "DesktopIconToggler";
        private static readonly string REG_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        
        private static Mutex mutex;
        private static NotifyIcon trayIcon;
        
        [STAThread]
        static void Main(string[] args)
        {
            // 隐藏控制台窗口
            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_HIDE);
            }
            
            // 单实例检查
            mutex = new Mutex(true, APP_NAME, out bool createdNew);
            if (!createdNew)
            {
                return; // 程序已在运行
            }
            
            try
            {
                // 添加到开机启动
                AddToStartup();
                
                // 创建托盘图标（用于调试和退出）
                CreateTrayIcon();
                
                // 创建隐藏窗口并运行
                Application.Run(new HiddenForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"程序启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 清理资源
                trayIcon?.Dispose();
                mutex?.ReleaseMutex();
                mutex?.Dispose();
            }
        }
        
        private static void CreateTrayIcon()
        {
            trayIcon = new NotifyIcon();
            
            // 创建一个简单的16x16图标
            Bitmap iconBitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(iconBitmap))
            {
                g.FillRectangle(Brushes.DarkBlue, 0, 0, 16, 16);
                g.FillRectangle(Brushes.White, 2, 2, 12, 12);
                g.DrawString("D", SystemFonts.DefaultFont, Brushes.DarkBlue, 3, 1);
            }
            
            trayIcon.Icon = Icon.FromHandle(iconBitmap.GetHicon());
            trayIcon.Text = "桌面图标切换器 - Alt+Q";
            trayIcon.Visible = true;
            
            // 右键菜单
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("状态: 运行中").Enabled = false;
            menu.Items.Add("快捷键: Alt+Q").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => Application.Exit();
            menu.Items.Add(exitItem);
            
            trayIcon.ContextMenuStrip = menu;
            
            // 双击托盘图标显示提示
            trayIcon.DoubleClick += (s, e) => {
                MessageBox.Show("桌面图标切换器正在运行\n\n快捷键: Alt + Q\n功能: 切换桌面图标显示/隐藏", 
                    "桌面图标切换器", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }
        
        private static void AddToStartup()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY, true))
                {
                    if (key?.GetValue(APP_NAME) == null)
                    {
                        key?.SetValue(APP_NAME, exePath);
                    }
                }
            }
            catch (Exception)
            {
                // 静默处理错误，不影响主功能
            }
        }
    }
}
