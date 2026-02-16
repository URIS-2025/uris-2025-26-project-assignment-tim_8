using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.AnonymousUser
{
    public class BoxAccessLink
    {
        public Guid Id { get; set; }

        public string AccessToken { get; set; }

        public bool IsActive { get; set; }  

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public Guid SuggestionBoxId { get; set; }
    }
}
