using AutoMapper;
using AnonymousDomain.Models.Attachment;
using AttachmentService.Models.DTOs;
using AttachmentService.Models.Attachment.DTOs;

namespace AnonymousRepository.Profiles
{
    public class AttachmentProfile : Profile
    {
        public AttachmentProfile()
        {
            CreateMap<Attachment, AttachmentDTO>();
            CreateMap<AttachmentCreationDTO, Attachment>();
            CreateMap<AttachmentUpdateDTO, Attachment>();
            CreateMap<Attachment, AttachmentDTO>().ReverseMap();
        }
    }
}