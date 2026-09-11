using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PlaywrightBasics.Tests;

/// <summary>
/// LESSON 5 — Base classes, hooks and configuration.
///
/// Microsoft.Playwright.NUnit gives you four base classes, from most to least
/// managed:
///
///   PageTest     -> Page + Context + Browser   (what you want almost always)
///   ContextTest  -> Context + Browser          (you create pages yourself)
///   BrowserTest  -> Browser                    (you create contexts yourself)
///   PlaywrightTest -> just the Playwright driver (you launch the browser)
///
/// Contexts are cheap and fully isolated — think "a brand new incognito
/// window". That isolation is what lets tests run in parallel safely.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Lesson05_FixturesAndHooks : PageTest
{
    /// <summary>
    /// Override ContextOptions to configure the context PageTest builds for you:
    /// viewport, locale, timezone, permissions, credentials, storage state...
    /// </summary>
    private static string DemoPage => new Uri(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "demo-form.html")).AbsoluteUri;

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        ViewportSize = new() { Width = 1280, Height = 800 },
        Locale = "de-AT",
        TimezoneId = "Europe/Vienna",
        IgnoreHTTPSErrors = true,
    };

    [SetUp]
    public async Task BeforeEachTest()
    {
        // Runs before every [Test]. Page is already created at this point.
        await Page.GotoAsync(DemoPage);
    }

    [TearDown]
    public void AfterEachTest()
    {
        // Page/Context are torn down by PageTest after this runs.
        TestContext.Out.WriteLine($"Finished: {TestContext.CurrentContext.Test.Name}");
    }

    [Test]
    public async Task Context_Options_Are_Applied()
    {
        var viewport = Page.ViewportSize;
        Assert.That(viewport!.Width, Is.EqualTo(1280));

        string locale = await Page.EvaluateAsync<string>("() => navigator.language");
        Assert.That(locale, Is.EqualTo("de-AT"));
    }

    [Test]
    public async Task Each_Test_Gets_A_Clean_Slate()
    {
        // Anything this test writes to localStorage is invisible to every other
        // test, because each one runs in its own context.
        await Page.EvaluateAsync("() => localStorage.setItem('seen', 'yes')");
        string? value = await Page.EvaluateAsync<string?>("() => localStorage.getItem('seen')");
        Assert.That(value, Is.EqualTo("yes"));
    }

    [Test]
    public async Task Opening_A_Second_Page_In_The_Same_Context()
    {
        // Two tabs that share cookies and storage — useful for testing things
        // like "log out in one tab, the other tab notices".
        
        var secondPage = await Context.NewPageAsync();
        
        await secondPage.GotoAsync(DemoPage);
        await Expect(secondPage.GetByRole(AriaRole.Heading, new() { Name = "Playwright Demo Form" })).ToBeVisibleAsync();
        await secondPage.CloseAsync();
    }

    [Test]
    public async Task Adjusting_Timeouts()
    {
        // Default action/assertion timeout is 5s (30s for navigation).
        Page.SetDefaultTimeout(10_000);
        Page.SetDefaultNavigationTimeout(20_000);

        await Expect(Page.GetByLabel("Username")).ToBeVisibleAsync();
    }
}
