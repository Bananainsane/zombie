# Multiplayer Lobby System - Setup Guide

## Overview
A modern multiplayer lobby browser system for your Unity zombie game, featuring game discovery, host options, connection status feedback, and smooth transitions.

## Architecture

### Core Components

1. **LobbyData.cs** - Data structure for lobby information
   - Stores lobby ID, host name, player count, max players, status
   - Provides helper properties like `IsFull`, `CanJoin`, `PlayerCountText`

2. **LobbyBrowser.cs** - Main lobby discovery and management system
   - Discovers available games on local network
   - Manages lobby creation and joining
   - Handles player connection/disconnection events
   - Auto-refreshes lobby list
   - Cleans up stale lobbies

3. **LobbyListItem.cs** - UI component for individual lobby entries
   - Displays lobby information (host name, player count, status)
   - Color-coded status indicators
   - Join button with enabled/disabled states

4. **MultiplayerMenuUI.cs** - Main multiplayer menu controller
   - Manages UI panels (main, create game, browse games)
   - Handles user input and validation
   - Provides connection status feedback
   - Integrates with LobbyBrowser

5. **NetworkUI.cs (Enhanced)** - Improved network connection UI
   - Visual status indicators with color coding
   - Real-time player count display
   - Smooth transitions between connection states
   - Event-driven updates

## Setup Instructions

### Step 1: Create Multiplayer Menu Scene

1. Create a new scene or use existing "MainMenu" scene
2. Add Canvas with UI Scale Mode set to "Scale with Screen Size"

### Step 2: Main Panel Structure

Create the following GameObject hierarchy:

```
Canvas
├── MultiplayerMenu
│   ├── MainPanel
│   │   ├── Title (TextMeshPro)
│   │   ├── CreateGameButton (Button + TMP)
│   │   ├── BrowseGamesButton (Button + TMP)
│   │   ├── QuickJoinButton (Button + TMP)
│   │   └── BackButton (Button + TMP)
│   │
│   ├── CreateGamePanel (Initially inactive)
│   │   ├── Title (TextMeshPro)
│   │   ├── HostNameLabel (TextMeshPro)
│   │   ├── HostNameInput (TMP_InputField)
│   │   ├── MaxPlayersLabel (TextMeshPro)
│   │   ├── MaxPlayersInput (TMP_InputField)
│   │   ├── CreateButton (Button + TMP)
│   │   └── CancelButton (Button + TMP)
│   │
│   ├── BrowseGamesPanel (Initially inactive)
│   │   ├── Title (TextMeshPro)
│   │   ├── LobbyListScrollView
│   │   │   └── Content (with Vertical Layout Group)
│   │   ├── NoGamesText (TextMeshPro - initially inactive)
│   │   ├── RefreshButton (Button + TMP)
│   │   └── BackButton (Button + TMP)
│   │
│   ├── StatusPanel (Initially inactive)
│   │   ├── StatusBackground (Image with transparency)
│   │   ├── StatusText (TextMeshPro)
│   │   └── LoadingSpinner (Image with rotation animation)
│   │
│   ├── ConnectionSuccessPanel (Initially inactive)
│   │   └── MessageText (TextMeshPro)
│   │
│   └── ConnectionFailedPanel (Initially inactive)
│       └── MessageText (TextMeshPro)
```

### Step 3: Create Lobby List Item Prefab

1. Create new GameObject: "LobbyListItem"
2. Add components:
   - RectTransform (width: flexible, height: 80)
   - Image (background)
   - Button component

3. Add children:
```
LobbyListItem
├── HostName (TextMeshPro - Align Left)
├── PlayerCount (TextMeshPro - Align Center)
├── StatusText (TextMeshPro - Align Right)
├── StatusIndicator (Image - small circle)
└── JoinButton (Button + TMP)
```

4. Add LobbyListItem.cs script to root
5. Wire up references in Inspector
6. Save as Prefab in Assets/Prefabs/

### Step 4: Configure MultiplayerMenuUI

1. Add MultiplayerMenuUI.cs to MultiplayerMenu GameObject
2. Assign all references in Inspector:
   - Drag panels to their respective fields
   - Assign all buttons
   - Assign input fields
   - Set LobbyListItem prefab
   - Set lobby list container (Content in ScrollView)
3. Configure settings:
   - Default Max Players: 12
   - Game Scene Name: "GameScene"

### Step 5: Style the UI (Recommended Colors)

**Color Palette:**
- Primary Background: `#1A1A2E` (Dark blue-gray)
- Secondary Background: `#16213E` (Darker blue)
- Accent/Buttons: `#0F4C75` (Blue)
- Accent Hover: `#3282B8` (Lighter blue)
- Success/Green: `#2ECC71` (Bright green)
- Warning/Orange: `#F39C12` (Orange)
- Error/Red: `#E74C3C` (Red)
- Text Primary: `#ECF0F1` (Light gray)
- Text Secondary: `#BDC3C7` (Gray)

**Button Styling:**
- Corner Radius: 8-12px
- Padding: 16px horizontal, 12px vertical
- Font Size: 18-24px
- Shadow: Subtle drop shadow
- Hover: Slightly lighter background
- Pressed: Darker background

