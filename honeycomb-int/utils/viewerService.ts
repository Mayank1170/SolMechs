// Simple viewer tracking service using our own Next.js API routes
// No external backend needed!

const API_BASE = typeof window !== 'undefined' ? window.location.origin : '';

export interface Viewer {
  userId: string;
  username: string;
  joinedAt: string;
  lastActive: string;
  droneId: number | null;
  selectedDroneType?: string; // e.g., "drone1", "drone2", etc.
}

export class ViewerService {
  /**
   * Join a game as a viewer
   */
  async joinViewer(gameId: string, userId: string, username: string, selectedDroneType?: string): Promise<{ success: boolean; data?: Viewer; error?: string }> {
    try {
      const response = await fetch(`${API_BASE}/api/viewers`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          action: 'join',
          gameId,
          userId,
          username,
          selectedDroneType
        })
      });

      const result = await response.json();

      if (!response.ok) {
        return {
          success: false,
          error: result.error || 'Failed to join as viewer'
        };
      }

      console.log('✅ Joined as viewer:', result.data);

      return {
        success: true,
        data: result.data
      };

    } catch (error: any) {
      console.error('❌ Join viewer error:', error);
      return {
        success: false,
        error: error.message || 'Failed to join as viewer'
      };
    }
  }

  /**
   * Get all viewers for a game
   */
  async getViewers(gameId: string): Promise<{ success: boolean; data?: Viewer[]; error?: string }> {
    try {
      const response = await fetch(`${API_BASE}/api/viewers?gameId=${gameId}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json'
        }
      });

      const result = await response.json();

      if (!response.ok) {
        return {
          success: false,
          error: result.error || 'Failed to get viewers'
        };
      }

      return {
        success: true,
        data: result.data?.viewers || []
      };

    } catch (error: any) {
      console.error('❌ Get viewers error:', error);
      return {
        success: false,
        error: error.message || 'Failed to get viewers'
      };
    }
  }

  /**
   * Send heartbeat to keep viewer active
   */
  async sendHeartbeat(gameId: string, userId: string): Promise<{ success: boolean; error?: string }> {
    try {
      const response = await fetch(`${API_BASE}/api/viewers`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          action: 'heartbeat',
          gameId,
          userId
        })
      });

      const result = await response.json();

      if (!response.ok) {
        return {
          success: false,
          error: result.error || 'Heartbeat failed'
        };
      }

      return { success: true };

    } catch (error: any) {
      return {
        success: false,
        error: error.message || 'Heartbeat failed'
      };
    }
  }

  /**
   * Leave a game
   */
  async leaveGame(gameId: string, userId: string): Promise<{ success: boolean; error?: string }> {
    try {
      const response = await fetch(`${API_BASE}/api/viewers?gameId=${gameId}&userId=${userId}`, {
        method: 'DELETE',
        headers: {
          'Content-Type': 'application/json'
        }
      });

      const result = await response.json();

      if (!response.ok) {
        return {
          success: false,
          error: result.error || 'Failed to leave game'
        };
      }

      console.log('✅ Left game successfully');

      return { success: true };

    } catch (error: any) {
      console.error('❌ Leave game error:', error);
      return {
        success: false,
        error: error.message || 'Failed to leave game'
      };
    }
  }
}

// Export singleton instance
export const viewerService = new ViewerService();
