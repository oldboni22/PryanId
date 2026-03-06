using System;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public sealed class User : IdentityUser<Guid>
{
    public required DisplayName DisplayName { get; set; }
}
