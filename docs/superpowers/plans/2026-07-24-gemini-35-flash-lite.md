# Gemini 3.5 Flash-Lite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Przełączyć domyślną i lokalnie zapisaną konfigurację FormaAI na `gemini-3.5-flash-lite` bez wykonywania żądań do Gemini.

**Architecture:** Wspólne stałe i dobór poziomu myślenia trafiają do kontraktów asystenta, aby API, Web i Infrastructure używały jednego źródła prawdy. Baza zachowuje zaszyfrowany klucz, a aktualizowany jest wyłącznie identyfikator modelu.

**Tech Stack:** .NET 8, ASP.NET Core, Blazor WebAssembly, xUnit, SQL Server LocalDB.

## Global Constraints

- Nie wykonywać żadnego żądania do API Gemini.
- Nie umieszczać klucza API w kodzie, logach, dokumentacji ani historii Git.
- Weryfikować tylko build, testy i endpoint `/health/live`.

---

### Task 1: Wspólna konfiguracja Gemini

**Files:**
- Modify: `src/FormaAI.Contracts/Assistant/AiAdminContracts.cs`
- Create: `tests/FormaAI.Application.Tests/AiDefaultsTests.cs`
- Modify: `src/FormaAI.Api/Program.cs`
- Modify: `src/FormaAI.Api/Controllers/AdminAiController.cs`
- Modify: `src/FormaAI.Api/appsettings.json`
- Modify: `src/FormaAI.Web/Pages/Admin.razor`
- Modify: `src/FormaAI.Infrastructure/External/GeminiAssistantModel.cs`

**Interfaces:**
- Produces: `AiDefaults.GeminiModel`, `AiDefaults.GeminiBaseUrl` i `AiDefaults.GeminiThinkingLevel(string)`.
- Consumes: wszystkie warstwy konfigurujące Gemini.

- [ ] **Step 1: Write failing defaults tests**

```csharp
[Fact]
public void Gemini35FlashLiteIsTheDefaultModel()
{
    Assert.Equal("gemini-3.5-flash-lite", AiDefaults.GeminiModel);
}

[Theory]
[InlineData("gemini-3.5-flash-lite", "minimal")]
[InlineData("gemini-3.6-flash", "low")]
public void ThinkingLevelMatchesModelFamily(string model, string expected)
{
    Assert.Equal(expected, AiDefaults.GeminiThinkingLevel(model));
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter AiDefaultsTests`

Expected: compilation fails because `AiDefaults` does not exist.

- [ ] **Step 3: Implement one source of truth**

```csharp
public static class AiDefaults
{
    public const string GeminiBaseUrl = "https://generativelanguage.googleapis.com/";
    public const string GeminiModel = "gemini-3.5-flash-lite";

    public static string GeminiThinkingLevel(string model) =>
        string.Equals(model, GeminiModel, StringComparison.OrdinalIgnoreCase)
            ? "minimal"
            : "low";
}
```

Replace every hard-coded `gemini-3.5-flash` default with the shared constant.
Use the thinking-level helper only for Gemini 3.x requests.

- [ ] **Step 4: Run focused and full tests**

Run:

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
```

Expected: build succeeds and all tests pass without contacting Gemini.

### Task 2: Local configuration and publication

**Files:**
- Runtime data only: LocalDB `FormaAI`, table `AiConfigurations`.

**Interfaces:**
- Consumes: existing encrypted API-key record.
- Produces: stored model `gemini-3.5-flash-lite`.

- [ ] **Step 1: Update only the stored model**

Execute a parameterized SQL update restricted to `Provider = Gemini`. Preserve
`EncryptedApiKey` and update `UpdatedAtUtc`.

- [ ] **Step 2: Verify persisted model without reading the key**

Select only `Provider`, `Model` and `UpdatedAtUtc`. Expected model:
`gemini-3.5-flash-lite`.

- [ ] **Step 3: Commit and push**

```powershell
git add src tests docs
git commit -m "Przełączyć asystenta na Gemini 3.5 Flash-Lite"
git push origin main
```

- [ ] **Step 4: Restart and expose**

Start LocalDB and FormaAI on port 5082. Reuse a live Cloudflare tunnel or
start a new Quick Tunnel. Verify only local and public `/health/live`; do not
call assistant, photo analysis, text analysis or admin connection test.
