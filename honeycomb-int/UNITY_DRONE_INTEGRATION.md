# Unity Drone Integration Guide

## Overview
The Next.js frontend now supports drone selection for viewers. Viewers choose which ENEMY DRONE (from 6 available types) will fight the streamer's mech on their behalf.

## Game Concept
- **Streamer**: Pilots the MECH (the player)
- **Viewers**: Each selects an ENEMY DRONE TYPE (from 6 options in Assets/Data/Drones)
- **4 Drone Slots**: Up to 4 viewers can join, each gets assigned a slot (droneId 1-4)
- **Enemy Drones**: The 4 drones in "EnemyDrones 4 Drones waves (slot)" are populated with viewer-selected types
- **Battle**: Streamer's mech fights the 4 enemy drones chosen by viewers

## What's Implemented on Next.js Side

### 1. **Drone Selection Modal**
- Viewers see a modal when joining a game
- 6 drone types available (from Unity Assets/Data/Drones):
  - `drone1`: Assault Drone
  - `drone2`: Tank Drone
  - `drone3`: Speed Drone
  - `drone4`: Artillery Drone
  - `drone5`: Stealth Drone
  - `drone6`: Berserker Drone

### 2. **Viewer Tracking**
- Each viewer gets assigned:
  - `droneId`: Enemy drone SLOT number (1-4) - automatically assigned
  - `selectedDroneType`: Which drone TYPE they chose (drone1-drone6)
  - `username`: Their display name
  - `userId`: Unique identifier

**Example:**
- Viewer1 joins → Gets slot droneId=1, selects "drone3" (Speed Drone)
- Viewer2 joins → Gets slot droneId=2, selects "drone6" (Berserker Drone)
- Viewer3 joins → Gets slot droneId=3, selects "drone2" (Tank Drone)
- Viewer4 joins → Gets slot droneId=4, selects "drone1" (Assault Drone)

Unity receives: Spawn Speed Drone at slot 1, Berserker at slot 2, Tank at slot 3, Assault at slot 4

### 3. **Unity Data Passing**
When streamer clicks "LAUNCH GAME", Unity page receives:
- URL: `/unity?gameId=ABC123&scene=8&role=streamer`
- Automatically fetches all viewers for that game
- Sends data to Unity via `SendMessage`

## What You Need to Do in Unity

### Step 1: GameManager Script (Already Created)

The GameManager.cs script has been created at `/Users/kartik/SolMechs/Assets/Scripts/GameManager.cs`.

**How it works**:
- Receives viewer data from JavaScript via `ReceiveGameData(string jsonData)`
- Maps viewer selections (drone1-drone6) to Drone ScriptableObject assets (D01-D06)
- Assigns the correct Drone assets to your existing `DroneSwarmLoader` component slots

**Key Features**:
- Works with your existing `EnemyDrones` GameObject and `DroneSwarmLoader` component
- No prefabs needed - uses your Drone ScriptableObject assets from `Assets/Data/Drones`
- Directly assigns to `slot0Drone`, `slot1Drone`, `slot2Drone`, `slot3Drone` based on viewer selection

### Step 2: Setup in Unity Scene (Scene 8 - 8_droneSwarm)

**IMPORTANT**: Follow these steps carefully in the Unity Editor:

1. **Create GameManager GameObject**:
   - Open Scene 8 (8_droneSwarm scene)
   - In Hierarchy, right-click → Create Empty
   - Name it: `GameManager`
   - Add the `GameManager.cs` script component to it

2. **Assign DroneSwarmLoader Reference**:
   - Select the `GameManager` GameObject in Hierarchy
   - In Inspector, find the `GameManager` component
   - Find the `Drone Swarm Loader` field
   - In Hierarchy, locate your existing `EnemyDrones` GameObject
   - Drag the `EnemyDrones` GameObject into the `Drone Swarm Loader` field

   **This connects GameManager to your existing drone system!**

