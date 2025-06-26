using FluentValidation.TestHelper;
using HuachinDevHabit.Api.DTOs.Entries;
using HuachinDevHabit.Api.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuachinDevHabit.UnitTests.Validators
{
	public sealed class CreateEntryBatchDtoValidatorTests
	{
		private readonly CreateEntryBatchDtoValidator _validator;
		private readonly CreateEntryDtoValidator _entryValidator = new();

		public CreateEntryBatchDtoValidatorTests()
		{
			_validator = new CreateEntryBatchDtoValidator(_entryValidator);
		}

		[Fact]
		public async Task Validate_ShouldNotReturnError_WhenAllPropertiesAreValid()
		{
			// Arrange
			var dto = new CreateEntryBatchDto
			{
				Entries = new List<CreateEntryDto>
				{
					new CreateEntryDto
					{
						HabitId = Habit.NewId(),
						Value = 1,
						Date = DateOnly.FromDateTime(DateTime.UtcNow)
					}
				}
			};
			// Act
			//var result = await _validator.ValidateAsync(dto);
			TestValidationResult<CreateEntryBatchDto>? result = await _validator.TestValidateAsync(dto);

			// Assert
			//Assert.True(result.IsValid);
			//Assert.Empty(result.Errors);
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Fact]
		public async Task Validate_ShouldReturnError_WhenEntriesIsEmpty()
		{
			// Arrange
			var dto = new CreateEntryBatchDto
			{
				Entries = new List<CreateEntryDto>()
			};

			// Act
			TestValidationResult<CreateEntryBatchDto>? result = await _validator.TestValidateAsync(dto);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Entries);
		}

		[Fact]
		public async Task Validate_ShouldReturnError_WhenEntriesExceedMaxBatchSize()
		{
			// Arrange
			var dto = new CreateEntryBatchDto
			{
				Entries = Enumerable.Range(0, 21)
					.Select(_ => new CreateEntryDto
					{
						HabitId = Habit.NewId(),
						Value = 1,
						Date = DateOnly.FromDateTime(DateTime.UtcNow)
					}).ToList()
			};

			// Act
			TestValidationResult<CreateEntryBatchDto>? result = await _validator.TestValidateAsync(dto);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Entries);
		}

		[Fact]
		public async Task Validate_ShouldReturnError_WhenEntryIsInvalid()
		{
			// Arrange
			var dto = new CreateEntryBatchDto
			{
				Entries =
				[
					new CreateEntryDto
					{
						HabitId = string.Empty, // Invalid
						Value = 1,
						Date = DateOnly.FromDateTime(DateTime.UtcNow)
					}
				]
			};

			// Act
			TestValidationResult<CreateEntryBatchDto>? result = await _validator.TestValidateAsync(dto);

			// Assert
			result.ShouldHaveValidationErrorFor("Entries[0].HabitId");
		}
	}
}
