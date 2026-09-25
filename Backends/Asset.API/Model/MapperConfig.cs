using AutoMapper;
using Helpdesk.Assets.Domain.Model;
using Helpdesk.Assets.Grpc;
using Helpdesk.Mapping;

namespace Helpdesk.Assets.Api.Model;

public class MapperConfig : Profile
{
    public MapperConfig()
    {
        CreateMap<Asset, AssetResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.PurchasedOnUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.PurchasedOnUtc)))
            .ForMember(d => d.CreatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.CreatedAtUtc)))
            .ForMember(d => d.ClientId, o => o.MapFrom(s => ProtoConverters.ToProto(s.ClientId)))
            .ForMember(d => d.AssignmentRecords, o => o.Ignore());

        CreateMap<AssignmentRecord, AssignmentRecordMessage>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.AssetId, o => o.MapFrom(s => ProtoConverters.ToProto(s.AssetId)))
            .ForMember(d => d.TicketId, o => o.MapFrom(s => ProtoConverters.ToProto(s.TicketId)))
            .ForMember(d => d.TechnicianId, o => o.MapFrom(s => ProtoConverters.ToProto(s.TechnicianId)))
            .ForMember(d => d.AssignedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.AssignedAtUtc)))
            .ForMember(d => d.ReturnedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.ReturnedAtUtc)));
    }
}
