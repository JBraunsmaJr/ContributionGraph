using System.Globalization;
using Github.Actions.ContributionGraph.Models;
using Github.Actions.ContributionGraph.ViewModels;
using Octokit.GraphQL;
using Octokit.GraphQL.Core;

namespace Github.Actions.ContributionGraph;

public static class Util
{
    public static string GetViewPath(string view)
    {
        if (!view.EndsWith(".cshtml"))
            view += ".cshtml";
        
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, view);

        if (!File.Exists(path))
            path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Views", "Shared", view);

        return path;
    }
    // ReSharper disable once InconsistentNaming
    static string ToGraphQL(this DateTime time) =>
        time.ToString("yyyy-MM-dd") + "T" + time.ToString("hh:mm:ss");
    
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
        if (viewBy == ContributionViewBy.Day)
            return items.GroupBy(x => x.Date.ToString("MM/dd"))
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));

        if (viewBy == ContributionViewBy.Month)
            return items.GroupBy(x => x.Date.ToString("MMM"))
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));

        if (viewBy == ContributionViewBy.Year)
            return items.GroupBy(x => x.Date.ToString("yyyy"))
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