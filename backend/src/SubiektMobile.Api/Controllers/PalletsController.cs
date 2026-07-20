using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Pallets;
using SubiektMobile.Application.Products;

namespace SubiektMobile.Api.Controllers;

[ApiController]
public sealed class PalletsController(ISender sender) : ControllerBase
{
    [HttpGet("api/pallets")]
    [Authorize(Policy = Permissions.PalletsManage)]
    [ProducesResponseType(typeof(PagedResult<PalletListItemDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<PalletListItemDto>> List([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        sender.Send(new ListPalletsQuery(page, pageSize), ct);

    [HttpGet("api/warehouse-orders/{warehouseOrderId:guid}/pallets/candidates")]
    [Authorize(Policy = Permissions.PalletsManage)]
    [ProducesResponseType(typeof(PalletCandidatesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PalletCandidatesDto> Candidates(Guid warehouseOrderId, CancellationToken ct) =>
        sender.Send(new ListPalletCandidatesQuery(warehouseOrderId), ct);

    [HttpPost("api/warehouse-orders/{warehouseOrderId:guid}/pallets")]
    [Authorize(Policy = Permissions.PalletsManage)]
    [ProducesResponseType(typeof(PalletDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PalletDetailsDto>> Create(Guid warehouseOrderId,
        CreatePalletRequest request, CancellationToken ct)
    {
        var pallet = await sender.Send(new CreatePalletCommand(warehouseOrderId, request.OperationId,
            request.EmptyPalletWeightKg,
            request.Items?.Select(x => new CreatePalletItemInput(x.WarehouseOrderItemId,
                x.Quantity, x.ItemVersion)).ToList() ?? []), ct);
        return CreatedAtAction(nameof(Get), new { palletId = pallet.Id }, pallet);
    }

    [HttpGet("api/pallets/{palletId:guid}")]
    [Authorize(Policy = Permissions.PalletsManage)]
    [ProducesResponseType(typeof(PalletDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PalletDetailsDto> Get(Guid palletId, CancellationToken ct) =>
        sender.Send(new GetPalletDetailsQuery(palletId), ct);

    [HttpGet("api/pallets/{palletId:guid}/label-preview")]
    [Authorize(Policy = Permissions.PalletsManage)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<FileContentResult> LabelPreview(Guid palletId,
        [FromQuery] PalletLabelLanguage language = PalletLabelLanguage.Polish, CancellationToken ct = default)
    {
        var document = await sender.Send(new GetPalletLabelPreviewQuery(palletId, language), ct);
        return File(document.Content, "application/pdf");
    }

    [HttpPost("api/pallets/{palletId:guid}/label-issues")]
    [Authorize(Policy = Permissions.PalletsManage)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<FileContentResult> IssueLabel(Guid palletId, PalletLabelIssueRequest request,
        CancellationToken ct)
    {
        var document = await sender.Send(new IssuePalletLabelCommand(palletId, request.Mode,
            request.Language), ct);
        return File(document.Content, "application/pdf", document.FileName);
    }
}

public sealed record CreatePalletRequest(Guid OperationId, decimal EmptyPalletWeightKg,
    IReadOnlyCollection<CreatePalletItemRequest>? Items);

public sealed record CreatePalletItemRequest(Guid WarehouseOrderItemId, decimal Quantity, long ItemVersion);

public sealed record PalletLabelIssueRequest(PalletLabelIssueMode Mode,
    PalletLabelLanguage Language = PalletLabelLanguage.Polish);
