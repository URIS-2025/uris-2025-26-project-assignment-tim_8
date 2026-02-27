using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.AnonymousUser
{
    public class AnonymousUser
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid BoxAccessLinkId { get; set; }
    }
}
