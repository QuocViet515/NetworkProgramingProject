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
    /// Interaction logic for ChatSeparator.xaml
    /// </summary>
    public partial class ChatSeparator : UserControl
    {
        public ChatSeparator()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string DateText
        {
            get => (string)GetValue(DateTextProperty);
            set => SetValue(DateTextProperty, value);
        }

        public static readonly DependencyProperty DateTextProperty =
            DependencyProperty.Register("DateText", typeof(string), typeof(ChatSeparator), new PropertyMetadata(""));

        
    }
}
