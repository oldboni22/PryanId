using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Contracts.JWT;

public interface IJwtProvider
{
    Task<string> Generate(Guid userId, string email, IEnumerable<string> roles);    
}
