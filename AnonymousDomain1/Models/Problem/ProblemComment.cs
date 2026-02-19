using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Problem
{
    public class ProblemComment
    {
        public Guid Id { get; set; }
        public string CommentText { get; set; }
        public bool IsAnonymous { get; set; }
        public Guid? ProblemCommentId { get; set; }
        public Guid? ProblemId { get; set; }
        public Guid ProblemCommentAuthorId { get; set; }
    }
}
