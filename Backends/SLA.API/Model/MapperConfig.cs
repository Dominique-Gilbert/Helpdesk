using AutoMapper;
using Helpdesk.Mapping;
using Helpdesk.Sla.Domain.Model;
using Helpdesk.Sla.Grpc;

namespace Helpdesk.Sla.Api.Model;

public class MapperConfig : Profile
{
    public MapperConfig()
    {
        CreateMap<SlaClock, SlaClockResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.TicketId, o => o.MapFrom(s => ProtoConverters.ToProto(s.TicketId)))
            .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.StartedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.StartedAtUtc)))
            .ForMember(d => d.DueAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.DueAtUtc)))
            .ForMember(d => d.StoppedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.StoppedAtUtc)))
            .ForMember(d => d.BreachedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.BreachedAtUtc)))
            // Computed at the call site, where "now" is known.
            .ForMember(d => d.RemainingSeconds, o => o.Ignore())
            .ForMember(d => d.EscalationLevel, o => o.Ignore())
            .ForMember(d => d.Escalations, o => o.Ignore())
            .ForMember(d => d.Adjustments, o => o.Ignore());

        CreateMap<Escalation, EscalationMessage>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.SlaClockId, o => o.MapFrom(s => ProtoConverters.ToProto(s.SlaClockId)))
            .ForMember(d => d.RaisedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.RaisedAtUtc)));

        CreateMap<SlaAdjustment, SlaAdjustmentMessage>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.SlaClockId, o => o.MapFrom(s => ProtoConverters.ToProto(s.SlaClockId)))
            .ForMember(d => d.AdjustedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.AdjustedAtUtc)));
    }
}
