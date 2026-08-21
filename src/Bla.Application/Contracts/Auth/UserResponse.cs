using Bla.Domain.Entities;

namespace Bla.Application.Contracts.Auth;

public sealed record UserResponse(Guid Id, string Email, string Name)
{
    public static UserResponse FromEntity(User user) =>
        new(user.Id, user.Email, user.Name);
}
