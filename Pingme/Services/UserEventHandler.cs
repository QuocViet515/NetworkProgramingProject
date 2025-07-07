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
            //MessageBox.Show($"✅ Đã tham gia kênh: {connection.channelId}, UID: {connection.localUid}");
        }

        public override void OnUserJoined(RtcConnection connection, uint remoteUid, int elapsed)
        {
            //MessageBox.Show($"👤 Người dùng mới: {remoteUid}");

            // Tạo panel và setup canvas trong UI thread
            // Pseudocode plan:
            // 1. Ensure that the Dispatcher.Invoke block is running on the correct thread (WPF UI thread).
            // 2. Check if _videoService.CreateRemotePanel(remoteUid) returns a valid panel with a non-null Handle.
            // 3. Ensure that the panel.Handle is valid and not IntPtr.Zero before using it in VideoCanvas.
            // 4. Add null/handle checks and error handling to prevent runtime exceptions.

            WpfApp.Current.Dispatcher.Invoke(() =>
            {
                var panel = _videoService.CreateRemotePanel(remoteUid);
                if (panel == null || panel.Handle == IntPtr.Zero)
                {
                    MessageBox.Show($"Failed to create remote panel for UID: {remoteUid}");
                    return;
                }

                var canvas = new VideoCanvas
                {
                    view = (long)panel.Handle,
                    renderMode = RENDER_MODE_TYPE.RENDER_MODE_FIT,
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
        public override void OnRemoteVideoStateChanged(
    RtcConnection connection,
    uint remoteUid,
    REMOTE_VIDEO_STATE state,
    REMOTE_VIDEO_STATE_REASON reason,
    int elapsed)
        {
            string message = $"📡 Remote video state changed:\n" +
                             $"- UID: {remoteUid}\n" +
                             $"- STATE: {state}\n" +
                             $"- REASON: {reason}\n" +
                             $"- Elapsed: {elapsed}ms";

            //System.Windows.MessageBox.Show(message);

            // Phân tích trạng thái
            switch (state)
            {
                case REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_STARTING:
                    Console.WriteLine($"⏳ Đang bắt đầu nhận video từ {remoteUid}...");
                    break;

                case REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_DECODING:
                    Console.WriteLine($"✅ Đang hiển thị video từ {remoteUid}");
                    // Đổi nền thành đen (hoặc trong suốt) nếu đang hiển thị bình thường
                    WpfApp.Current.Dispatcher.Invoke(() =>
                    {
                        _videoService.SetRemotePanelColor(remoteUid, System.Drawing.Color.Black);
                    });
                    break;

                case REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_STOPPED:
                    Console.WriteLine($"⛔ Video từ {remoteUid} đã bị dừng (do user tắt cam?)");
                    WpfApp.Current.Dispatcher.Invoke(() =>
                    {
                        _videoService.SetRemotePanelColor(remoteUid, System.Drawing.Color.Red);
                    });
                    break;

                case REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_FAILED:
                    Console.WriteLine($"❌ Lỗi hiển thị video từ {remoteUid} (lý do: {reason})");
                    WpfApp.Current.Dispatcher.Invoke(() =>
                    {
                        _videoService.SetRemotePanelColor(remoteUid, System.Drawing.Color.Red);
                    });
                    break;

                case REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_FROZEN:
                    Console.WriteLine($"🥶 Video từ {remoteUid} bị đứng hình (mạng yếu?)");
                    WpfApp.Current.Dispatcher.Invoke(() =>
                    {
                        _videoService.SetRemotePanelColor(remoteUid, System.Drawing.Color.OrangeRed);
                    });
                    break;
            }
        }



    }

}
