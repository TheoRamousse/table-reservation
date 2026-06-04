using FluentValidation;
using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Application.Customers.Commands;

public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Phone,
    string? Email) : IRequest<CustomerDto>;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => x.Email is not null);
    }
}

public sealed class CreateCustomerCommandHandler(ICustomerRepository customerRepo)
    : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = Customer.Create(cmd.FirstName, cmd.LastName, cmd.Phone, cmd.Email);
        await customerRepo.AddAsync(customer, ct);
        await customerRepo.SaveChangesAsync(ct);
        return customer.ToDto();
    }
}
