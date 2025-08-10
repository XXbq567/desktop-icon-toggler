using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
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
        private static readonly string LOG_FILE = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopIconToggler.log");
        
        private static Mutex mutex;
        private static NotifyIcon trayIcon;
        
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                WriteLog("程序启动开始...");
                
                // 如果有参数 --debug，显示控制台窗口
                bool debugMode = args.Length > 0 && args[0] == "--debug";
                
                if (!debugMode)
                {
                    // 隐藏控制台窗口
                    IntPtr consoleWindow = GetConsoleWindow();
                    if (consoleWindow != IntPtr.Zero)
                    {
                        ShowWindow(consoleWindow, SW_HIDE);
                    }
                }
                else
                {
                    AllocConsole();
                    Console.WriteLine("调试模式启动...");
                }
                
                WriteLog("检查单实例...");
                // 单实例检查
                mutex = new Mutex(true, APP_NAME, out bool createdNew);
                if (!createdNew)
                {
                    WriteLog("程序已在运行中，退出");
                    if (debugMode) Console.WriteLine("程序已在运行中");
                    return;
                }
                
                WriteLog("添加开机启动...");
                // 添加到开机启动
                AddToStartup();
                
                WriteLog("创建托盘图标...");
                // 创建托盘图标（用于调试和退出）
                CreateTrayIcon();
                
                WriteLog("创建隐藏窗口...");
                // 创建隐藏窗口并运行
                HiddenForm form = new HiddenForm(debugMode);
                WriteLog("启动应用程序消息循环...");
                Application.Run(form);
            }
            catch (Exception ex)
            {
                string error = $"程序启动失败: {ex.Message}\n\n{ex.StackTrace}";
                WriteLog(error);
                MessageBox.Show(error, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                WriteLog("清理资源...");
                // 清理资源
                trayIcon?.Dispose();
                mutex?.ReleaseMutex();
                mutex?.Dispose();
                WriteLog("程序退出");
            }
        }
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        
        private static void WriteLog(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(LOG_FILE, logEntry + Environment.NewLine);
            }
            catch { }
        }
        
        private static void CreateTrayIcon()
        {
            try
            {
                WriteLog("开始创建托盘图标...");
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
                trayIcon.Text = "桌面图标切换器 - Alt+Q (点击查看日志)";
                trayIcon.Visible = true;
                
                WriteLog("托盘图标创建成功");
                
                // 右键菜单
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("状态: 运行中").Enabled = false;
                menu.Items.Add("快捷键: Alt+Q").Enabled = false;
                menu.Items.Add(new ToolStripSeparator());
                
                ToolStripMenuItem logItem = new ToolStripMenuItem("查看日志");
                logItem.Click += (s, e) => ShowLogFile();
                menu.Items.Add(logItem);
                
                ToolStripMenuItem testItem = new ToolStripMenuItem("测试切换");
                testItem.Click += (s, e) => TestToggle();
                menu.Items.Add(testItem);
                
                ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
                exitItem.Click += (s, e) => Application.Exit();
                menu.Items.Add(exitItem);
                
                trayIcon.ContextMenuStrip = menu;
                
                // 双击托盘图标显示提示
                trayIcon.DoubleClick += (s, e) => {
                    ShowLogFile();
                };
                
                WriteLog("托盘菜单设置完成");
            }
            catch (Exception ex)
            {
                WriteLog($"创建托盘图标失败: {ex.Message}");
                throw;
            }
        }
        
        private static void ShowLogFile()
        {
            try
            {
                if (File.Exists(LOG_FILE))
                {
                    Process.Start("notepad.exe", LOG_FILE);
                }
                else
                {
                    MessageBox.Show("日志文件不存在", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开日志文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private static void TestToggle()
        {
            WriteLog("手动测试切换功能...");
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
                        WriteLog($"注册表已设置 HideIcons = {newValue}");
                        
                        // 刷新桌面
                        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                        WriteLog("已发送桌面刷新信号");
                        
                        string status = newValue == 1 ? "隐藏" : "显示";
                        MessageBox.Show($"桌面图标已{status}", "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        WriteLog($"切换成功: {status}");
                    }
                    else
                    {
                        WriteLog("无法打开注册表项");
                        MessageBox.Show("无法打开注册表项", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"测试切换失败: {ex.Message}");
                MessageBox.Show($"测试切换失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        private static void AddToStartup()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                WriteLog($"程序路径: {exePath}");
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY, true))
                {
                    if (key?.GetValue(APP_NAME) == null)
                    {
                        key?.SetValue(APP_NAME, exePath);
                        WriteLog("已添加到开机启动");
                    }
                    else
                    {
                        WriteLog("开机启动项已存在");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"添加开机启动失败: {ex.Message}");
            }
        }
    }
}
