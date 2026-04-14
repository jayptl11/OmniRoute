using OmniRoute.Application.Common.Models;
using MediatR;

namespace OmniRoute.Application.Features.Auth.Commands.ResendOtp;

public record ResendOtpCommand(string Email) : IRequest<Result>;

