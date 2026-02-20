using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Organization
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid RoleId { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
