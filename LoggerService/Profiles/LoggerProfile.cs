using AutoMapper;
using LoggerService.Models;
using LoggerService.Models.DTOs;


namespace LoggerService.Profiles
{
    public class LoggerProfile : Profile
    {
        public LoggerProfile()
        {
            CreateMap<Log, LogDTO>();
            CreateMap<LogCreationDTO, Log>();
        }
    }
}
