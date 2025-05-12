using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    class CallRequest
    {
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string ChannelName { get; set; }
        public long timestamp { get; set; }   
    }
}
