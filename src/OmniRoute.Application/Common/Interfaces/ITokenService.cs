using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OmniRoute.Domain.Entities;

namespace OmniRoute.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateResetPasswordToken(string email);
        string? ValidateResetPasswordToken(string token);

        string GenerateAccessToken(User user);

        string GenerateRefreshToken();
        string HashRefreshToken(string token);

        int RefreshTokenExpirationDays { get; }

        (string? TokenId, DateTime? ExpiresAt) ExtractTokenInfo(string token);
    }
}

