import { NextRequest, NextResponse } from 'next/server';

// In-memory storage for viewers (resets on server restart)
// For production, replace with Redis or database
const viewersStore = new Map<string, Array<{
  userId: string;
  username: string;
  joinedAt: string;
  lastActive: string;
  droneId: number | null;
  selectedDroneType?: string; // Drone type selected by viewer
}>>();

// Cleanup inactive viewers (no heartbeat for 60 seconds)
// Increased from 30s to 60s to be more tolerant
function cleanupInactiveViewers(gameId: string) {
  const viewers = viewersStore.get(gameId) || [];
  const sixtySecondsAgo = Date.now() - 60000;

  const activeViewers = viewers.filter(v => {
    const lastActive = new Date(v.lastActive).getTime();
    return lastActive > sixtySecondsAgo;
  });

  if (activeViewers.length !== viewers.length) {
    viewersStore.set(gameId, activeViewers);
    console.log(`🧹 Cleaned up ${viewers.length - activeViewers.length} inactive viewers from ${gameId}`);
  }

  return activeViewers;
}

// GET /api/viewers?gameId=XXX - Get all viewers for a game
export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const gameId = searchParams.get('gameId');

    if (!gameId) {
      return NextResponse.json({
        success: false,
        error: 'gameId is required'
      }, { status: 400 });
    }

    // Clean up inactive viewers
    const viewers = cleanupInactiveViewers(gameId);

    console.log(`📊 GET viewers for ${gameId}: ${viewers.length} active`);

    return NextResponse.json({
      success: true,
      data: {
        viewers,
        count: viewers.length
      }
    });

  } catch (error: any) {
    console.error('❌ GET viewers error:', error);
    return NextResponse.json({
      success: false,
      error: error.message
    }, { status: 500 });
  }
}

// POST /api/viewers - Join as viewer or send heartbeat
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { action, gameId, userId, username, selectedDroneType } = body;

    if (!gameId || !userId) {
      return NextResponse.json({
        success: false,
        error: 'gameId and userId are required'
      }, { status: 400 });
    }

    // Clean up inactive viewers first
    cleanupInactiveViewers(gameId);

    const viewers = viewersStore.get(gameId) || [];

    if (action === 'join') {
      // Join as viewer
      if (!username) {
        return NextResponse.json({
          success: false,
          error: 'username is required for join'
        }, { status: 400 });
      }

      // Check if viewer already exists
      const existingIndex = viewers.findIndex(v => v.userId === userId);

      if (existingIndex >= 0) {
        // Update existing viewer
        viewers[existingIndex].username = username;
        viewers[existingIndex].lastActive = new Date().toISOString();
        if (selectedDroneType) {
          viewers[existingIndex].selectedDroneType = selectedDroneType;
        }
        console.log(`🔄 Updated existing viewer: ${username} in ${gameId}`);
      } else {
        // Check max viewers (4 drones)
        if (viewers.length >= 4) {
          return NextResponse.json({
            success: false,
            error: 'Game is full (max 4 viewers)'
          }, { status: 400 });
        }

        // Assign drone ID (1-4)
        const usedDroneIds = viewers.map(v => v.droneId).filter(id => id !== null);
        let droneId = null;
        for (let i = 1; i <= 4; i++) {
          if (!usedDroneIds.includes(i)) {
            droneId = i;
            break;
          }
        }

        // Add new viewer
        const newViewer = {
          userId,
          username,
          joinedAt: new Date().toISOString(),
          lastActive: new Date().toISOString(),
          droneId,
          selectedDroneType: selectedDroneType || undefined
        };

        viewers.push(newViewer);
        console.log(`✅ New viewer joined: ${username} (Drone ${droneId}, Type: ${selectedDroneType || 'default'}) in ${gameId}`);
      }

      viewersStore.set(gameId, viewers);

      const viewer = viewers.find(v => v.userId === userId);
      return NextResponse.json({
        success: true,
        data: viewer
      });

    } else if (action === 'heartbeat') {
      // Update heartbeat
      const viewer = viewers.find(v => v.userId === userId);

      if (!viewer) {
        return NextResponse.json({
          success: false,
          error: 'Viewer not found'
        }, { status: 404 });
      }

      viewer.lastActive = new Date().toISOString();
      viewersStore.set(gameId, viewers);

      return NextResponse.json({
        success: true,
        data: {
          userId: viewer.userId,
          lastActive: viewer.lastActive
        }
      });

    } else if (action === 'leave') {
      // Remove viewer
      const filteredViewers = viewers.filter(v => v.userId !== userId);
      viewersStore.set(gameId, filteredViewers);

      console.log(`👋 Viewer left: ${userId} from ${gameId}`);

      return NextResponse.json({
        success: true,
        data: { message: 'Viewer left successfully' }
      });

    } else {
      return NextResponse.json({
        success: false,
        error: 'Invalid action. Use: join, heartbeat, or leave'
      }, { status: 400 });
    }

  } catch (error: any) {
    console.error('❌ POST viewers error:', error);
    return NextResponse.json({
      success: false,
      error: error.message
    }, { status: 500 });
  }
}

// DELETE /api/viewers?gameId=XXX&userId=YYY - Leave game
export async function DELETE(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const gameId = searchParams.get('gameId');
    const userId = searchParams.get('userId');

    if (!gameId || !userId) {
      return NextResponse.json({
        success: false,
        error: 'gameId and userId are required'
      }, { status: 400 });
    }

    const viewers = viewersStore.get(gameId) || [];
    const filteredViewers = viewers.filter(v => v.userId !== userId);
    viewersStore.set(gameId, filteredViewers);

    console.log(`👋 Viewer left: ${userId} from ${gameId}`);

    return NextResponse.json({
      success: true,
      data: { message: 'Viewer left successfully' }
    });

  } catch (error: any) {
    console.error('❌ DELETE viewers error:', error);
    return NextResponse.json({
      success: false,
      error: error.message
    }, { status: 500 });
  }
}
