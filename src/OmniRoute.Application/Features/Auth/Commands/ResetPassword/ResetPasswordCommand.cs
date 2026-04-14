using OmniRoute.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniRoute.Application.Features.Auth.Commands.ResetPassword
{
    public record ResetPasswordCommand(string ResetToken, string NewPassword) : IRequest<Result>;
}

