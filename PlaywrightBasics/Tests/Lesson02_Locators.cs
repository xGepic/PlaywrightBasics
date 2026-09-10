using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PlaywrightBasics.Tests;

/// <summary>
/// LESSON 2 — Locators: how you find things on the page.
///
/// A locator is LAZY. Creating one touches nothing; the DOM is only queried
/// when you act on it or assert against it. That is why a locator survives the
/// page re-rendering underneath it, and why CSS selectors stored in variables
/// (the old Selenium habit) are not needed here.
///
/// Preference order, best first:
///   1. GetByRole        — how assistive tech sees the page
///   2. GetByLabel / GetByPlaceholder  — form fields
///   3. GetByText / GetByAltText / GetByTitle
///   4. GetByTestId      — when the above are ambiguous
///   5. CSS / XPath      — last resort, breaks when markup changes
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Lesson02_Locators : PageTest
{
    /// <summary>file:// URL of the local page in TestData, copied next to the test DLL.</summary>
    private static string DemoPage =>
        new Uri(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "demo-form.html")).AbsoluteUri;

    [SetUp]
    public async Task OpenDemoPage() => await Page.GotoAsync(DemoPage);

    [Test]
    public async Task The_Recommended_Locators()
    {
        // By ARIA role + accessible name.
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Playwright Demo Form" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Create account" })).ToBeEnabledAsync();

        // By the <label> text tied to an input. Best choice for forms.
        await Expect(Page.GetByLabel("Username")).ToBeEmptyAsync();

        // By placeholder text.
        await Expect(Page.GetByPlaceholder("you@example.com")).ToBeVisibleAsync();

        // By visible text. Exact = false (the default) is a substring, case-insensitive match.
        await Expect(Page.GetByText("Not submitted yet.")).ToBeVisibleAsync();

        // By data-testid attribute. Use when the markup has no good semantics.
        await Expect(Page.GetByTestId("orders")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Css_And_XPath_Still_Exist()
    {
        // These work, but they couple your test to the markup. Reach for them
        // only when nothing above fits.
        await Expect(Page.Locator("#username")).ToBeVisibleAsync();
        await Expect(Page.Locator("css=input[type=email]")).ToBeVisibleAsync();
        await Expect(Page.Locator("xpath=//table[@data-testid='orders']//tbody/tr")).ToHaveCountAsync(3);
    }

    [Test]
    public async Task Chaining_Narrows_The_Search()
    {
        // Each step searches only inside the previous match.
        var ordersTable = Page.GetByTestId("orders");
        var rows = ordersTable.Locator("tbody tr");

        await Expect(rows).ToHaveCountAsync(3);

        // Filter keeps only the matches containing given text...
        var shipped = rows.Filter(new() { HasText = "Shipped" });
        await Expect(shipped).ToHaveCountAsync(2);

        // ...and HasNotText / Has / HasNot do the inverse and the nested cases.
        await Expect(rows.Filter(new() { HasNotText = "Shipped" })).ToHaveCountAsync(1);
    }

    [Test]
    public async Task Picking_One_Out_Of_Many()
    {
        var rows = Page.GetByTestId("orders").Locator("tbody tr");

        await Expect(rows.First).ToContainTextAsync("Keyboard");
        await Expect(rows.Nth(1)).ToContainTextAsync("Monitor");   // zero-based
        await Expect(rows.Last).ToContainTextAsync("Mouse");

        // Acting on a locator that matches several elements throws "strict mode
        // violation" instead of silently using the first one. That strictness is
        // a feature: it catches ambiguous selectors early.
        var ambiguous = Page.Locator("input[type=checkbox]");
        await Assert.ThatAsync(() => ambiguous.CheckAsync(),
            Throws.TypeOf<PlaywrightException>().With.Message.Contains("strict mode"));
    }

    [Test]
    public async Task Combining_Locators()
    {
        // Or: matches either. And: must match both.
        var submitOrReset = Page.GetByRole(AriaRole.Button, new() { Name = "Create account" })
            .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Reset" }));
        await Expect(submitOrReset).ToHaveCountAsync(2);

        var checkedRadio = Page.Locator("input[name=plan]").And(Page.Locator(":checked"));
        await Expect(checkedRadio).ToHaveValueAsync("free");
    }
}
