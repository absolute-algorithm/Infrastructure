# Secure NuGet Feed Access for FileFormula

## Why Credentials Matter
Never put your real password or Personal Access Token (PAT) in clear text in your code or scripts. If you commit these to your repository, anyone with access can steal your identity.

## 1. In the GitLab CI Pipeline (Safe & Automatic)
In the `.gitlab-ci.yml` file, use **pre-defined variables** provided by GitLab:

- **Username:** `gitlab-ci-token` (a special robot user for CI jobs)
- **Password:** `$CI_JOB_TOKEN` (a temporary, one-time-use key that expires after the job)

**CI Command Example:**
```bash
dotnet nuget add source "$ORG_NUGET_SOURCE" --name FileFormulaOrg --username gitlab-ci-token --password $CI_JOB_TOKEN --store-password-in-clear-text
```
> **Why it's safe:** `$CI_JOB_TOKEN` is never saved and only exists for the duration of the job.

---

## 2. On Your Local Laptop (The Deploy Token Way)
To download packages locally, use a **Deploy Token** instead of your personal login.

### How to Create a Deploy Token
1. Go to your **FileFormula Group** (127290252) on GitLab.
2. Navigate to **Settings > Repository > Deploy Tokens**.
3. Click **Add token**:
    - **Name:** `Laptop-NuGet-Access`
    - **Username:** (Leave blank; GitLab generates one)
    - **Scopes:** Select `read_package_registry` and `write_package_registry`
4. GitLab provides a **Username** (e.g., `gitlab+deploy-token-XXXX`) and a **Password**.

### Local Machine Command Example
```bash
dotnet nuget add source "https://gitlab.com/api/v4/groups/127290252/-/packages/nuget/index.json" \
  --name FileFormula \
  --username "YOUR_DEPLOY_TOKEN_USERNAME" \
  --password "YOUR_DEPLOY_TOKEN_PASSWORD" \
  --store-password-in-clear-text
```

### Why a Deploy Token is Better
- **Scoped:** Only allows package read/write, not repo or account changes.
- **Separate:** Can be revoked independently if compromised.
- **Long-lived:** Remains active for regular use.

---

## 🛡️ Summary for FileFormula
- **CI Pipeline:** Use `gitlab-ci-token` and `$CI_JOB_TOKEN`. **Zero risk.**
- **Local Machine:** Use a **Deploy Token**. **Controlled risk.**
- **Never:** Use your main password in a command line or script.

---

## Optional: Using `nuget.config`
Would you like to see how to add these credentials to your `nuget.config` file for easier use?
