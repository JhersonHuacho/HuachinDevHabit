using FluentValidation;

namespace HuachinDevHabit.Api.DTOs.Auth;

public sealed class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}
