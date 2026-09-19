using Microsoft.Playwright;

namespace Lolcode.Docs.Tests;

public sealed class PagesSmokeTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly string _baseUrl = Environment.GetEnvironmentVariable("LOLCODE_SITE_URL")!;
    private readonly string _screenshotDirectory =
        Environment.GetEnvironmentVariable("LOLCODE_SCREENSHOT_DIR")
        ?? Path.Combine("artifacts", "playwright");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_screenshotDirectory);
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
    public async Task Desktop_site_connects_landing_docs_and_playground()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1000 },
            ColorScheme = ColorScheme.Dark,
        });
        var page = await context.NewPageAsync();
        var browserErrors = new List<string>();
        page.PageError += (_, error) => browserErrors.Add(error);
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
                browserErrors.Add(message.Text);
        };

        await page.GotoAsync(_baseUrl);
        await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = "HAI. WHAT DO YOU WANT TO MAKE?" }));
        await ExpectVisible(page.Locator("a[href='docs/']"));
        await ExpectVisible(page.Locator("a[href='playground/']"));
        await AssertNoHorizontalOverflow(page);
        await Screenshot(page, "landing-desktop.png");

        await page.GotoAsync(new Uri(new Uri(_baseUrl), "docs/").ToString());
        await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = "The Trail Map :3" }));
        var documentationBrand = page.Locator(".navbar-brand");
        await ExpectVisible(documentationBrand);
        (await documentationBrand.InnerTextAsync()).Trim().Should().Be("dotnet-lolcode");
        await ExpectVisible(documentationBrand.Locator("img"));
        (await page.TitleAsync()).Should().Contain("Trail Map");
        await AssertNoHorizontalOverflow(page);
        await Screenshot(page, "docs-desktop.png");

        await page.GotoAsync(new Uri(new Uri(_baseUrl), "docs/api/Lolcode.html").ToString());
        (await page.TitleAsync()).Should().Contain("Lolcode");
        await ExpectVisible(page.Locator("main"));
        await AssertNoHorizontalOverflow(page);

        await page.GotoAsync(new Uri(new Uri(_baseUrl), "docs/reference/language-spec.html").ToString());
        await page.WaitForFunctionAsync("() => window.docfx?.ready === true");
        await Screenshot(page, "reference-desktop.png");
        browserErrors.Should().BeEmpty();
        await ExpectVisible(page.Locator(".toc-offcanvas"));
        (await page.Locator("#toc a").CountAsync()).Should().BeGreaterThan(0);
        await ExpectVisible(page.Locator("#toc a").First);
        await ExpectVisible(page.Locator(".affix"));
        await ExpectVisible(page.Locator("#affix a").First);
        await AssertNoHorizontalOverflow(page);

        await page.GotoAsync(new Uri(new Uri(_baseUrl), "playground/").ToString());
        await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = "HAI, browser. I CAN HAZ code?" }));
        await ExpectVisible(page.GetByRole(AriaRole.Button, new() { Name = "Run" }));
        await AssertNoHorizontalOverflow(page);
        await Screenshot(page, "playground-desktop.png");
    }

    [SiteTestFact]
    public async Task Mobile_landing_stacks_both_destinations_without_overflow()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IsMobile = true,
            ColorScheme = ColorScheme.Dark,
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_baseUrl);
        await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = "Documentation" }));
        await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = "Playground" }));
        await AssertNoHorizontalOverflow(page);
        await Screenshot(page, "landing-mobile.png", fullPage: true);
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

    private Task Screenshot(IPage page, string fileName, bool fullPage = false) =>
        page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_screenshotDirectory, fileName),
            FullPage = fullPage,
        });
}

public sealed class SiteTestFactAttribute : FactAttribute
{
    public SiteTestFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LOLCODE_SITE_URL")))
            Skip = "Set LOLCODE_SITE_URL to run the hosted website tests.";
    }
}
