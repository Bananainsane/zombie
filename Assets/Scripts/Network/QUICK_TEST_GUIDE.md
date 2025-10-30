# Quick Test Guide - Multiplayer Lobby System

## Quick Setup Checklist

### 1. Scene Setup (5 minutes)

**Required GameObjects:**
- [ ] Canvas with UI scale mode: "Scale with Screen Size"
- [ ] MultiplayerMenu GameObject with MultiplayerMenuUI.cs script
- [ ] NetworkManager with UnityTransport
- [ ] EventSystem

**Required Panels:**
- [ ] MainPanel (active)
- [ ] CreateGamePanel (inactive)
- [ ] BrowseGamesPanel (inactive)
- [ ] StatusPanel (inactive)

**Required Buttons:**
- [ ] Create Game Button
- [ ] Browse Games Button
- [ ] Quick Join Button
- [ ] Back Button

### 2. Prefab Setup

**LobbyListItem Prefab:**
- [ ] Create from template hierarchy
- [ ] Add LobbyListItem.cs script
- [ ] Wire up all references
- [ ] Save to Assets/Prefabs/

### 3. Script References

**MultiplayerMenuUI Inspector:**
- [ ] All panels assigned
- [ ] All buttons assigned
- [ ] Input fields assigned (HostName, MaxPlayers)
- [ ] LobbyListItem prefab assigned
- [ ] LobbyListContainer (ScrollView Content) assigned
- [ ] NoGamesText assigned

## Testing Scenarios

### Scenario 1: Host Game Test

**Steps:**
1. Play scene in Unity Editor
2. Click "Create Game"
3. Enter host name (or use default)
4. Click "Create Game"
5. Check Console for "Lobby created: [name]"
6. Verify status shows "Game created: [name]"
7. Game should load GameScene as Host

**Expected Result:**
- Status indicator turns green
- "Creating game..." → "Game created!"
- Scene transitions to GameScene
- Console shows "Starting as HOST"

### Scenario 2: Browse Games Test

**Setup:**
- Run Build #1 as Host
- Run Unity Editor as Client

**Steps:**
1. In Editor, click "Browse Games"
2. Wait 2 seconds for refresh
3. See hosted game in list
4. Verify host name matches Build #1
5. Verify player count shows "1/12"
6. Status should show "Waiting" in green

**Expected Result:**
- Lobby appears in list
- Join button is enabled
- Refresh updates the list
- No console errors

### Scenario 3: Join Game Test

**Setup:**
- Build #1 running as Host
- Editor as Client at Browse Games screen

**Steps:**
1. Click "Join" on available lobby
2. Status shows "Joining [hostname]..."
3. Wait for connection
4. Verify "Connected!" message
5. Scene loads GameScene
6. Check Console for "Starting as CLIENT"

**Expected Result:**
- Status indicator turns green during connection
- Scene transition occurs
- Build #1 player count updates to "2/12"
- Both instances in GameScene

### Scenario 4: Quick Join Test

**Setup:**
- Build #1 running as Host

**Steps:**
1. Editor at main multiplayer menu
2. Click "Quick Join"
3. Status shows "Finding game..."
4. Automatically connects to first available
5. Scene loads

**Expected Result:**
- Finds and joins immediately
- No manual lobby selection needed
- Connection status updates properly

### Scenario 5: Player Count Update Test

**Setup:**
- Build #1 as Host
- Build #2 joins
- Build #3 joins

**Steps:**
1. Monitor host's lobby list
2. Check player count updates
3. Verify "3/12" shown
4. One player disconnects
5. Verify count decreases

**Expected Result:**
- Real-time player count updates
- Host sees accurate count
- Lobby browser shows current count

### Scenario 6: Full Lobby Test

**Setup:**
- Create lobby with max 2 players
- Host + 1 client joined

**Steps:**
1. Try to join from third client
2. Check lobby status
3. Verify "Full" status
4. Join button should be disabled

**Expected Result:**
- Status changes to "Full" (red)
- Join button disabled
- Cannot join full lobby

## Debug Console Messages

### Successful Flow:

**Host:**
```
Lobby created: MyGame (12 max players)
Starting as HOST
Host started successfully!
Client 0 connected
Client 1 connected
Current players: 3
```

**Client:**
```
Joining lobby: MyGame
Starting as CLIENT
Connecting...
Connected!
Client 2 connected
```

### Error Messages:

**Common Errors:**
- "NetworkManager not found!" → Add NetworkManager to scene
- "Failed to start Host" → Check transport configuration
- "Connection failed" → Verify IP address and port
- "No available games found" → Host needs to create game first

## Performance Check

**Monitor these during testing:**
- [ ] Lobby list updates smoothly (no lag)
- [ ] No memory leaks (check Profiler)
- [ ] UI transitions are smooth
- [ ] No console spam
- [ ] Player count updates in < 1 second

## Common Issues & Quick Fixes

### Issue: Buttons don't respond
**Fix:** Check EventSystem exists in scene

### Issue: No lobbies show up
**Fix:**
1. Check NetworkManager is configured
2. Verify LobbyBrowser is on MultiplayerMenu
3. Check Console for errors

### Issue: Can't join lobby
**Fix:**
1. Verify IP address is correct (127.0.0.1 for local)
2. Check port 7777 is not blocked
3. Ensure lobby status is "Waiting"

### Issue: UI doesn't hide after connection
**Fix:**
1. Check MultiplayerMenuUI loads correct scene
2. Verify scene name matches "GameScene"
3. Check GameSceneStarter exists in GameScene

## Build Testing (Recommended)

### Method 1: Multiple Builds
1. Build → Build Settings
2. Build to folder (e.g., "Build1")
3. Run Build1.exe
4. Play in Unity Editor
5. Test connection between them

### Method 2: ParrelSync (Unity Editor Only)
1. Install ParrelSync from Package Manager
2. Create clone project
3. Open both in separate Unity instances
4. Test multiplayer locally

## Automation Test Script (Optional)

```csharp
// Add to test scene for automated testing
public class LobbySystemTests : MonoBehaviour
{
    private MultiplayerMenuUI menuUI;

    void Start()
    {
        menuUI = FindObjectOfType<MultiplayerMenuUI>();
        StartCoroutine(AutoTest());
    }

    IEnumerator AutoTest()
    {
        // Test 1: Create lobby
        Debug.Log("TEST: Creating lobby...");
        menuUI.OnCreateGame();
        yield return new WaitForSeconds(2f);

        // Test 2: Check if lobby exists
        var browser = FindObjectOfType<LobbyBrowser>();
        Assert.IsTrue(browser.AvailableLobbies.Count > 0, "Lobby created");

        Debug.Log("All tests passed!");
    }
}
```

## Before Committing

- [ ] All scripts compile without errors
- [ ] Prefabs saved correctly
- [ ] Scene references assigned
- [ ] Tested host functionality
- [ ] Tested client functionality
- [ ] Tested Quick Join
- [ ] No console errors during normal flow
- [ ] UI looks good and is readable
- [ ] Documentation updated

## Next Steps

After basic functionality works:
1. Add visual polish (animations, effects)
2. Implement proper network discovery
3. Add lobby filtering/sorting
4. Implement password protection
5. Add player kick/ban functionality
6. Create lobby chat system

---

**Estimated Setup Time:** 30-45 minutes
**Estimated Test Time:** 15-20 minutes
**Total Time:** ~1 hour for complete setup and testing
