using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ClinicDbContext _context;

    public PaymentRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Payment payment)
        => await _context.Payments.AddAsync(payment);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}