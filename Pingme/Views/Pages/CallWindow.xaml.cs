using System.Windows;
using Pingme.Services;
using System;

namespace Pingme.Views.Pages
{
    public partial class CallWindow : Window
    {
        private readonly string _appId;
        private readonly string _channelName;

        private readonly AgoraVideoService _videoService;

        public CallWindow(string appId, string channel)
        {
            InitializeComponent();

            _appId = appId;
            _channelName = channel;

            // Khởi tạo dịch vụ video, truyền 2 container
            _videoService = new AgoraVideoService(LocalVideoContainer, RemoteVideoContainer);

            Loaded += CallWindow_Loaded;
            Closed += CallWindow_Closed;
        }

        private void CallWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _videoService.InitializeAgora(_appId, _channelName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi khi khởi tạo cuộc gọi: " + ex.Message);
            }
        }

        private void CallWindow_Closed(object sender, EventArgs e)
        {
            _videoService.LeaveChannel(); // Dọn dẹp khi đóng cửa sổ
        }

        // (Nếu bạn vẫn dùng VideoContainer để test riêng thì giữ lại, còn không thì có thể bỏ)
        //public void SetVideoControl(UIElement videoElement)
        //{
        //    VideoContainer.Content = videoElement;
        //}
    }
}
