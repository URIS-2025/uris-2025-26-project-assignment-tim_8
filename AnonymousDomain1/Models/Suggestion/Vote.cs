using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Suggestion
{
    public class Vote
    {
        public Guid Id {  get; set; }
        public DateTime CreatedAt {  get; set; }
        public Guid VoteAuthorId {  get; set; }
        public Guid SuggestionId {  get; set; }
    }
}
