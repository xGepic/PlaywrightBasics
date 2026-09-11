using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PlaywrightBasics.Tests;

/// <summary>
/// LESSON 4 — Interacting with the page.
///
/// Every action below performs "actionability checks" first: the element must
/// be attached, visible, stable (not animating), able to receive events and
/// enabled. Playwright waits for all of that automatically, then acts. This is
/// why you almost never need an explicit wait.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Lesson04_Interactions : PageTest
{
    private static string DemoPage => new Uri(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "demo-form.html")).AbsoluteUri;

    [SetUp]
    public async Task OpenDemoPage() => await Page.GotoAsync(DemoPage);

    [Test]
    public async Task Filling_Text_Fields()
    {
        // FillAsync clears the field and sets the value in one go. Prefer it.
        // PressSequentiallyAsync types character by character, firing each keydown/keyup. Only needed for autocomplete widgets that listen to keys.

        await Page.GetByLabel("Username").FillAsync("ada.lovelace");
        await Page.GetByLabel("Email").FillAsync("ada@example.com");
        await Page.GetByLabel("Bio").FillAsync("First programmer.");
        await Page.GetByLabel("Username").ClearAsync();
        await Page.GetByLabel("Username").PressSequentiallyAsync("ada", new() { Delay = 50 });
        await Expect(Page.GetByLabel("Username")).ToHaveValueAsync("ada");
    }

    [Test]
    public async Task Checkboxes_And_Radios()
    {
        // SetCheckedAsync takes the desired state directly.

        var newsletter = Page.GetByLabel("Subscribe to newsletter");

        await newsletter.CheckAsync();
        await Expect(newsletter).ToBeCheckedAsync();
        await newsletter.UncheckAsync();
        await Expect(newsletter).Not.ToBeCheckedAsync();
        await Page.GetByLabel("I accept the terms").SetCheckedAsync(true);
        await Page.GetByLabel("Pro plan").CheckAsync();
        await Expect(Page.GetByLabel("Pro plan")).ToBeCheckedAsync();
        await Expect(Page.GetByLabel("Free plan")).Not.ToBeCheckedAsync();
    }

    [Test]
    public async Task Dropdowns()
    {
        var country = Page.GetByLabel("Country");

        await country.SelectOptionAsync("at");
        await Expect(country).ToHaveValueAsync("at");
        await country.SelectOptionAsync(new SelectOptionValue { Label = "Germany" });
        await Expect(country).ToHaveValueAsync("de");
        await country.SelectOptionAsync(new SelectOptionValue { Index = 3 });
        await Expect(country).ToHaveValueAsync("ch");
    }

    [Test]
    public async Task Clicking_In_All_Its_Variations()
    {
        // Both fields are `required`, so the browser's own validation would
        // block the submit if we left one empty.
        // Modifiers, position, button, double-click all live in the options.

        var submit = Page.GetByRole(AriaRole.Button, new() { Name = "Create account" });

        await Page.GetByLabel("Username").FillAsync("ada");
        await Page.GetByLabel("Email").FillAsync("ada@example.com");
        await submit.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Welcome, ada!");
        await submit.ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await submit.DblClickAsync();
        await submit.ClickAsync(new() { Button = MouseButton.Right });
    }

    [Test]
    public async Task Keyboard_And_Hover()
    {
        // Key names follow the DOM: Enter, Tab, ArrowDown, Escape, "Control+A"...

        await Page.GetByLabel("Username").FillAsync("ada");
        await Page.GetByLabel("Username").PressAsync("Tab");
        await Expect(Page.GetByLabel("Email")).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("ada@example.com");
        await Expect(Page.GetByLabel("Email")).ToHaveValueAsync("ada@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).HoverAsync();
    }

    [Test]
    public async Task Handling_A_Native_Dialog()
    {
        // Register the handler BEFORE triggering the dialog. Without a handler Playwright auto-dismisses dialogs, so the page would take the "cancel" path.
        Page.Dialog += async (_, dialog) =>
        {
            Assert.That(dialog.Message, Is.EqualTo("Really delete your account?"));
            await dialog.AcceptAsync();
        };

        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete account" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Account deleted.");
    }

    [Test]
    public async Task When_You_Genuinely_Have_To_Wait()
    {
        // 99% of the time auto-waiting covers you.
        // Page.WaitForTimeoutAsync exists but is discouraged — it is Thread.Sleep by another name and will make your suite slow and flaky.
        
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load profile" }).ClickAsync();
        await Page.GetByTestId("profile-card").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByTestId("profile-card")).ToBeVisibleAsync();
    }
}
