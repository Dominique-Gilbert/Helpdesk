using AutoMapper;
using Helpdesk.Assets.Grpc;
using Helpdesk.Assignments.Grpc;
using Helpdesk.Clients.Grpc;
using Helpdesk.Gateway.Dto;
using Helpdesk.Mapping;
using Helpdesk.Sla.Grpc;
using Helpdesk.Tickets.Grpc;
using Helpdesk.Users.Grpc;

namespace Helpdesk.Gateway.Interfacing.Mapping;

/// <summary>
/// The second mapping hop: generated proto message -> hand-written DTO. It lives here, in
/// the interfacing layer, and never in a backend API project - the backends do not know
/// this DTO shape exists.
/// </summary>
public class GatewayMapperConfig : Profile
{
    public GatewayMapperConfig()
    {
        CreateMap<TicketResponse, DtoTicket>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.requestedByUserId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.RequestedByUserId)))
            .ForMember(d => d.clientId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.ClientId)))
            .ForMember(d => d.slaBreachedAtUtc, o => o.MapFrom(s => ProtoConverters.ToNullableDateTime(s.SlaBreachedAtUtc)))
            .ForMember(d => d.closedAtUtc, o => o.MapFrom(s => ProtoConverters.ToNullableDateTime(s.ClosedAtUtc)))
            .ForMember(d => d.createdAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.CreatedAtUtc)))
            .ForMember(d => d.updatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToNullableDateTime(s.UpdatedAtUtc)));

        CreateMap<CommentMessage, DtoComment>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.ticketId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.TicketId)))
            .ForMember(d => d.createdAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.CreatedAtUtc)));

        CreateMap<AssetResponse, DtoAsset>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.purchasedOnUtc, o => o.MapFrom(s => ProtoConverters.ToNullableDateTime(s.PurchasedOnUtc)))
            .ForMember(d => d.createdAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.CreatedAtUtc)))
            .ForMember(d => d.clientId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.ClientId)));

        CreateMap<AssignmentRecordMessage, DtoAssignmentRecord>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.assetId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.AssetId)))
            .ForMember(d => d.ticketId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.TicketId)))
            .ForMember(d => d.technicianId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.TechnicianId)))
            .ForMember(d => d.assignedAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.AssignedAtUtc)))
            .ForMember(d => d.returnedAtUtc, o => o.MapFrom(s => ProtoConverters.ToNullableDateTime(s.ReturnedAtUtc)));

        CreateMap<TechnicianResponse, DtoTechnician>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.clientId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.ClientId)));

        CreateMap<RoutingRuleResponse, DtoRoutingRule>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.clientId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.ClientId)));

        CreateMap<AssignmentResponse, DtoAssignment>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.ticketId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.TicketId)))
            .ForMember(d => d.technicianId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.TechnicianId)))
            .ForMember(d => d.assignedAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.AssignedAtUtc)))
            .ForMember(d => d.clientId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.ClientId)));

        CreateMap<SlaClockResponse, DtoSlaClock>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.ticketId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.TicketId)))
            .ForMember(d => d.startedAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.StartedAtUtc)))
            .ForMember(d => d.dueAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.DueAtUtc)))
            .ForMember(d => d.stoppedAtUtc, o => o.MapFrom(s => ProtoConverters.ToNullableDateTime(s.StoppedAtUtc)))
            .ForMember(d => d.breachedAtUtc, o => o.MapFrom(s => ProtoConverters.ToNullableDateTime(s.BreachedAtUtc)));

        CreateMap<EscalationMessage, DtoEscalation>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.slaClockId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.SlaClockId)))
            .ForMember(d => d.raisedAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.RaisedAtUtc)));

        CreateMap<SlaAdjustmentMessage, DtoSlaAdjustment>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.slaClockId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.SlaClockId)))
            .ForMember(d => d.adjustedAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.AdjustedAtUtc)));

        CreateMap<TechnicianCategoryLevelMessage, DtoTechnicianCategoryLevel>();

        CreateMap<BacklogEntryResponse, DtoBacklogEntry>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.ticketId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.TicketId)))
            .ForMember(d => d.queuedAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.QueuedAtUtc)));

        CreateMap<LoginResponse, DtoLoginResult>()
            .ForMember(d => d.userId, o => o.MapFrom(s => ProtoConverters.ToGuid(s.UserId)))
            .ForMember(d => d.expiresAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.ExpiresAtUtc)))
            .ForMember(d => d.technicianId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.TechnicianId)));

        CreateMap<UserSummary, DtoUserSummary>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.technicianId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.TechnicianId)))
            .ForMember(d => d.clientId, o => o.MapFrom(s => ProtoConverters.ToNullableGuid(s.ClientId)));

        CreateMap<ClientResponse, DtoClient>()
            .ForMember(d => d.id, o => o.MapFrom(s => ProtoConverters.ToGuid(s.Id)))
            .ForMember(d => d.createdAtUtc, o => o.MapFrom(s => ProtoConverters.ToDateTime(s.CreatedAtUtc)))
            .ForMember(d => d.updatedAtUtc, o => o.MapFrom(s => ProtoConverters.ToNullableDateTime(s.UpdatedAtUtc)))
            .ForMember(d => d.primaryColor, o => o.MapFrom(s => ProtoConverters.ToNullableString(s.PrimaryColor)))
            .ForMember(d => d.logoUrl, o => o.MapFrom(s => ProtoConverters.ToNullableString(s.LogoUrl)));
    }
}
