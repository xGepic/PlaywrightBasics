using Microsoft.Playwright;

namespace PlaywrightBasics.Pages;

/// <summary>
/// A Page Object: one class per screen. It owns the locators and exposes
/// intent-revealing methods ("register this user") instead of mechanics
/// ("fill #username, click #submit").
///
/// Two rules that keep page objects healthy:
///   1. Store locators as fields, not strings. They are lazy, so building them
///      in the constructor is free and they stay valid across re-renders.
///   2. Keep assertions OUT of the page object — they belong in the test, so a
///      failure reads as a business rule, not a plumbing detail. (The one
///      pragmatic exception is a "wait until loaded" helper.)
/// </summary>
public class SignupPage(IPage page)
{
    private readonly IPage _page = page;

    // --- Locators -----------------------------------------------------------
    private ILocator Username => _page.GetByLabel("Username");
    private ILocator Email => _page.GetByLabel("Email");
    private ILocator Country => _page.GetByLabel("Country");
    private ILocator Bio => _page.GetByLabel("Bio");
    private ILocator Newsletter => _page.GetByLabel("Subscribe to newsletter");
    private ILocator Terms => _page.GetByLabel("I accept the terms");
    private ILocator SubmitButton => _page.GetByRole(AriaRole.Button, new() { Name = "Create account" });
    private ILocator ResetButton => _page.GetByRole(AriaRole.Button, new() { Name = "Reset" });

    /// <summary>Exposed so tests can assert on it. Returning a locator, not a
    /// string, keeps the auto-retrying behaviour of Expect() available.</summary>
    public ILocator Status => _page.GetByRole(AriaRole.Status);

    public ILocator PlanRadio(string plan) => _page.GetByLabel($"{plan} plan");

    // --- Actions ------------------------------------------------------------
    public async Task GotoAsync()
    {
        string url = new Uri(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "TestData", "demo-form.html")).AbsoluteUri;
        await _page.GotoAsync(url);
    }

    public async Task RegisterAsync(
        string username,
        string email,
        string country = "Austria",
        string plan = "Free",
        bool acceptTerms = true,
        bool subscribeNewsletter = false)
    {
        await Username.FillAsync(username);
        await Email.FillAsync(email);
        await Country.SelectOptionAsync(new SelectOptionValue { Label = country });
        await PlanRadio(plan).CheckAsync();
        await Terms.SetCheckedAsync(acceptTerms);
        await Newsletter.SetCheckedAsync(subscribeNewsletter);
        await SubmitButton.ClickAsync();
    }

    public async Task FillBioAsync(string text) => await Bio.FillAsync(text);

    public async Task ResetAsync() => await ResetButton.ClickAsync();
}
