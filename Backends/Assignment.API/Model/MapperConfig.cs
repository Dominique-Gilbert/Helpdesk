using AutoMapper;
using Helpdesk.Assignments.Domain.Model;
using Helpdesk.Assignments.Grpc;
using Helpdesk.Mapping;

namespace Helpdesk.Assignments.Api.Model;

public class MapperConfig : Profile
{
    public MapperConfig()
    {
        CreateMap<Technician, TechnicianResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.CreatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.CreatedAtUtc)))
            .ForMember(d => d.ClientId, o => o.MapFrom(s => ProtoConverters.ToProto(s.ClientId)))
            .ForMember(d => d.Skills, o => o.Ignore())
            .ForMember(d => d.CategoryLevels, o => o.Ignore());

        CreateMap<TechnicianCategoryLevel, TechnicianCategoryLevelMessage>()
            .ForMember(d => d.Level, o => o.MapFrom(s => s.Level.ToString()));

        CreateMap<AssignmentBacklogEntry, BacklogEntryResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.TicketId, o => o.MapFrom(s => ProtoConverters.ToProto(s.TicketId)))
            .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority.ToString()))
            .ForMember(d => d.StartingLevel, o => o.MapFrom(s => s.StartingLevel.ToString()))
            .ForMember(d => d.QueuedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.QueuedAtUtc)));

        CreateMap<RoutingRule, RoutingRuleResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Category ?? string.Empty))
            .ForMember(d => d.MinPriority, o => o.MapFrom(s => s.MinPriority.ToString()))
            .ForMember(d => d.ClientId, o => o.MapFrom(s => ProtoConverters.ToProto(s.ClientId)));

        CreateMap<TicketAssignment, AssignmentResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => ProtoConverters.ToProto(s.Id)))
            .ForMember(d => d.TicketId, o => o.MapFrom(s => ProtoConverters.ToProto(s.TicketId)))
            .ForMember(d => d.TechnicianId, o => o.MapFrom(s => ProtoConverters.ToProto(s.TechnicianId)))
            .ForMember(d => d.TechnicianName, o => o.MapFrom(s => s.Technician != null ? s.Technician.FullName : string.Empty))
            .ForMember(d => d.Team, o => o.MapFrom(s => s.Technician != null ? s.Technician.Team : string.Empty))
            .ForMember(d => d.RoutingRuleId, o => o.MapFrom(s => ProtoConverters.ToProto(s.RoutingRuleId)))
            .ForMember(d => d.RoutingRuleName, o => o.MapFrom(s => s.RoutingRule != null ? s.RoutingRule.Name : string.Empty))
            .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority.ToString()))
            .ForMember(d => d.AssignedAtUtc, o => o.MapFrom(s => ProtoConverters.ToProto(s.AssignedAtUtc)))
            .ForMember(d => d.ClientId, o => o.MapFrom(s => s.Technician != null ? ProtoConverters.ToProto(s.Technician.ClientId) : string.Empty));
    }
}
