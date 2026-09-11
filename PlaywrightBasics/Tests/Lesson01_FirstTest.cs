using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PlaywrightBasics.Tests;

/// <summary>
/// LESSON 1 — The shape of a Playwright test.
///
/// Inherit from <see cref="PageTest"/> and you get a fresh browser, a fresh
/// isolated context (own cookies / localStorage) and a fresh <c>Page</c> for
/// every single test. You never create or dispose them yourself.
///
/// Everything in Playwright is async, so every test returns a Task.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Lesson01_FirstTest : PageTest
{
    [Test]
    public async Task Navigate_And_Check_The_Title()
    {
        // Expect(...) is Playwright's own assertion library. It RETRIES until the condition holds or the timeout expires 

        await Page.GotoAsync("https://playwright.dev/");
        await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));
    }

    [Test]
    public async Task Click_A_Link_And_Check_Where_We_Landed()
    {
        // Find the link the way a user (or a screen reader) would: by its role and its visible name. This is the locator style Playwright recommends.

        await Page.GotoAsync("https://playwright.dev/");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Get started" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*intro"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Installation" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Reading_Values_Out_Of_The_Page()
    {
        // Sometimes you really do want the raw value rather than an assertion.

        await Page.GotoAsync("https://playwright.dev/");

        string title = await Page.TitleAsync();
        string heading = await Page.GetByRole(AriaRole.Heading, new() { Level = 1 }).InnerTextAsync();
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(title, Does.Contain("Playwright"));
            Assert.That(heading, Is.Not.Empty);
        }

        TestContext.Out.WriteLine($"Title: {title}");
        TestContext.Out.WriteLine($"H1:    {heading}");
    }
}
