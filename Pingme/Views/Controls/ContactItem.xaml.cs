using Pingme.Models;
using System.Windows;
using System.Windows.Controls;

namespace Pingme.Views.Controls
{
    public partial class ContactItem : UserControl
    {
        public ContactItem()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ContactNameProperty =
            DependencyProperty.Register("ContactName", typeof(string), typeof(ContactItem), new PropertyMetadata(""));

        public string ContactName
        {
            get => (string)GetValue(ContactNameProperty);
            set => SetValue(ContactNameProperty, value);
        }

        public static readonly DependencyProperty AvatarPathProperty =
            DependencyProperty.Register("AvatarPath", typeof(string), typeof(ContactItem), new PropertyMetadata(""));

        public string AvatarPath
        {
            get => (string)GetValue(AvatarPathProperty);
            set => SetValue(AvatarPathProperty, value);
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle", typeof(string), typeof(ContactItem), new PropertyMetadata(""));

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }
    }
}
