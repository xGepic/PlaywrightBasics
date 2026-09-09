# Playwright Basics (.NET + NUnit)

A hands-on tour of Playwright for .NET. Each file is a lesson; read them in
order and run them as you go.

## Setup

Browsers are a separate download from the NuGet package. Once per machine:

```powershell
dotnet build
pwsh PlaywrightBasics/bin/Debug/net10.0/playwright.ps1 install chromium
```

(Use `install` with no argument to also get Firefox and WebKit.)

## Running

```powershell
dotnet test                      # all lessons
dotnet test -s .runsettings      # with the config in PlaywrightBasics/.runsettings

# just one lesson
dotnet test --filter "FullyQualifiedName~Lesson03"

# watch it happen in a real browser window, slowed down
$env:HEADED=1; $env:PWDEBUG=0; dotnet test --filter "FullyQualifiedName~Lesson04"

# step through with Playwright Inspector
$env:PWDEBUG=1; dotnet test --filter "FullyQualifiedName~Lesson01"
```

## The lessons

| # | File | What it covers |
|---|------|----------------|
| 1 | [Lesson01_FirstTest.cs](PlaywrightBasics/Basics/Lesson01_FirstTest.cs) | `PageTest` base class, navigation, first assertions |
| 2 | [Lesson02_Locators.cs](PlaywrightBasics/Basics/Lesson02_Locators.cs) | `GetByRole` & friends, chaining, filtering, strict mode |
| 3 | [Lesson03_Assertions.cs](PlaywrightBasics/Basics/Lesson03_Assertions.cs) | Auto-retrying `Expect()` vs. one-shot `Assert` — the flakiness lesson |
| 4 | [Lesson04_Interactions.cs](PlaywrightBasics/Basics/Lesson04_Interactions.cs) | Fill, click, check, select, keyboard, dialogs, auto-waiting |
| 5 | [Lesson05_FixturesAndHooks.cs](PlaywrightBasics/Structure/Lesson05_FixturesAndHooks.cs) | Base classes, `ContextOptions`, isolation, timeouts |
| 6 | [Lesson06_PageObjectModel.cs](PlaywrightBasics/Structure/Lesson06_PageObjectModel.cs) | Page objects ([SignupPage.cs](PlaywrightBasics/Pages/SignupPage.cs)), `[TestCase]` |
| 7 | [Lesson07_NetworkAndDebugging.cs](PlaywrightBasics/Advanced/Lesson07_NetworkAndDebugging.cs) | Mocking APIs, blocking requests, screenshots, traces |

Lessons 2–6 drive [TestData/demo-form.html](PlaywrightBasics/TestData/demo-form.html),
a local page loaded over `file://` — no network, no flakiness. Lessons 1 and
parts of 7 hit `https://playwright.dev`, so those need internet.

## The three ideas that matter most

1. **Locators are lazy.** `Page.GetByRole(...)` queries nothing until you act on
   it. That is why a locator keeps working after the page re-renders.
2. **`Expect()` retries, `Assert` does not.** Anything the page produces should
   be asserted with `Expect()`. `Lesson03` has a test that deliberately fails
   the naive way to show you why.
3. **You almost never need a wait.** Actions run actionability checks (visible,
   stable, enabled) first. Reaching for `WaitForTimeoutAsync` is usually a sign
   something else is wrong.

## Debugging a failing test

```powershell
# Record everything, then open the recording
dotnet test --filter "FullyQualifiedName~Recording_A_Trace"
pwsh PlaywrightBasics/bin/Debug/net10.0/playwright.ps1 show-trace `
    PlaywrightBasics/bin/Debug/net10.0/artifacts/trace.zip
```

The trace viewer gives you a DOM snapshot before and after every action, plus
the network log and console. It is the fastest way to find out what the page
actually looked like when your test failed.

## Generating tests by clicking around

```powershell
pwsh PlaywrightBasics/bin/Debug/net10.0/playwright.ps1 codegen https://playwright.dev
```

Pick **C#** in the language dropdown. Good for discovering the right locator;
the generated code still wants cleaning up before you commit it.
