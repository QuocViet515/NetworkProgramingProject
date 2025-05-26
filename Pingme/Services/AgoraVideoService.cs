using System;
using System.Windows;
using System.Windows.Controls; // WPF UI
using System.Windows.Forms.Integration; // WindowsFormsHost
using System.Drawing; // For Color
using Agora.Rtc;
using AREA_CODE = Agora.Rtc.AREA_CODE;
using WF = System.Windows.Forms; // Alias cho WinForms
using System.Collections.Generic;
using WpfApp = System.Windows.Application; // Alias cho WPF Application

namespace Pingme.Services
{
    public class AgoraVideoService
    {
        private IRtcEngine _engine;
        private bool _isJoined = false;

        private WF.Panel _videoPanel;
        private WindowsFormsHost _host;
        private ContentControl _localContainer;
        public static StackPanel RemoteVideoContainer;

        private readonly Dictionary<uint, WindowsFormsHost> _remoteHosts = new Dictionary<uint, WindowsFormsHost>();


        public AgoraVideoService(ContentControl localContainer, StackPanel remoteContainer)
        {
            _localContainer = localContainer;
            RemoteVideoContainer = remoteContainer;
        }

        public void InitializeAgora(string appId, string rawChannelName)
        {
            if (_isJoined) return;

            _engine = RtcEngine.CreateAgoraRtcEngine();

            var context = new RtcEngineContext(
                appId,
                0,
                CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_COMMUNICATION,
                AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT,
                AREA_CODE.AREA_CODE_GLOB,
                new LogConfig()
            );

            int initCode = _engine.Initialize(context);
            if (initCode != 0)
            {
                Console.WriteLine($"❌ Init failed: {initCode}");
                return;
            }

            _engine.InitEventHandler(new UserEventHandler(this));
            _engine.EnableVideo();       // ✅ Bắt buộc để bật camera
            SetupLocalVideo();           // ✅ Khởi tạo UI và gán canvas
            _engine.StartPreview();      // ✅ Hiển thị local video sau khi đã gán view

            string safeChannelName = ChannelNameHelper.EncodeChannelName(rawChannelName);

            int joinCode = _engine.JoinChannel("", safeChannelName, "", 0);
            if (joinCode != 0)
            {
                Console.WriteLine($"❌ JoinChannel failed: {joinCode}");
                return;
            }

            _isJoined = true;
        }



        public void LeaveChannel()
        {
            if (!_isJoined || _engine == null) return;

            _engine.LeaveChannel();
            _engine.Dispose();
            _engine = null;

            _videoPanel?.Dispose();
            _host = null;
            _isJoined = false;

            Console.WriteLine("🚪 Đã rời khỏi kênh.");
        }

        private void SetupLocalVideo()
        {
            if (_videoPanel == null)
            {
                _videoPanel = new WF.Panel
                {
                    BackColor = Color.Black,
                    Dock = WF.DockStyle.Fill // ✅ Thêm WF.
                };

            }

            var canvas = new VideoCanvas
            {
                view = (long)_videoPanel.Handle,
                renderMode = RENDER_MODE_TYPE.RENDER_MODE_FIT,
                uid = 0
            };

            _engine.SetupLocalVideo(canvas);

            _host = new WindowsFormsHost
            {
                Child = _videoPanel
            };

            WpfApp.Current.Dispatcher.Invoke(() =>
            {
                _localContainer.Content = _host;
            });
        }

        public UIElement GetVideoControl()
        {
            if (_host == null)
                SetupLocalVideo();

            return _host;
        }

        public void ToggleMic(bool enable)
        {
            _engine?.MuteLocalAudioStream(!enable);
            Console.WriteLine(enable ? "🎤 Mic bật" : "🔇 Mic tắt");
        }

        public void ToggleCamera(bool enable)
        {
            _engine?.MuteLocalVideoStream(!enable);
            Console.WriteLine(enable ? "📷 Camera bật" : "🚫 Camera tắt");
        }

        public IRtcEngine Engine => _engine;

        public WF.Panel CreateRemotePanel(uint uid)
        {
            WF.Panel panel = null;
            WindowsFormsHost host = null;

            WpfApp.Current.Dispatcher.Invoke(() =>
            {
                panel = new WF.Panel
                {
                    Dock = WF.DockStyle.Fill,
                    BackColor = Color.Gray
                };

                host = new WindowsFormsHost
                {
                    Child = panel
                };

                _remoteHosts[uid] = host;

                //RemoteVideoContainer.Children.Clear();  // hoặc .Add nếu support nhiều người
                RemoteVideoContainer.Children.Add(host);
            });

            return panel;
        }


        public void RemoveRemoteVideo(uint uid)
        {
            if (_remoteHosts.TryGetValue(uid, out var host))
            {
                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    RemoteVideoContainer.Children.Remove(host);
                });
                _remoteHosts.Remove(uid);
            }
        }
        public void SetLocalVideoEnabled(bool enabled)
        {
            if (enabled)
                _engine.EnableLocalVideo(true);
            else
                _engine.EnableLocalVideo(false);
        }

        public void SetLocalAudioEnabled(bool enabled)
        {
            if (enabled)
                _engine.EnableLocalAudio(true);
            else
                _engine.EnableLocalAudio(false);
        }


    }
}
