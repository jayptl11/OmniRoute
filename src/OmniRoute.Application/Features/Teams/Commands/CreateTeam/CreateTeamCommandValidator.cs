using FluentValidation;

namespace OmniRoute.Application.Features.Teams.Commands.CreateTeam;

public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.TeamName)
            .NotEmpty().WithMessage("TeamName is required.")
            .MaximumLength(200).WithMessage("TeamName must not exceed 200 characters.");
    }
}
