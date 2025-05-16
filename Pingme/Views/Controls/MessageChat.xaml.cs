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
    /// Interaction logic for MessageChat.xaml
    /// </summary>
    public partial class MessageChat : UserControl
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(MessageChat), new PropertyMetadata(""));
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        

        public static readonly DependencyProperty AvatarPathProperty =
            DependencyProperty.Register("AvatarPath", typeof(string), typeof(MessageChat), new PropertyMetadata(""));

        public string AvatarPath
        {
            get => (string)GetValue(AvatarPathProperty);
            set => SetValue(AvatarPathProperty, value);
        }
        public MessageChat()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}

