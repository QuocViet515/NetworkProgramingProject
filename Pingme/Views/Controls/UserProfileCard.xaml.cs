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
    /// Interaction logic for UserProfileCard.xaml
    /// </summary>
    public partial class UserProfileCard : UserControl
    {
        public UserProfileCard()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty UserNameProperty =
           DependencyProperty.Register(nameof(UserName), typeof(string), typeof(UserProfileCard));

        public static readonly DependencyProperty FullNameProperty =
            DependencyProperty.Register(nameof(FullName), typeof(string), typeof(UserProfileCard));

        public static readonly DependencyProperty EmailProperty =
            DependencyProperty.Register(nameof(Email), typeof(string), typeof(UserProfileCard));

        public static readonly DependencyProperty PhoneProperty =
            DependencyProperty.Register(nameof(Phone), typeof(string), typeof(UserProfileCard));

        public static readonly DependencyProperty BirthdayProperty =
            DependencyProperty.Register(nameof(Birthday), typeof(DateTime), typeof(UserProfileCard));

        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register(nameof(Address), typeof(string), typeof(UserProfileCard));

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(string), typeof(UserProfileCard));

        public string UserName { get => (string)GetValue(UserNameProperty); set => SetValue(UserNameProperty, value); }
        public string FullName { get => (string)GetValue(FullNameProperty); set => SetValue(FullNameProperty, value); }
        public string Email { get => (string)GetValue(EmailProperty); set => SetValue(EmailProperty, value); }
        public string Phone { get => (string)GetValue(PhoneProperty); set => SetValue(PhoneProperty, value); }
        public DateTime Birthday { get => (DateTime)GetValue(BirthdayProperty); set => SetValue(BirthdayProperty, value); }
        public string Address { get => (string)GetValue(AddressProperty); set => SetValue(AddressProperty, value); }
        public string Status { get => (string)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
    }
}
