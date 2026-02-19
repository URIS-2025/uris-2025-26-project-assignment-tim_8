using AnonymousDomain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Problem
{
    public class Problem
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid ProblemBoxId { get; set; }
        public ProblemSuggestionStatus Status {  get; set; }
        public ProblemPriority Priority { get; set; }
    }
}
