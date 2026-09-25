using AutoMapper;
using Grpc.Core;
using Helpdesk.Assets.Domain.Model;
using Helpdesk.Assets.Domain.Rules;
using Helpdesk.Assets.Grpc;
using Helpdesk.Assets.Infrastructure;
using Helpdesk.Assignments.Grpc;
using Helpdesk.Mapping;
using Helpdesk.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assets.Api.Services;

/// <summary>
/// The only place in the system where one backend calls another directly. It is gRPC, it
/// goes through a configured client address, and it reads Assignment.API's API rather than
/// its database - which is the whole point of the rule.
///
/// Class-level [Authorize(Roles = "Admin")]: the Gateway's AssetController already requires
/// Admin, but per JwtAuthExtensions this service must not blindly trust that - it validates the
/// forwarded token (see InterfacingExtensions' AddBearerForwarding on the Asset gRPC client)
/// and the role itself, independently.
/// </summary>
[Authorize(Roles = "Admin,BaseRole")]
public class AssetService(
    AssetContext db,
    IMapper mapper,
    AssignmentGrpc.AssignmentGrpcClient assignmentClient,
    ILogger<AssetService> logger) : AssetGrpc.AssetGrpcBase
{
    public override async Task<AssetResponse> CreateAsset(CreateAssetRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Tag))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Asset tag is required."));
        }

        var tag = request.Tag.Trim().ToUpperInvariant();
        ValidateLength(tag, TagMaxLength, nameof(request.Tag));
        ValidateLength(request.Name, NameMaxLength, nameof(request.Name));
        ValidateLength(request.SerialNumber, SerialNumberMaxLength, nameof(request.SerialNumber));
        ValidateLength(request.Location, LocationMaxLength, nameof(request.Location));

        if (await db.Assets.AnyAsync(a => a.Tag == tag, context.CancellationToken))
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"Asset tag '{tag}' is already in use."));
        }

        var asset = new Asset
        {
            Tag = tag,
            Name = request.Name?.Trim() ?? string.Empty,
            SerialNumber = request.SerialNumber ?? string.Empty,
            Location = request.Location ?? string.Empty,
            Type = ProtoConverters.ToEnum(request.Type, AssetType.Laptop),
            Status = AssetStatus.InStock,
            ClientId = CurrentClientId(context)
        };

        db.Assets.Add(asset);
        await db.SaveChangesAsync(context.CancellationToken);

        return ToResponse(asset);
    }

    public override async Task<AssetResponse> GetAsset(AssetRequest request, ServerCallContext context)
    {
        var asset = await Query().FirstOrDefaultAsync(a => a.Id == ParseId(request.Id, "asset"), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Asset '{request.Id}' was not found."));

        return ToResponse(asset);
    }

    public override async Task<AssetListResponse> ListAssets(ListAssetsRequest request, ServerCallContext context)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 200);

        var query = Query();

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<AssetStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Type)
            && Enum.TryParse<AssetType>(request.Type, ignoreCase: true, out var type))
        {
            query = query.Where(a => a.Type == type);
        }

        var total = await query.CountAsync(context.CancellationToken);
        var assets = await query
            .OrderBy(a => a.Tag)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(context.CancellationToken);

        var response = new AssetListResponse { Total = total, Page = page, PageSize = pageSize };
        response.Assets.AddRange(assets.Select(ToResponse));
        return response;
    }

    public override async Task<AssetResponse> UpdateAsset(UpdateAssetRequest request, ServerCallContext context)
    {
        var asset = await db.Assets
            .Include(a => a.AssignmentRecords)
            .FirstOrDefaultAsync(a => a.Id == ParseId(request.Id, "asset"), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Asset '{request.Id}' was not found."));

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            ValidateLength(request.Name, NameMaxLength, nameof(request.Name));
            asset.Name = request.Name.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            ValidateLength(request.SerialNumber, SerialNumberMaxLength, nameof(request.SerialNumber));
            asset.SerialNumber = request.SerialNumber.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            ValidateLength(request.Location, LocationMaxLength, nameof(request.Location));
            asset.Location = request.Location.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Type)) asset.Type = ProtoConverters.ToEnum(request.Type, asset.Type);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ProtoConverters.ToEnum(request.Status, asset.Status);
            var hasOpenAssignment = asset.AssignmentRecords.Any(r => r.ReturnedAtUtc is null);

            switch (AssetStatusTransitionRules.Validate(status, hasOpenAssignment))
            {
                case AssetStatusTransitionResult.MustUseAssignAsset:
                    throw new RpcException(new Status(StatusCode.InvalidArgument,
                        "Use AssignAsset to put an asset into the Assigned state."));
                case AssetStatusTransitionResult.MustUseReturnAssetFirst:
                    throw new RpcException(new Status(StatusCode.FailedPrecondition,
                        $"Asset '{asset.Tag}' has an open assignment; use ReturnAsset first."));
            }

            asset.Status = status;
        }

        asset.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        return ToResponse(asset);
    }

    public override async Task<AssetResponse> AssignAsset(AssignAssetRequest request, ServerCallContext context)
    {
        var assetId = ParseId(request.AssetId, "asset");

        var asset = await db.Assets
            .Include(a => a.AssignmentRecords)
            .FirstOrDefaultAsync(a => a.Id == assetId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Asset '{request.AssetId}' was not found."));

        if (asset.Status == AssetStatus.Retired)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "A retired asset cannot be assigned."));
        }

        if (asset.AssignmentRecords.Any(r => r.ReturnedAtUtc is null))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"Asset '{asset.Tag}' is already out on an open assignment."));
        }

        var technicianId = ProtoConverters.ToNullableGuid(request.TechnicianId);
        var assignedTo = request.AssignedTo ?? string.Empty;
        ValidateLength(assignedTo, AssignedToMaxLength, nameof(request.AssignedTo));
        ValidateLength(request.Notes, NotesMaxLength, nameof(request.Notes));

        if (technicianId.HasValue)
        {
            // Cross-service read over gRPC. Assignment.API owns technicians; this service
            // asks it rather than guessing or holding a stale copy. The gRPC call is the only
            // thing in this try - the cross-tenant check below throws its own RpcException, and
            // that must not be caught and rewrapped by the "Assignment.API is unreachable"
            // handler meant only for the call itself.
            TechnicianResponse technician;
            try
            {
                technician = await assignmentClient.GetTechnicianAsync(
                    new TechnicianRequest { Id = technicianId.Value.ToString() },
                    cancellationToken: context.CancellationToken);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"Technician '{technicianId}' does not exist in Assignment.API."));
            }
            catch (RpcException ex)
            {
                logger.LogError(ex, "Assignment.API was unreachable while validating technician {TechnicianId}", technicianId);
                throw new RpcException(new Status(StatusCode.Unavailable,
                    "Technician validation is unavailable; try again shortly."));
            }

            // A technician never works another company's equipment, same rule
            // AssignmentTicketCreatedConsumer already enforces for ticket routing (strictly the
            // ticket's own Client, no cross-company fallback). asset.ClientId is only ever null
            // for a platform-wide asset (a BaseRole caller with no Client of their own) - in
            // that case every technician is fair game, same as a RoutingRule with no ClientId
            // applying to every Client.
            var technicianClientId = ProtoConverters.ToNullableGuid(technician.ClientId);
            if (asset.ClientId is not null && technicianClientId != asset.ClientId)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"Technician '{technicianId}' belongs to a different Client than asset '{asset.Tag}'."));
            }

            if (string.IsNullOrWhiteSpace(assignedTo)) assignedTo = technician.FullName;
        }

        // Added directly on the DbSet, not via asset.AssignmentRecords.Add(...) - the latter is
        // navigation-fixup on an already-tracked parent, and since this entity's Guid Id is a
        // non-default client-generated key, EF Core's change tracker treats it as Modified
        // (an existing row) rather than Added, generating an UPDATE for a row that doesn't
        // exist yet and throwing DbUpdateConcurrencyException. Same pattern Ticket.API's
        // AddComment already uses (db.Comments.Add(...), not ticket.Comments.Add(...)).
        db.AssignmentRecords.Add(new AssignmentRecord
        {
            AssetId = asset.Id,
            TicketId = ProtoConverters.ToNullableGuid(request.TicketId),
            TechnicianId = technicianId,
            AssignedTo = assignedTo,
            Notes = request.Notes ?? string.Empty,
            AssignedAtUtc = DateTime.UtcNow
        });

        asset.Status = AssetStatus.Assigned;
        asset.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(context.CancellationToken);
        return ToResponse(asset);
    }

    public override async Task<AssetResponse> ReturnAsset(ReturnAssetRequest request, ServerCallContext context)
    {
        var assetId = ParseId(request.AssetId, "asset");

        var asset = await db.Assets
            .Include(a => a.AssignmentRecords)
            .FirstOrDefaultAsync(a => a.Id == assetId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Asset '{request.AssetId}' was not found."));

        var open = asset.AssignmentRecords.FirstOrDefault(r => r.ReturnedAtUtc is null)
            ?? throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"Asset '{asset.Tag}' has no open assignment to return."));

        open.ReturnedAtUtc = DateTime.UtcNow;
        open.UpdatedAtUtc = open.ReturnedAtUtc;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            ValidateLength(request.Notes, NotesMaxLength, nameof(request.Notes));
            open.Notes = request.Notes;
        }

        asset.Status = AssetStatus.InStock;
        asset.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(context.CancellationToken);
        return ToResponse(asset);
    }

    private IQueryable<Asset> Query() =>
        db.Assets.AsNoTracking().Include(a => a.AssignmentRecords);

    /// <summary>The creating caller's own Client, from their forwarded JWT - never client-supplied.
    /// Same discipline as Ticket.API's CurrentUser.</summary>
    private static Guid? CurrentClientId(ServerCallContext context) =>
        ProtoConverters.ToNullableGuid(
            context.GetHttpContext()?.User.FindFirst(HelpdeskClaimTypes.ClientId)?.Value);

    private AssetResponse ToResponse(Asset asset)
    {
        var response = mapper.Map<AssetResponse>(asset);
        response.AssignmentRecords.AddRange(
            asset.AssignmentRecords
                .OrderByDescending(r => r.AssignedAtUtc)
                .Select(mapper.Map<AssignmentRecordMessage>));
        return response;
    }

    private static Guid ParseId(string raw, string label) =>
        Guid.TryParse(raw, out var id) && id != Guid.Empty
            ? id
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{raw}' is not a valid {label} id."));

    // Mirrors AssetContext's HasMaxLength calls - checked here too so an oversized field fails
    // with a clean 400 instead of reaching SaveChangesAsync and throwing an unhandled
    // DbUpdateException, which the Gateway can only surface as a bare 500. Reachable straight
    // through the real "New asset" drawer, not just Swagger - AssetDrawer.razor has no
    // client-side MaxLength on the Tag field.
    private const int TagMaxLength = 32;
    private const int NameMaxLength = 128;
    private const int SerialNumberMaxLength = 64;
    private const int LocationMaxLength = 128;
    private const int AssignedToMaxLength = 128;
    private const int NotesMaxLength = 512;

    private static void ValidateLength(string? value, int maxLength, string fieldName)
    {
        if (value is { Length: var length } && length > maxLength)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"{fieldName} must not exceed {maxLength} characters (was {length})."));
        }
    }
}
