using AutoMapper;
using Helpdesk.Clients.Domain.Model;
using Helpdesk.Clients.Grpc;
using Helpdesk.Mapping;

namespace Helpdesk.Clients.Api.Model;

public class MapperConfig : Profile
{
    public MapperConfig()
    {
        CreateMap<Client, ClientResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.CreatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.CreatedAtUtc)))
            .ForMember(d => d.UpdatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.UpdatedAtUtc)))
            // Client.PrimaryColor/LogoUrl are nullable; a proto3 string field's generated setter
            // throws on null (it has no "unset" state, only ""), so these need the explicit
            // null-coalesce - the plain convention mapping AutoMapper would otherwise use here
            // would crash the very first time a Client has no branding set.
            .ForMember(d => d.PrimaryColor, o => o.MapFrom(s => s.PrimaryColor ?? string.Empty))
            .ForMember(d => d.LogoUrl, o => o.MapFrom(s => s.LogoUrl ?? string.Empty));
    }
}
