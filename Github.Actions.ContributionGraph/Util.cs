using System.Globalization;
using Github.Actions.ContributionGraph.Models;
using Github.Actions.ContributionGraph.ViewModels;
using Octokit.GraphQL;
using Octokit.GraphQL.Core;

namespace Github.Actions.ContributionGraph;

public static class Util
{
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

    public static Dictionary<string, int> GetViewBy(this List<ContributionItem> items, ContributionViewBy viewBy)
    {
        items = items.OrderBy(x => x.Date).ToList();
        
        if (viewBy == ContributionViewBy.Day)
            return items.GroupBy(x => x.Date.ToString("MM/dd"))
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));

        if (viewBy == ContributionViewBy.Month)
            return items.GroupBy(x => x.Date.ToString("MMM"))
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));

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