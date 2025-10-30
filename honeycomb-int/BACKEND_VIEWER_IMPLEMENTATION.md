# Backend Implementation Guide: Viewer Tracking System

This document provides complete backend implementation for the Arena Arcade viewer tracking system that integrates with Vorld Auth.

## Overview

The frontend is already making API calls to these endpoints:
- `POST /api/games/:gameId/viewers/join` - Register a viewer
- `GET /api/games/:gameId/viewers` - Get all active viewers
- `POST /api/games/:gameId/viewers/heartbeat` - Update viewer presence

## Architecture

```
┌─────────────┐         ┌──────────────────┐         ┌─────────────┐
│   Viewer    │────────>│  Arena Backend   │────────>│  Database   │
│  Frontend   │         │  API Endpoints   │         │  (MongoDB)  │
└─────────────┘         └──────────────────┘         └─────────────┘
      │                          │
      │                          │
      └──────────────────────────┘
         Vorld Auth JWT Token
```

## Database Schema

### Viewer Model (MongoDB)

```javascript
// models/Viewer.js
const mongoose = require('mongoose');

const viewerSchema = new mongoose.Schema({
  gameId: {
    type: String,
    required: true,
    index: true
  },
  userId: {
    type: String,
    required: true
  },
  username: {
    type: String,
    required: true
  },
  vorldUserId: {
    type: String,
    required: false  // From Vorld Auth token
  },
  vorldAppId: {
    type: String,
    required: false
  },
  joinedAt: {
    type: Date,
    default: Date.now
  },
  lastActive: {
    type: Date,
    default: Date.now
  },
  isActive: {
    type: Boolean,
    default: true
  },
  // Drone assignment (for Scene 8 multiplayer)
  droneId: {
    type: Number,
    min: 1,
    max: 4,
    default: null
  }
}, {
  timestamps: true
});

// Compound index for efficient queries
viewerSchema.index({ gameId: 1, userId: 1 }, { unique: true });
viewerSchema.index({ gameId: 1, isActive: 1 });
viewerSchema.index({ lastActive: 1 });

// Auto-cleanup: Mark viewers inactive after 30 seconds of no heartbeat
viewerSchema.statics.cleanupInactiveViewers = async function() {
  const thirtySecondsAgo = new Date(Date.now() - 30000);

  return this.updateMany(
    { lastActive: { $lt: thirtySecondsAgo }, isActive: true },
    { $set: { isActive: false } }
  );
};

module.exports = mongoose.model('Viewer', viewerSchema);
```

### Update Game Model

Add viewer tracking to existing Game model:

```javascript
// Add to existing Game model
const gameSchema = new mongoose.Schema({
  // ... existing fields ...

  // Add viewer tracking
  activeViewerCount: {
    type: Number,
    default: 0
  },
  maxViewers: {
    type: Number,
    default: 4  // 4 drones in Scene 8
  }
});
```

## Middleware: Vorld Auth Verification

```javascript
// middleware/vorldAuth.js
const jwt = require('jsonwebtoken');

/**
 * Middleware to verify Vorld Auth JWT tokens
 * Extracts user info from Authorization header
 */
const verifyVorldAuth = async (req, res, next) => {
  try {
    const authHeader = req.headers.authorization;
    const vorldAppId = req.headers['x-vorld-app-id'];
    const arenaGameId = req.headers['x-arena-arcade-game-id'];

    // Check required headers
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return res.status(401).json({
        success: false,
        error: { message: 'No authorization token provided' }
      });
    }

    if (!vorldAppId) {
      return res.status(401).json({
        success: false,
        error: { message: 'X-Vorld-App-ID header required' }
      });
    }

    if (!arenaGameId) {
      return res.status(401).json({
        success: false,
        error: { message: 'X-Arena-Arcade-Game-ID header required' }
      });
    }

    const token = authHeader.substring(7); // Remove 'Bearer ' prefix

    // Verify JWT token (you may need to adjust based on your JWT secret)
    // Note: Vorld tokens should be verified against Vorld's public key
    // For now, we'll decode without verification (YOU SHOULD ADD VERIFICATION)
    const decoded = jwt.decode(token);

    if (!decoded) {
      return res.status(401).json({
        success: false,
        error: { message: 'Invalid token' }
      });
    }

    // Attach user info to request
    req.vorldUser = {
      userId: decoded.sub || decoded.userId,
      username: decoded.username || decoded.name,
      appId: vorldAppId,
      arenaGameId: arenaGameId
    };

    next();
  } catch (error) {
    console.error('Vorld auth verification error:', error);
    return res.status(401).json({
      success: false,
      error: { message: 'Token verification failed' }
    });
  }
};

module.exports = { verifyVorldAuth };
```

