# ?? Telegram Publisher Integration Tests

## Overview

This test suite validates the **TelegramAlertingPublisher** using **REAL Telegram Bot API** calls. These tests are marked with `[Explicit]` attribute, meaning they **will NOT run automatically** during regular test executions and must be manually triggered.

---

## ?? Prerequisites

Before running these tests, you need:

1. ? A Telegram account
2. ? A Telegram Bot (created via @BotFather)
3. ? Your Bot's API Token
4. ? Your Chat ID (personal or group)

---

## ?? Setting Up Your Telegram Bot

### Step 1: Create a Bot

1. Open Telegram and search for **@BotFather**
2. Send the command: `/newbot`
3. Follow the prompts to:
   - Choose a name for your bot (e.g., "Health Monitor Test Bot")
   - Choose a username (must end with 'bot', e.g., "my_health_monitor_bot")
4. **Copy the API Token** you receive (looks like: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`)

### Step 2: Get Your Chat ID

#### Option A: Personal Chat

1. Start a chat with your bot in Telegram
2. Send any message to your bot (e.g., "Hello")
3. Open this URL in your browser:
   ```
   https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates
   ```
   Replace `<YOUR_BOT_TOKEN>` with your actual bot token
4. Look for `"chat":{"id":123456789}` in the JSON response
5. Copy the `id` value (e.g., `123456789`)

#### Option B: Group/Channel Chat

1. Create a group or channel in Telegram
2. Add your bot to the group/channel
3. Make the bot an administrator (required for channels)
4. Send a message in the group
5. Visit the getUpdates URL (same as above)
6. Find the `chat` object in the JSON
7. Copy the negative `id` value (e.g., `-1001234567890`)

**Pro Tip**: Use a tool like [JSON Formatter](https://jsonformatter.org/) to prettify the response for easier reading!

---

## ?? Configuring the Tests

### 1. Open the Test File

Navigate to: `Simple.Service.Monitoring.Tests\Publishers\TelegramPublisherIntegrationShould.cs`

### 2. Update Credentials (Lines 26-27)

```csharp
// ?? CONFIGURE THESE VALUES BEFORE RUNNING TESTS ??
private const string BOT_API_TOKEN = "123456789:ABCdefGHIjklMNOpqrsTUVwxyz"; // Your bot token
private const string CHAT_ID = "123456789";  // Your chat ID or "-1001234567890" for groups
```

**Replace:**
- `YOUR_BOT_TOKEN_HERE` ? Your actual bot API token
- `YOUR_CHAT_ID_HERE` ? Your actual chat ID

### 3. Save the File

**?? SECURITY WARNING:** 
- **DO NOT commit these credentials to version control!**
- Add this file to `.gitignore` or use environment variables
- Keep your bot token secret!

---

## ?? Running the Tests

### Option 1: Visual Studio Test Explorer

1. Open **Test Explorer** (Test ? Test Explorer)
2. Search for "TelegramPublisher"
3. Right-click on any test
4. Select **"Run"** or **"Debug"**
5. Check your Telegram chat for messages!

### Option 2: Command Line

```bash
# Run all Telegram publisher tests
dotnet test --filter "Category=TelegramPublisher"

# Run a specific test
dotnet test --filter "FullyQualifiedName~AlertImmediately_OnFirstFailure"

# Run with verbose output
dotnet test --filter "Category=TelegramPublisher" --verbosity detailed
```

**Note:** You'll need to temporarily remove the `[Explicit]` attribute or use `--filter` with the test name for command-line execution.

---

## ?? Test Scenarios Covered

### ? Failure Scenarios (4 tests)

| Test | What It Does | Expected Telegram Messages |
|------|--------------|---------------------------|
| `AlertImmediately_OnFirstFailure` | Sends alert on first failure | 1 message (? failure) |
| `AlertAfterConsecutiveFailures` | Waits for 3 consecutive failures | 1 message after 3 failures |
| `AlertWithDifferentSeverities_Degraded` | Tests degraded status alert | 1 message (?? degraded) |
| `NotAlert_WhenHealthy` | Verifies no alert for healthy status | 0 messages |

### ? Recovery Scenarios (3 tests)

| Test | What It Does | Expected Telegram Messages |
|------|--------------|---------------------------|
| `AlertOnRecovery_WhenConfigured` | Sends both failure and recovery alerts | 2 messages (? failure, ? recovery) |
| `NotAlertOnRecovery_WhenDisabled` | Only sends failure, no recovery alert | 1 message (? failure only) |
| `ResetFailCount_OnRecovery` | Verifies fail count resets on recovery | 0 messages (never reaches threshold) |

### ? Scheduling Tests (2 tests)

| Test | What It Does | Expected Telegram Messages |
|------|--------------|---------------------------|
| `RespectCooldownPeriod_BetweenAlerts` | Tests 5-second cooldown between alerts | 2 messages (1 initial, 1 after cooldown) |
| `AlertOnce_ThenSilenceUntilRecovery` | Only alerts once per failure episode | 3 messages (fail ? recover ? fail again) |

### ? Complex Scenarios (1 test)

| Test | What It Does | Expected Telegram Messages |
|------|--------------|---------------------------|
| `HandleFlappingService_WithThreshold` | Tests flapping service with threshold=3 | 1 message (after 3 consecutive failures) |

### ?? Configuration Guide (1 test)

| Test | What It Does |
|------|--------------|
| `DisplayConfigurationGuide` | Displays setup instructions in test output |

---

## ?? Message Format

Messages sent to Telegram include:

```
? [Unhealthy] telegram_first_failure

?? Triggered On: 2024-01-15 14:30:25
?? Machine: YOUR-PC-NAME
?? Service Type: Http
?? Endpoint: https://api.example.com
? Duration: 100 ms
?? Status: Unhealthy
? Error Details: Service unavailable

?? Last updated: 2024-01-15 14:30:25
```

**Status Emojis:**
- ? Healthy
- ?? Degraded
- ? Unhealthy
- ? Unknown

---

## ?? Verifying Tests

### Check Test Output

In Test Explorer or console, you'll see output like:

```
? Telegram message sent successfully for first failure!
? Telegram message sent after 3 consecutive failures!
? Telegram degraded message sent with ?? emoji!
? No Telegram message sent for healthy status (as expected)
```

### Check Your Telegram Chat

1. Open your Telegram chat/group
2. You should see messages from your bot
3. Verify the content matches the test scenario
4. Check timestamps to ensure cooldown periods are respected

---

## ?? Troubleshooting

### Error: "Telegram bot credentials not configured"

**Problem:** You haven't set `BOT_API_TOKEN` and `CHAT_ID`

**Solution:** 
1. Open `TelegramPublisherIntegrationShould.cs`
2. Update lines 26-27 with your credentials
3. Save and re-run

---

### Error: "Unauthorized" (401)

**Problem:** Invalid or incorrect bot token

**Solution:**
1. Verify your bot token from @BotFather
2. Make sure there are no extra spaces
3. Format: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`

---

### Error: "Bad Request: chat not found" (400)

**Problem:** Invalid or incorrect chat ID

**Solution:**
1. Send a message to your bot first
2. Visit the getUpdates URL
3. Verify the chat ID format:
   - Personal chat: positive number (e.g., `123456789`)
   - Group/channel: negative number (e.g., `-1001234567890`)

---

### No Messages Received

**Problem:** Bot can't send messages to the chat

**Solution:**
- **Personal chat**: Make sure you've started a conversation with the bot
- **Group**: Ensure the bot is added to the group
- **Channel**: Make sure the bot is an administrator
- Check that the bot isn't blocked

---

## ?? Security Best Practices

### ? DO:
- Keep your bot token secret
- Use environment variables for credentials
- Add credentials file to `.gitignore`
- Rotate tokens if compromised
- Use different bots for dev/prod

### ? DON'T:
- Commit credentials to Git
- Share bot tokens publicly
- Use production bot for testing
- Hard-code credentials in deployed code

### Environment Variable Alternative

Instead of hard-coding, use environment variables:

```csharp
private static readonly string BOT_API_TOKEN = 
    Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") 
    ?? "YOUR_BOT_TOKEN_HERE";

private static readonly string CHAT_ID = 
    Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") 
    ?? "YOUR_CHAT_ID_HERE";
```

Set environment variables:

**Windows (PowerShell):**
```powershell
$env:TELEGRAM_BOT_TOKEN="123456789:ABCdefGHIjklMNOpqrsTUVwxyz"
$env:TELEGRAM_CHAT_ID="123456789"
```

**Linux/Mac:**
```bash
export TELEGRAM_BOT_TOKEN="123456789:ABCdefGHIjklMNOpqrsTUVwxyz"
export TELEGRAM_CHAT_ID="123456789"
```

---

## ?? Test Coverage Summary

| Category | Tests | Duration | Messages Sent |
|----------|-------|----------|---------------|
| Failure Scenarios | 4 | ~8s | 3 |
| Recovery Scenarios | 3 | ~15s | 4 |
| Scheduling Tests | 2 | ~20s | 5 |
| Complex Scenarios | 1 | ~6s | 1 |
| **Total** | **10** | **~49s** | **~13** |

**Note:** Actual message count may vary based on test execution order and timing.

---

## ?? Test Execution Tips

### Run Tests Individually

For easier debugging, run one test at a time:
1. This avoids flooding your Telegram with messages
2. Easier to verify each scenario
3. Better for timing-sensitive tests

### Clean Up Test Messages

After testing:
1. Delete test messages from your Telegram chat
2. Or use a dedicated test group/channel
3. Consider using a test bot separate from production

### Monitor Rate Limits

Telegram has rate limits:
- ~30 messages per second to the same chat
- If exceeded, you'll get "Too Many Requests" (429) errors
- Tests include delays to respect limits

---

## ?? Related Documentation

- [Telegram Bot API](https://core.telegram.org/bots/api)
- [BotFather Commands](https://core.telegram.org/bots#6-botfather)
- [Telegram API Documentation](https://core.telegram.org/api)

---

## ?? Success!

If you see messages in your Telegram chat with ?, ??, and ? emojis, congratulations! Your tests are working correctly.

**Happy Testing!** ???

---

**Last Updated:** 2024
**Maintained By:** Simple.Service.Monitoring Team
