using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    class GroupChat
    {
        public string GroupId { get; set; }           
        public string GroupName { get; set; }          
        public string AvatarUrl { get; set; }           
        public List<string> MemberIds { get; set; }    
        public DateTime CreatedAt { get; set; }         
        public string LastMessage { get; set; } 
        public int CreateById { get; set; }
    }
}