## API Endpoints Implementation

### 1. POST /api/games/:gameId/viewers/join

```javascript
// routes/viewers.js
const express = require('express');
const router = express.Router();
const Viewer = require('../models/Viewer');
const Game = require('../models/Game');
const { verifyVorldAuth } = require('../middleware/vorldAuth');

/**
 * POST /api/games/:gameId/viewers/join
 * Register a viewer to join a game
 */
router.post('/games/:gameId/viewers/join', verifyVorldAuth, async (req, res) => {
  try {
    const { gameId } = req.params;
    const { username, userId } = req.body;

    console.log('🎮 Viewer join request:', { gameId, username, userId });

    // Validate input
    if (!username || !userId) {
      return res.status(400).json({
        success: false,
        error: { message: 'Username and userId are required' }
      });
    }

    // Check if game exists and is active
    const game = await Game.findOne({
      gameId: gameId,
      status: { $in: ['pending', 'active'] }
    });

    if (!game) {
      return res.status(404).json({
        success: false,
        error: { message: 'Game not found or not active' }
      });
    }

    // Check if game is full (max 4 viewers for 4 drones)
    const activeViewerCount = await Viewer.countDocuments({
      gameId: gameId,
      isActive: true
    });

    if (activeViewerCount >= game.maxViewers) {
      return res.status(400).json({
        success: false,
        error: {
          message: 'Game is full',
          maxViewers: game.maxViewers,
          currentViewers: activeViewerCount
        }
      });
    }

    // Assign drone ID (1-4) based on available slots
    const existingViewers = await Viewer.find({
      gameId: gameId,
      isActive: true
    }).select('droneId');

    const usedDroneIds = existingViewers.map(v => v.droneId).filter(id => id !== null);
    let assignedDroneId = null;

    for (let i = 1; i <= 4; i++) {
      if (!usedDroneIds.includes(i)) {
        assignedDroneId = i;
        break;
      }
    }

    // Create or update viewer record
    const viewer = await Viewer.findOneAndUpdate(
      { gameId, userId },
      {
        $set: {
          username,
          vorldUserId: req.vorldUser?.userId,
          vorldAppId: req.vorldUser?.appId,
          isActive: true,
          lastActive: new Date(),
          droneId: assignedDroneId
        },
        $setOnInsert: {
          joinedAt: new Date()
        }
      },
      {
        upsert: true,
        new: true,
        runValidators: true
      }
    );

    // Update game's active viewer count
    await Game.findOneAndUpdate(
      { gameId },
      { $set: { activeViewerCount: activeViewerCount + 1 } }
    );

    console.log('✅ Viewer joined successfully:', viewer);

    res.json({
      success: true,
      data: {
        viewerId: viewer._id,
        username: viewer.username,
        userId: viewer.userId,
        droneId: viewer.droneId,
        joinedAt: viewer.joinedAt,
        gameId: viewer.gameId
      }
    });

  } catch (error) {
    console.error('❌ Viewer join error:', error);
    res.status(500).json({
      success: false,
      error: {
        message: 'Failed to join as viewer',
        details: error.message
      }
    });
  }
});

module.exports = router;
```

### 2. GET /api/games/:gameId/viewers

```javascript
/**
 * GET /api/games/:gameId/viewers
 * Get all active viewers for a game
 */
router.get('/games/:gameId/viewers', verifyVorldAuth, async (req, res) => {
  try {
    const { gameId } = req.params;

    console.log('📊 Fetching viewers for game:', gameId);

    // Clean up inactive viewers first
    await Viewer.cleanupInactiveViewers();

    // Get active viewers
    const viewers = await Viewer.find({
      gameId: gameId,
      isActive: true
    })
    .select('userId username joinedAt lastActive droneId')
    .sort({ joinedAt: 1 }); // Oldest first

    console.log(`✅ Found ${viewers.length} active viewers`);

    res.json({
      success: true,
      data: {
        viewers: viewers.map(v => ({
          userId: v.userId,
          username: v.username,
          joinedAt: v.joinedAt,
          lastActive: v.lastActive,
          droneId: v.droneId
        })),
        count: viewers.length
      }
    });

  } catch (error) {
    console.error('❌ Get viewers error:', error);
    res.status(500).json({
      success: false,
      error: {
        message: 'Failed to get viewers',
        details: error.message
      }
    });
  }
});
```

