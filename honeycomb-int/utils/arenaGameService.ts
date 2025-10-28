import { io, Socket } from 'socket.io-client';
import axios from 'axios';

const ARENA_SERVER_URL = process.env.NEXT_PUBLIC_ARENA_SERVER_URL || 'wss://airdrop-arcade.onrender.com';
const GAME_API_URL = process.env.NEXT_PUBLIC_GAME_API_URL || 'https://airdrop-arcade.onrender.com/api';
const VORLD_APP_ID = process.env.NEXT_PUBLIC_VORLD_APP_ID || '';
const ARENA_GAME_ID = process.env.NEXT_PUBLIC_ARENA_GAME_ID || '';

export interface GamePlayer {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
}

export interface GameEvent {
  id: string;
  eventName: string;
  isFinal: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface GamePackage {
  id: string;
  name: string;
  image: string;
  stats: Array<{
    name: string;
    currentValue: number;
    maxValue: number;
    description: string;
  }>;
  players: string[];
  type: string;
  cost: number;
  unlockAtPoints: number;
  metadata: {
    id: string;
    type: string;
    quantity: string;
  };
}

export interface EvaGameDetails {
  _id: string;
  gameId: string;
  vorldAppId: string;
  appName: string;
  gameDeveloperId: string;
  arcadeGameId: string;
  isActive: boolean;
  numberOfCycles: number;
  cycleTime: number;
  waitingTime: number;
  players: GamePlayer[];
  events: GameEvent[];
  packages: GamePackage[];
  createdAt: string;
  updatedAt: string;
}

export interface GameState {
  gameId: string;
  expiresAt: string;
  status: 'pending' | 'active' | 'completed' | 'cancelled';
  websocketUrl: string;
  evaGameDetails: EvaGameDetails;
  arenaActive: boolean;
  countdownStarted: boolean;
}

export interface BoostData {
  playerId: string;
  playerName: string;
  currentCyclePoints: number;
  totalPoints: number;
  arenaCoinsSpent: number;
  newArenaCoinsBalance: number;
}

export interface ItemDrop {
  itemId: string;
  itemName: string;
  targetPlayer: string;
  cost: number;
}

export class ArenaGameService {
  private socket: Socket | null = null;
  private gameState: GameState | null = null;
  private userToken: string = '';

  // Set user token (useful for viewers joining existing games)
  setUserToken(token: string): void {
    this.userToken = token;
  }

  // Get active games for current user
  async getActiveGames(): Promise<{ success: boolean; data?: any; error?: string }> {
    try {
      const response = await axios.get(`${GAME_API_URL}/games/active`, {
        headers: {
          'Authorization': `Bearer ${this.userToken}`,
          'X-Arena-Arcade-Game-ID': ARENA_GAME_ID,
          'X-Vorld-App-ID': VORLD_APP_ID
        }
      });

      return {
        success: true,
        data: response.data.data
      };
    } catch (error: any) {
      console.error('Get active games error:', error.response?.data);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to get active games'
      };
    }
  }

  // Initialize game with stream URL
  async initializeGame(streamUrl: string, userToken: string): Promise<{ success: boolean; data?: GameState; error?: string }> {
    try {
      this.userToken = userToken;

      const response = await axios.post(`${GAME_API_URL}/games/init`, {
        streamUrl
      }, {
        headers: {
          'Authorization': `Bearer ${userToken}`,
          'X-Arena-Arcade-Game-ID': ARENA_GAME_ID,
          'X-Vorld-App-ID': VORLD_APP_ID,
          'Content-Type': 'application/json'
        }
      });

      this.gameState = response.data.data;

      // Connect to WebSocket (non-blocking, optional)
      if (this.gameState?.websocketUrl) {
        this.connectWebSocket().catch(err => {
          console.warn('WebSocket connection failed, will continue without real-time updates:', err);
        });
      }

      return {
        success: true,
        data: this.gameState
      };
    } catch (error: any) {
      console.error('Game initialization error:', error.response?.data);
      console.error('Error status:', error.response?.status);

      // Handle error object from server
      const serverError = error.response?.data?.error;
      let errorMessage = 'Failed to initialize game';

      if (typeof serverError === 'string') {
        errorMessage = serverError;
      } else if (typeof serverError === 'object' && serverError !== null) {
        errorMessage = serverError.message || JSON.stringify(serverError);
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      }

      console.error('Error message:', errorMessage);

      return {
        success: false,
        error: errorMessage
      };
    }
  }

