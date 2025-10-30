# Unity Setup Instructions - Drone Selection System

## ✅ What's Already Done (Next.js Side)

- Viewers can select from 6 drone types (drone1-drone6)
- Drone selection modal with 3x2 grid layout
- Viewer tracking with automatic slot assignment (droneId 1-4)
- Viewer data sent to Unity via SendMessage
- Fixed viewer disconnection issues
- Fixed empty viewers array issue

## 🎮 What You Need to Do in Unity

### Step 1: Open Unity Scene

1. Open Unity Editor
2. Open Scene 8 (`8_droneSwarm`)

### Step 2: Create GameManager GameObject

1. In Hierarchy, right-click → Create Empty
2. Name it: `GameManager`
3. Add Component → Scripts → GameManager (the script is already at `/Assets/Scripts/GameManager.cs`)

### Step 3: Configure GameManager in Inspector

#### A. Assign DroneSwarmLoader Reference

1. Select `GameManager` in Hierarchy
2. In Inspector, find the `Drone Swarm Loader` field
3. Drag your existing `EnemyDrones` GameObject from Hierarchy into this field

#### B. Assign All 6 Drone Assets

In Project window, navigate to `Assets/Data/Drones`. You should see:
- D01.asset
- D02.asset
- D03.asset
- D04.asset
- D05.asset
- D06.asset

Drag each one into the corresponding Inspector field:
- **drone1Asset** ← D01 (Assault Drone)
- **drone2Asset** ← D02 (Tank Drone)
- **drone3Asset** ← D03 (Speed Drone)
- **drone4Asset** ← D04 (Artillery Drone)
- **drone5Asset** ← D05 (Stealth Drone)
- **drone6Asset** ← D06 (Berserker Drone)

### Step 4: Save Scene

- File → Save Scene (or Ctrl+S / Cmd+S)

### Step 5: Build WebGL

1. File → Build Settings
2. Select WebGL platform
3. Click "Build"
4. Save to `/Users/kartik/SolMechs/honeycomb-int/public/unity/Build`
5. Name the build: `webgl-build`

## 🧪 Testing

### Test Flow:

1. **Start Next.js dev server** (should already be running):
   ```bash
   cd /Users/kartik/SolMechs/honeycomb-int
   npm run dev
   ```

2. **Create a game** (Streamer):
   - Go to http://localhost:3002/arena
   - Click "CREATE GAME"
   - Note the room code

3. **Join as viewer**:
   - Open new browser tab/window
   - Go to http://localhost:3002/arena/viewer
   - Enter the room code
   - Select a drone type (e.g., drone5 - Stealth Drone)
   - Click "CONFIRM"

4. **Join more viewers** (optional, up to 4 total):
   - Repeat step 3 with different drone selections
   - Each viewer gets assigned slot 1, 2, 3, or 4 automatically

5. **Launch game** (Streamer):
   - Click "LAUNCH GAME" button
   - Unity WebGL will load
   - Check browser console for logs:
     ```
     🎮 Fetching viewer data for game: XXXXXX
     📊 Number of viewers: X
     📤 Sending to Unity: {"gameId":"...","role":"streamer","viewers":[...]}
     ✅ Sent game data to Unity
     ```

6. **Check Unity Console** (if running in Editor):
   ```
   [GameManager] Received game data: {...}
   [GameManager] Game ID: XXXXXX
   [GameManager] Viewers: X
   [GameManager] Assigning X viewer-selected drones to DroneSwarmLoader
   [GameManager] ✅ Assigned drone5 (D05) to Slot 0 for PlayerName
   [GameManager] ✅ All viewer drones assigned to DroneSwarmLoader!
   ```

## 🔧 How It Works

### Frontend (Next.js):
1. Viewer selects drone type → stored as `selectedDroneType` ("drone1" - "drone6")
2. Viewer gets assigned a slot → stored as `droneId` (1-4)
3. On launch, data sent to Unity: `{ gameId, role, viewers: [{userId, username, droneId, selectedDroneType}] }`

### Unity (GameManager.cs):
1. Receives JSON data via `ReceiveGameData(string jsonData)`
2. Parses viewer data
3. Maps `selectedDroneType` to Drone ScriptableObject asset:
   - "drone1" → D01.asset
   - "drone2" → D02.asset
   - "drone3" → D03.asset
   - "drone4" → D04.asset
   - "drone5" → D05.asset
   - "drone6" → D06.asset
4. Assigns to DroneSwarmLoader slots:
   - droneId=1 → slot0Drone
   - droneId=2 → slot1Drone
   - droneId=3 → slot2Drone
   - droneId=4 → slot3Drone
5. DroneSwarmLoader handles spawning during gameplay

## 🐛 Troubleshooting

### Issue: "SendMessage: object GameManager not found!"
**Solution**: Make sure you created a GameObject named exactly `GameManager` (case-sensitive) in your Unity scene and attached the GameManager.cs script.

### Issue: "DroneSwarmLoader reference not assigned!"
**Solution**: In Unity Inspector, drag your `EnemyDrones` GameObject into the `Drone Swarm Loader` field.

### Issue: Drones don't change / same drones appear
**Solution**:
1. Check all 6 drone assets are assigned in Inspector
2. Check Unity Console for GameManager logs showing drone assignments
3. Verify viewers actually selected different drone types

### Issue: Empty viewers array `"viewers":[]`
**Solution**: This was already fixed by increasing heartbeat timeout to 60s. If still happening, check:
- Viewers are successfully joining (check `/api/viewers` endpoint)
- Viewers stay connected (check browser console for heartbeat logs)

## 📝 Next Steps

After confirming basic integration works:

1. **Real-time control**: Allow viewers to control their drones
2. **Viewer count display**: Show viewer count in streamer dashboard
3. **Dynamic join/leave**: Handle viewers joining mid-game
4. **Combat stats**: Track which viewer's drone dealt most damage
5. **Rewards**: Distribute rewards based on drone performance
