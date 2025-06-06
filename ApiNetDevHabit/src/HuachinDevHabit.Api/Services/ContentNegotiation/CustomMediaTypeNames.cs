﻿namespace HuachinDevHabit.Api.Services.ContentNegotiation
{
	public static class CustomMediaTypeNames
	{
		public static class Application
		{
			public const string JsonV1 = "application/json;v=1";
			public const string JsonV2 = "application/json;v=2";
			public const string HateosJson = "application/vnd.dev-habit.hateoas+json";
			public const string HateosJsonV1 = "application/vnd.dev-habit.hateoas.1+json";
			public const string HateosJsonV2 = "application/vnd.dev-habit.hateoas.2+json";
		}
	}
}
