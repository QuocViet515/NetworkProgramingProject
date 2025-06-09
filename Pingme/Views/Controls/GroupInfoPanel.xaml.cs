using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Pingme.Views.Controls;

namespace Pingme.Views.Controls
{
    /// <summary>
    /// Interaction logic for GroupInfoPanel.xaml
    /// </summary>
    public partial class GroupInfoPanel : UserControl
    {
        public bool IsGroupChat { get; set; } = true;
        public GroupInfoPanel()
        {
            InitializeComponent();
            Loaded += GroupInfoPanel_Loaded;
        }
        private void GroupInfoPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsGroupChat)
            {
                SetupGroupInfo();
                InfoContent.Visibility = Visibility.Visible;
                ToggleInfo.Visibility = Visibility.Collapsed;
            }
            else
            {
                SetupUserProfile();
                InfoContent.Visibility = Visibility.Collapsed;
                ToggleInfo.Visibility = Visibility.Visible;

                ToggleInfo.Checked += (s, ev) => InfoContent.Visibility = Visibility.Visible;
                ToggleInfo.Unchecked += (s, ev) => InfoContent.Visibility = Visibility.Collapsed;
            }
        }
        private void SetupGroupInfo()
        {
            var stack = new StackPanel();

            stack.Children.Add(new Border
            {
                Background = System.Windows.Media.Brushes.LightGray,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock
                {
                    Text = "Thành viên",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            });

            stack.Children.Add(new GroupMember
            {
                ContactName = "Anh Tú",
                AvatarPath = "/Assets/Icons/avater-default.png"
            });

            stack.Children.Add(new GroupMember
            {
                ContactName = "Quốc Việt",
                AvatarPath = "/Assets/Icons/avater-default.png"
            });

            InfoContent.Content = stack;
        }

        private void SetupUserProfile()
        {
            InfoContent.Content = new UserProfileCard
            {
                FullName = "Nhật Trường",
                UserName = "truongnt",
                Email = "nt@example.com",
                Phone = "0123456789",
                Birthday = new DateTime(2003, 4, 12),
                Address = "123 Lê Lợi, Q1, TP.HCM",
                Status = "Online"
            };
        }
    }
}
