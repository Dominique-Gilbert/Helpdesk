using System.Security.Claims;
using AutoMapper;
using Grpc.Core;
using Helpdesk.Clients.Domain.Model;
using Helpdesk.Clients.Grpc;
using Helpdesk.Clients.Infrastructure;
using Helpdesk.Mapping;
using Helpdesk.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Clients.Api.Services;

/// <summary>
/// Per-method [Authorize], not class-level: Create/List/Update are exclusive to BaseRole (unlike
/// every other backend here, which accepts "Admin,BaseRole" - Admin does not get these). GetClient
/// is deliberately the odd one out - see its own doc comment and the .proto's service comment.
/// </summary>
public class ClientService(ClientContext db, IMapper mapper) : ClientGrpc.ClientGrpcBase
{
    [Authorize(Roles = "BaseRole")]
    public override async Task<ClientResponse> CreateClient(CreateClientRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Company))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Company is required."));
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required."));
        ValidateLength(request.Company, CompanyMaxLength, nameof(request.Company));
        ValidateLength(request.Name, NameMaxLength, nameof(request.Name));
        ValidateLength(request.Email, EmailMaxLength, nameof(request.Email));
        ValidateLength(request.ContactNumber, ContactNumberMaxLength, nameof(request.ContactNumber));
        ValidateLength(request.Description, DescriptionMaxLength, nameof(request.Description));
        ValidateLength(request.AboutInfo, AboutInfoMaxLength, nameof(request.AboutInfo));
        ValidateLength(request.PrimaryColor, PrimaryColorMaxLength, nameof(request.PrimaryColor));
        ValidateLength(request.LogoUrl, LogoUrlMaxLength, nameof(request.LogoUrl));

        var client = new Client
        {
            Company = request.Company.Trim(),
            Name = request.Name.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
            ContactNumber = request.ContactNumber?.Trim() ?? string.Empty,
            Description = request.Description ?? string.Empty,
            AboutInfo = request.AboutInfo ?? string.Empty,
            PrimaryColor = string.IsNullOrWhiteSpace(request.PrimaryColor) ? null : request.PrimaryColor.Trim(),
            LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim()
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync(context.CancellationToken);

        return mapper.Map<ClientResponse>(client);
    }

    /// <summary>Deliberately not BaseRole-only, unlike every other method here - any authenticated
    /// caller may read a single Client by id. This is what makes the Gateway's self-service
    /// branding lookup possible: an Admin/Technician/Support user's own bearer token gets
    /// forwarded straight through to fetch their own Client's public/branding fields, without
    /// needing a whole separate BaseRole-exclusive-bypass mechanism. The Gateway itself still
    /// never lets a caller ask for anyone else's ClientId - see UserService.GetMyBrandingAsync.</summary>
    public override async Task<ClientResponse> GetClient(ClientRequest request, ServerCallContext context)
    {
        var client = await db.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ParseId(request.Id), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Client '{request.Id}' was not found."));

        return mapper.Map<ClientResponse>(client);
    }

    [Authorize(Roles = "BaseRole")]
    public override async Task<ClientListResponse> ListClients(ListClientsRequest request, ServerCallContext context)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 200);

        var query = db.Clients.AsNoTracking();

        var total = await query.CountAsync(context.CancellationToken);
        var clients = await query
            .OrderBy(c => c.Company)
            .ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(context.CancellationToken);

        var response = new ClientListResponse { Total = total, Page = page, PageSize = pageSize };
        response.Clients.AddRange(clients.Select(mapper.Map<ClientResponse>));
        return response;
    }

    [Authorize(Roles = "BaseRole")]
    public override async Task<ClientResponse> UpdateClient(UpdateClientRequest request, ServerCallContext context)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == ParseId(request.Id), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Client '{request.Id}' was not found."));

        if (!string.IsNullOrWhiteSpace(request.Company))
        {
            ValidateLength(request.Company, CompanyMaxLength, nameof(request.Company));
            client.Company = request.Company.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            ValidateLength(request.Name, NameMaxLength, nameof(request.Name));
            client.Name = request.Name.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            ValidateLength(request.Email, EmailMaxLength, nameof(request.Email));
            client.Email = request.Email.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.ContactNumber))
        {
            ValidateLength(request.ContactNumber, ContactNumberMaxLength, nameof(request.ContactNumber));
            client.ContactNumber = request.ContactNumber.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            ValidateLength(request.Description, DescriptionMaxLength, nameof(request.Description));
            client.Description = request.Description;
        }
        if (!string.IsNullOrWhiteSpace(request.AboutInfo))
        {
            ValidateLength(request.AboutInfo, AboutInfoMaxLength, nameof(request.AboutInfo));
            client.AboutInfo = request.AboutInfo;
        }
        if (!string.IsNullOrWhiteSpace(request.PrimaryColor))
        {
            ValidateLength(request.PrimaryColor, PrimaryColorMaxLength, nameof(request.PrimaryColor));
            client.PrimaryColor = request.PrimaryColor.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.LogoUrl))
        {
            ValidateLength(request.LogoUrl, LogoUrlMaxLength, nameof(request.LogoUrl));
            client.LogoUrl = request.LogoUrl.Trim();
        }

        client.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        return mapper.Map<ClientResponse>(client);
    }

    /// <summary>Fully anonymous, unlike GetClient - see the .proto's own doc comment on why this
    /// is a distinct, slimmer response rather than reusing ClientResponse. Never throws NotFound
    /// (an anonymous caller gets an empty response instead) - same non-enumeration discipline as
    /// User.API's GetLoginBrandingHint, which is the only thing that ever calls this.</summary>
    [AllowAnonymous]
    public override async Task<ClientBrandingResponse> GetClientBranding(ClientRequest request, ServerCallContext context)
    {
        var client = await db.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ParseId(request.Id), context.CancellationToken);

        return client is null
            ? new ClientBrandingResponse()
            : new ClientBrandingResponse
            {
                ClientId = ProtoConverters.ToProto(client.Id),
                DisplayName = client.DisplayName ?? client.Company,
                PrimaryColor = client.PrimaryColor ?? string.Empty,
                LogoUrl = client.LogoUrl ?? string.Empty
            };
    }

    /// <summary>Self-service, Admin-only - see the .proto's own doc comment on the independent
    /// re-check this does. Only touches DisplayName/PrimaryColor/LogoUrl; Company (the legal/BaseRole-
    /// managed name) stays untouched, unlike UpdateClient - an Admin gets a branding override, not a
    /// way to rename their own company. Sending an empty display_name clears the override back to
    /// Company.</summary>
    [Authorize(Roles = "Admin")]
    public override async Task<ClientBrandingResponse> UpdateClientBranding(UpdateClientBrandingRequest request, ServerCallContext context)
    {
        var callerClientId = context.GetHttpContext().User.FindFirstValue(HelpdeskClaimTypes.ClientId);
        if (string.IsNullOrEmpty(callerClientId) || !string.Equals(callerClientId, request.Id, StringComparison.OrdinalIgnoreCase))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "You may only change your own Client's theme."));

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == ParseId(request.Id), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Client '{request.Id}' was not found."));

        client.DisplayName = ProtoConverters.ToNullableString(request.DisplayName)?.Trim();
        if (!string.IsNullOrWhiteSpace(request.PrimaryColor))
        {
            ValidateLength(request.PrimaryColor, PrimaryColorMaxLength, nameof(request.PrimaryColor));
            client.PrimaryColor = request.PrimaryColor.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.LogoUrl))
        {
            ValidateLength(request.LogoUrl, LogoUrlMaxLength, nameof(request.LogoUrl));
            client.LogoUrl = request.LogoUrl.Trim();
        }
        client.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(context.CancellationToken);

        return new ClientBrandingResponse
        {
            ClientId = ProtoConverters.ToProto(client.Id),
            DisplayName = client.DisplayName ?? client.Company,
            PrimaryColor = client.PrimaryColor ?? string.Empty,
            LogoUrl = client.LogoUrl ?? string.Empty
        };
    }

    private static Guid ParseId(string raw) =>
        Guid.TryParse(raw, out var id) && id != Guid.Empty
            ? id
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{raw}' is not a valid client id."));

    // Mirrors ClientContext's HasMaxLength calls - checked here too so an oversized field fails
    // with a clean 400 instead of reaching SaveChangesAsync and throwing an unhandled
    // DbUpdateException, which the Gateway can only surface as a bare 500 (same bug class fixed
    // in Ticket.API and Asset.API this round).
    private const int CompanyMaxLength = 128;
    private const int NameMaxLength = 128;
    private const int EmailMaxLength = 256;
    private const int ContactNumberMaxLength = 32;
    private const int DescriptionMaxLength = 512;
    private const int AboutInfoMaxLength = 4000;
    private const int PrimaryColorMaxLength = 9;
    private const int LogoUrlMaxLength = 2048;

    private static void ValidateLength(string? value, int maxLength, string fieldName)
    {
        if (value is { Length: var length } && length > maxLength)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"{fieldName} must not exceed {maxLength} characters (was {length})."));
        }
    }
}
