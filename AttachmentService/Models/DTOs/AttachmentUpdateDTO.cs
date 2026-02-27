namespace AttachmentService.Models.DTOs
{
    public class AttachmentUpdateDTO
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Url { get; set; }
    }
}
