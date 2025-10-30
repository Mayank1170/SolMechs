# Backend Implementation Checklist

## Quick Start Guide

Your frontend is **already complete and working**. You just need to implement 3 backend endpoints on the Arena Arcade backend server.

## Current Status

✅ **Frontend Complete**:
- Streamer dashboard polls for viewers every 5 seconds
- Viewer portal calls joinAsViewer on mount
- Heartbeat system keeps viewers alive
- Fallback shows Demo1 player until backend is ready

❌ **Backend Missing**:
- 404 error: `GET /api/games/DASANQ/viewers`
- Backend endpoints don't exist yet on `https://airdrop-arcade.onrender.com`

## What You Need to Do

### Step 1: Add These 3 Endpoints to Arena Arcade Backend

Copy the code from `BACKEND_VIEWER_IMPLEMENTATION.md` and add:

1. **POST /api/games/:gameId/viewers/join**
   - Registers a viewer
   - Assigns drone ID (1-4)
   - Returns viewer data

2. **GET /api/games/:gameId/viewers**
   - Returns list of active viewers
   - Frontend calls this every 5 seconds

3. **POST /api/games/:gameId/viewers/heartbeat**
   - Updates viewer's lastActive timestamp
   - Frontend calls this every 10 seconds

### Step 2: Add Database Model

```javascript
// models/Viewer.js
const mongoose = require('mongoose');

const viewerSchema = new mongoose.Schema({
  gameId: { type: String, required: true, index: true },
  userId: { type: String, required: true },
  username: { type: String, required: true },
  vorldUserId: { type: String },
  joinedAt: { type: Date, default: Date.now },
  lastActive: { type: Date, default: Date.now },
  isActive: { type: Boolean, default: true },
  droneId: { type: Number, min: 1, max: 4 }
});

viewerSchema.index({ gameId: 1, userId: 1 }, { unique: true });

module.exports = mongoose.model('Viewer', viewerSchema);
```

### Step 3: Add Auth Middleware

```javascript
// middleware/vorldAuth.js
const verifyVorldAuth = async (req, res, next) => {
  const authHeader = req.headers.authorization;
  const vorldAppId = req.headers['x-vorld-app-id'];
  const arenaGameId = req.headers['x-arena-arcade-game-id'];

  if (!authHeader || !vorldAppId || !arenaGameId) {
    return res.status(401).json({
      success: false,
      error: { message: 'Missing required headers' }
    });
  }

  // Verify token and attach user to req.vorldUser
  next();
};

module.exports = { verifyVorldAuth };
```

### Step 4: Register Routes

```javascript
// In your main app.js or server.js
const viewerRoutes = require('./routes/viewers');
app.use('/api', viewerRoutes);
```

### Step 5: Test It

Once deployed, your frontend will automatically start working because it's already calling these endpoints!

**Test manually:**
```bash
# Test join
curl -X POST https://airdrop-arcade.onrender.com/api/games/DASANQ/viewers/join \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "X-Vorld-App-ID: your_app_id" \
  -H "X-Arena-Arcade-Game-ID: your_game_id" \
  -d '{"username": "TestUser", "userId": "test123"}'

# Test get viewers
curl https://airdrop-arcade.onrender.com/api/games/DASANQ/viewers \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "X-Vorld-App-ID: your_app_id" \
  -H "X-Arena-Arcade-Game-ID: your_game_id"
```

## Files to Add to Arena Arcade Backend

```
arena-arcade-backend/
├── models/
│   └── Viewer.js          # New viewer model
├── middleware/
│   └── vorldAuth.js       # Auth verification
├── routes/
│   └── viewers.js         # 3 new endpoints
└── app.js                 # Register routes
```

## Expected Behavior After Implementation

1. **Streamer creates game** → Gets room code
2. **Viewer joins with code** → POST /viewers/join → Assigned drone 1
3. **Streamer dashboard** → GET /viewers → Shows "TestUser (Drone 1)"
4. **Viewer heartbeat** → POST /viewers/heartbeat every 10s
5. **After 30s no heartbeat** → Cleanup marks viewer inactive
6. **Streamer sees real-time** → Viewer count updates every 5s

## Current Frontend Behavior (No Backend)

- ✅ Streamer can create games
- ✅ Viewer can "join" (frontend state only)
- ⚠️ Shows "Demo1" fallback player
- ⚠️ Viewer list not persisted
- ⚠️ Can't see viewers across devices

## After Backend Implementation

- ✅ Real viewer tracking
- ✅ Cross-device visibility
- ✅ Drone assignment (1-4)
- ✅ Auto-cleanup of inactive viewers
- ✅ Real-time viewer count
- ✅ Viewer names displayed

## Time Estimate

- **Adding endpoints**: 30-60 minutes
- **Testing**: 15 minutes
- **Deployment**: Depends on your CI/CD

## Questions?

All the code you need is in `BACKEND_VIEWER_IMPLEMENTATION.md` - just copy it to your Arena Arcade backend and deploy!

The frontend is already done and waiting for the backend! 🚀
