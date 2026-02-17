using AnonymousDomain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Suggestion
{
    public class Suggestion
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid SuggestionBoxId { get; set; }
        public Guid AnonymousUserId { get; set; }
        public ProblemSuggestionStatus Status { get; set; }
    }
}
