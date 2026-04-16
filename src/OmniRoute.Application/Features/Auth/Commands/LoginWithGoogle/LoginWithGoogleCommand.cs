using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Auth.DTOs;

namespace OmniRoute.Application.Features.Auth.Commands.LoginWithGoogle;

public record LoginWithGoogleCommand(string IdToken) : ICommand<LoginResponse>;

