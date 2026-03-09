using System;

namespace Application.Models.User;

public sealed record ReadUserModel(Guid Id, string DisplayName,  string? Email);
