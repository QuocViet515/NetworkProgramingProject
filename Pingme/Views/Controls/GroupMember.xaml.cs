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
    /// Interaction logic for GroupMember.xaml
    /// </summary>
    public partial class GroupMember : UserControl
    {
        public GroupMember()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ContactNameProperty =
           DependencyProperty.Register("ContactName", typeof(string), typeof(GroupMember), new PropertyMetadata(""));

        public string ContactName
        {
            get => (string)GetValue(ContactNameProperty);
            set => SetValue(ContactNameProperty, value);
        }

        public static readonly DependencyProperty AvatarPathProperty =
            DependencyProperty.Register("AvatarPath", typeof(string), typeof(GroupMember), new PropertyMetadata(""));

        public string AvatarPath
        {
            get => (string)GetValue(AvatarPathProperty);
            set => SetValue(AvatarPathProperty, value);
        }
    }
}
