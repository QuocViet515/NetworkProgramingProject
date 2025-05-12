using Pingme.Models;
using Pingme.Services;
using Pingme.ViewModels;
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

namespace Pingme.Views.Controls
{
    /// <summary>
    /// Interaction logic for Sidebar.xaml
    /// </summary>
    public partial class Sidebar : UserControl
    {
        public ChatViewModel ViewModel { get;  set; }

        public Sidebar()
        {
            InitializeComponent();
        }
        private void UserList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = this.DataContext as ChatViewModel;
            MessageBox.Show($"Sidebar.SelectedUser = {vm?.SelectedUser?.fullName ?? "null"}");
        }

    }

}
