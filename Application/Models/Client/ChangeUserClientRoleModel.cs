using Domain.Enums;

namespace Application.Models.Client;

public sealed record ChangeUserClientRoleModel(Guid TargetId, UserClientRole TargetRole);
