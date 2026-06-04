using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Customers.Commands;

public record LiftBlacklistCommand(Guid CustomerId) : IRequest<CustomerDto>;

public sealed class LiftBlacklistCommandHandler(ICustomerRepository customerRepo)
    : IRequestHandler<LiftBlacklistCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(LiftBlacklistCommand cmd, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(cmd.CustomerId, ct)
            ?? throw new EntityNotFoundException("Customer", cmd.CustomerId);

        customer.LiftBlacklist();
        await customerRepo.SaveChangesAsync(ct);
        return customer.ToDto();
    }
}
