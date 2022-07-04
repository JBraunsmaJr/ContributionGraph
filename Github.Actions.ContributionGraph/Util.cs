using System.Globalization;
using Github.Actions.ContributionGraph.Models;
using Github.Actions.ContributionGraph.ViewModels;
using Octokit.GraphQL;
using Octokit.GraphQL.Core;

namespace Github.Actions.ContributionGraph;

public static class Util
{
    /// <summary>
    /// Convert <see cref="string"/> version of 'viewBy' into <see cref="ContributionViewBy"/>
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static ContributionViewBy GetViewBy(this ActionInputs config)
    {
        var viewBy = string.IsNullOrEmpty(config.ViewBy)
            ? ContributionViewBy.Month
            : config.ViewBy.ToLower() switch
            {
                "month" => ContributionViewBy.Month,
                "week" => ContributionViewBy.Week,
                _ => ContributionViewBy.Day
            };

        return viewBy;
    }
    
    /// <summary>
    /// Assists with figuring out how far back to make the query. Contains some logic to prevent our query from
    /// exceeding 1 year
    /// </summary>
    /// <param name="config"></param>
    /// <param name="viewBy"></param>
    /// <returns></returns>
    public static DateTime FarBack(this ActionInputs config, ContributionViewBy viewBy)
    {
        var farBackConfig = config.FarBack;
        var farBack = farBackConfig ?? 31;
        
        // Ensure the value we have is sanitized... cuz users....
        farBack = Math.Abs(farBack);
        
        var today = DateTime.Today;

        // GraphQL for github limits us to <= 1 year of stats
        switch (viewBy)
        {
            case ContributionViewBy.Day:
                if (farBack >= 365)
                    farBack = 364;
                return today.AddDays(-farBack);
            case ContributionViewBy.Month:
                if (farBack >= 12)
                    farBack = 11;
                return today.AddMonths(-farBack);
            default:
                if (farBack >= 52)
                    farBack = 51;
                return today.AddDays(farBack * 7);
        }
    }
    
    public static async Task<List<ContributionItem>> FetchUserContributions(string user, DateTime from, DateTime to, Connection connection)
    {
        var response = new Query()
            .User(new Arg<string>(user))
            .ContributionsCollection(from: new Arg<DateTimeOffset>(from), to: new Arg<DateTimeOffset>(to))
            .ContributionCalendar.Weeks
            .Select(week => new
            {
                days = week.ContributionDays.Select(y=>new
                {
                     y.ContributionCount,
                     y.Date
                }).ToList()
            }).Compile();

        var result = await connection.Run(response);

        return result.SelectMany(x => x.days.ToList()).Select(x => new ContributionItem
        {
            Date = DateTime.Parse(x.Date),
            Count = x.ContributionCount
        }).ToList();
    }

    /// <summary>
    /// Groups <paramref name="items"/> by specified <paramref name="viewBy"/>.
    /// </summary>
    /// <param name="items"><see cref="ContributionItem"/>'s to group</param>
    /// <param name="viewBy">Grouping methodology</param>
    /// <returns></returns>
    public static Dictionary<string, int> GetViewBy(this List<ContributionItem> items, ContributionViewBy viewBy)
    {
        items = items.OrderBy(x => x.Date).ToList();
        
        // Simple day display
        if (viewBy == ContributionViewBy.Day)
            return items.GroupBy(x => x.Date.ToString("MM/dd"))
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));

        // Displays month abbreviation
        if (viewBy == ContributionViewBy.Month)
            return items.GroupBy(x => x.Date.ToString("MMM"))
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));

        // This portion could probably be improved because we're just grabbing the first item for a given week
        // versus figuring out the actual starting point for said week.
        Dictionary<string, int> results = new();

        var weeks = items.GroupBy(x =>
            CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(x.Date, CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Sunday));

        foreach (var week in weeks)
        {
            var firstDayOfWeek = week.First().Date.ToString("MM/dd");
            results.Add(firstDayOfWeek, week.Sum(x=>x.Count));
        }

        return results;
    }
}