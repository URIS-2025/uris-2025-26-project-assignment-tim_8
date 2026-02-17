using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.SuggestionBox
{
    public class SuggestionBox
    {
        public Guid id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public bool IsDarkTheme { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Password { get; set; } 

        public string CreatedBy { get; set; }

        public Guid OrganizationID { get; set; }
    }
}
