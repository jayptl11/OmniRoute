using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Auth.DTOs;
using MediatR;

namespace OmniRoute.Application.Features.Auth.Commands.LoginWithGoogle;

public record LoginWithGoogleCommand(string IdToken) : IRequest<Result<LoginResponse>>;

