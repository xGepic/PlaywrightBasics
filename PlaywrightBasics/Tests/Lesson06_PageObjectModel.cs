using Microsoft.Playwright.NUnit;
using PlaywrightBasics.Pages;

namespace PlaywrightBasics.Basics;

/// <summary>
/// LESSON 6 — Page Object Model.
///
/// Compare these tests with Lesson 4. Same browser work, but the tests now read
/// as sentences about the product, and the day the markup changes you edit one
/// file (<see cref="SignupPage"/>) instead of twenty tests.
///
/// Don't over-apply it: a page object earns its keep once two or more tests
/// share the same screen. A single one-off test is clearer written inline.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Lesson06_PageObjectModel : PageTest
{
    private SignupPage _signup = null!;

    [SetUp]
    public async Task SetUp()
    {
        // Built per test, because Page itself is built per test.
        _signup = new SignupPage(Page);
        await _signup.GotoAsync();
    }

    [Test]
    public async Task Registering_Greets_The_User_By_Name()
    {
        await _signup.RegisterAsync("ada.lovelace", "ada@example.com");

        await Expect(_signup.Status).ToContainTextAsync("Welcome, ada.lovelace!");
    }

    [Test]
    public async Task The_Chosen_Plan_Is_Reflected_In_The_Confirmation()
    {
        await _signup.RegisterAsync("grace", "grace@example.com", plan: "Pro");

        await Expect(_signup.Status).ToContainTextAsync("Plan: pro.");
    }

    [Test]
    public async Task Resetting_Clears_The_Form()
    {
        await _signup.RegisterAsync("alan", "alan@example.com");
        await Expect(_signup.Status).ToContainTextAsync("Welcome, alan!");

        await _signup.ResetAsync();

        await Expect(_signup.Status).ToHaveTextAsync("Not submitted yet.");
        await Expect(_signup.PlanRadio("Free")).ToBeCheckedAsync();
    }

    // NUnit's data-driven attributes compose with Playwright exactly as usual.
    [TestCase("ada", "Austria", "free")]
    [TestCase("hedy", "Germany", "free")]
    public async Task Several_Users_Can_Register(string user, string country, string expectedPlan)
    {
        await _signup.RegisterAsync(user, $"{user}@example.com", country: country);

        await Expect(_signup.Status).ToContainTextAsync($"Welcome, {user}!");
        await Expect(_signup.Status).ToContainTextAsync($"Plan: {expectedPlan}.");
    }
}
