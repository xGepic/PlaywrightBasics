using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PlaywrightBasics.Tests;

/// <summary>
/// LESSON 7 — Network interception, screenshots and traces.
///
/// Intercepting the network lets you test the UI against data you control:
/// empty states, error states, huge lists — all without a backend.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Lesson07_NetworkAndDebugging : PageTest
{
    [Test]
    public async Task Mocking_An_Api_Response()
    {
        // Serve a page whose content comes from an API call...
        await Page.RouteAsync("**/api/orders", async route =>
        {
            var body = JsonSerializer.Serialize(new[]
            {
                new { id = 1, item = "Keyboard" },
                new { id = 2, item = "Monitor" },
            });

            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "application/json",
                Body = body,
            });
        });

        // ...and serve the page itself from a route too, so nothing leaves the
        // machine and the relative fetch('/api/orders') has a real origin.
        await ServePageAsync("""
            <ul id="orders"></ul>
            <script>
              fetch('/api/orders').then(r => r.json()).then(rows => {
                document.getElementById('orders').innerHTML =
                  rows.map(r => `<li>${r.item}</li>`).join('');
              });
            </script>
            """);

        await Expect(Page.Locator("#orders li")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#orders li").First).ToHaveTextAsync("Keyboard");
    }

    [Test]
    public async Task Simulating_A_Server_Error()
    {
        await Page.RouteAsync("**/api/orders", async route => await route.FulfillAsync(new() { Status = 500, Body = "boom" }));

        await ServePageAsync("""
            <p id="msg">loading…</p>
            <script>
              fetch('/api/orders')
                .then(r => { if (!r.ok) throw new Error(); })
                .catch(() => document.getElementById('msg').textContent = 'Could not load orders.');
            </script>
            """);

        await Expect(Page.Locator("#msg")).ToHaveTextAsync("Could not load orders.");
    }

    [Test]
    public async Task Blocking_Requests_You_Do_Not_Care_About()
    {
        // Aborting images/fonts/analytics speeds a suite up noticeably.
        await Page.RouteAsync("**/*.{png,jpg,jpeg,svg,woff2}", route => route.AbortAsync());

        await Page.GotoAsync("https://playwright.dev/");
        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("Playwright"));
    }

    [Test]
    public async Task Observing_Requests_Without_Changing_Them()
    {
        var urls = new List<string>();
        Page.Request += (_, request) => urls.Add(request.Url);

        await Page.GotoAsync("https://playwright.dev/");

        Assert.That(urls, Is.Not.Empty);
        TestContext.Out.WriteLine($"{urls.Count} requests, first: {urls[0]}");
    }

    [Test]
    public async Task Screenshots()
    {
        string dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts");
        Directory.CreateDirectory(dir);

        await Page.GotoAsync("https://playwright.dev/");

        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(dir, "homepage.png"),
            FullPage = true,
        });

        // A single element, rather than the whole viewport.
        await Page.GetByRole(AriaRole.Navigation).First.ScreenshotAsync(new() { Path = Path.Combine(dir, "navbar.png") });
        Assert.That(File.Exists(Path.Combine(dir, "homepage.png")));
    }

    /// <summary>
    /// A trace is a recording of the whole test: DOM snapshot per action,
    /// network log, console, source. Open it with:
    ///     pwsh bin/Debug/net10.0/playwright.ps1 show-trace trace.zip
    /// It is the single best debugging tool Playwright offers.
    /// </summary>
    [Test]
    public async Task Recording_A_Trace()
    {
        string dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts");
        Directory.CreateDirectory(dir);

        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.CurrentContext.Test.Name,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });

        try
        {
            await Page.GotoAsync("https://playwright.dev/");
            await Page.GetByRole(AriaRole.Link, new() { Name = "Get started" }).ClickAsync();
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*intro"));
        }
        finally
        {
            await Context.Tracing.StopAsync(new()
            {
                Path = Path.Combine(dir, "trace.zip"),
            });
        }
    }

    /// <summary>
    /// Serves <paramref name="html"/> as if it came from https://demo.test/, so
    /// that relative URLs inside the page resolve. SetContentAsync alone would
    /// leave the page on about:blank, where fetch('/api/...') has no origin.
    /// </summary>
    private async Task ServePageAsync(string html)
    {
        await Page.RouteAsync("https://demo.test/", async route => await route.FulfillAsync(new() { ContentType = "text/html", Body = html }));
        await Page.GotoAsync("https://demo.test/");
    }
}
