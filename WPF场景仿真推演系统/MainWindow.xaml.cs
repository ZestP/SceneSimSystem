using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

    
    public class KeyframeData
    {
        public string time { get; set; }
        public string action { get; set; }
        public KeyframeData(string ti, string ac)
        {
            time = ti;
            action = ac;
        }
    }
    public class ParamsData
    {
        public string name { get; set; }
        public string value { get; set; }
        public ParamsData(string n, string v)
        {
            name = n;
            value = v;
        }
    }
    public class UnitData
    {
        public string ID { get; set; }
        public string name { get; set; }
        public string type{ get; set; }
        
        public UnitData(string id, string n,string t)
        {
            ID = id;
            name = n;
            type = t;
        }
    }
    public class DopesheetData
    {
        public string time { get; set; }
        public string camid { get; set; }
        public string camname { get; set; }
    }
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
        private DispatcherTimer dispatcherTimer,dispatcherTimer2,clockDispatcher;
        private const int WM_ACTIVATE = 0x0006;
        private readonly IntPtr WA_ACTIVE = new IntPtr(1);
        private readonly IntPtr WA_INACTIVE = new IntPtr(0);

        private Point u3dLeftUpPos;

        internal TcpServer WpfServer { get; private set; }
        internal TcpServer TcpFileServer { get; private set; }
        public UnitManager mUnitMan;
        private bool isU3DLoaded = false;
        public Clock mClock;

        InitData mInitData;
        public MainWindow(InitData id)
        {
            mInitData = id;
            InitializeComponent();
            mUnitMan = new UnitManager(this);
            WpfServer = new TcpServer(IPAddress.Parse("127.0.0.1"),500,1,TcpServer.TcpServerMode.EDITOR);
            try
           {
               string HostName = Dns.GetHostName(); //得到主机名
               IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
               for (int i = 0; i < IpEntry.AddressList.Length; i++)
               {
                   //从IP地址列表中筛选出IPv4类型的IP地址
                   //AddressFamily.InterNetwork表示此IP为IPv4,
                   //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                   if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                   {
                        TcpFileServer = new TcpServer(IPAddress.Any, 12345, 10, TcpServer.TcpServerMode.PLAYER);
                        break;
                    }
               }
               
           }
           catch (Exception ex)
           {
               Console.WriteLine(ex.Message);
           }
            
            WpfServer.BindRefs(mUnitMan);
            mClock = new Clock(id.mTimeSpan);
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
            //process.StartInfo.FileName = "Oven\\Ovencooked.exe";
            process.StartInfo.Arguments = "-parentHWND " + hwnd.ToInt32() + " " + Environment.CommandLine;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            process.WaitForInputIdle();
            isU3DLoaded = true;
            // Doesn't work for some reason ?!
            //unityHWND = process.MainWindowHandle;
            EnumChildWindows(hwnd, WindowEnum, IntPtr.Zero);
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(InitialResize);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);

            dispatcherTimer.Start();
            dispatcherTimer2 = new DispatcherTimer();
            dispatcherTimer2.Tick += new EventHandler(MsgHandler);
            dispatcherTimer2.Interval = new TimeSpan(0, 0, 0, 0, 10);

            dispatcherTimer2.Start();

            clockDispatcher = new DispatcherTimer();
            clockDispatcher.Tick += new EventHandler(ClockUpdate);
            clockDispatcher.Interval = new TimeSpan(0, 0, 0, 1);
            clockDispatcher.Start();
            WpfServer.StartServer();
            TcpFileServer.StartServer();
            UnitsGrid.DataContext = mUnitMan.unitsDisplayList;
            //KeyDataGrid.ItemsSource = list;
            
            timeSlider.Maximum = mClock.TimeSpan;
            timeSlider.Value = mClock.CurrentTime;
            currentTimeIndicator.Content = $"{mClock.CurrentTime}";
            maxTimeIndicator.Content = $"{mClock.TimeSpan}";
            camid.ItemsSource = UnitManager.camList;
            
        }

        private void ClockUpdate(object sender, EventArgs e)
        {
            if (mClock.IsPlaying && mClock.CurrentTime < mClock.TimeSpan)
            {
                mClock.CurrentTime += 1;
                currentTimeIndicator.Content = $"{mClock.CurrentTime}";
                maxTimeIndicator.Content = $"{mClock.TimeSpan}";
                timeSlider.Value = (int)mClock.CurrentTime;
            }
        }

        private void MsgHandler(object sender, EventArgs e)
        {
            List<string> tmsg = WpfServer.GetMsg("Spawn");
            if (tmsg!=null)
            {
                Console.WriteLine("msgget");
                mUnitMan.ParseMsg(tmsg);
            }
            tmsg = WpfServer.GetMsg("Select");
            if (tmsg != null)
            {
                Console.WriteLine("msgsel");
                mUnitMan.ParseMsg(tmsg);
            }
            tmsg = WpfServer.GetMsg("Modify");
            if (tmsg != null)
            {
                Console.WriteLine("msgmod");
                mUnitMan.ParseMsg(tmsg);
            }
            tmsg = WpfServer.GetMsg("Add");
            if (tmsg != null)
            {
                Console.WriteLine("msgadd");
                mUnitMan.ParseMsg(tmsg);
            }
            tmsg = WpfServer.GetMsg("Disselect");
            if (tmsg != null)
            {
                Console.WriteLine("msgdissel");
                mUnitMan.ParseMsg(tmsg);
            }
            tmsg = WpfServer.GetMsg("Fire");
            if (tmsg != null)
            {
                Console.WriteLine("msgfire");
                mUnitMan.ParseMsg(tmsg);
            }
            tmsg = WpfServer.GetMsg("SyncTime");
            if (tmsg != null)
            {
                Console.WriteLine("msgsync");
                mUnitMan.ParseMsg(tmsg);
            }
            //dispatcherTimer2.Start();
        }

        private void InitialResize(object sender, EventArgs e)
        {
            //MessageBox.Show("Timer");
            ResizeU3D();
            dispatcherTimer.Stop();


        }
        private void ResizeU3D()
        {
            if (isU3DLoaded)
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
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeU3D();
        }


        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            //statusBar.Text = $"Mouse At {e.MouseDevice.GetPosition(this)} & Window At {u3dLeftUpPos}";
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
                TcpFileServer.QuitServer();
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
            try
            {

                InitWindow iw = new InitWindow();
                iw.Show();
                while (process.HasExited == false)
                    process.Kill();
                //FreeConsole();
                WpfServer.QuitServer();
                this.Close();

            }
            catch (Exception)
            {

            }
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ListBoxItem ts = (UnitCreatorList.SelectedItem as ListBoxItem);
            if (ts != null)
            {
                string con=ts.Content as string;
                Console.WriteLine(con);
                if (con=="驱逐舰")
                {
                    statusBar.Text = $"Select {ts.Content.ToString()}";
                    WpfServer.SendMessage("Spawn DD");
                    statusBar.Text = "按ESC键退出创建模式";
                }else if(con=="摄像机")
                {
                    statusBar.Text = $"Select {ts.Content.ToString()}";
                    WpfServer.SendMessage("Spawn Camera");
                    statusBar.Text = "按ESC键退出创建模式";
                }
                else if (con == "战列舰")
                {
                    statusBar.Text = $"Select {ts.Content.ToString()}";
                    WpfServer.SendMessage("Spawn BB");
                    statusBar.Text = "按ESC键退出创建模式";
                }
                else if (con == "航空母舰")
                {
                    statusBar.Text = $"Select {ts.Content.ToString()}";
                    WpfServer.SendMessage("Spawn CV");
                    statusBar.Text = "按ESC键退出创建模式";
                }
                else if (con == "炮弹")
                {
                    statusBar.Text = $"Select {ts.Content.ToString()}";
                    WpfServer.SendMessage("Spawn Shell");
                    statusBar.Text = "按ESC键退出创建模式";
                }

            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Escape)
            {
                UnitCreatorList.SelectedItem = null;
                WpfServer.SendMessage("Cancel");
                statusBar.Text = "就绪";
            }
        }

        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ResizeU3D();
        }

        

        private void UnitsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnitsGrid.SelectedItem == null) return;
            string selID = (UnitsGrid.SelectedItem as UnitData).ID;
            mUnitMan.selUnit = mUnitMan.GetUnit(int.Parse(selID));
            mUnitMan.UpdateParamList();
            mUnitMan.UpdateKeyframeList();
            WpfServer.SendMessage($"Select {selID}");
            LeftTabCtrl.SelectedIndex = 2;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if(isU3DLoaded)
                dispatcherTimer.Start();
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (isU3DLoaded)
                dispatcherTimer.Start();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mClock.IsPlaying) return;
            mClock.CurrentTime=(int)(timeSlider.Value);
            currentTimeIndicator.Content = $"{mClock.CurrentTime}";
            maxTimeIndicator.Content = $"{mClock.TimeSpan}";
            WpfServer.SendMessage($"Timeleap {(int)mClock.CurrentTime}");
            mUnitMan.UpdateParamList();
        }
        public void Timeshift(int to)
        {
            mClock.CurrentTime = to;
            currentTimeIndicator.Content = $"{mClock.CurrentTime}";
            maxTimeIndicator.Content = $"{mClock.TimeSpan}";
            timeSlider.Value = (int)mClock.CurrentTime;
            WpfServer.SendMessage($"Timeleap {(int)mClock.CurrentTime}");
            mUnitMan.UpdateParamList();
        }
        private void ParamsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            string newKey=(e.Row.DataContext as ParamsData).name;
            string newValue = (e.EditingElement as TextBox).Text;
            float res;
            UnitType ta;
            if (newKey == "类型" && !UnitType.TryParse(newValue, out ta)) { e.Cancel = true; mUnitMan.UpdateParamList(); return; }
            if (newKey != "类型"&&newKey!="名称"&&!float.TryParse(newValue, out res)) { e.Cancel = true; mUnitMan.UpdateParamList(); return; }
            Console.WriteLine(newValue);
            ObservableCollection<ParamsData> toc=ParamsDataGrid.DataContext as ObservableCollection<ParamsData>;
            string[] args = new string[6];
            for (int i = 0; i < toc.Count;i++)
            {
                
                if(toc[i].name== "X坐标")
                {
                    args[0] = toc[i].name==newKey?newValue:toc[i].value;
                }else if (toc[i].name == "Y坐标")
                {
                    args[1] = toc[i].name == newKey ? newValue : toc[i].value;
                }
                else if (toc[i].name == "Z坐标")
                {
                    args[2] = toc[i].name == newKey ? newValue : toc[i].value;
                }else if(toc[i].name=="类型")
                {
                    UnitType tmp2= (UnitType)UnitType.Parse(typeof(UnitType), toc[i].value);
                    args[4] = ((int)tmp2).ToString();
                }else if(toc[i].name=="名称")
                {
                    if(toc[i].name==newKey)
                    {
                        args[5] = newValue;
                        mUnitMan.selUnit.mName = newValue;

                        mUnitMan.UpdateDisplayList();
                    }
                    else
                    {
                        args[5] = toc[i].value;
                    }
                    
                    
                }
            }
            args[3] = mClock.CurrentTime.ToString();
            Position tp = new Position();
            tp.X = args[0];
            tp.Y = args[1];
            tp.Z = args[2];
            tp.T = (int)mClock.CurrentTime;
            if (mUnitMan.selUnit.ContainsTargetAtSameTime(tp))
            {
                WpfServer.SendMessage($"Modify {mUnitMan.selUnit.mID} {args[0]} {args[1]} {args[2]} {args[3]} {args[4]} {args[5]}");
                mUnitMan.selUnit.ModifyTarget(tp);
            }
            else
            {
                WpfServer.SendMessage($"Add {mUnitMan.selUnit.mID} {args[0]} {args[1]} {args[2]} {args[3]} {args[4]} {args[5]}");
                mUnitMan.selUnit.AddTarget(tp.X,tp.Y,tp.Z,tp.T);
            }
            if(int.Parse(args[4])!=(int)mUnitMan.selUnit.mType)
            {
                mUnitMan.selUnit.mType = (UnitType)int.Parse(args[4]);
                mUnitMan.UpdateDisplayList();
            }
            mUnitMan.UpdateKeyframeList();
            
        }

        private void KeyDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if((e.Column.Header as string)=="动作")
            {
                string oldValue = (e.Row.DataContext as KeyframeData).time;
                string newValue = (e.EditingElement as TextBox).Text;
                mUnitMan.selUnit.SetKeyframeMemo(newValue, int.Parse(oldValue));
                mUnitMan.UpdateKeyframeList();
            }
            else if((e.Column.Header as string) == "时刻")
            {
                string oldValue = (e.Row.DataContext as KeyframeData).time;
                string newValue = (e.EditingElement as TextBox).Text;
                Console.WriteLine(oldValue + " " + newValue);
                int tmp;
                if (oldValue == newValue) return;
                if (!int.TryParse(newValue, out tmp)) { e.Cancel = true; return; }
                if (mUnitMan.selUnit.ContainsTargetAtSameTime(tmp))
                {
                    Console.WriteLine("SWAP KEYS");
                    WpfServer.SendMessage($"SwapKey {mUnitMan.selUnit.mID} {oldValue} {newValue}");
                    mUnitMan.selUnit.SwapTime(int.Parse(oldValue), int.Parse(newValue));
                }
                else
                {
                    WpfServer.SendMessage($"Timeshift {mUnitMan.selUnit.mID} {oldValue} {newValue}");
                    mUnitMan.selUnit.TimeshiftTarget(int.Parse(oldValue), int.Parse(newValue));
                }

                Timeshift(tmp);
                mUnitMan.UpdateKeyframeList();
            }
                
        }

        private void KeyDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KeyDataGrid.SelectedItem != null)
            {
                Timeshift(int.Parse((KeyDataGrid.SelectedItem as KeyframeData).time));
                Console.WriteLine(KeyDataGrid.SelectedIndex);
            }
            
        }

        private void LeftTabCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                string ts = LeftTabCtrl.SelectedContent as string;
                if (ts!="单位创建")
                {
                    UnitCreatorList.SelectedItem = null;
                    WpfServer.SendMessage("Cancel");
                    statusBar.Text = "就绪";
                }
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Version(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("场景仿真推演系统\n2015201123杨振华","版本信息",MessageBoxButton.OK);
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDialog = new System.Windows.Forms.FolderBrowserDialog();  //选择文件夹
            openFileDialog.ShowNewFolderButton = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)//注意，此处一定要手动引入System.Window.Forms空间，否则你如果使用默认的DialogResult会发现没有OK属性
            {
                Console.WriteLine(openFileDialog.SelectedPath);

                tmpfilepath = openFileDialog.SelectedPath;
                ScanFiles(tmpfilepath);
                statusBar.Text = "载入成功";
            }

            

            
        }
        string tmpfilepath = null;
        private void SaveFile(object sender, RoutedEventArgs e)
        {


            TrySaveFile();
            
        }
        private bool TrySaveFile()
        {
            if (tmpfilepath == null)
            {
                System.Windows.Forms.FolderBrowserDialog openFileDialog = new System.Windows.Forms.FolderBrowserDialog();  //选择文件夹
                openFileDialog.ShowNewFolderButton = true;
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)//注意，此处一定要手动引入System.Window.Forms空间，否则你如果使用默认的DialogResult会发现没有OK属性
                {
                    Console.WriteLine(openFileDialog.SelectedPath);

                    tmpfilepath = openFileDialog.SelectedPath;
                    ProduceFiles(tmpfilepath);
                    statusBar.Text = "保存成功";
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                ProduceFiles(tmpfilepath);
                statusBar.Text = "保存成功";
                return true;
            }
        }
        private void SaveAs(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDialog = new System.Windows.Forms.FolderBrowserDialog();  //选择文件夹
            openFileDialog.ShowNewFolderButton = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)//注意，此处一定要手动引入System.Window.Forms空间，否则你如果使用默认的DialogResult会发现没有OK属性
            {
                Console.WriteLine(openFileDialog.SelectedPath);

                tmpfilepath = openFileDialog.SelectedPath;
                ProduceFiles(tmpfilepath);

                statusBar.Text = "保存成功";
            }

            
        }

        private void PlaystateBtn_Click(object sender, RoutedEventArgs e)
        {
            if(mClock.IsPlaying)
            {
                mClock.IsPlaying = false;
                PlaystateBtn.Content = "播放";
                WpfServer.SendMessage("Pause");
                LeftPanels.IsEnabled = true;
            }
            else
            {
                mClock.IsPlaying = true;
                PlaystateBtn.Content = "暂停";
                WpfServer.SendMessage("Play");
                LeftPanels.IsEnabled = false;
            }

        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            //ResizeU3D();
        }


        
        List<string> LoadFile(string filePath, string fileName)
        {//载入文件
            
            try
            {
                List<string> ans = new List<string>();
                string name = filePath + '/' + fileName;
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (StreamReader sr = new StreamReader(name))
                {
                    String line;
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        ans.Add(line);
                    }
                }
                return ans;
            }
            catch (Exception e)
            {
                
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                return null;
            }
        }
        
        void ScanFiles(string filePath)
        {
            mInitData.Deserialize(LoadFile(filePath, "InitSettings.sss"));
            mUnitMan.Deserialize(LoadFile(filePath, "UnitList.sss"));
            mUnitMan.mCamMan.Deserialize(LoadFile(filePath, "Dopesheet.sss"));
            mUnitMan.UpdateParamList();
            mUnitMan.UpdateDisplayList();
            mUnitMan.UpdateKeyframeList();
            mUnitMan.UpdateCamList();
            mUnitMan.mCamMan.UpdateDopesheet();
        }
        
        private void Run(object sender, RoutedEventArgs e)
        {
            Timeshift(0);
            mClock.IsPlaying = true;
            PlaystateBtn.Content = "暂停";
            List<string> cmd = mUnitMan.mCamMan.PrepareDopesheetCmd();
            foreach (string s in cmd)
                WpfServer.SendMessage(s);
            WpfServer.SendMessage("Run");
            LeftPanels.IsEnabled = false;
        }

        private void OnDopeSheetNew(object sender, RoutedEventArgs e)
        {
            if (UnitManager.camList.Count > 0)
            {
                mUnitMan.mCamMan.AddTarget(int.Parse(UnitManager.camList[0]), (int)mClock.CurrentTime);
                mUnitMan.mCamMan.UpdateDopesheet();
            }
            else
            {
                MessageBox.Show("请先创建摄像机！", "错误", MessageBoxButton.OK);
            }
        }

        private void DopesheetGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(mUnitMan.mCamMan!=null)
            {
                if ((e.Column.Header as string) == "摄像机ID")
                {
                    string oldValue = (e.Row.DataContext as DopesheetData).camid;
                    string newValue = (e.EditingElement as ComboBox).Text;
                    string newKey = (e.Row.DataContext as DopesheetData).time;
                    mUnitMan.mCamMan.ModifyTarget(new CamManager.CamKey(int.Parse(newValue),int.Parse(newKey)));
                    mUnitMan.mCamMan.UpdateDopesheet();
                }
                else if ((e.Column.Header as string) == "开始时刻")
                {
                    string oldValue = (e.Row.DataContext as DopesheetData).time;
                    string newValue = (e.EditingElement as TextBox).Text;
                    Console.WriteLine(oldValue + " " + newValue);
                    int tmp;
                    if (oldValue == newValue) return;
                    if (!int.TryParse(newValue, out tmp)) { e.Cancel = true; return; }
                    if (mUnitMan.mCamMan.ContainsTargetAtSameTime(tmp))
                    {
                        Console.WriteLine("SWAP KEYS");
                        //WpfServer.SendMessage($"SwapKey {mUnitMan.selUnit.mID} {oldValue} {newValue}");
                        mUnitMan.mCamMan.SwapTime(int.Parse(oldValue), int.Parse(newValue));
                    }
                    else
                    {
                        //WpfServer.SendMessage($"Timeshift {mUnitMan.selUnit.mID} {oldValue} {newValue}");
                        mUnitMan.mCamMan.TimeshiftTarget(int.Parse(oldValue), int.Parse(newValue));
                    }
                    mUnitMan.mCamMan.UpdateDopesheet();
                }
            }
            
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (UnitCreatorTree != null && TeamComboBox != null)
            {
                TreeViewItem ts = (UnitCreatorTree.SelectedItem as TreeViewItem);
                ComboBoxItem cb = (TeamComboBox.SelectedItem as ComboBoxItem);
                if (ts != null && cb != null)
                {
                    string con = ts.Header as string;
                    int team = int.Parse(cb.Content as string);
                    Console.WriteLine(con);
                    SpawnCall(con, team);

                }
            }

        }
        private void SpawnCall(string con,int team)
        {
            Console.WriteLine(con);
            Console.WriteLine(team);
            if (con == "驱逐舰")
            {

                WpfServer.SendMessage($"Spawn DD {team}");
                statusBar.Text = "按ESC键退出创建模式";
            }
            else if (con == "摄像机")
            {

                WpfServer.SendMessage($"Spawn Camera {team}");
                statusBar.Text = "按ESC键退出创建模式";
            }
            else if (con == "战列舰")
            {

                WpfServer.SendMessage($"Spawn BB {team}");
                statusBar.Text = "按ESC键退出创建模式";
            }
            else if (con == "航空母舰")
            {

                WpfServer.SendMessage($"Spawn CV {team}");
                statusBar.Text = "按ESC键退出创建模式";
            }
            else if (con == "炮弹")
            {

                WpfServer.SendMessage($"Spawn Shell {team}");
                statusBar.Text = "按ESC键退出创建模式";
            }
        }
        private void DisableDeleteInDataGrid(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                e.Handled = true;
            }
        }

        private void LaunchShellBtn_Click(object sender, RoutedEventArgs e)
        {
            
                WpfServer.SendMessage($"Aim Shell {mUnitMan.selUnit.mID}");
                statusBar.Text = "左键单击指定攻击目标点，按ESC键取消攻击";
            
        }

        private void LaunchTorpedoBtn_Click(object sender, RoutedEventArgs e)
        {


            WpfServer.SendMessage($"Aim Torpedo {mUnitMan.selUnit.mID}");
            statusBar.Text = "左键单击指定攻击目标点，按ESC键取消攻击";

        }

        private void TeamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(UnitCreatorTree!=null&&TeamComboBox!=null)
            {
                TreeViewItem ts = (UnitCreatorTree.SelectedItem as TreeViewItem);
                ComboBoxItem cb = (TeamComboBox.SelectedItem as ComboBoxItem);
                if (ts != null && cb != null)
                {
                    string con = ts.Header as string;
                    int team = int.Parse(cb.Content as string);

                    SpawnCall(con, team);

                }
            }
            
        }

        private void Publish(object sender, RoutedEventArgs e)
        {
            if (TrySaveFile())
            {


                TcpFileServer.SendFile(tmpfilepath, "InitSettings.sss");
                TcpFileServer.SendFile(tmpfilepath, "UnitList.sss");
                TcpFileServer.SendFile(tmpfilepath, "Dopesheet.sss");
                statusBar.Text = "发布成功";
            }
            else
            {
                statusBar.Text = "发布失败，请重新发布";
            }
        }

        private void LinkSurvey(object sender, RoutedEventArgs e)
        {
            ConnectionControlPage mw = new ConnectionControlPage(TcpFileServer);
            mw.Show();
        }

        void WriteFile(string filePath,string fileName,List<string> content)
        {//写入文件
            string name = filePath + '/' + fileName;
            StreamWriter sw;
            FileInfo t = new FileInfo(name);
            if (!t.Exists)
            {
                //Debug.Log("writing");
                sw = t.CreateText();
            }
            else
            {
                //Debug.Log("Rewriting");
                t.Delete();
                sw = t.CreateText();
            }

            foreach (string s in content)
                sw.WriteLine(s);
            sw.Close();
            sw.Dispose();
        }
        void ProduceFiles(string filePath)
        {//写入文件内容
            WriteFile(filePath, "InitSettings.sss", mInitData.Serialize());
            WriteFile(filePath, "UnitList.sss", mUnitMan.Serialize());
            WriteFile(filePath, "Dopesheet.sss",mUnitMan.mCamMan.Serialize());
        }
    }


}
