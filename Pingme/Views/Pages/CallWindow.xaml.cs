using System.Windows;
using Pingme.Services;

namespace Pingme.Views.Pages
{
    public partial class CallWindow : Window
    {
        private string _appId;
        private string _channelName;

        private readonly AgoraVideoService _videoService;

        public CallWindow(string appId, string channel)
        {
            InitializeComponent();

            _videoService = new AgoraVideoService(LocalVideoContainer, RemoteVideoContainer); // truyền container
            _videoService.InitializeAgora(appId, channel);
        }

        private void JoinCall_Click(object sender, RoutedEventArgs e)
        {
            _videoService.InitializeAgora(_appId, _channelName);
            var videoElement = _videoService.GetVideoControl();
            VideoContainer.Content = videoElement;
        }

        public void SetVideoControl(UIElement videoElement)
        {
            VideoContainer.Content = videoElement;
        }
    }
}