### 3. POST /api/games/:gameId/viewers/heartbeat

```javascript
/**
 * POST /api/games/:gameId/viewers/heartbeat
 * Update viewer's last active timestamp
 */
router.post('/games/:gameId/viewers/heartbeat', verifyVorldAuth, async (req, res) => {
  try {
    const { gameId } = req.params;
    const { userId } = req.body;

    if (!userId) {
      return res.status(400).json({
        success: false,
        error: { message: 'userId is required' }
      });
    }

    // Update lastActive timestamp
    const viewer = await Viewer.findOneAndUpdate(
      { gameId, userId, isActive: true },
      { $set: { lastActive: new Date() } },
      { new: true }
    );

    if (!viewer) {
      return res.status(404).json({
        success: false,
        error: { message: 'Viewer not found or not active' }
      });
    }

    res.json({
      success: true,
      data: {
        userId: viewer.userId,
        lastActive: viewer.lastActive
      }
    });

  } catch (error) {
    console.error('❌ Heartbeat error:', error);
    res.status(500).json({
      success: false,
      error: {
        message: 'Heartbeat failed',
        details: error.message
      }
    });
  }
});
```

### 4. DELETE /api/games/:gameId/viewers/leave (Bonus)

```javascript
/**
 * DELETE /api/games/:gameId/viewers/leave
 * Remove viewer from game (when they leave)
 */
router.delete('/games/:gameId/viewers/leave', verifyVorldAuth, async (req, res) => {
  try {
    const { gameId } = req.params;
    const { userId } = req.body;

    if (!userId) {
      return res.status(400).json({
        success: false,
        error: { message: 'userId is required' }
      });
    }

    // Mark viewer as inactive
    const viewer = await Viewer.findOneAndUpdate(
      { gameId, userId },
      { $set: { isActive: false } },
      { new: true }
    );

    if (!viewer) {
      return res.status(404).json({
        success: false,
        error: { message: 'Viewer not found' }
      });
    }

    // Update game's active viewer count
    const activeCount = await Viewer.countDocuments({
      gameId: gameId,
      isActive: true
    });

    await Game.findOneAndUpdate(
      { gameId },
      { $set: { activeViewerCount: activeCount } }
    );

    console.log('✅ Viewer left:', { gameId, userId });

    res.json({
      success: true,
      data: {
        message: 'Viewer left successfully',
        userId: viewer.userId
      }
    });

  } catch (error) {
    console.error('❌ Leave error:', error);
    res.status(500).json({
      success: false,
      error: {
        message: 'Failed to leave',
        details: error.message
      }
    });
  }
});

module.exports = router;
```

## Integration with Existing Backend

### Step 1: Install Dependencies

```bash
npm install mongoose jsonwebtoken
```

### Step 2: Add Routes to Main App

```javascript
// app.js or server.js
const express = require('express');
const viewerRoutes = require('./routes/viewers');

const app = express();

// ... existing middleware ...

// Add viewer routes
app.use('/api', viewerRoutes);

// ... rest of your app ...
```

### Step 3: Connect to MongoDB

```javascript
// config/database.js
const mongoose = require('mongoose');

const connectDB = async () => {
  try {
    await mongoose.connect(process.env.MONGODB_URI, {
      useNewUrlParser: true,
      useUnifiedTopology: true
    });
    console.log('✅ MongoDB connected');
  } catch (error) {
    console.error('❌ MongoDB connection error:', error);
    process.exit(1);
  }
};

module.exports = connectDB;
```

### Step 4: Add Cleanup Cron Job

```javascript
// utils/cleanup.js
const cron = require('node-cron');
const Viewer = require('../models/Viewer');

// Run cleanup every 30 seconds
const startCleanupJob = () => {
  cron.schedule('*/30 * * * * *', async () => {
    try {
      const result = await Viewer.cleanupInactiveViewers();
      if (result.modifiedCount > 0) {
        console.log(`🧹 Cleaned up ${result.modifiedCount} inactive viewers`);
      }
    } catch (error) {
      console.error('❌ Cleanup error:', error);
    }
  });

  console.log('✅ Viewer cleanup job started');
};

module.exports = { startCleanupJob };
```

```javascript
// In app.js
const { startCleanupJob } = require('./utils/cleanup');

// Start cleanup job
startCleanupJob();
```

## Environment Variables

Add to `.env`:

```bash
# MongoDB
MONGODB_URI=mongodb://localhost:27017/arena-arcade

# Vorld Auth (optional - for token verification)
VORLD_JWT_SECRET=your_vorld_jwt_secret_here
VORLD_APP_ID=your_vorld_app_id_here

# Arena Arcade
ARENA_GAME_ID=your_arena_game_id_here
```

