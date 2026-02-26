
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.SystemNotification
{
    public class SystemNotification
    {

        public Guid Id { get; set; }
        public string Text { get; set; }
        public bool IsRead { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? AnonymousUserId { get; set; }
        public Guid? ProblemCommentId { get; set; }
        public Guid? SuggestionCommentId { get; set; }


    }
}
