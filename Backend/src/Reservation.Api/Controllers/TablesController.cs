using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.Api.Requests;
using Reservation.Application.DTOs;
using Reservation.Application.Tables.Commands;
using Reservation.Application.Tables.Queries;
using Reservation.Domain.Enums;

namespace Reservation.Api.Controllers;

[ApiController]
[Route("api/tables")]
[Authorize]
public sealed class TablesController(IMediator mediator) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll(CancellationToken ct)
    {
        var dtos = await mediator.Send(new GetTablesQuery(), ct);
        return Ok(new { tables = dtos });
    }

    [HttpGet("availability")]
    [ProducesResponseType<AvailabilityResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AvailabilityResultDto>> GetAvailability(
        [FromQuery] DateOnly date,
        [FromQuery] Guid serviceId,
        [FromQuery] int guestsCount,
        [FromQuery] TableZone? zone,
        CancellationToken ct)
    {
        var dto = await mediator.Send(new GetAvailabilityQuery(date, serviceId, guestsCount, zone), ct);
        return Ok(dto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType<TableDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TableDto>> Create([FromBody] CreateTableRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(
            new CreateTableCommand(request.Number, request.Capacity, request.MinCapacity, request.Zone, request.IsCombinable), ct);
        return CreatedAtAction(null, new { id = dto.Id }, dto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TableDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableDto>> Update(Guid id, [FromBody] UpdateTableRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(
            new UpdateTableCommand(id, request.Capacity, request.MinCapacity, request.Zone, request.IsCombinable, request.IsActive), ct);
        return Ok(dto);
    }
}
