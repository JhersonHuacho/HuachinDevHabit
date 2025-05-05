using HuachinDevHabit.Api.Entities;

namespace HuachinDevHabit.Api.DTOs.Habits
{
	internal static class HabitMapping
	{
		public static HabitDto ToDto(this Habit habit)
		{
			HabitDto habitDto = new()
			{
				Id = habit.Id,
				Name = habit.Name,
				Description = habit.Description,
				Type = habit.Type,
				Frequency = new FrequencyDto
				{
					Type = habit.Frequency.Type,
					TimesPerPeriod = habit.Frequency.TimesPerPeriod
				},
				Target = new TargetDto
				{
					Value = habit.Target.Value,
					Unit = habit.Target.Unit
				},
				Status = habit.Status,
				IsArchived = habit.IsArchived,
				EndDate = habit.EndDate,
				Milestone = habit.Milestone == null ? null : new MilestoneDto
				{
					Target = habit.Milestone.Target,
					Current = habit.Milestone.Current
				},
				CreatedAtUtc = habit.CreatedAtUtc,
			};

			return habitDto;
		}
		public static Habit ToEntity(this CreateHabitDto createHabitDto)
		{
			Habit habit = new()
			{
				Id = $"h_{Guid.NewGuid()}",
				Name = createHabitDto.Name,
				Description = createHabitDto.Description,
				Type = createHabitDto.Type,
				Frequency = new Frequency
				{
					Type = createHabitDto.Frequency.Type,
					TimesPerPeriod = createHabitDto.Frequency.TimesPerPeriod
				},
				Target = new Target
				{
					Value = createHabitDto.Target.Value,
					Unit = createHabitDto.Target.Unit
				},
				Status = HabitStatus.Ongoing,
				IsArchived = false,
				EndDate = createHabitDto.EndDate,
				Milestone = createHabitDto.Milestone == null ? null : new Milestone
				{
					Target = createHabitDto.Milestone.Target,
					Current = 0 // Initialize current progress to 0
				},
				CreatedAtUtc = DateTime.UtcNow
			};

			return habit;
		}

		public static void UpdateFromDto(this Habit habit, UpdateHabitDto updateHabitDto)
		{
			// Update basic properties
			habit.Name = updateHabitDto.Name;
			habit.Description = updateHabitDto.Description;
			habit.Type = updateHabitDto.Type;
			habit.EndDate = updateHabitDto.EndDate;

			// Update frequency (assuming it's immutable, create new instance)
			habit.Frequency = new Frequency
			{
				Type = updateHabitDto.Frequency.Type,
				TimesPerPeriod = updateHabitDto.Frequency.TimesPerPeriod
			};

			// Update target
			habit.Target.Value = updateHabitDto.Target.Value;
			habit.Target.Unit = updateHabitDto.Target.Unit;

			// Update milestone if provided
			if (updateHabitDto.Milestone != null)
			{
				habit.Milestone = habit.Milestone ?? new Milestone(); // Create new if doesn't exist
				habit.Milestone.Target = updateHabitDto.Milestone.Target;
				// Note: We don't update Milestone.Current from DTO to preserve progress
			}

			habit.UpdatedAtUtc = DateTime.UtcNow;
		}
	}
}
