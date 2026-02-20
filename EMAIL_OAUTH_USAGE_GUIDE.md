# Email OAuth Device Code Flow - Usage Guide

This guide explains how to set up and use the Microsoft OAuth email authentication feature.

## Prerequisites

1. **Azure App Registration**: You need a Microsoft Azure AD application registered
2. **Backend Configuration**: Configure email settings in `appsettings.json`
3. **User Access**: Users need to be logged into the application

---

## Step 1: Azure App Registration Setup

### 1.1 Create Azure AD App Registration

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Click **New registration**
4. Fill in:
   - **Name**: `InnriGreifi Email Service` (or your preferred name)
   - **Supported account types**: Choose based on your needs (usually "Accounts in this organizational directory only")
   - **Redirect URI**: Leave empty (Device Code Flow doesn't need redirect URIs)
5. Click **Register**

### 1.2 Configure API Permissions

1. In your app registration, go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Select **Delegated permissions**
5. Add the following permissions:
   - `Mail.Read` - Read user mail
   - `Mail.Send` - Send mail as user
   - `User.Read` - Read user profile
6. Click **Add permissions**
7. **Important**: For production, you may need admin consent for these permissions

### 1.3 Get Application Credentials

1. Go to **Overview** in your app registration
2. Copy the following values:
   - **Application (client) ID** → This is your `ClientId`
   - **Directory (tenant) ID** → This is your `TenantId`
3. Go to **Certificates & secrets**
4. Click **New client secret**
5. Add a description and set expiration
6. Click **Add**
7. **Copy the secret value immediately** (you won't see it again) → This is your `ClientSecret`

---

## Step 2: Backend Configuration

Edit `backend/appsettings.json` and configure the email settings:

```json
{
  "Email": {
    "SharedInboxEmail": "your-email@example.com",  // Optional: Default system inbox email
    "TenantId": "your-tenant-id-here",              // From Azure App Registration
    "ClientId": "your-client-id-here",              // From Azure App Registration
    "ClientSecret": "your-client-secret-here",      // From Azure App Registration
    "PollIntervalMinutes": 5,                        // How often to check for new emails
    "MaxContextMessages": 10,                        // Max messages for AI context
    "DeviceCodeTimeoutMinutes": 15                   // Device code expiry time
  }
}
```

**Note**: 
- `SharedInboxEmail` is optional - it's used as a fallback if no system inbox is connected via OAuth
- `ClientSecret` is optional if you only use OAuth (not app-only auth)
- The system will work with just `TenantId` and `ClientId` for OAuth-only setup

---

## Step 3: Connect Your Email (User Guide)

### 3.1 Access Email Settings

1. Log into the application
2. Navigate to **Settings** → **Email Settings** (`/settings/email`)
3. You'll see the **OAuth Tölvupóststillingar** section

### 3.2 Connect System Inbox (First Time Setup)

The system inbox is used for reading incoming emails. Only one system inbox can be connected.

1. Click **Tengja netfang** (Connect Email) button
2. In the modal:
   - Enter your email address (e.g., `your-email@example.com`)
   - Check **Is System Inbox** checkbox
   - Click **Connect**
3. A modal will appear with:
   - **Verification URL**: `https://microsoft.com/devicelogin`
   - **Device Code**: A code like `ABCD EFGH`
4. **On another device or browser tab**:
   - Open the verification URL
   - Enter the device code
   - Sign in with your Microsoft account
   - Grant the requested permissions
5. The modal will automatically detect when you've authenticated
6. You'll see a success message when the connection is complete

### 3.3 Connect Personal Email (For Sending)

Users can connect their personal emails to send emails from their own addresses:

1. Click **Tengja netfang** (Connect Email) button
2. Enter your email address
3. **Don't** check "Is System Inbox" (unless you're setting up the system inbox)
4. Follow the same device code authentication process
5. Once connected, you can select this email when sending replies

### 3.4 View Connection Status

The settings page shows:
- **Connected emails** with status indicators
- **System inbox** badge (purple) for the system inbox
- **Connected** badge (green) for active connections
- **Last refreshed** timestamp

### 3.5 Disconnect Email

1. Find the email in the connections list
2. Click **Aftengja** (Disconnect) button
3. Confirm the disconnection
4. The email will be removed and tokens revoked

---

## Step 4: How It Works

### 4.1 Reading Emails (System Inbox)

1. The `EmailPollingBackgroundService` runs every `PollIntervalMinutes`
2. It checks for a connected system inbox via OAuth
3. If found, uses delegated authentication to read emails
4. If not found, falls back to app-only auth (if `ClientSecret` is configured)
5. New emails are processed and stored in the database

### 4.2 Sending Emails (User Emails)

1. When a user sends a reply:
   - The system checks if the user has connected their email
   - Retrieves the user's access token (refreshes if expired)
   - Uses delegated authentication to send the email
   - The email appears to come from the user's connected email address

### 4.3 Token Management

- **Access tokens** are stored temporarily and refreshed automatically
- **Refresh tokens** are encrypted and stored in the database
- Tokens are automatically refreshed when they expire (5 minutes before expiry)
- If refresh fails, users are prompted to reconnect

---

## Step 5: Troubleshooting

### Issue: "Device code expired"

**Solution**: 
- Device codes expire after 15 minutes (configurable)
- Click "Reyna aftur" (Try again) to get a new code
- Make sure to complete authentication quickly

### Issue: "System inbox is not connected" warning

**Solution**:
- Connect a system inbox email in Settings → Email Settings
- Mark it as "System Inbox" when connecting
- Only one system inbox can be connected at a time

### Issue: "Failed to request device code"

**Solution**:
- Check that `TenantId` and `ClientId` are correctly configured in `appsettings.json`
- Verify the Azure App Registration is active
- Check backend logs for detailed error messages

### Issue: "Token refresh failed"

**Solution**:
- The refresh token may have been revoked
- Disconnect and reconnect the email
- Check if the user revoked access in their Microsoft account settings

### Issue: Emails not being received

**Solution**:
- Check that system inbox is connected (green "Tengt" badge)
- Verify `PollIntervalMinutes` is set appropriately
- Check backend logs for polling errors
- Ensure the email account has emails in the inbox

### Issue: Can't send emails

**Solution**:
- Ensure you have connected your personal email (not just system inbox)
- Check that the email has "Mail.Send" permission granted
- Verify the email appears in the connections list with "Tengt" status

---

## Step 6: API Endpoints (For Developers)

### Connect Email
```
POST /api/auth/email/connect
Body: { "emailAddress": "user@example.com", "isSystemInbox": true }
Returns: { "deviceCode": "...", "verificationUrl": "...", "expiresIn": 900, "interval": 5 }
```

### Poll for Token
```
POST /api/auth/email/poll
Body: { "deviceCode": "..." }
Returns: { "success": true, "emailAddress": "user@example.com" }
```

### Get Connection Status
```
GET /api/auth/email/status
Returns: [{ "emailAddress": "...", "isConnected": true, "isSystemInbox": true, "lastRefreshedAt": "..." }]
```

### Disconnect Email
```
DELETE /api/auth/email/disconnect/{emailAddress}
Returns: 204 No Content
```

---

## Security Notes

1. **Refresh tokens are encrypted** using ASP.NET Core Data Protection
2. **Access tokens are temporary** and stored in memory/database temporarily
3. **Tokens are scoped** to minimum required permissions (Mail.Read, Mail.Send, User.Read)
4. **Users can revoke access** at any time via Microsoft account settings
5. **Device codes expire** after 15 minutes (configurable)
6. **Only one system inbox** can be connected at a time

---

## Best Practices

1. **System Inbox**: Connect a dedicated email account for receiving customer emails
2. **User Emails**: Each user should connect their own email for sending
3. **Token Refresh**: The system handles token refresh automatically - no manual intervention needed
4. **Monitoring**: Check connection status regularly in Settings → Email Settings
5. **Backup**: Keep Azure App Registration credentials secure and backed up

---

## Next Steps

After setting up:
1. Connect the system inbox email
2. Test receiving emails (check `/emails` page)
3. Connect user emails for sending
4. Test sending a reply to verify delegated auth works
5. Monitor logs for any authentication issues

For questions or issues, check the backend logs or contact your system administrator.

