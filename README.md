# Thrive Stream Controller

A volunteer-friendly livestream management application for churches and organizations that simplifies multi-platform streaming to YouTube and Facebook.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![React](https://img.shields.io/badge/React-19-blue.svg)

## What Is This?

Thrive Stream Controller is an all-in-one control panel that makes livestreaming simple for volunteers who aren't technical experts. Instead of juggling multiple browser tabs, OBS settings, and platform-specific dashboards, volunteers get **three big buttons**:

1. **Start Stream** – Creates the YouTube broadcast, starts OBS streaming, and goes live
2. **Open Facebook** – Opens Facebook Live Producer for the one manual "Go Live" click
3. **End Stream** – Stops everything cleanly

The goal: **Sundays should be stress-free.** Volunteers follow a simple checklist without touching stream keys, RTMP URLs, or API credentials.

## Features

- 🎬 **OBS Integration** – Full control via WebSocket (start/stop streaming, switch scenes, monitor audio)
- 📺 **YouTube Live API** – Automated broadcast creation with persistent stream keys
- 📘 **Facebook Live** – Streamlined workflow with persistent keys (one manual click required)
- 🎛️ **Real-time Dashboard** – Live audio meters, scene switching, connection status
- 🔒 **Secure Credential Storage** – OAuth tokens encrypted in local database
- 🐳 **Docker Ready** – Single container deployment for production use

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Streaming PC                              │
│  ┌─────────────┐     ┌─────────────────────────────────┐    │
│  │             │     │   Thrive Stream Controller      │    │
│  │     OBS     │◄────┤                                 │    │
│  │   Studio    │     │  ┌─────────┐    ┌───────────┐  │    │
│  │             │     │  │ React   │    │  .NET 9   │  │    │
│  └──────┬──────┘     │  │   UI    │◄──►│   API     │  │    │
│         │            │  └─────────┘    └─────┬─────┘  │    │
│         │            └───────────────────────┼────────┘    │
│         │ RTMP                               │              │
│         ▼                                    ▼              │
│  ┌─────────────┐                    ┌─────────────┐        │
│  │   Castr     │                    │  SQLite DB  │        │
│  │  (or OBS    │                    │ (encrypted  │        │
│  │  direct)    │                    │  tokens)    │        │
│  └──────┬──────┘                    └─────────────┘        │
└─────────┼──────────────────────────────────────────────────┘
          │ RTMP Distribution
          ▼
    ┌─────────────┐     ┌─────────────┐
    │   YouTube   │     │  Facebook   │
    │    Live     │     │    Live     │
    └─────────────┘     └─────────────┘
```

**Key Points:**
- OBS handles the actual video encoding and RTMP streaming
- The controller manages OBS via WebSocket and creates/controls platform broadcasts via APIs
- Castr (or OBS multi-output) distributes the single RTMP stream to multiple platforms
- All credentials are stored locally and encrypted


## Quick Start (Docker)

The easiest way to run Thrive Stream Controller is with Docker.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed
- [OBS Studio](https://obsproject.com/) with WebSocket enabled
- YouTube channel with live streaming enabled
- Facebook Page with persistent stream key configured

### Setup

```bash
# 1. Clone the repository
git clone https://github.com/ThriveCommunityChurch/Thrive_Stream_Controller.git
cd Thrive_Stream_Controller

# 2. Create your environment file
cp .env.example .env

# 3. Edit .env with your credentials (see Configuration section below)
notepad .env  # Windows
nano .env     # Linux/Mac

# 4. Build and start the container
docker-compose up -d

# 5. Open in browser
start http://localhost:8080  # Windows
open http://localhost:8080   # Mac
```

### First-Time YouTube Authorization

After starting the app:

1. Open http://localhost:8080
2. Go to **Settings**
3. Click **Connect YouTube Account**
4. Complete the Google OAuth flow
5. The refresh token is stored encrypted – you won't need to do this again

## Development Setup

If you want to run the application without Docker for development:

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)
- [OBS Studio](https://obsproject.com/)

### Running Locally

**Terminal 1 – API:**
```bash
cd API/ThriveStreamController.API
dotnet run
```

**Terminal 2 – UI:**
```bash
cd UI
npm install
npm run dev
```

**Access:**
- UI: http://localhost:5173
- API: http://localhost:5080
- Swagger: http://localhost:5080/swagger

## Configuration

### Environment Variables

Copy `.env.example` to `.env` and configure:

| Variable | Description | Where to Get It |
|----------|-------------|-----------------|
| `OBS__WebSocketUrl` | OBS WebSocket URL | Default: `ws://host.docker.internal:4455` |
| `OBS__Password` | OBS WebSocket password | OBS → Tools → WebSocket Server Settings |
| `YouTube__ClientId` | Google OAuth Client ID | [Google Cloud Console](https://console.cloud.google.com/apis/credentials) |
| `YouTube__ClientSecret` | Google OAuth Client Secret | Same as above |
| `Facebook__LiveProducerUrl` | Your Page's Live Producer URL | Facebook → Your Page → Live Video |

### Google Cloud Setup (YouTube)

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project (or select existing)
3. Enable **YouTube Data API v3**
4. Go to **Credentials** → **Create Credentials** → **OAuth 2.0 Client ID**
5. Application type: **Web application**
6. Add authorized redirect URI: `http://localhost:8080/api/auth/youtube/callback`
7. Copy the Client ID and Client Secret to your `.env` file

### OBS Setup

1. Open OBS Studio
2. Go to **Tools → WebSocket Server Settings**
3. Check **Enable WebSocket server**
4. Set port to **4455** (default)
5. Set a password (recommended) or leave blank
6. Click **Apply**

### Facebook Setup

Facebook requires a persistent stream key and one manual "Go Live" click:

1. Go to your Facebook Page
2. Click **Live Video** or navigate to [facebook.com/live/producer](https://facebook.com/live/producer)
3. Select your Page
4. Go to **Settings** → Enable **Persistent Stream Key**
5. Configure OBS (or Castr) to send RTMP to this key
6. Save the Live Producer URL to your `.env` file


## Volunteer Workflow (Sunday Morning)

This is the simple process volunteers follow each week:

### Before Service

1. ✅ Turn on the streaming PC
2. ✅ Open **OBS Studio** – verify correct profile and scenes are loaded
3. ✅ Open **Thrive Stream Controller** (http://localhost:8080)
4. ✅ Confirm OBS shows "Connected" in the dashboard

### Starting the Stream

1. Click **"▶ Start Stream (OBS + YouTube)"**
2. Wait for the status to show "LIVE" (takes ~15-30 seconds)
3. Click **"📺 Open Facebook to Go Live"**
4. In the Facebook browser tab:
   - Verify preview video is showing
   - Click the **"Go Live"** button
   - Close the tab and return to the controller

### During Service

- Use OBS to switch scenes as needed
- Monitor audio levels in the dashboard
- The controller shows real-time streaming status

### Ending the Stream

1. Click **"⏹ End Stream"**
2. That's it! YouTube and Facebook will automatically end when the stream stops

## Troubleshooting

### OBS Shows "Not Connected"

- Make sure OBS Studio is running
- Check that WebSocket server is enabled in OBS (Tools → WebSocket Server Settings)
- Verify the password in `.env` matches OBS
- Try restarting the Thrive Stream Controller

### YouTube Won't Go Live

- Check your internet connection
- Verify YouTube credentials are configured (Settings → YouTube status)
- Make sure your YouTube channel has live streaming enabled (requires 24-hour wait for new channels)
- Check the browser console for error details

### Facebook Preview Shows No Video

- Confirm OBS is actually streaming (check OBS status bar)
- Verify the correct OBS profile with Facebook's stream key is active
- Wait 10-15 seconds for the preview to appear
- If still no video, check your RTMP distribution service (Castr) or OBS output settings

### Container Won't Start

```bash
# Check logs for errors
docker-compose logs

# Rebuild the container
docker-compose down
docker-compose up --build -d
```

## Project Structure

```
Thrive_Stream_Controller/
├── API/                              # .NET 9 Backend
│   ├── ThriveStreamController.API/   # Web API, Controllers, SignalR Hubs
│   ├── ThriveStreamController.Core/  # Business logic, Services, Models
│   └── ThriveStreamController.Data/  # Entity Framework, SQLite
├── UI/                               # React 19 Frontend
│   └── src/
│       ├── components/               # React components
│       ├── hooks/                    # Custom React hooks
│       └── services/                 # API client services
├── docker-compose.yml                # Docker orchestration
├── Dockerfile                        # Multi-stage build
├── .env.example                      # Environment template
└── livestream-workflow.md            # Detailed workflow documentation
```

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | React 19, TypeScript, Vite, TailwindCSS |
| **Backend** | .NET 9, ASP.NET Core, SignalR |
| **Database** | SQLite with Entity Framework Core |
| **OBS Integration** | Custom WebSocket client (OBS WebSocket Protocol v5) |
| **YouTube** | Google APIs Client Library for .NET |
| **Containerization** | Docker, Docker Compose |

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/obs/status` | GET | Get OBS connection status |
| `/api/obs/connect` | POST | Connect to OBS WebSocket |
| `/api/obs/scenes` | GET | List available scenes |
| `/api/obs/scenes/switch` | POST | Switch to a scene |
| `/api/obs/streaming/start` | POST | Start OBS streaming |
| `/api/obs/streaming/stop` | POST | Stop OBS streaming |
| `/api/auth/youtube/status` | GET | Get YouTube auth status |
| `/api/auth/youtube/authorize` | GET | Start OAuth flow |
| `/api/youtube/broadcast` | POST | Create new broadcast |
| `/api/youtube/broadcast/{id}/live` | POST | Transition to live |
| `/api/youtube/broadcast/{id}/end` | POST | End broadcast |

Full API documentation available at `/swagger` when running in development mode.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [OBS Studio](https://obsproject.com/) and the OBS WebSocket plugin team
- [Google YouTube Data API](https://developers.google.com/youtube/v3)
- Built with ❤️ for [Thrive Community Church](https://thrive-fl.org/)