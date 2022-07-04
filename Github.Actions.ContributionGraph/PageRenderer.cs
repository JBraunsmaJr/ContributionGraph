using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using HtmlAgilityPack;
using PuppeteerSharp;
using SkiaSharp;

namespace Github.Actions.ContributionGraph;

public class PageRenderer : IDisposable
{
    private BrowserFetcher _browserFetcher;

    public PageRenderer()
    {
        _browserFetcher = new();
    }
    
    /// <summary>
    /// Render <paramref name="htmlContents"/>, and download a full-page render
    /// </summary>
    /// <param name="htmlContents">HTML contents to render</param>
    /// <param name="renderWaitPeriod">Optional. Wait a specified amount of time until taking the screenshot</param>
    /// <returns>Byte array of screenshot, or null if failure</returns>
    /// <exception cref="ArgumentNullException">When browser or htmlContents are not provided</exception>
    public async Task<byte[]?> DownloadPage(string htmlContents, TimeSpan? renderWaitPeriod = null)
    {
        if (string.IsNullOrEmpty(htmlContents))
            throw new ArgumentNullException(nameof(htmlContents));

        try
        {
            await using var browser = await Launch();
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(htmlContents);


            if (renderWaitPeriod != null)
                await page.WaitForTimeoutAsync((int)renderWaitPeriod.Value.TotalMilliseconds);

            var data = await page.ScreenshotDataAsync(new ScreenshotOptions
            {
                Type = ScreenshotType.Png,
                FullPage = true
            });

            await page.CloseAsync();
            await browser.CloseAsync();

            return data;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }

        return null;
    }
    
    /// <summary>
    /// Render <paramref name="htmlContents"/>, grab a base64 encoded string from the DOM using <paramref name="elementId"/>.
    /// </summary>
    /// <param name="htmlContents">HTML Content to render</param>
    /// <param name="elementId">Element containing the base64 image text</param>
    /// <param name="renderWaitPeriod">Optional, wait for this amount of time before downloading image</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">When browser, htmlContents, or elementId are null/not provided</exception>
    public async Task<byte[]?> DownloadImageFromPage(string htmlContents, string elementId, TimeSpan? renderWaitPeriod = null)
    {
        if (string.IsNullOrEmpty(htmlContents))
            throw new ArgumentNullException(nameof(HtmlEncoder));
        if (string.IsNullOrEmpty(elementId))
            throw new ArgumentNullException(nameof(elementId));

        try
        {
            await using var browser = await Launch();    
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(htmlContents);

            if (renderWaitPeriod != null)
                await page.WaitForTimeoutAsync((int)renderWaitPeriod.Value.TotalMilliseconds);

            var content = await page.GetContentAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            elementId = elementId[0] == '#' ? elementId.Substring(1) : elementId;
            var element = doc.GetElementbyId(elementId);
            
            var base64 = element.InnerText;
            var bytes = Convert.FromBase64String(base64[(base64.IndexOf(',') + 1)..]);

            using var ms = new MemoryStream(bytes);
            var image = SKImage.FromEncodedData(ms);
            var data = image.Encode();
            
            using var outstream = new MemoryStream();
            data.SaveTo(outstream);

            await page.CloseAsync();
            await browser.CloseAsync();
            return outstream.ToArray();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }

        return null;
    }
    
    /// <summary>
    /// Helper method for executing a bash command
    /// </summary>
    /// <param name="cmd"></param>
    private async Task Exec(string cmd)
    {
        var escapedArgs = cmd.Replace("\"", "\\\"");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo()
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\""
            }
        };

        process.Start();
        await process.WaitForExitAsync();
    }
    
    /// <summary>
    /// <para>
    ///  Downloads the browser needed for Puppeteer to do its thing
    /// </para>
    /// <para>
    ///    After download, if on a non-windows platform will update
    ///    permissions on the 'downloaded browser'. 
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentNullException">When our local browser fetcher instance is null</exception>
    private async Task DownloadBrowser()
    {
        try
        {
            await _browserFetcher.DownloadAsync();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var path = _browserFetcher.GetExecutablePath(BrowserFetcher.DefaultChromiumRevision);
                Console.WriteLine($"Setting permissions for: {path}");

                await Exec($"chmod 777 {path}");

            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Environment.Exit(1);
        }
    }
    
    /// <summary>
    /// Consolidates downloading and launch logic into 1 place
    /// </summary>
    /// <returns></returns>
    private async Task<Browser> Launch()
    {
        await DownloadBrowser();
        return await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-gpu", "--no-sandbox" }
        });
    }
    
    public void Dispose()
    {
        _browserFetcher?.Dispose();
    }
}