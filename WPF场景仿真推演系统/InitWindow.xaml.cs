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

namespace WPF场景仿真推演系统
{
    /// <summary>
    /// InitWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InitWindow : Window
    {
        
        public InitWindow()
        {
            InitializeComponent();
            
        }
        public void FinishCreation()
        {
            MainWindow mw = new MainWindow(new InitData(int.Parse(timespanText.Text)));
            mw.Show();
            this.Close();
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FinishCreation();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int o;
            if(!int.TryParse(timespanText.Text,out o))
            {
                timespanText.Text = 100.ToString();
            }
        }
    }
}
