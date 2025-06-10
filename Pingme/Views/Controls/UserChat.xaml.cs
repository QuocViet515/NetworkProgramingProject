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
    /// Interaction logic for UserChat.xaml
    /// </summary>
    public partial class UserChat : UserControl
    {
        public UserChat()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(UserChat), new PropertyMetadata(""));
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }



        public static readonly DependencyProperty AvatarPathProperty =
            DependencyProperty.Register("AvatarPath", typeof(string), typeof(UserChat), new PropertyMetadata(""));

        public string AvatarPath
        {
            get => (string)GetValue(AvatarPathProperty);
            set => SetValue(AvatarPathProperty, value);
        }
    }
}
