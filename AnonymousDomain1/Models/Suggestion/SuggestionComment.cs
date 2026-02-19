using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Suggestion
{
    public class SuggestionComment
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public bool IsAnonymous {  get; set; }
        public string CreatedBy {  get; set; }
        public DateTime CreatedAt {  get; set; }
        public Guid? SuggestionCommentId { get; set; }
        public Guid? SuggestionId {  get; set; }
        public Guid CommentAuthorId {  get; set; }
    }
}
