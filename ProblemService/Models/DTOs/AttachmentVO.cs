namespace ProblemService.Models.DTOs
{
    public class AttachmentVO
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Url { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
