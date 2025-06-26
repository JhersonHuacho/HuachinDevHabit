using FluentValidation;

namespace HuachinDevHabit.Api.DTOs.Users;

public sealed class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserProfileDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
    }
}
