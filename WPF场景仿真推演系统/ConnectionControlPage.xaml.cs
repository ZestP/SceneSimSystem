using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WPF场景仿真推演系统
{
    /// <summary>
    /// ConnectionControlPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectionControlPage : Window
    {
        public TcpServer mTcpServer;
        public ConnectionControlPage(TcpServer tcp)
        {
            mTcpServer = tcp;
            InitializeComponent();
            mTcpServer.lb = LinkSurveyList;
            mTcpServer.PrepareDisplayList();
            DispatcherTimer clockDispatcher = new DispatcherTimer();
            clockDispatcher.Tick += new EventHandler(UpdateIP);
            clockDispatcher.Interval = new TimeSpan(0, 0, 0, 1);
            clockDispatcher.Start();
        }
        private void UpdateIP(object sender, EventArgs e)
        {
            mTcpServer.PrepareDisplayList();
        }
    }
}
