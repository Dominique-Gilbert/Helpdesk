using AutoMapper;
using Helpdesk.Clients.Grpc;
using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Helpdesk.Mapping;

namespace Helpdesk.Gateway.Interfacing.Services;

public class ClientService(ClientGrpc.ClientGrpcClient client, IMapper mapper) : IClientService
{
    public async Task<DtoClient> CreateAsync(CreateClientDto request, CancellationToken ct = default)
    {
        var response = await client.CreateClientAsync(new CreateClientRequest
        {
            Company = request.company ?? string.Empty,
            Name = request.name ?? string.Empty,
            Email = request.email ?? string.Empty,
            ContactNumber = request.contactNumber ?? string.Empty,
            Description = request.description ?? string.Empty,
            AboutInfo = request.aboutInfo ?? string.Empty,
            PrimaryColor = request.primaryColor ?? string.Empty,
            LogoUrl = request.logoUrl ?? string.Empty
        }, cancellationToken: ct);

        return mapper.Map<DtoClient>(response);
    }

    public Task<DtoClient?> GetAsync(Guid id, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var response = await client.GetClientAsync(new ClientRequest { Id = id.ToString() }, cancellationToken: ct);
            return mapper.Map<DtoClient>(response);
        });

    public async Task<DtoClientPage> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var response = await client.ListClientsAsync(
            new ListClientsRequest { Page = page, PageSize = pageSize }, cancellationToken: ct);

        return new DtoClientPage
        {
            items = response.Clients.Select(mapper.Map<DtoClient>).ToList(),
            total = response.Total,
            page = response.Page,
            pageSize = response.PageSize
        };
    }

    public Task<DtoClient?> UpdateAsync(Guid id, UpdateClientDto request, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var response = await client.UpdateClientAsync(new UpdateClientRequest
            {
                Id = id.ToString(),
                Company = request.company ?? string.Empty,
                Name = request.name ?? string.Empty,
                Email = request.email ?? string.Empty,
                ContactNumber = request.contactNumber ?? string.Empty,
                Description = request.description ?? string.Empty,
                AboutInfo = request.aboutInfo ?? string.Empty,
                PrimaryColor = request.primaryColor ?? string.Empty,
                LogoUrl = request.logoUrl ?? string.Empty
            }, cancellationToken: ct);

            return mapper.Map<DtoClient>(response);
        });

    public async Task<DtoClientBranding> GetPublicBrandingAsync(Guid clientId, CancellationToken ct = default)
    {
        var response = await client.GetClientBrandingAsync(new ClientRequest { Id = clientId.ToString() }, cancellationToken: ct);
        return new DtoClientBranding
        {
            clientId = ProtoConverters.ToGuid(response.ClientId),
            displayName = response.DisplayName,
            primaryColor = ProtoConverters.ToNullableString(response.PrimaryColor),
            logoUrl = ProtoConverters.ToNullableString(response.LogoUrl)
        };
    }

    public Task<DtoClientBranding?> UpdateMyBrandingAsync(Guid clientId, UpdateMyBrandingDto request, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var response = await client.UpdateClientBrandingAsync(new UpdateClientBrandingRequest
            {
                Id = clientId.ToString(),
                DisplayName = request.displayName ?? string.Empty,
                PrimaryColor = request.primaryColor ?? string.Empty,
                LogoUrl = request.logoUrl ?? string.Empty
            }, cancellationToken: ct);

            return new DtoClientBranding
            {
                clientId = ProtoConverters.ToGuid(response.ClientId),
                displayName = response.DisplayName,
                primaryColor = ProtoConverters.ToNullableString(response.PrimaryColor),
                logoUrl = ProtoConverters.ToNullableString(response.LogoUrl)
            };
        });
}