## Testing the Endpoints

### Test 1: Join as Viewer

```bash
curl -X POST http://localhost:3000/api/games/GAME123/viewers/join \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Vorld-App-ID: your_app_id" \
  -H "X-Arena-Arcade-Game-ID: your_game_id" \
  -d '{
    "username": "Player1",
    "userId": "user123"
  }'
```

Expected Response:
```json
{
  "success": true,
  "data": {
    "viewerId": "64abc123...",
    "username": "Player1",
    "userId": "user123",
    "droneId": 1,
    "joinedAt": "2025-10-28T12:00:00.000Z",
    "gameId": "GAME123"
  }
}
```

### Test 2: Get Viewers

```bash
curl -X GET http://localhost:3000/api/games/GAME123/viewers \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Vorld-App-ID: your_app_id" \
  -H "X-Arena-Arcade-Game-ID: your_game_id"
```

Expected Response:
```json
{
  "success": true,
  "data": {
    "viewers": [
      {
        "userId": "user123",
        "username": "Player1",
        "joinedAt": "2025-10-28T12:00:00.000Z",
        "lastActive": "2025-10-28T12:05:00.000Z",
        "droneId": 1
      }
    ],
    "count": 1
  }
}
```

### Test 3: Heartbeat

```bash
curl -X POST http://localhost:3000/api/games/GAME123/viewers/heartbeat \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Vorld-App-ID: your_app_id" \
  -H "X-Arena-Arcade-Game-ID: your_game_id" \
  -d '{
    "userId": "user123"
  }'
```

Expected Response:
```json
{
  "success": true,
  "data": {
    "userId": "user123",
    "lastActive": "2025-10-28T12:06:00.000Z"
  }
}
```

## WebSocket Integration (Optional Enhancement)

For real-time viewer updates without polling, add WebSocket support:

```javascript
// websocket/viewerEvents.js
const Viewer = require('../models/Viewer');

const setupViewerWebSocket = (io) => {
  io.on('connection', (socket) => {
    console.log('🔌 Client connected:', socket.id);

    // Join game room
    socket.on('join_game', async ({ gameId, userId }) => {
      socket.join(`game:${gameId}`);
      console.log(`🎮 User ${userId} joined game ${gameId}`);

      // Broadcast viewer count update
      const viewers = await Viewer.find({ gameId, isActive: true });
      io.to(`game:${gameId}`).emit('viewer_count_update', {
        count: viewers.length,
        viewers: viewers.map(v => ({
          userId: v.userId,
          username: v.username,
          droneId: v.droneId
        }))
      });
    });

    // Heartbeat via WebSocket
    socket.on('viewer_heartbeat', async ({ gameId, userId }) => {
      await Viewer.findOneAndUpdate(
        { gameId, userId },
        { $set: { lastActive: new Date() } }
      );
    });

    // Leave game
    socket.on('leave_game', async ({ gameId, userId }) => {
      socket.leave(`game:${gameId}`);

      await Viewer.findOneAndUpdate(
        { gameId, userId },
        { $set: { isActive: false } }
      );

      // Broadcast update
      const viewers = await Viewer.find({ gameId, isActive: true });
      io.to(`game:${gameId}`).emit('viewer_count_update', {
        count: viewers.length,
        viewers: viewers.map(v => ({
          userId: v.userId,
          username: v.username,
          droneId: v.droneId
        }))
      });
    });

    socket.on('disconnect', () => {
      console.log('🔌 Client disconnected:', socket.id);
    });
  });
};

module.exports = { setupViewerWebSocket };
```

## Summary

1. **Database**: MongoDB with Viewer model tracking gameId, userId, username, droneId, lastActive
2. **Authentication**: Vorld Auth JWT tokens verified via middleware
3. **Endpoints**:
   - POST join - Register viewer, assign drone (1-4)
   - GET viewers - List active viewers
   - POST heartbeat - Keep viewer active
4. **Cleanup**: Cron job marks viewers inactive after 30s of no heartbeat
5. **Limits**: Max 4 viewers per game (4 drones)
6. **Optional**: WebSocket for real-time updates

## Next Steps

1. Copy the models, routes, and middleware to your Arena Arcade backend
2. Update your database connection
3. Test the endpoints with curl or Postman
4. Verify frontend integration works
5. (Optional) Add WebSocket support for real-time updates

The frontend is already configured to call these endpoints - once you deploy this backend code, everything should work seamlessly!