**Status Indicator:**
- Waiting: Green (#2ECC71)
- In Progress: Orange (#F39C12)
- Full: Red (#E74C3C)

### Step 6: Animations (Optional but Recommended)

Add smooth transitions using Unity Animator or DOTween:

1. Panel transitions: Fade in/out or slide
2. Button hover effects: Scale 1.05x
3. Loading spinner: Continuous rotation
4. Lobby list items: Fade in on create
5. Connection feedback: Slide up from bottom

### Step 7: Testing Setup

1. Build two instances:
   - Build → Build Settings → Build
   - Or use ParrelSync for Unity Editor testing

2. Test scenarios:
   - Create game from Instance 1
   - Browse games from Instance 2
   - Join game
   - Verify player count updates
   - Test Quick Join
   - Test disconnect

## Usage Flow

### For Players Creating Games:

1. Click "Create Game" from main menu
2. Enter host name (or use default)
3. Set max players (2-32)
4. Click "Create Game"
5. Wait for connection status
6. Game scene loads as Host

### For Players Joining Games:

**Option 1: Browse Games**
1. Click "Browse Games"
2. See list of available games
3. Click "Join" on desired game
4. Wait for connection
5. Game scene loads as Client

**Option 2: Quick Join**
1. Click "Quick Join"
2. Automatically joins first available game
3. Game scene loads as Client

## Advanced Configuration

### Lobby Refresh Settings (LobbyBrowser.cs)

```csharp
[SerializeField] private float refreshInterval = 2f;  // How often to refresh
[SerializeField] private float lobbyTimeout = 10f;    // When to remove stale lobbies
[SerializeField] private int defaultMaxPlayers = 12;  // Default player limit
```

### Network Configuration

The system uses Unity Transport (UTP) by default:
- Default Port: 7777
- Connection Type: UDP
- Address: 127.0.0.1 (localhost for testing)

For LAN/Online play, update in LobbyBrowser.cs:
```csharp
currentHostedLobby.ipAddress = "YOUR_LAN_IP"; // e.g., "192.168.1.100"
```

## Troubleshooting

### Issue: No lobbies appear in browser
**Solution:**
- Check NetworkManager is in scene
- Verify transport component is attached
- Ensure firewall allows Unity traffic
- Try localhost (127.0.0.1) first

### Issue: Connection fails
**Solution:**
- Check IP address is correct
- Verify port 7777 is not blocked
- Ensure NetworkManager is configured properly
- Check Console for error messages

### Issue: Player count not updating
**Solution:**
- Verify LobbyBrowser events are subscribed
- Check NetworkManager callbacks are working
- Ensure UpdateHostedLobby() is running in Update()

### Issue: UI doesn't hide after connection
**Solution:**
- Check "Hide UI On Connect" is enabled in NetworkUI
- Verify MultiplayerMenuUI is loading correct scene
- Check GameSceneStarter is properly configured

## Code Integration Example

### Creating a game programmatically:

```csharp
var lobbyBrowser = GetComponent<LobbyBrowser>();
lobbyBrowser.CreateLobby("My Awesome Game", 12);
```

### Joining a game programmatically:

```csharp
var lobbyBrowser = GetComponent<LobbyBrowser>();
LobbyData firstAvailable = lobbyBrowser.AvailableLobbies.FirstOrDefault();
if (firstAvailable != null)
{
    lobbyBrowser.JoinLobby(firstAvailable);
}
```

### Listening to connection status:

```csharp
lobbyBrowser.OnConnectionStatusChanged.AddListener((status) => {
    Debug.Log($"Connection Status: {status}");
});
```

## Performance Considerations

- Lobby list refreshes every 2 seconds (configurable)
- Stale lobbies removed after 10 seconds of no updates
- UI updates only when lobby data changes
- Efficient dictionary lookup for lobby management

## Future Enhancements

Potential improvements for production:
- [ ] Proper network discovery using UDP broadcast
- [ ] Relay server support for online play
- [ ] Password-protected lobbies
- [ ] Lobby chat system
- [ ] Game mode selection
- [ ] Map voting
- [ ] Player avatars/icons
- [ ] Ping display
- [ ] Region selection
- [ ] Friends system integration

## API Reference

### LobbyData
```csharp
public class LobbyData
{
    public string lobbyId;
    public string hostName;
    public int currentPlayers;
    public int maxPlayers;
    public LobbyStatus status;
    public string ipAddress;
    public ushort port;

    public bool IsFull { get; }
    public bool CanJoin { get; }
    public string PlayerCountText { get; }
}
```

### LobbyBrowser Methods
```csharp
public void CreateLobby(string hostName, int maxPlayers)
public void JoinLobby(LobbyData lobby)
public void QuickJoin()
public void LeaveLobby()
public void StartRefreshing()
public void StopRefreshing()
public void RefreshLobbyList()
```

### LobbyBrowser Events
```csharp
public UnityEvent<List<LobbyData>> OnLobbiesUpdated
public UnityEvent<string> OnConnectionStatusChanged
```

---

**Version:** 1.0.0
**Compatible With:** Unity Netcode for GameObjects 1.0+
**Tested On:** Unity 2022.3 LTS
**Last Updated:** 2025-10-30