3. **Assign ALL 6 Drone Assets**:
   - Still in GameManager Inspector, find the "Drone Assets - All 6 Types" section
   - In Project window, navigate to `Assets/Data/Drones`
   - You should see 6 Drone assets: D01, D02, D03, D04, D05, D06
   - Drag each asset into the corresponding slot:
     - `drone1Asset`: D01 (Assault Drone)
     - `drone2Asset`: D02 (Tank Drone)
     - `drone3Asset`: D03 (Speed Drone)
     - `drone4Asset`: D04 (Artillery Drone)
     - `drone5Asset`: D05 (Stealth Drone)
     - `drone6Asset`: D06 (Berserker Drone)

   **CRITICAL**: Make sure ALL 6 asset slots are filled! If a viewer selects drone5 but the asset is missing, it will default to drone1.

4. **Verify Setup**:
   - Expand `EnemyDrones` in Hierarchy
   - Verify it has the `DroneSwarmLoader` component
   - You should see 4 slots: slot0Drone, slot1Drone, slot2Drone, slot3Drone
   - These will be automatically updated by GameManager when the game starts

### Step 3: Save Your Scene

- Save the scene: File → Save Scene (or Ctrl+S / Cmd+S)
- The GameManager is now ready to receive viewer data from the web!

## Data Flow

```
1. Viewer joins game → Selects drone type (drone1-drone6)
2. Selection saved to backend with viewer data (userId, username, droneId=slot, selectedDroneType)
3. Streamer clicks "LAUNCH GAME"
4. Unity page loads with gameId parameter
5. Next.js fetches all viewers from API
6. JavaScript calls: unityInstance.SendMessage('GameManager', 'ReceiveGameData', jsonData)
7. Unity GameManager.ReceiveGameData() is triggered
8. GameManager.AssignDronesToLoader() assigns the correct Drone assets to DroneSwarmLoader slots
9. DroneSwarmLoader spawns the viewer-selected drones during gameplay
```

## Example Data Sent to Unity

```json
{
  "gameId": "DASANQ",
  "role": "streamer",
  "viewers": [
    {
      "userId": "user_123",
      "username": "PlayerOne",
      "droneId": 1,
      "selectedDroneType": "drone1"
    },
    {
      "userId": "user_456",
      "username": "PlayerTwo",
      "droneId": 2,
      "selectedDroneType": "drone3"
    }
  ]
}
```

## Testing

1. **Start Next.js**: `npm run dev`
2. **Create game** on `/arena` (streamer page)
3. **Join as viewer** on `/arena/viewer` with room code
4. **Select a drone** in the modal
5. **Launch game** from streamer dashboard
6. **Check Unity console** - should see:
   ```
   [GameManager] Received game data: {...}
   [GameManager] Spawned drone1 for PlayerOne at slot 1
   ```

## Drone Prefab Mapping

Update the drone names in `utils/droneData.ts` to match your Unity prefabs:

| ID | Frontend Name | Unity Prefab Path |
|----|---------------|-------------------|
| drone1 | Assault Drone | Assets/Data/Drones/Drone1 |
| drone2 | Tank Drone | Assets/Data/Drones/Drone2 |
| drone3 | Speed Drone | Assets/Data/Drones/Drone3 |
| drone4 | Artillery Drone | Assets/Data/Drones/Drone4 |
| drone5 | Stealth Drone | Assets/Data/Drones/Drone5 |
| drone6 | Berserker Drone | Assets/Data/Drones/Drone6 |

**IMPORTANT**: Replace the placeholder names above with the ACTUAL drone names from your Unity Assets/Data/Drones folder!

## Adding Drone Images

To replace the placeholder emoji with actual drone images:

1. Export PNG images from Unity (512x512 recommended)
2. Save to `/public/images/drones/drone1.png`, `drone2.png`, etc.
3. Images will automatically appear in the selection modal

## Next Steps

After this basic integration works:

1. **Add drone control**: Viewers can control their drones directly
2. **Real-time updates**: Sync drone positions/health with backend
3. **Combat system**: Track drone battles and winner
4. **Rewards**: Distribute rewards to viewer based on drone performance

## Questions?

Check the console logs:
- Browser console: Shows data being sent to Unity
- Unity console: Shows data being received and drones being spawned
