using Agora.Rtc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using WF = System.Windows.Forms;
using WpfApp = System.Windows.Application;

namespace Pingme.Services
{
    public class AgoraVideoService
    {
        private IRtcEngine _engine;
        private bool _isJoined = false;

        private WF.Panel _videoPanel;
        private WindowsFormsHost _host;

        private ContentControl _localContainer;         // cho kiểu 1
        private StackPanel _remoteContainer;            // cho kiểu 1

        private IntPtr _localVideoHandle;               // cho kiểu 2
        private WindowsFormsHost _remoteHost;           // cho kiểu 2

        private readonly Dictionary<uint, WindowsFormsHost> _remoteHosts = new Dictionary<uint, WindowsFormsHost>();

        // Constructor kiểu 1: dùng WPF Controls
        public AgoraVideoService(ContentControl localContainer, StackPanel remoteContainer)
        {
            _localContainer = localContainer;
            _remoteContainer = remoteContainer;
        }

        // Constructor kiểu 2: dùng handle và WindowsFormsHost
        public AgoraVideoService(IntPtr localVideoHandle, WindowsFormsHost remoteHost)
        {
            _localVideoHandle = localVideoHandle;
            _remoteHost = remoteHost;
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
                Console.WriteLine("❌ Init failed: " + initCode);
                _engine = null;
                return;
            }

            _engine.InitEventHandler(new UserEventHandler(this));
            _engine.EnableVideo();
            SetupLocalVideo();
            _engine.StartPreview();

            string safeChannelName = ChannelNameHelper.EncodeChannelName(rawChannelName);
            int joinCode = _engine.JoinChannel("", safeChannelName, "", 0);
            if (joinCode != 0)
            {
                Console.WriteLine("❌ JoinChannel failed: " + joinCode);
                return;
            }

            _isJoined = true;
        }

        private void SetupLocalVideo()
        {
            if (_videoPanel == null)
            {
                _videoPanel = new WF.Panel
                {
                    Width = 640,
                    Height = 360,
                    Dock = DockStyle.Fill,
                    BackColor = Color.DarkGray
                };
            }

            var canvas = new VideoCanvas
            {
                view = (long)(_localContainer != null ? IntPtr.Zero : _localVideoHandle), // nếu dùng localContainer thì chưa gán view
                renderMode = RENDER_MODE_TYPE.RENDER_MODE_FIT,
                uid = 0
            };
            _engine.SetupLocalVideo(canvas);

            _host = new WindowsFormsHost
            {
                Child = _videoPanel
            };

            if (_localContainer != null)
            {
                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    _localContainer.Content = _host;
                });
            }
            else if (_remoteHost != null)
            {
                _remoteHost.Child = _videoPanel;
            }
        }

        public WF.Panel CreateRemotePanel(uint uid)
        {
            var panel = new WF.Panel
            {
                BackColor = Color.Black,
                Dock = DockStyle.Fill
            };

            var host = new WindowsFormsHost
            {
                Child = panel
            };

            _remoteHosts[uid] = host;

            if (_remoteContainer != null)
            {
                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    _remoteContainer.Children.Add(host);
                });
            }
            else if (_remoteHost != null)
            {
                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    _remoteHost.Child = panel;
                });
            }

            WpfApp.Current.Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(100);
                _engine.SetupRemoteVideo(new VideoCanvas
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
                    if (_remoteContainer != null)
                        _remoteContainer.Children.Remove(host);
                    else if (_remoteHost != null && _remoteHost.Child == host.Child)
                        _remoteHost.Child = null;
                });
                _remoteHosts.Remove(uid);
            }
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

        public void SetLocalVideoEnabled(bool enabled)
        {
            if (_engine == null)
            {
                Console.WriteLine("⚠️ _engine is null in SetLocalVideoEnabled");
                return;
            }
            _engine.EnableLocalVideo(enabled);
        }

        public void SetLocalAudioEnabled(bool enabled)
        {
            if (_engine == null)
            {
                Console.WriteLine("⚠️ _engine is null in SetLocalAudioEnabled");
                return;
            }
            _engine.EnableLocalAudio(enabled);
        }

        public void SetRemotePanelColor(uint uid, Color color)
        {
            if (_remoteHosts.TryGetValue(uid, out var host) && host.Child is WF.Panel panel)
            {
                panel.BackColor = color;
            }
        }

        public IRtcEngine Engine => _engine;
    }
}
