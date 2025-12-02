# RaidPopup

A client-side mod for **SPT 4.0 + Fika** that displays a popup notification when other players start a raid.

<img width="1377" height="566" alt="image" src="https://github.com/user-attachments/assets/b5c91a3e-351a-4e10-ab3a-d822ddb6451f" />

## Features

- **Raid Notifications**: See when other players start a raid without them having to tell you
- **Map & Time Display**: Shows the map name (Customs, Factory, etc.) and time of day (DAY/NIGHT)
- **Host Name**: See who started the raid
- **Click to Dismiss**: Simply click the notification to dismiss it
- **Stacking**: Multiple raid notifications stack vertically
- **Non-intrusive**: Positioned in the top-right corner, doesn't interfere with gameplay

## Requirements

- SPT 4.0.x
- Fika 2.x (required - this mod hooks into Fika's notification system)
- BepInEx 5.4.x (included with SPT)

## Installation

1. Download the latest release
2. Extract `BepInEx/plugins/RaidPopup/` to your SPT installation folder
3. Start the game

## Configuration

Press **F12** in-game to open the BepInEx Configuration Manager. Find **RaidPopup** to access:

| Setting | Description |
|---------|-------------|
| **Enable Notifications** | Master toggle for the notification panel |
| **Debug Mode** | Show fake notifications for testing (for developers) |

## How It Works

When a player connected to your Fika server starts a raid, the server broadcasts a notification via WebSocket. This mod intercepts that notification and displays it as a persistent UI panel instead of a brief toast message.

## Building from Source

### Prerequisites

- Visual Studio 2022 or later (or MSBuild)
- .NET Framework 4.7.1 Developer Pack
- SPT 4.0 installation (for reference DLLs)

### Build Steps

1. Clone the repository
2. Update `$(SPTPath)` in `Client/RaidPopup.csproj` to your SPT installation path
3. Build the solution:

```bash
dotnet build RaidPopup.sln
```

4. Output will be in `dist/BepInEx/plugins/RaidPopup/`

## Project Structure

```
RaidPopup/
├── Client/
│   ├── RaidPopupPlugin.cs         # Main plugin entry point
│   ├── Models/
│   │   └── ActiveRaid.cs          # Raid data model
│   ├── Patches/
│   │   └── StartRaidNotificationPatch.cs  # Fika WebSocket hook
│   └── UI/
│       └── RaidNotificationPanel.cs       # UI rendering
├── RaidPopup.sln
└── README.md
```

## Credits

- Built for the [SPT](https://sp-tarkov.com/) and [Fika](https://github.com/project-fika) communities
- Uses [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony)

## License

This project is released into the public domain under [The Unlicense](LICENSE). Do whatever you want with it.
