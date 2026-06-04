using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reservation.Api.Requests;
using Reservation.Application.Bookings.Commands;
using Reservation.Application.Bookings.Queries;
using Reservation.Application.DTOs;

namespace Reservation.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(IMediator mediator) : ControllerBase
{
    // POST /api/bookings
    [HttpPost]
    [ProducesResponseType<BookingDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookingDto>> Create(
        [FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        var cmd = new CreateBookingCommand(
            request.CustomerId, request.TableId, request.ServiceId,
            request.BookingDate, request.ArrivalTime, request.GuestsCount,
            request.Source, request.SpecialRequests);

        var dto = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    // GET /api/bookings/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BookingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetBookingByIdQuery(id), ct);
        return Ok(dto);
    }

    // GET /api/bookings?customerId={id}
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BookingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetByCustomer(
        [FromQuery] Guid customerId, CancellationToken ct)
    {
        var dtos = await mediator.Send(new GetBookingsByCustomerQuery(customerId), ct);
        return Ok(dtos);
    }

    // PATCH /api/bookings/{id}/status
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<BookingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookingDto>> ChangeStatus(
        Guid id, [FromBody] ChangeBookingStatusRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(new ChangeBookingStatusCommand(id, request.NewStatus), ct);
        return Ok(dto);
    }

    // DELETE /api/bookings/{id}  — corps : { "cancellationReason": "..." }
    [HttpDelete("{id:guid}")]
    [ProducesResponseType<BookingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookingDto>> Cancel(
        Guid id, [FromBody] CancelBookingRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(new CancelBookingCommand(id, request.CancellationReason), ct);
        return Ok(dto);
    }
}
