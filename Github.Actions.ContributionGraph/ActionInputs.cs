using CommandLine;

namespace Github.Actions.ContributionGraph;

public class ActionInputs
{
    private string _viewBy;
    private string _title;

    [Option('o', "owner",
        Required = false,
        HelpText = "The owner, for example: \"dotnet\". Assign from 'github.repository_owner'.")]
    public string Owner { get; set; } = null!;

    [Option('t', "token",
        Required = true,
        HelpText = "Your Personal Access Token for Github GraphQL API")]
    public string Token { get; set; }

    [Option('l', "title",
        Required = false,
        HelpText = "Graph Title")]
    public string Title
    {
        get => _title;
        set => _title = value;
    }

    [Option('v', "viewby",
        Required = true,
        HelpText = "View by day, week, or month")]
    public string ViewBy
    {
        get => _viewBy;
        set
        {
            if (string.IsNullOrEmpty(value))
                return;

            value = value.ToLower();
            switch (value.ToLower())
            {
                case "month":
                case "day":
                case "days":
                case "week": _viewBy = value;
                    break;
            }
        }
    }

    [Option('f', "farback",
        Required = false,
        HelpText = "Amount of time (based on viewby) to go back")]
    public string? FarBackText { get; set; }

    public int? FarBack
    {
        get
        {
            if (string.IsNullOrEmpty(FarBackText))
                return null;
            return int.Parse(FarBackText);
        }
    }
    
    static void ParseAndAssign(string? value, Action<string> assign)
    {
        if (value is { Length: > 0 } && assign is not null)
            assign(value.Split("/")[^1]);
    }
}