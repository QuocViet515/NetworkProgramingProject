using System.Collections.Generic;

namespace Pingme.Models
{
    public class ChatTag
    {
        public string Id { get; set; }            // ID của tag
        public string Name { get; set; }          // Tên tag (ví dụ: Wk, Fam, B.F)
        public string ImageUrl { get; set; }      // Đường dẫn ảnh đại diện cho tag
        public List<string> ChatIds { get; set; } // Danh sách ID các chat thuộc tag này

        public ChatTag()
        {
            ChatIds = new List<string>();
        }
    }
}
