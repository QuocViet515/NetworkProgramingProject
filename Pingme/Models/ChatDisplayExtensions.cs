using Pingme.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pingme.Models
{
    public partial class Chat
    {
        public string DisplayName
        {
            get
            {
                string currentUid = SessionManager.UID;
                if (currentUid == User1 && SessionManager.CurrentUserMap.TryGetValue(User2, out var user))
                    return user.FullName ?? user.UserName ?? User2;

                if (currentUid == User2 && SessionManager.CurrentUserMap.TryGetValue(User1, out var user2))
                    return user2.FullName ?? user2.UserName ?? User1;

                return "Không rõ";
            }
        }

        public string AvatarUrl
        {
            get
            {
                string currentUid = SessionManager.UID;
                if (currentUid == User1 && SessionManager.CurrentUserMap.TryGetValue(User2, out var user))
                    return user.AvatarUrl ?? "/Assets/Icons/avatar-default.png";

                if (currentUid == User2 && SessionManager.CurrentUserMap.TryGetValue(User1, out var user2))
                    return user2.AvatarUrl ?? "/Assets/Icons/avatar-default.png";

                return "/Assets/Icons/avatar-default.png";
            }
        }
        public string LastMessageText
        {
            get
            {
                if (!string.IsNullOrEmpty(LastMessageId) &&
                    SessionManager.LastMessages.TryGetValue(LastMessageId, out var msg))
                {
                    return msg.Content ?? "[File]";
                }
                return "";
            }
        }

    }
    public partial class Chat : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    public partial class ChatGroup
    {
        public string DisplayName => Name ?? "(Nhóm)";
        public string DisplayAvatar => AvatarUrl ?? "/Assets/Icons/avatar-default.png";
        public string LastMessageText
        {
            get
            {
                if (!string.IsNullOrEmpty(LastMessageId) &&
                    SessionManager.LastMessages.TryGetValue(LastMessageId, out var msg))
                {
                    return msg.Content ?? "[File]";
                }
                return "";
            }
        }

    }
    public partial class ChatGroup : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
