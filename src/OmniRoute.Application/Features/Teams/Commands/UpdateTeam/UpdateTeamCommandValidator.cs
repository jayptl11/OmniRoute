using FluentValidation;

namespace OmniRoute.Application.Features.Teams.Commands.UpdateTeam;

public class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.TeamName)
            .NotEmpty().WithMessage("TeamName is required.")
            .MaximumLength(200).WithMessage("TeamName must not exceed 200 characters.");
    }
}
