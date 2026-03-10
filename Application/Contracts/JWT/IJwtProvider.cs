using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Contracts.JWT;

public interface IJwtProvider
{
    Task<TokenPair> Generate(string email, Guid userId);    
}
