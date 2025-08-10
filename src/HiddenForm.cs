using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace DesktopIconToggler
{
    public partial class HiddenForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        private const int MOD_ALT = 0x0001;
        private const int VK_Q = 0x51;
        
        private static readonly string LOG_FILE = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopIconToggler.log");
        private bool debugMode;
        private bool showConsole;
        private DebugWindow debugWindow;
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        [DllImport("user32.dll")]
        private static extern int GetLastError();
        
        public HiddenForm(bool debug = false, bool console = false, DebugWindow debugWin = null)
        {
            debugMode = debug;
            showConsole = console;
            debugWindow = debugWin;
            
            WriteLog("HiddenForm构造函数开始...");
            
            // 创建完全隐藏的窗口
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(1, 1); // 最小但不为0的尺寸
            
            WriteLog("窗口属性设置完成");
            
            // 注册热键
            this.Load += OnFormLoad;
            
            // 窗口关闭时取消注册热键
            this.FormClosing += OnFormClosing;
            
            WriteLog("事件处理器设置完成");
        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            WriteLog("窗口Load事件触发...");
            try
            {
                WriteLog($"尝试注册热键: MOD_ALT({MOD_ALT}) + VK_Q({VK_Q}), ID:{HOTKEY_ID}");
                WriteLog($"窗口句柄: {this.Handle}");
                
                if (showConsole) Console.WriteLine($"窗口句柄: {this.Handle}");
                
                bool success = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, VK_Q);
                WriteLog($"热键注册结果: {success}");
                
                if (showConsole) Console.WriteLine($"热键注册结果: {success}");
                
                if (!success)
                {
                    int error = GetLastError();
                    WriteLog($"热键注册失败，错误代码: {error}");
                    
                    if (showConsole) Console.WriteLine($"热键注册失败，错误代码: {error}");
                    
                    string errorMsg = $"热键注册失败！\n\n可能原因：\n1. Alt+Q 已被其他程序占用\n2. 权限不足\n3. 杀毒软件拦截\n\n错误代码: {error}\n\n建议：\n- 关闭可能占用快捷键的程序\n- 以管理员身份运行\n- 检查杀毒软件设置";
                    
                    MessageBox.Show(errorMsg, "热键注册失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    WriteLog("热键 Alt+Q 注册成功！");
                    if (debugMode || showConsole)
                    {
                        Console.WriteLine("热键 Alt+Q 注册成功！");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"OnFormLoad异常: {ex.Message}\n{ex.StackTrace}");
                if (showConsole) Console.WriteLine($"OnFormLoad异常: {ex.Message}");
                MessageBox.Show($"窗口初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            WriteLog("窗口关闭，取消注册热键...");
            try
            {
                bool success = UnregisterHotKey(this.Handle, HOTKEY_ID);
                WriteLog($"热键取消注册结果: {success}");
                if (showConsole) Console.WriteLine($"热键取消注册结果: {success}");
            }
            catch (Exception ex)
            {
                WriteLog($"取消注册热键异常: {ex.Message}");
                if (showConsole) Console.WriteLine($"取消注册热键异常: {ex.Message}");
            }
        }
        
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                WriteLog($"接收到热键消息: WM_HOTKEY, wParam={m.WParam}, lParam={m.LParam}");
                if (debugMode || showConsole)
                {
                    Console.WriteLine("热键 Alt+Q 被按下！");
                }
                ToggleDesktopIcons();
            }
            base.WndProc(ref m);
        }
        
        private void ToggleDesktopIcons()
        {
            WriteLog("开始切换桌面图标...");
            if (showConsole) Console.WriteLine("开始切换桌面图标...");
            
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        object currentValue = key.GetValue("HideIcons");
                        WriteLog($"当前 HideIcons 值: {currentValue ?? "null"}");
                        if (showConsole) Console.WriteLine($"当前 HideIcons 值: {currentValue ?? "null"}");
                        
                        int newValue = (currentValue != null && (int)currentValue == 1) ? 0 : 1;
                        WriteLog($"设置新值: {newValue}");
                        if (showConsole) Console.WriteLine($"设置新值: {newValue}");
                        
                        key.SetValue("HideIcons", newValue, RegistryValueKind.DWord);
                        WriteLog("注册表写入成功");
                        if (showConsole) Console.WriteLine("注册表写入成功");
                        
                        // 立即刷新桌面
                        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                        WriteLog("发送桌面刷新信号");
                        if (showConsole) Console.WriteLine("发送桌面刷新信号");
                        
                        // 强制刷新资源管理器
                        RefreshDesktop();
                        
                        string status = newValue == # 桌面图标切换器 - GitHub项目设置

## 1. 创建GitHub仓库

1. 登录GitHub，点击右上角"+"号 → "New repository"
2. 填写仓库名：`desktop-icon-toggler`
3. 描述：`一键切换Windows桌面图标显示/隐藏 - Alt+Q快捷键`
4. 选择"Public"（公开）或"Private"（私有）
5. 勾选"Add a README file"
6. 点击"Create repository"

## 2. 项目文件夹结构
