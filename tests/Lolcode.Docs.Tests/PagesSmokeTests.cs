using Microsoft.Playwright;

namespace Lolcode.Docs.Tests;

public sealed class PagesSmokeTests : IAsyncLifetime
{
    private static readonly string[] RootGroups =
    [
        "Learn LOLCODE",
        "Language & .NET SDK",
        "Build a compiler",
        "API reference",
    ];

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
    public async Task Desktop_docs_use_one_shared_sidebar_and_utility_header()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1000 },
            ColorScheme = ColorScheme.Dark,
        });
        var page = await context.NewPageAsync();
        var browserErrors = CaptureBrowserErrors(page);

        await page.GotoAsync(_baseUrl);
        await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = "HAI. WHAT DO YOU WANT TO MAKE?" }));
        await ExpectVisible(page.Locator("a[href='docs/']"));
        await ExpectVisible(page.Locator("a[href='playground/']"));
        await AssertNoHorizontalOverflow(page);

        var destinations = new[]
        {
            new Destination("docs/", "dotnet-lolcode :3", null, null),
            new Destination("docs/learn/first-program.html", "01 Your first program", "Learn LOLCODE", "01 Your first program"),
            new Destination("docs/projects/multiple-files.html", "Multiple source files", "Language & .NET SDK", "Multiple source files"),
            new Destination("docs/reference/language-spec.html", "LOLCODE 1.2 Language Specification", "Language & .NET SDK", "Stable LOLCODE 1.2"),
            new Destination("docs/compiler-course/index.html", "Build your own .NET language compiler", "Build a compiler", "Course overview"),
            new Destination("docs/api/Lolcode.CodeAnalysis.html", null, "API reference", "Lolcode.CodeAnalysis"),
        };

        foreach (Destination destination in destinations)
        {
            await page.GotoAsync(new Uri(new Uri(_baseUrl), destination.Path).ToString());
            await page.WaitForFunctionAsync("() => window.docfx?.ready === true");

            if (destination.Heading is not null)
                await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = destination.Heading }));
            else
                await ExpectVisible(page.Locator("main"));

            await AssertUtilityHeader(page);
            await AssertSharedRoots(page);
            await ExpectVisible(page.Locator(".toc-offcanvas"));
            await AssertNoHorizontalOverflow(page);

            if (destination.ActiveRoot is not null)
            {
                await ExpectVisible(page.Locator("#toc li.active > a", new() { HasText = destination.ActiveRoot }).First);
                await ExpectVisible(page.Locator("#toc li.active > a", new() { HasText = destination.ActiveLeaf! }).Last);
            }
        }

        await Screenshot(page, "api-shared-navigation.png");

        var redirectPage = await context.NewPageAsync();
        await redirectPage.GotoAsync(new Uri(new Uri(_baseUrl), "docs/api/index.html").ToString());
        await redirectPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        redirectPage.Url.Should().EndWith("/docs/api/Lolcode.CodeAnalysis.html");
        await redirectPage.CloseAsync();

        await page.GotoAsync(new Uri(new Uri(_baseUrl), "playground/").ToString());
        await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = "HAI, browser. I CAN HAZ code?" }));
        await ExpectVisible(page.GetByRole(AriaRole.Button, new() { Name = "Run" }));
        await AssertNoHorizontalOverflow(page);

        browserErrors.Should().BeEmpty();
    }

    [SiteTestFact]
    public async Task Mobile_docs_menu_and_sidebar_do_not_overflow()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IsMobile = true,
            ColorScheme = ColorScheme.Dark,
        });
        var page = await context.NewPageAsync();
        var browserErrors = CaptureBrowserErrors(page);

        await page.GotoAsync(new Uri(new Uri(_baseUrl), "docs/learn/first-program.html").ToString());
        await page.WaitForFunctionAsync("() => window.docfx?.ready === true");
        await ExpectVisible(page.GetByRole(AriaRole.Heading, new() { Name = "01 Your first program" }));

        var headerToggle = page.Locator("button[data-bs-target='#navpanel']");
        await ExpectVisible(headerToggle);
        await headerToggle.ClickAsync();
        await ExpectVisible(page.Locator(".lol-utility-links a", new() { HasText = "Playground" }));
        await ExpectVisible(page.Locator(".lol-utility-links a", new() { HasText = "GitHub" }));
        await headerToggle.ClickAsync();

        var tocToggle = page.Locator(".lol-toc-toggle");
        await ExpectVisible(tocToggle);
        await tocToggle.ClickAsync();
        await AssertSharedRoots(page);
        await ExpectVisible(page.Locator("#toc > .flex-fill > ul > li > a").First);
        await AssertNoHorizontalOverflow(page);
        await Screenshot(page, "docs-mobile-navigation.png", fullPage: true);
        browserErrors.Should().BeEmpty();
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

    private static async Task AssertUtilityHeader(IPage page)
    {
        var brand = page.Locator(".navbar-brand");
        await ExpectVisible(brand);
        (await brand.InnerTextAsync()).Trim().Should().Be("dotnet-lolcode");
        await ExpectVisible(brand.Locator("img"));
        await ExpectVisible(page.Locator("#search-query"));
        await ExpectVisible(page.Locator(".lol-utility-links a", new() { HasText = "Playground" }));
        await ExpectVisible(page.Locator(".lol-utility-links a", new() { HasText = "GitHub" }));
        (await page.Locator(".navbar .navbar-nav > li > a").CountAsync()).Should().Be(0);
    }

    private static async Task AssertSharedRoots(IPage page)
    {
        await page.Locator("#toc").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 30_000,
        });
        var roots = page.Locator("#toc > .flex-fill > ul > li > a");
        await roots.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 30_000,
        });
        (await roots.AllTextContentsAsync()).Select(static text => text.Trim()).Should().Equal(RootGroups);
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

    private sealed record Destination(string Path, string? Heading, string? ActiveRoot, string? ActiveLeaf);
}

public sealed class SiteTestFactAttribute : FactAttribute
{
    public SiteTestFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LOLCODE_SITE_URL")))
            Skip = "Set LOLCODE_SITE_URL to run the hosted website tests.";
    }
}
