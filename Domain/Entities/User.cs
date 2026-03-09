using System;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public sealed class User : IdentityUser<Guid>
{
    public IEnumerable<UserClient> UserClients { get; init; } = [];
}
