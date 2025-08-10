using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
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
        
        private const int MOD_ALT = 0x0001;
        private const int VK_Q = 0x51;
        private const int HOTKEY_ID = 9000;
        
        private static readonly string APP_NAME = "DesktopIconToggler";
        private static readonly string REG_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        
        private static Mutex mutex;
        
        [STAThread]
        static void Main(string[] args)
        {
            // 单实例检查
            mutex = new Mutex(true, APP_NAME, out bool createdNew);
            if (!createdNew)
            {
                Console.WriteLine("程序已在运行中");
                return;
            }
            
            try
            {
                Console.WriteLine("桌面图标切换器启动中...");
                Console.WriteLine("快捷键: Alt + Q");
                Console.WriteLine("按 Ctrl+C 退出程序");
                
                // 添加到开机启动
                AddToStartup();
                
                // 注册热键
                RegisterHotKey(Process.GetCurrentProcess().MainWindowHandle, HOTKEY_ID, MOD_ALT, VK_Q);
                Console.WriteLine("热键 Alt+Q 注册成功");
                
                // 创建隐藏的窗口来接收热键消息
                Application.Run(new HiddenForm());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                // 清理资源
                UnregisterHotKey(Process.GetCurrentProcess().MainWindowHandle, HOTKEY_ID);
                mutex?.ReleaseMutex();
                mutex?.Dispose();
            }
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
                        Console.WriteLine("已添加到开机启动");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加开机启动失败: {ex.Message}");
            }
        }
    }
}
