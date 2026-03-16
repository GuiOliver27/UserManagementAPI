using AutoMapper;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;

namespace UserManagementAPI.Data;

public class MappingProfile : Profile {
    public MappingProfile() {
        CreateMap<User, UserResponse>();
        CreateMap<RegisterRequest, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
    }
}