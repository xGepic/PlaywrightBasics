using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PlaywrightBasics.Tests;

/// <summary>
/// LESSON 3 — Assertions, and why Expect() beats Assert.That().
///
/// The single most important idea in this file:
///
///   Expect(locator).ToBeVisibleAsync()   retries for up to 5 seconds
///   Assert.That(await locator.IsVisibleAsync(), Is.True)   checks once, right now
///
/// Web pages are asynchronous. The second form is the classic source of flaky
/// tests. Use Expect() for anything the page produces; use NUnit's Assert only
/// for plain values you already hold in a variable.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Lesson03_Assertions : PageTest
{
    private static string DemoPage =>
        new Uri(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "demo-form.html")).AbsoluteUri;

    [SetUp]
    public async Task OpenDemoPage() => await Page.GotoAsync(DemoPage);

    [Test]
    public async Task Auto_Retrying_Assertions_Handle_Slow_Ui()
    {
        // The profile card only appears 1.2 seconds after the click.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load profile" }).ClickAsync();

        // No Thread.Sleep, no explicit wait. Expect() polls until it shows up.
        await Expect(Page.GetByTestId("profile-card")).ToBeVisibleAsync();
    }

    [Test]
    public async Task The_Same_Thing_Without_Retrying_Is_Flaky()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load profile" }).ClickAsync();

        // IsVisibleAsync() is a one-shot snapshot: no waiting at all.
        bool visibleRightNow = await Page.GetByTestId("profile-card").IsVisibleAsync();

        // It is still hidden, because we asked 1.2s too early.
        Assert.That(visibleRightNow, Is.False,
            "This is the trap: one-shot checks race the UI. Prefer Expect().");
    }

    [Test]
    public async Task A_Tour_Of_The_Common_Assertions()
    {
        var username = Page.GetByLabel("Username");
        var status = Page.GetByRole(AriaRole.Status);

        await Expect(username).ToBeVisibleAsync();
        await Expect(username).ToBeEditableAsync();
        await Expect(username).ToBeEmptyAsync();

        await username.FillAsync("ada");
        await Expect(username).ToHaveValueAsync("ada");
        await Expect(username).ToHaveAttributeAsync("type", "text");

        await Expect(status).ToHaveTextAsync("Not submitted yet.");        // full, exact text
        await Expect(status).ToContainTextAsync("Not submitted");          // substring
        await Expect(status).ToHaveTextAsync(new Regex(@"^Not submitted")); // regex

        await Expect(Page.GetByTestId("profile-card")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("orders").Locator("tbody tr")).ToHaveCountAsync(3);

        // Negate any of them with .Not
        await Expect(username).Not.ToHaveValueAsync("bob");
    }

    [Test]
    public async Task Assertions_On_The_Page_Itself()
    {
        await Expect(Page).ToHaveTitleAsync("Playwright Demo Form");
        await Expect(Page).ToHaveURLAsync(new Regex("demo-form.html$"));
    }

    [Test]
    public async Task Overriding_The_Timeout_For_One_Assertion()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load profile" }).ClickAsync();

        // Default is 5s. Raise it for a genuinely slow operation rather than
        // sprinkling sleeps through the test.
        await Expect(Page.GetByTestId("profile-card"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task A_Soft_Alternative_Collect_Several_Failures()
    {
        // Playwright .NET has no soft assertions, but NUnit's Assert.Multiple
        // works for the non-retrying checks you have already awaited.
        string status = await Page.GetByRole(AriaRole.Status).InnerTextAsync();
        int rowCount = await Page.GetByTestId("orders").Locator("tbody tr").CountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo("Not submitted yet."));
            Assert.That(rowCount, Is.EqualTo(3));
        });
    }
}
