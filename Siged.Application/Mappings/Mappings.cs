using AutoMapper;
using Siged.Application.DTOs.Security;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Security;

namespace Siged.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeo unificado: Del DTO a las entidades de la UNAS
            CreateMap<UserCreateDto, Estudiante>();
            CreateMap<UserCreateDto, Encargado>();

            // Si necesitas mapear hacia Usuario también
            CreateMap<UserCreateDto, Usuario>()
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password));
        }
    }
}