  // Connect to WebSocket
  private async connectWebSocket(): Promise<boolean> {
    try {
      if (!this.gameState?.websocketUrl) return false;

      console.log('🔌 Attempting WebSocket connection...');

      // The websocketUrl includes the full path, so we connect to the base URL
      const baseUrl = ARENA_SERVER_URL;
      const path = this.gameState.websocketUrl.replace(baseUrl, '');

      this.socket = io(baseUrl, {
        path: path,
        transports: ['websocket'],
        auth: {
          token: this.userToken,
          appId: VORLD_APP_ID
        },
        reconnection: false, // Disable reconnection attempts to reduce console spam
        timeout: 5000
      });

      this.setupEventListeners();

      return new Promise((resolve) => {
        const timeout = setTimeout(() => {
          console.warn('⚠️ WebSocket connection timeout - continuing without real-time updates');
          resolve(false);
        }, 5000);

        this.socket?.on('connect', () => {
          clearTimeout(timeout);
          console.log('✅ Connected to Arena WebSocket');
          resolve(true);
        });

        this.socket?.on('connect_error', () => {
          clearTimeout(timeout);
          console.warn('⚠️ WebSocket unavailable - continuing without real-time updates');
          resolve(false);
        });

        this.socket?.on('disconnect', (reason) => {
          console.log('WebSocket disconnected:', reason);
        });
      });
    } catch (error) {
      console.warn('⚠️ WebSocket unavailable - continuing without real-time updates');
      return false;
    }
  }

  // Set up WebSocket event listeners
  private setupEventListeners(): void {
    // Arena Events
    this.socket?.on('arena_countdown_started', (data) => {
      this.onArenaCountdownStarted?.(data);
    });

    this.socket?.on('countdown_update', (data) => {
      this.onCountdownUpdate?.(data);
    });

    this.socket?.on('arena_begins', (data) => {
      this.onArenaBegins?.(data);
    });

    // Boost Events
    this.socket?.on('player_boost_activated', (data) => {
      this.onPlayerBoostActivated?.(data);
    });

    this.socket?.on('boost_cycle_update', (data) => {
      this.onBoostCycleUpdate?.(data);
    });

    this.socket?.on('boost_cycle_complete', (data) => {
      this.onBoostCycleComplete?.(data);
    });

    // Package Events
    this.socket?.on('package_drop', (data) => {
      this.onPackageDrop?.(data);
    });

    this.socket?.on('immediate_item_drop', (data) => {
      this.onImmediateItemDrop?.(data);
    });

    // Game Events
    this.socket?.on('event_triggered', (data) => {
      this.onEventTriggered?.(data);
    });

    this.socket?.on('player_joined', (data) => {
      this.onPlayerJoined?.(data);
    });

    this.socket?.on('game_completed', (data) => {
      this.onGameCompleted?.(data);
    });

    this.socket?.on('game_stopped', (data) => {
      this.onGameStopped?.(data);
    });
  }

