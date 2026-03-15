using Domain.Enums;

namespace Application.Models.Client;

public sealed record ClientUserReadModel(string Id, string ClientName, UserClientRole Role);
