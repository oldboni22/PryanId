using Domain.Enums;

namespace Application.Models.User;

public sealed record UserClientReadModel(Guid Id, string Name, UserClientRole Role);
