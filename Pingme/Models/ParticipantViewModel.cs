using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    public class ParticipantViewModel : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        private bool _hasNewMessage;
        public bool HasNewMessage
        {
            get => _hasNewMessage;
            set { _hasNewMessage = value; OnPropertyChanged(); }
        }

        // (Optional: Avatar, IsOnline, etc.)
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
