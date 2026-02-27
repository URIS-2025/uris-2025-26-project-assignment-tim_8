namespace OrganizationService.Models.DTOs
{
    public class UserUpdateDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Username { get; set; }
        public Guid RoleId { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
