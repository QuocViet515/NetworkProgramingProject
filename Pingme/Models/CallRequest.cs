using Pingme.Views.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    public class CallRequest
    {
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string ChannelName { get; set; }
        public readonly string AppId = "c94888a36cee4d71a2d36eb0e2cc6f9b";
        //public string AvatarUrl { get; set; }
        public long Timestamp { get; set; }
        public string status { get; set; } = "waiting"; // waiting, accepted, declined, missed, canceled
        public string CallerAvatarUrl { get; set; }
        public string ReceiverAvatarUrl { get; set; }
        public string Type { get; set; }
        public string PushId { get; set; }
        public string ChatId { get; set; }
        public bool IsGroup { get; set; }
    }
}
