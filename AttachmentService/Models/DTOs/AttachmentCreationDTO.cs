using AnonymousAPI.Validation;

namespace AttachmentService.Models.Attachment.DTOs
{
    [RequiresParent]
    public class AttachmentCreationDTO
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Url { get; set; }
        public Guid? SuggestionId { get; set; }
        public Guid? ProblemId { get; set; }
    }
}