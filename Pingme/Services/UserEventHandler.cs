using Agora.Rtc;
using System;
using System.Windows.Forms;
using WpfApp = System.Windows.Application;

namespace Pingme.Services
{
    public class UserEventHandler : IRtcEngineEventHandler
    {
        private readonly AgoraVideoService _videoService;
        public UserEventHandler(AgoraVideoService service)
        {
            _videoService = service;
        }

        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            Console.WriteLine($"✅ Đã tham gia kênh: {connection.channelId}, UID: {connection.localUid}");
        }

        public override void OnUserJoined(RtcConnection connection, uint remoteUid, int elapsed)
        {
            Console.WriteLine($"👤 Người dùng mới: {remoteUid}");

            // Tạo panel và setup canvas trong UI thread
            WpfApp.Current.Dispatcher.Invoke(() =>
            {
                var panel = _videoService.CreateRemotePanel(remoteUid);

                var canvas = new VideoCanvas
                {
                    view = (long)panel.Handle,
                    renderMode = RENDER_MODE_TYPE.RENDER_MODE_HIDDEN,
                    uid = remoteUid
                };

                _videoService.Engine.SetupRemoteVideo(canvas);
            });
        }


        public override void OnUserOffline(RtcConnection connection, uint remoteUid, USER_OFFLINE_REASON_TYPE reason)
        {
            Console.WriteLine($"👋 Người dùng rời kênh: {remoteUid}");
            _videoService.RemoveRemoteVideo(remoteUid);
        }

    }

}
