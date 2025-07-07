using Agora.Rtc;
using System;
using System.Collections.Generic;
using System.Drawing; // For Color
using System.Windows;
using System.Windows.Controls; // WPF UI
using System.Windows.Forms;
using System.Windows.Forms.Integration; // WindowsFormsHost
using AREA_CODE = Agora.Rtc.AREA_CODE;
using WF = System.Windows.Forms; // Alias cho WinForms
using WpfApp = System.Windows.Application; // Alias cho WPF Application
using System.Threading.Tasks;

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

        private readonly IntPtr _localVideoContainer;
        private readonly IntPtr _remoteVideoContainer;
        private IntPtr _localVideoHandle;
        private StackPanel _remoteContainer;

        public AgoraVideoService(IntPtr localVideoHandle, StackPanel remoteContainer)
        {
            _localVideoHandle = localVideoHandle;
            _remoteContainer = remoteContainer;
        }

        public AgoraVideoService(IntPtr localVideoContainer, IntPtr remoteVideoContainer)
        {
            _localVideoContainer = localVideoContainer;
            _remoteVideoContainer = remoteVideoContainer;
        }
        public AgoraVideoService(ContentControl localContainer, StackPanel remoteContainer)
        {
            _localContainer = localContainer;
            RemoteVideoContainer = remoteContainer;
        }
        
        public void InitializeAgora(string appId, string rawChannelName)
        {
            if (_isJoined) return;

            if (_localContainer == null)
            {
                _localContainer = new ContentControl(); // Initialize or assign a valid instance.
            }

            if (RemoteVideoContainer == null)
            {
                RemoteVideoContainer = new StackPanel();
            }

            _remoteContainer = new StackPanel();

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
                    Width = 640,
                    Height = 360,
                    Dock = DockStyle.None,
                    BackColor = Color.DarkGray
                };


            }

            var canvas = new VideoCanvas
            {
                view = (long)_localVideoHandle,
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
            WF.Panel panel = new WF.Panel
            {
                BackColor = Color.Red,
                Dock = DockStyle.Fill
            };

            WindowsFormsHost host = new WindowsFormsHost
            {
                Child = panel
            };

            _remoteHosts[uid] = host;

            // Thêm vào UI
            WpfApp.Current.Dispatcher.Invoke(() =>
            {
                _remoteContainer.Children.Add(host);
            });

            // Delay 100ms rồi gán handle → tránh lỗi "view = 0"
            WpfApp.Current.Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(100);

                this.Engine.SetupRemoteVideo(new VideoCanvas
                {
                    uid = uid,
                    view = (long)panel.Handle,
                    renderMode = RENDER_MODE_TYPE.RENDER_MODE_FIT
                });
            });

            return panel;
        }





        public void RemoveRemoteVideo(uint uid)
        {
            if (_remoteHosts.TryGetValue(uid, out var host))
            {
                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    _remoteContainer.Children.Remove(host);
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
        public void SetRemotePanelColor(uint uid, System.Drawing.Color color)
        {
            if (_remoteHosts.TryGetValue(uid, out var host))
            {
                if (host.Child is System.Windows.Forms.Panel panel)
                {
                    panel.BackColor = color;
                }
            }
        }

    }
}
