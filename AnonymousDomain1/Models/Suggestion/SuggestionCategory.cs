using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Suggestion
{
    public class SuggestionCategory
    {
        public Guid Id {  get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
