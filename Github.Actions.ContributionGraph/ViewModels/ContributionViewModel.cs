using Github.Actions.ContributionGraph.Models;

namespace Github.Actions.ContributionGraph.ViewModels;

public enum ContributionViewBy
{
    Day,
    Week,
    Month,
    Year
}

public class ContributionViewModel
{
    public List<ContributionItem> Items { get; set; } = new();
    public ContributionViewBy ViewBy { get; set; }
}