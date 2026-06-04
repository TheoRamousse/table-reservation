using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reservation.Application.DTOs;
using Reservation.Application.Floor.Queries;

namespace Reservation.Api.Controllers;

[ApiController]
[Route("api/floor")]
public sealed class FloorController(IMediator mediator) : ControllerBase
{
    [HttpGet("snapshot")]
    [ProducesResponseType<FloorSnapshotDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FloorSnapshotDto>> GetSnapshot(
        [FromQuery] DateOnly date, [FromQuery] Guid serviceId, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetFloorSnapshotQuery(date, serviceId), ct);
        return Ok(dto);
    }
}
