// See https://aka.ms/new-console-template for more information

using System.Text.Encodings.Web;
using System.Text.Unicode;
using Github.Actions.ContributionGraph.Models;
using Github.Actions.ContributionGraph.ViewModels;
using Microsoft.Extensions.WebEncoders;
using Octokit.GraphQL;
using Razor.Templating.Core;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection()
    .AddLogging()
    .AddOptions();

services.AddRazorTemplating();

var serviceProvider = services.BuildServiceProvider();
var productInformation = new ProductHeaderValue("BadgerStats", "0.1");
var connection = new Connection(productInformation, config["Token"]);

var model = new ContributionViewModel();
model.ViewBy = ContributionViewBy.Month;

Random random = new();
for (var i = 0; i < 100; i++)
{
    model.Items.Add(new ContributionItem
    {
        Count = random.Next(1,50),
        Date = DateTime.Now.AddDays(-random.Next(0,180))
    });
}

var response = await RazorTemplateEngine.RenderAsync("~/Views/Shared/Test.cshtml", model);
await File.WriteAllTextAsync("test.html", response);
//var response = await RazorTemplateEngine.RenderAsync("~/Views/Shared/Test.cshtml");

//await Util.FetchUserContributions("jbraunsmajr", DateTime.Today.AddDays(-31), DateTime.Today, connection);

Console.WriteLine("Hello, World!");