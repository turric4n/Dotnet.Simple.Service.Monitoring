# Telegram Message Formatting and Duration Fix - Summary

## Overview

Fixed two issues in the Telegram alerting publisher:
1. **Duration reporting bug** - showing incorrect milliseconds value
2. **Message formatting** - changed from Markdown to HTML bold formatting

---

## Issue 1: Incorrect Duration Reporting

### Problem

Telegram messages were showing inconsistent duration values compared to actual health check execution time.

**Example**:
```
Telegram Message: ?? *Duration:* 1 ms
Actual Log:       Health check completed after 2001.0871ms
```

### Root Cause

In `HealthCheckData.cs`, the duration was using `.Milliseconds` property instead of `.TotalMilliseconds`:

```csharp
// ? WRONG - Only returns milliseconds component (0-999)
Duration = healthReportEntry.Duration.Milliseconds.ToString();

// .Milliseconds returns the milliseconds component of the TimeSpan
// For TimeSpan of 2001ms, .Milliseconds returns 1 (the remainder after seconds)
```

### Solution

Changed to use `.TotalMilliseconds` which returns the complete duration:

```csharp
// ? CORRECT - Returns total milliseconds with precision
Duration = healthReportEntry.Duration.TotalMilliseconds.ToString("F2");

// For TimeSpan of 2001.0871ms, .TotalMilliseconds returns "2001.09"
```

### Files Modified

**`HealthCheckData.cs`**:
```csharp
public HealthCheckData(HealthReportEntry healthReportEntry, string name)
{
    // ...existing code...
    Duration = healthReportEntry.Duration.TotalMilliseconds.ToString("F2");
    // ...existing code...
}
```

---

## Issue 2: Message Formatting (Markdown to HTML)

### Problem

User requested to replace Markdown bold formatting (`*text*`) with HTML bold tags (`<b>text</b>`) for better Telegram rendering.

### Before (Markdown)

```csharp
var body = $"{currentStatus} *{healthCheckData.Name}*{Environment.NewLine}{Environment.NewLine}" +
           $"?? *Triggered On:* {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}";
```

### After (HTML)

```csharp
var body = $"{currentStatus} <b>{healthCheckData.Name}</b>\n\n" +
           $"?? <b>Triggered On:</b> {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

await _telegramBot.SendMessage(
    _telegramTransportSettings.ChatId, 
    body, 
    parseMode: ParseMode.Html,  // ? Changed to HTML
    cancellationToken: cancellationToken);
```

---

## Benefits Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Duration Accuracy** | ? Incorrect | ? Accurate (2001.09 ms) |
| **Text Formatting** | Markdown `*text*` | HTML `<b>text</b>` |
| **Telegram Rendering** | Inconsistent | Consistent |

---

## Files Changed

1. **`HealthCheckData.cs`** - Duration calculation fix
2. **`TelegramAlertingPublisher.cs`** - HTML formatting

? **Build Status**: Successful  
? **Breaking Changes**: None