  // Get game details
  async getGameDetails(gameId: string): Promise<{ success: boolean; data?: GameState; error?: string }> {
    try {
      const response = await axios.get(`${GAME_API_URL}/games/${gameId}`, {
        headers: {
          'Authorization': `Bearer ${this.userToken}`,
          'X-Arena-Arcade-Game-ID': ARENA_GAME_ID,
          'X-Vorld-App-ID': VORLD_APP_ID
        }
      });

      console.log('Raw game details response:', response.data);

      // Handle nested game object
      const gameData = response.data.data?.game || response.data.data || response.data.game;

      return {
        success: true,
        data: gameData
      };
    } catch (error: any) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to get game details'
      };
    }
  }

  // Boost a player
  async boostPlayer(gameId: string, playerId: string, amount: number, username: string): Promise<{ success: boolean; data?: BoostData; error?: string }> {
    try {
      const response = await axios.post(`${GAME_API_URL}/games/boost/player/${gameId}/${playerId}`, {
        amount,
        username
      }, {
        headers: {
          'Authorization': `Bearer ${this.userToken}`,
          'X-Arena-Arcade-Game-ID': ARENA_GAME_ID,
          'X-Vorld-App-ID': VORLD_APP_ID,
          'Content-Type': 'application/json'
        }
      });

      return {
        success: true,
        data: response.data.data
      };
    } catch (error: any) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to boost player'
      };
    }
  }

  // Update stream URL
  async updateStreamUrl(gameId: string, streamUrl: string, oldStreamUrl: string): Promise<{ success: boolean; data?: any; error?: string }> {
    try {
      const response = await axios.put(`${GAME_API_URL}/games/${gameId}/stream-url`, {
        streamUrl,
        oldStreamUrl
      }, {
        headers: {
          'Authorization': `Bearer ${this.userToken}`,
          'X-Arena-Arcade-Game-ID': ARENA_GAME_ID,
          'X-Vorld-App-ID': VORLD_APP_ID,
          'Content-Type': 'application/json'
        }
      });

      return {
        success: true,
        data: response.data.data
      };
    } catch (error: any) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update stream URL'
      };
    }
  }

  // Get items catalog
  async getItemsCatalog(): Promise<{ success: boolean; data?: any; error?: string }> {
    try {
      const response = await axios.get(`${GAME_API_URL}/items/catalog`, {
        headers: {
          'Authorization': `Bearer ${this.userToken}`,
          'X-Arena-Arcade-Game-ID': ARENA_GAME_ID,
          'X-Vorld-App-ID': VORLD_APP_ID
        }
      });

      return {
        success: true,
        data: response.data.data
      };
    } catch (error: any) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to get items catalog'
      };
    }
  }

  // Drop immediate item
  async dropImmediateItem(gameId: string, itemId: string, targetPlayer: string): Promise<{ success: boolean; data?: ItemDrop; error?: string }> {
    try {
      const response = await axios.post(`${GAME_API_URL}/items/drop/${gameId}`, {
        itemId,
        targetPlayer
      }, {
        headers: {
          'Authorization': `Bearer ${this.userToken}`,
          'X-Arena-Arcade-Game-ID': ARENA_GAME_ID,
          'X-Vorld-App-ID': VORLD_APP_ID,
          'Content-Type': 'application/json'
        }
      });

      return {
        success: true,
        data: response.data.data
      };
    } catch (error: any) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to drop item'
      };
    }
  }

  // Event handlers (to be set by components)
  onArenaCountdownStarted?: (data: any) => void;
  onCountdownUpdate?: (data: any) => void;
  onArenaBegins?: (data: any) => void;
  onPlayerBoostActivated?: (data: any) => void;
  onBoostCycleUpdate?: (data: any) => void;
  onBoostCycleComplete?: (data: any) => void;
  onPackageDrop?: (data: any) => void;
  onImmediateItemDrop?: (data: any) => void;
  onEventTriggered?: (data: any) => void;
  onPlayerJoined?: (data: any) => void;
  onGameCompleted?: (data: any) => void;
  onGameStopped?: (data: any) => void;

  // Disconnect from WebSocket
  disconnect(): void {
    this.socket?.disconnect();
    this.socket = null;
    this.gameState = null;
  }

  // Get current game state
  getGameState(): GameState | null {
    return this.gameState;
  }
}
