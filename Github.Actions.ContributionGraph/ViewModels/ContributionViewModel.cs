using Github.Actions.ContributionGraph.Models;

namespace Github.Actions.ContributionGraph.ViewModels;

public enum ContributionViewBy
{
    Day,
    Week,
    Month
}

public class ContributionViewModel
{
    public string? Title { get; set; }
    public List<ContributionItem> Items { get; set; } = new();
    public ContributionViewBy ViewBy { get; set; }
}