using Pingme.Views.Pages;
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

namespace Pingme.Views.Windows
{
    /// <summary>
    /// Interaction logic for IncomingCallWindows.xaml
    /// </summary>
    public partial class IncomingCallWindows : Window
    {
        private string callerName;

        // Constructor nhận thông tin người gọi và tên người gọi
        public IncomingCallWindows(string callerName)
        {
            InitializeComponent();

            // Cập nhật tên người gọi từ Firebase hoặc đối tượng cuộc gọi
            this.callerName = callerName;
            IncomingCallText.Text = $"Cuộc gọi đến từ: {callerName}";  // Cập nhật TextBlock
        }

        // Khi nhấn "Chấp nhận"
        private void BtnAcceptCall_Click(object sender, RoutedEventArgs e)
        {
            // Logic chấp nhận cuộc gọi (ví dụ: mở cửa sổ video call)
            MessageBox.Show($"Cuộc gọi từ {callerName} đã được chấp nhận.");

            // Đóng cửa sổ thông báo
            this.Close();

            // Tạo một cửa sổ video call mới (thay thế bằng cửa sổ cuộc gọi thực tế của bạn)
            var callWindow = new CallWindow("appId", "channelName");
            callWindow.Show();
        }

        // Khi nhấn "Từ chối"
        private void BtnRejectCall_Click(object sender, RoutedEventArgs e)
        {
            // Logic từ chối cuộc gọi (ví dụ: gửi thông báo cho người gọi)
            MessageBox.Show($"Cuộc gọi từ {callerName} đã bị từ chối.");

            // Đóng cửa sổ thông báo
            this.Close();
        }
    }
}
