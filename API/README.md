# Thrive Stream Controller API

## Configuration

### OBS WebSocket Password

The API needs to connect to OBS Studio via WebSocket. You can configure the password in several ways:

#### Option 1: appsettings.Development.json (Recommended for Development)

Edit `ThriveStreamController.API/appsettings.Development.json`:

```json
{
  "OBS": {
    "Password": "your-obs-password-here"
  }
}
```

**Note:** This file is gitignored by default, so your password won't be committed to source control.

#### Option 2: User Secrets (Alternative for Development)

```bash
cd ThriveStreamController.API
dotnet user-secrets set "OBS:Password" "your-obs-password-here"
```

User secrets are stored outside the project directory and are never committed to source control.

#### Option 3: appsettings.json (For Production/Deployment)

Edit `ThriveStreamController.API/appsettings.json`:

```json
{
  "OBS": {
    "WebSocketUrl": "ws://localhost:4455",
    "Password": "your-obs-password-here"
  }
}
```

**Warning:** Be careful not to commit sensitive passwords to source control. Use environment variables or a secure configuration provider in production.

#### Option 4: Environment Variables (For Production)

Set the environment variable:

```bash
# Windows PowerShell
$env:OBS__Password = "your-obs-password-here"

# Windows CMD
set OBS__Password=your-obs-password-here

# Linux/Mac
export OBS__Password="your-obs-password-here"
```

**Note:** Use double underscores (`__`) to represent nested configuration keys.

### OBS Studio Setup

1. Open OBS Studio
2. Go to **Tools → WebSocket Server Settings**
3. Enable the WebSocket server
4. Set the port to **4455** (default)
5. Set a password (or leave blank for no password)
6. Click **Apply** and **OK**

### Running the API

```bash
# From the repository root
dotnet run --project API/ThriveStreamController.API/ThriveStreamController.API.csproj

# Or from the API directory
cd API
dotnet run --project ThriveStreamController.API/ThriveStreamController.API.csproj
```

The API will start on `http://localhost:5080`

### Testing the Connection

Once the API is running, you can test the OBS connection:

1. Open the UI at `http://localhost:5173`
2. The UI should show "Backend Connection: Connected"
3. Click "Connect to OBS"
4. If configured correctly, you should see "OBS Studio: Connected"

### Troubleshooting

**"Failed to connect to OBS"**
- Ensure OBS Studio is running
- Verify the WebSocket server is enabled in OBS (Tools → WebSocket Server Settings)
- Check that the password in your configuration matches the OBS WebSocket password
- Verify the port is 4455 (or update `appsettings.json` if using a different port)

**"Backend Connection: Disconnected"**
- Ensure the API is running on port 5080
- Check the API logs for errors
- Verify CORS is configured correctly (should allow `http://localhost:5173`)

**"ObjectDisposedException" or "IDisposable" errors**
- These have been fixed in the latest version
- Make sure you're running the latest code
- Restart the API if you see these errors

