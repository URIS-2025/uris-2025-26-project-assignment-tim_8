using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProblemService.Models.Problem
{
    public class ProblemCategory
    {   
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
