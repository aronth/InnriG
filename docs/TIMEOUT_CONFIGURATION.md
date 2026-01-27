# Timeout Configuration Guide

## Problem: 504 Gateway Timeout on Order Sheet Uploads

When uploading large order sheets (Excel files), you may encounter a **504 Gateway Timeout** error. This happens because:

1. Large Excel files take time to process (row-by-row parsing and database insertion)
2. The reverse proxy (nginx-proxy-manager) has default timeout settings that are too short
3. The backend Kestrel server also has default timeout limits

## Solution

### 1. Backend Configuration (Already Applied)

The backend has been configured with:
- **Kestrel timeout**: 30 minutes (configured in `Program.cs` and `appsettings.json`)
- **Progress logging**: Added to `OrderImportService` to track import progress

### 2. Nginx Proxy Manager Configuration

You need to configure nginx-proxy-manager to allow longer request timeouts. Follow these steps:

#### Option A: Via Nginx Proxy Manager UI (Recommended)

1. Log into Nginx Proxy Manager (usually at `http://your-server:81`)
2. Go to **Proxy Hosts** → Select your backend proxy host
3. Click **Edit** → Go to **Advanced** tab
4. Add the following custom nginx configuration in the **Custom Nginx Configuration** section:

```nginx
# Increase timeouts for long-running requests (file uploads)
proxy_read_timeout 1800s;      # 30 minutes
proxy_connect_timeout 1800s;   # 30 minutes
proxy_send_timeout 1800s;       # 30 minutes

# Increase client body size if needed (default is usually 1MB)
client_max_body_size 100M;

# Disable buffering for better progress tracking
proxy_buffering off;
```

5. Click **Save**

#### Option B: Via Docker Volume (If UI doesn't work)

If the UI doesn't persist the settings, you can directly edit the nginx configuration:

1. Find the nginx-proxy-manager data volume:
   ```bash
   docker volume inspect innrig-nginx_proxy_manager_data
   ```

2. Access the container:
   ```bash
   docker exec -it innrig-nginx-proxy-manager sh
   ```

3. Edit the proxy host configuration file (usually in `/data/nginx/proxy_host/`)
   - Find your backend proxy host file (check the `domain_names` field)
   - Add the timeout settings as shown in Option A

4. Reload nginx:
   ```bash
   nginx -s reload
   ```

### 3. Alternative: Environment Variable Configuration

You can also configure nginx-proxy-manager via environment variables in `docker-compose.yml`:

```yaml
nginx-proxy-manager:
  image: jc21/nginx-proxy-manager:latest
  container_name: innrig-nginx-proxy-manager
  restart: unless-stopped
  environment:
    - NGINX_PROXY_READ_TIMEOUT=1800
    - NGINX_PROXY_CONNECT_TIMEOUT=1800
    - NGINX_PROXY_SEND_TIMEOUT=1800
  # ... rest of configuration
```

**Note**: Check nginx-proxy-manager documentation for exact environment variable names, as they may vary.

## Monitoring Import Progress

The backend now logs import progress every 10 seconds. Check your backend logs to monitor:

```bash
docker logs -f innrig-server
```

You'll see messages like:
```
Import progress: 5000/10000 (50.0%) - Imported: 4500, Skipped: 500
```

## Troubleshooting

### Still Getting 504 Timeout?

1. **Check backend logs**: Verify the import is actually running
   ```bash
   docker logs innrig-server | grep -i "import"
   ```

2. **Check nginx logs**: See if nginx is timing out
   ```bash
   docker logs innrig-nginx-proxy-manager
   ```

3. **Verify timeout settings**: Check that nginx configuration was saved correctly

4. **Consider file size**: Very large files (>100MB) may need additional configuration

### For Very Large Files (>100MB)

If you're uploading very large files, you may also need to:

1. Increase `client_max_body_size` in nginx (shown in Option A above)
2. Increase Kestrel `MaxRequestBodySize` in `Program.cs` (currently commented out)
3. Consider implementing async/background job processing for extremely large files

## Performance Tips

- The import service processes rows in batches of 1000
- Change tracking is disabled during bulk inserts for better performance
- Progress is logged every 10 seconds to help diagnose issues
- Consider splitting very large files into smaller chunks if timeouts persist

## Related Files

- `backend/Program.cs` - Kestrel timeout configuration
- `backend/appsettings.json` - Kestrel timeout settings
- `backend/Services/OrderImportService.cs` - Import service with progress logging
- `docker-compose.yml` - Docker configuration

