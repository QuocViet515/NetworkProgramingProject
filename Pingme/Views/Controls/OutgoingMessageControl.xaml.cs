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
    /// Interaction logic for OutgoingMessageControl.xaml
    /// </summary>
    public partial class OutgoingMessageControl : UserControl
    {
        public OutgoingMessageControl(string text)
        {
            InitializeComponent();
            MessageTextBlock.Text = text;
        }
        public OutgoingMessageControl(FrameworkElement element)
        {
            //InitializeComponent();
            //MessageContainer.Children.Add(element);
            InitializeComponent();
            MessageTextBlock.Visibility = Visibility.Collapsed;
            MessageContainer.Children.Add(element);
        }
    }
}
