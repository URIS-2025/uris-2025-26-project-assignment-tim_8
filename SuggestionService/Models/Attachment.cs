namespace AnonymousDomain.Models.Attachment
{
    public class Attachment
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Url { get; set; }
        public DateTime UploadedAt { get; set; }
        public Guid? ProblemId { get; set; }
        public Guid? SuggestionId { get; set; }
    }
}
