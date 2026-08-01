using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, IList<string> roles);
}