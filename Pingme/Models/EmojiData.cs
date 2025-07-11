using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    public class EmojiData
    {
        public string emoji { get; set; }
        public string description { get; set; }
        public List<string> aliases { get; set; }
        public List<string> tags { get; set; }
        public string category { get; set; }
    }
}
