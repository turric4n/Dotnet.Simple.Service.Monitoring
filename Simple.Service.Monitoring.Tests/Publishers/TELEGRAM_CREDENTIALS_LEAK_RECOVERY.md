# ?? URGENT: Leaked Telegram Credentials - Recovery Guide

## ?? IMMEDIATE ACTIONS REQUIRED

Your Telegram bot credentials have been exposed in Git history and are visible on GitHub!

**Compromised Credentials:**
- Bot Token: `6030340647:AAFHv9HMz0nuxuI9450tjVUuYJoCe4jf7JQ`
- Chat ID: `-960612732`

---

## ?? Step-by-Step Recovery

### Step 1: Revoke the Bot Token (DO THIS FIRST!) ?

1. Open **Telegram**
2. Search for **@BotFather**
3. Send command: `/mybots`
4. Select your bot
5. Click **"API Token"**
6. Click **"Revoke current token"**
7. Click **"Generate new token"**
8. **Copy the new token** (you'll need it later)

?? **The old token is now invalid and can't be used by attackers!**

---

### Step 2: Remove Credentials from Git History

#### Option A: Using BFG Repo-Cleaner (Recommended - Easiest)

```powershell
# 1. Create a backup
cd C:\git\Dotnet.Simple.Service.Monitoring\..
git clone --mirror C:\git\Dotnet.Simple.Service.Monitoring dotnet-monitoring-backup.git

# 2. Download BFG
# Visit: https://rtyley.github.io/bfg-repo-cleaner/
# Download bfg-1.14.0.jar

# 3. Create passwords.txt file with compromised credentials
@"
6030340647:AAFHv9HMz0nuxuI9450tjVUuYJoCe4jf7JQ
-960612732
"@ | Out-File -Encoding UTF8 passwords.txt

# 4. Run BFG to remove credentials from history
java -jar bfg-1.14.0.jar --replace-text passwords.txt C:\git\Dotnet.Simple.Service.Monitoring

# 5. Clean up
cd C:\git\Dotnet.Simple.Service.Monitoring
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 6. Force push (?? WARNING: This rewrites GitHub history!)
git push --force --all origin
git push --force --tags origin
```

#### Option B: Using git-filter-repo

```powershell
# Install git-filter-repo
pip install git-filter-repo

# Create backup
cd C:\git\Dotnet.Simple.Service.Monitoring\..
git clone C:\git\Dotnet.Simple.Service.Monitoring dotnet-monitoring-backup

# Remove sensitive data
cd C:\git\Dotnet.Simple.Service.Monitoring

# Create a script to replace credentials
@"
6030340647:AAFHv9HMz0nuxuI9450tjVUuYJoCe4jf7JQ==>YOUR_BOT_TOKEN_HERE
-960612732==>YOUR_CHAT_ID_HERE
"@ | Out-File -Encoding UTF8 replacements.txt

git filter-repo --replace-text replacements.txt

# Force push
git push --force --all origin
git push --force --tags origin
```

#### Option C: Manual Rewrite (If other options fail)

```powershell
# Create new branch from first commit
git checkout --orphan temp-branch

# Add all files
git add -A

# Commit
git commit -m "Initial commit with credentials removed"

# Delete old master
git branch -D master

# Rename temp to master
git branch -m master

# Force push
git push -f origin master
```

---

### Step 3: Fix the Code

Update `TelegramPublisherIntegrationShould.cs` lines 26-27:

**BEFORE (Insecure):**
```csharp
private const string BOT_API_TOKEN = "6030340647:AAFHv9HMz0nuxuI9450tjVUuYJoCe4jf7JQ";
private const string CHAT_ID = "-960612732";
```

**AFTER (Secure):**
```csharp
// ?? CONFIGURE VIA ENVIRONMENT VARIABLES - NEVER COMMIT ACTUAL VALUES ??
private static readonly string BOT_API_TOKEN = 
    Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") 
    ?? "YOUR_BOT_TOKEN_HERE";

private static readonly string CHAT_ID = 
    Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") 
    ?? "YOUR_CHAT_ID_HERE";
```

---

### Step 4: Set Environment Variables

#### Windows (PowerShell - Per Session):
```powershell
$env:TELEGRAM_BOT_TOKEN="YOUR_NEW_TOKEN_FROM_BOTFATHER"
$env:TELEGRAM_CHAT_ID="-960612732"
```

#### Windows (Permanent - User Level):
```powershell
[Environment]::SetEnvironmentVariable("TELEGRAM_BOT_TOKEN", "YOUR_NEW_TOKEN", "User")
[Environment]::SetEnvironmentVariable("TELEGRAM_CHAT_ID", "-960612732", "User")
```

#### Windows (System Settings GUI):
1. Press `Win + R`
2. Type: `SystemPropertiesAdvanced`
3. Click **"Environment Variables"**
4. Under **"User variables"**, click **"New"**
5. Add:
   - Variable: `TELEGRAM_BOT_TOKEN`
   - Value: `YOUR_NEW_TOKEN`
6. Click **"New"** again
7. Add:
   - Variable: `TELEGRAM_CHAT_ID`
   - Value: `-960612732`

#### Linux/Mac:
```bash
# Add to ~/.bashrc or ~/.zshrc
export TELEGRAM_BOT_TOKEN="YOUR_NEW_TOKEN_FROM_BOTFATHER"
export TELEGRAM_CHAT_ID="-960612732"

# Reload
source ~/.bashrc
```

---

### Step 5: Update .gitignore

Add this to `.gitignore`:

```.gitignore
# Sensitive test configuration
**/TelegramPublisherIntegrationShould.cs.user
*.runsettings.user
launchSettings.json

# Environment files
.env
.env.local
*.env

# User secrets
**/secrets.json
```

---

### Step 6: Alternative - Use User Secrets (Recommended for .NET)

```powershell
# Navigate to test project
cd C:\git\Dotnet.Simple.Service.Monitoring\Simple.Service.Monitoring.Tests

# Initialize user secrets
dotnet user-secrets init

# Add secrets
dotnet user-secrets set "Telegram:BotToken" "YOUR_NEW_TOKEN"
dotnet user-secrets set "Telegram:ChatId" "-960612732"
```

Then update the test code:

```csharp
// In Setup() method
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<TelegramPublisherIntegrationShould>()
    .Build();

BOT_API_TOKEN = configuration["Telegram:BotToken"] ?? "YOUR_BOT_TOKEN_HERE";
CHAT_ID = configuration["Telegram:ChatId"] ?? "YOUR_CHAT_ID_HERE";
```

---

## ?? Verify the Leak is Gone

### Check GitHub:

1. Go to: `https://github.com/turric4n/Dotnet.Simple.Service.Monitoring`
2. Search for: `6030340647`
3. Should return **0 results**

### Check Git History:

```powershell
cd C:\git\Dotnet.Simple.Service.Monitoring
git log -p --all -S "6030340647" --source --all
```

Should return **nothing**.

---

## ?? Timeline

| Time | Action | Status |
|------|--------|--------|
| **NOW** | Revoke bot token | ? Pending |
| **+5 min** | Remove from Git history | ? Pending |
| **+10 min** | Update code with env vars | ? Pending |
| **+15 min** | Force push to GitHub | ? Pending |
| **+20 min** | Verify leak is gone | ? Pending |
| **+25 min** | Set up env variables | ? Pending |

---

## ?? What Attackers Could Do

With the exposed credentials, someone could:

- ? Send messages to your Telegram chat
- ? Spam your bot
- ? Monitor your health check alerts
- ? Identify your infrastructure from error messages

**That's why you must revoke the token IMMEDIATELY!**

---

## ??? Prevention for Future

### DO ?:
- Use environment variables
- Use .NET User Secrets
- Use Azure Key Vault / AWS Secrets Manager
- Add `*.env` to `.gitignore`
- Review code before committing
- Use pre-commit hooks to scan for secrets

### DON'T ?:
- Hardcode secrets in code
- Commit `.env` files
- Share secrets in chat/email
- Use production credentials in tests
- Push before reviewing changes

---

## ?? Helpful Tools

### Secret Scanners (Prevent Future Leaks):

1. **git-secrets** (Amazon)
```powershell
git secrets --install
git secrets --register-aws
```

2. **detect-secrets** (Yelp)
```powershell
pip install detect-secrets
detect-secrets scan > .secrets.baseline
```

3. **gitleaks** (Gitleaks)
```powershell
# Download from https://github.com/zricethezav/gitleaks
gitleaks detect --source . --verbose
```

### GitHub Secret Scanning:

GitHub may have already detected this! Check:
- `https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/security`

---

## ?? Need Help?

If you're stuck:

1. **GitHub Support**: https://support.github.com/
2. **Stack Overflow**: Tag `git` and `security`
3. **Telegram @BotFather**: Can help with bot issues

---

## ? Checklist

Before continuing development:

- [ ] Bot token revoked via @BotFather
- [ ] New token generated
- [ ] Git history cleaned (BFG/filter-repo)
- [ ] Changes force-pushed to GitHub
- [ ] Code updated to use env variables
- [ ] Environment variables set locally
- [ ] `.gitignore` updated
- [ ] Verified leak is gone (GitHub search)
- [ ] Secret scanner installed (optional)
- [ ] Tests run successfully with new token

---

## ?? Quick Commands Summary

```powershell
# 1. Revoke token in Telegram (@BotFather)

# 2. Clean Git history
java -jar bfg.jar --replace-text passwords.txt .

# 3. Force push
git reflog expire --expire=now --all
git gc --prune=now --aggressive
git push --force --all origin

# 4. Set environment variables
$env:TELEGRAM_BOT_TOKEN="NEW_TOKEN_HERE"
$env:TELEGRAM_CHAT_ID="-960612732"

# 5. Verify
git log -p -S "6030340647"  # Should return nothing
```

---

## ?? After Recovery

Once you've completed all steps:

1. ? The old token is revoked (useless)
2. ? Git history is clean
3. ? GitHub shows no secrets
4. ? New token is in environment variables
5. ? Code uses secure pattern
6. ? Future leaks prevented

**You're safe!** ??

---

**? Do Step 1 (Revoke Token) RIGHT NOW before doing anything else!**
