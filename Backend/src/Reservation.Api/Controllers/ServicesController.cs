using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reservation.Api.Requests;
using Reservation.Application.DTOs;
using Reservation.Application.Services.Commands;
using Reservation.Application.Services.Queries;

namespace Reservation.Api.Controllers;

[ApiController]
[Route("api/services")]
public sealed class ServicesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DiningServiceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DiningServiceDto>>> GetAll(CancellationToken ct)
    {
        var dtos = await mediator.Send(new GetServicesQuery(), ct);
        return Ok(new { services = dtos });
    }

    [HttpPost]
    [ProducesResponseType<DiningServiceDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DiningServiceDto>> Create([FromBody] CreateDiningServiceRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(
            new CreateDiningServiceCommand(request.Name, request.StartTime, request.EndTime,
                request.LastBookingTime, request.DurationMinutes, request.MaxCovers), ct);
        return CreatedAtAction(null, new { id = dto.Id }, dto);
    }
}
