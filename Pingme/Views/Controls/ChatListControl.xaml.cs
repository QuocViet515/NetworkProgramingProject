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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pingme.Views.Controls
{
    /// <summary>
    /// Interaction logic for ChatListControl.xaml
    /// </summary>
    public partial class ChatListControl : UserControl
    {
        public ChatListControl()
        {
            InitializeComponent();
        }

        private void Search_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Search.Text == "Tìm kiếm")
            {
                Search.Text = "";
                Search.Foreground = Brushes.Black;
                Search.HorizontalContentAlignment = HorizontalAlignment.Left;
            }
        }

        private void Search_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Search.Text))
            {
                Search.Text = "Tìm kiếm";
                Search.Foreground = Brushes.Black;
                Search.HorizontalContentAlignment = HorizontalAlignment.Center;
            }
        }
    }
}
