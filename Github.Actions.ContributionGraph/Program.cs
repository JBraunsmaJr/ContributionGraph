using System.Diagnostics;
using CommandLine;
using Github.Actions.ContributionGraph;
using Github.Actions.ContributionGraph.ViewModels;
using Octokit.GraphQL;
using Razor.Templating.Core;
using static CommandLine.Parser;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        #if DEBUG
        config.AddJsonFile("appsettings");
        config.AddUserSecrets<Program>();
        #endif
        config.AddEnvironmentVariables();

    })
    .Build();

var parser = Default.ParseArguments<ActionInputs>(() => new(), args);
parser.WithNotParsed(
    errors =>
    {
        host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Github.Actions.ContributionGraph")
            .LogError(string.Join(Environment.NewLine, errors.Select(x => x.ToString())));

        Environment.Exit(2);
    });

await parser.WithParsedAsync(options => StartAsync(options, host));
await host.RunAsync();

static async Task StartAsync(ActionInputs inputs, IHost host)
{
    var productInformation = new ProductHeaderValue("BadgerStats", "0.1");
    var connection = new Connection(productInformation, inputs.Token);
    
    var model = new ContributionViewModel
    {
        Title = string.IsNullOrEmpty(inputs.Title)
            ? "Contribution Graph"
            : inputs.Title,
        ViewBy = inputs.GetViewBy()
    };
    
    model.Items = await Util.FetchUserContributions(inputs.Owner, inputs.FarBack(model.ViewBy), DateTime.Today, connection);
    
    var response = await RazorTemplateEngine.RenderAsync("~/Views/Shared/ContributionGraph.cshtml", model);
    
    using var renderer = new PageRenderer();
    var data = await renderer.DownloadImageFromPage(response, "myimage", TimeSpan.FromSeconds(2));
    
    if (data is not null)
    {
        if (!Directory.Exists("images"))
            Directory.CreateDirectory("images");
        
        await File.WriteAllBytesAsync("images/contribution-graph.png", data);
        
        Console.WriteLine("Created contribution graph...");
    }

    Console.WriteLine("Done...");
    Environment.Exit(0);
}

