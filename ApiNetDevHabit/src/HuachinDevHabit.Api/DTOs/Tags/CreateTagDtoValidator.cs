using FluentValidation;

namespace HuachinDevHabit.Api.DTOs.Tags
{
	public sealed class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
	{
		public CreateTagDtoValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty()
				.WithMessage("Name is required.")
				.MinimumLength(3)
				.WithMessage("Name must be at least 3 characters long.");

			RuleFor(x => x.Description)
				.MaximumLength(50)
				.WithMessage("Description must be at most 50 characters long.");
		}
	}
}
