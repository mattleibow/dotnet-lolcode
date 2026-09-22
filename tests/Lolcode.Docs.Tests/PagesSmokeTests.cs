using Microsoft.Playwright;

namespace Lolcode.Docs.Tests;

public sealed class PagesSmokeTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly string _baseUrl = Environment.GetEnvironmentVariable("LOLCODE_SITE_URL")!;

    public async Task InitializeAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "chrome",
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();
    }

    [SiteTestFact]
    public async Task Desktop_site_surfaces_load_without_browser_errors()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1000 },
            ColorScheme = ColorScheme.Dark,
        });
        var page = await context.NewPageAsync();
        var browserErrors = CaptureBrowserErrors(page);

        var destinations = new[]
        {
            new Destination("", "HAI. WHAT DO YOU WANT TO MAKE?"),
            new Destination("docs/", "dotnet-lolcode :3"),
            new Destination("docs/learn/first-program.html", "01 Your first program"),
            new Destination("docs/projects/multiple-files.html", "Multiple source files"),
            new Destination("docs/reference/language-spec.html", "LOLCODE 1.2 Language Specification"),
            new Destination("docs/compiler-course/index.html", "Build your own .NET language compiler"),
            new Destination("docs/api/Lolcode.CodeAnalysis.html", null),
            new Destination("playground/", "HAI, browser. I CAN HAZ code?"),
        };

        foreach (Destination destination in destinations)
        {
            await page.GotoAsync(new Uri(new Uri(_baseUrl), destination.Path).ToString());
            if (destination.Heading is not null)
                await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = destination.Heading }));
            else
                await ExpectVisible(page.Locator("main"));
            await AssertNoHorizontalOverflow(page);
        }

        browserErrors.Should().BeEmpty();
    }

    [SiteTestFact]
    public async Task Mobile_pages_load_without_overflow()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IsMobile = true,
            ColorScheme = ColorScheme.Dark,
        });
        var page = await context.NewPageAsync();

        foreach (string path in new[] { "", "docs/", "docs/learn/first-program.html", "playground/" })
        {
            await page.GotoAsync(new Uri(new Uri(_baseUrl), path).ToString());
            await ExpectVisible(page.Locator("main"));
            await AssertNoHorizontalOverflow(page);
        }
    }

    private static List<string> CaptureBrowserErrors(IPage page)
    {
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
                errors.Add(message.Text);
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
                errors.Add($"HTTP {response.Status}: {response.Url}");
        };
        return errors;
    }

    private static async Task ExpectVisible(ILocator locator)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000,
        });
    }

    private static async Task AssertNoHorizontalOverflow(IPage page)
    {
        var hasOverflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        hasOverflow.Should().BeFalse();
    }

    private sealed record Destination(string Path, string? Heading);
}

public sealed class SiteTestFactAttribute : FactAttribute
{
    public SiteTestFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LOLCODE_SITE_URL")))
            Skip = "Set LOLCODE_SITE_URL to run the hosted website tests.";
    }
}
