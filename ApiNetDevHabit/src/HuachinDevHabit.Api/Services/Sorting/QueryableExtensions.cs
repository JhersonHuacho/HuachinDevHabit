using System.Linq.Dynamic.Core;

namespace HuachinDevHabit.Api.Services.Sorting
{
	internal static class QueryableExtensions
	{
		public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, 
			string? sortBy, 
			SortMapping[] mappings,
			string defaultOrderBy = "Id")
		{
			if (string.IsNullOrWhiteSpace(sortBy))
			{
				return query.OrderBy(defaultOrderBy);
			}

			string[] sortFields = sortBy.Split(',')
				.Select(s => s.Trim())
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.ToArray();

			var orderByParts = new List<string>();

			foreach (string field in sortFields)
			{
				(string sortField, bool isDesceding) = ParseSortField(field);

				SortMapping mapping = mappings.First(m =>
					m.SortField.Equals(sortField, StringComparison.OrdinalIgnoreCase));

				string direction = (isDesceding, mapping.Reverse) switch 
				{
					(true, true) => "ASC",
					(true, false) => "DESC",
					(false, true) => "DESC",
					(false, false) => "ASC"
				};

				orderByParts.Add($"{mapping.PropertyName} {direction}");
			}

			string orderBy = string.Join(",", orderByParts);
			
			return query.OrderBy(orderBy);
		}

		private static (string SortField, bool IsDescending) ParseSortField(string field)
		{
			string[] parts = field.Split(' ');
			string sortField = parts[0];
			bool isDescending = parts.Length > 1 &&
								parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

			return (sortField, isDescending);
		}
	}
}
