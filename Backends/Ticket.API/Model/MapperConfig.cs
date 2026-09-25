using AutoMapper;
using Helpdesk.Mapping;
using Helpdesk.Tickets.Domain.Model;
using Helpdesk.Tickets.Grpc;

namespace Helpdesk.Tickets.Api.Model;

/// <summary>
/// EF entity -> proto message. The second hop (proto -> hand-written DTO) belongs to the
/// gateway's interfacing layer and must not leak in here.
///
/// Repeated fields are Ignore()d and filled with AddRange at the call site: protobuf's
/// RepeatedField is a get-only property and letting AutoMapper guess at it is the kind of
/// thing that works until it silently does not.
/// </summary>
public class MapperConfig : Profile
{
    public MapperConfig()
    {
        CreateMap<Ticket, TicketResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.RequestedByUserId, o => o.MapFrom(s => ProtoConverters.ToProto(s.RequestedByUserId)))
            .ForMember(d => d.RequestedByUsername, o => o.MapFrom(s => s.RequestedByUsername))
            .ForMember(d => d.RequestedByRole, o => o.MapFrom(s => s.RequestedByRole))
            .ForMember(d => d.ClientId, o => o.MapFrom(s => ProtoConverters.ToProto(s.ClientId)))
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Category.ToString()))
            .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.SlaBreachedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.SlaBreachedAtUtc)))
            .ForMember(d => d.ClosedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.ClosedAtUtc)))
            .ForMember(d => d.CreatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.CreatedAtUtc)))
            .ForMember(d => d.UpdatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.UpdatedAtUtc)))
            .ForMember(d => d.Comments, o => o.Ignore());

        CreateMap<Comment, CommentMessage>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.TicketId, o => o.MapFrom(s => ProtoConverters.ToProto(s.TicketId)))
            .ForMember(d => d.CreatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.CreatedAtUtc)));
    }
}
