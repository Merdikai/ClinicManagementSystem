using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Doctors.Queries;

#pragma warning disable EXTEXP0018
public class GetDoctorsQueryHandler : IRequestHandler<GetDoctorsQuery, IEnumerable<UserResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly HybridCache _cache;

    public GetDoctorsQueryHandler(IUserRepository userRepository, HybridCache cache)
    {
        _userRepository = userRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<UserResponseDto>> Handle(GetDoctorsQuery request, CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(
            "doctors:all",
            async ct =>
            {
                var users = await _userRepository.GetAllAsync();

                // Filter users with Doctor role
                var doctors = users
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Doctor") && u.IsActive)
                    .Select(u => new UserResponseDto(
                        u.Id,
                        u.Username,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.PhoneNumber
                    ))
                    .ToList();

                return doctors;
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(15)
            },
            tags: ["doctors"],
            cancellationToken: cancellationToken
        );
    }
}
#pragma warning restore EXTEXP0018
