using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace WPF场景仿真推演系统
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public class DPIUtils
    {
        private static double _dpiX = 1.0;
        private static double _dpiY = 1.0;
        public static double DPIX
        {
            get
            {
                return DPIUtils._dpiX;
            }
        }
        public static double DPIY
        {
            get
            {
                return DPIUtils._dpiY;
            }
        }
        public static void Init(System.Windows.Media.Visual visual)
        {
            Matrix transformToDevice = System.Windows.PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
            DPIUtils._dpiX = transformToDevice.M11;
            DPIUtils._dpiY = transformToDevice.M22;
        }
        public static Point DivideByDPI(Point p)
        {
            return new Point(p.X / DPIUtils.DPIX, p.Y / DPIUtils.DPIY);
        }
        public static Rect DivideByDPI(Rect r)
        {
            return new Rect(r.Left / DPIUtils.DPIX, r.Top / DPIUtils.DPIY, r.Width, r.Height);
        }
    }
    public partial class MainWindow : Window
    {

        [DllImport("User32.dll")]
        static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        internal delegate int WindowEnumProc(IntPtr hwnd, IntPtr lparam);
        [DllImport("user32.dll")]
        internal static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc func, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        //private const string Kernel32_DllName = "kernel32.dll";
        //[DllImport(Kernel32_DllName)]
        //public static extern bool FreeConsole();
        //[DllImport(Kernel32_DllName)]
        //public static extern bool AllocConsole();

        private Process process;
        private IntPtr unityHWND = IntPtr.Zero;
        private DispatcherTimer dispatcherTimer;
        private const int WM_ACTIVATE = 0x0006;
        private readonly IntPtr WA_ACTIVE = new IntPtr(1);
        private readonly IntPtr WA_INACTIVE = new IntPtr(0);

        private Point u3dLeftUpPos;

        internal TcpServer WpfServer { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private int WindowEnum(IntPtr hwnd, IntPtr lparam)
        {
            unityHWND = hwnd;
            ActivateUnityWindow();
            return 0;
        }
        private void ActivateUnityWindow()
        {
            SendMessage(unityHWND, WM_ACTIVATE, WA_ACTIVE, IntPtr.Zero);
            //statusBar.Text =$"Refreshing At {DateTime.Now.ToString()}";
        }

        private void DeactivateUnityWindow()
        {
            SendMessage(unityHWND, WM_ACTIVATE, WA_INACTIVE, IntPtr.Zero);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //AllocConsole();

            IntPtr hwnd = ((HwndSource)PresentationSource.FromVisual(Panel1)).Handle;
            process = new Process();
            process.StartInfo.FileName = "Data\\场景仿真推演系统.exe";
            process.StartInfo.Arguments = "-parentHWND " + hwnd.ToInt32() + " " + Environment.CommandLine;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            process.WaitForInputIdle();

            // Doesn't work for some reason ?!
            //unityHWND = process.MainWindowHandle;
            EnumChildWindows(hwnd, WindowEnum, IntPtr.Zero);
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(InitialResize);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);

            dispatcherTimer.Start();
            WpfServer = new TcpServer();
            WpfServer.StartServer();
            
        }
        private void InitialResize(object sender, EventArgs e)
        {
            //MessageBox.Show("Timer");
            ResizeU3D();
            dispatcherTimer.Stop();


        }
        private void ResizeU3D()
        {
            Window window = Window.GetWindow(this);
            u3dLeftUpPos = Panel1.TransformToAncestor(window).Transform(new Point(0, 0));
            DPIUtils.Init(this);
            //MessageBox.Show($"{DPIUtils.DPIX},{DPIUtils.DPIY}");
            u3dLeftUpPos.X *= DPIUtils.DPIX;
            u3dLeftUpPos.Y *= DPIUtils.DPIY;
            MoveWindow(unityHWND, (int)u3dLeftUpPos.X, (int)u3dLeftUpPos.Y, (int)(Panel1.ActualWidth * DPIUtils.DPIX), (int)(Panel1.ActualHeight * DPIUtils.DPIY), true);
            ActivateUnityWindow();
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeU3D();
            ActivateUnityWindow();
        }


        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            statusBar.Text = $"Mouse At {e.MouseDevice.GetPosition(this)} & Window At {u3dLeftUpPos}";
            //if (e.MouseDevice.GetPosition(this).X > u3dLeftUpPos.X/DPIUtils.DPIX && e.MouseDevice.GetPosition(this).X < u3dLeftUpPos.X / DPIUtils.DPIX + Panel1.ActualWidth &&
            //   e.MouseDevice.GetPosition(this).Y > u3dLeftUpPos.Y / DPIUtils.DPIY && e.MouseDevice.GetPosition(this).Y < u3dLeftUpPos.Y / DPIUtils.DPIY + Panel1.ActualHeight)
            //    MessageBox.Show("Act");
                //ActivateUnityWindow();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                process.CloseMainWindow();

                Thread.Sleep(1000);
                while (process.HasExited == false)
                    process.Kill();
                //FreeConsole();
                WpfServer.QuitServer();
            }
            catch (Exception)
            {

            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            DeactivateUnityWindow();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            ActivateUnityWindow();
        }
        private void NewFile(object sender, RoutedEventArgs e)
        {
            WpfServer.SendMessage("NewFile");
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ListBoxItem ts = (UnitCreatorList.SelectedItem as ListBoxItem);
            if (ts != null)
            {
                statusBar.Text = $"Select {ts.Content.ToString()}";
                WpfServer.SendMessage("Spawn DD");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Escape)
            {
                UnitCreatorList.SelectedItem = null;
                WpfServer.SendMessage("Cancel");
            }
        }
    }


}
