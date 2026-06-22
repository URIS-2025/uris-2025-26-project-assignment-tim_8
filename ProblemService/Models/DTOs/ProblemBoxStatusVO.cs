namespace ProblemService.Models.DTOs
{
    public class ProblemBoxStatusVO
    {
        public int Status { get; set; } // 0 = Active, 1 = Inactive
        public bool HasPassword { get; set; } // true = password-protected box
    }
}
