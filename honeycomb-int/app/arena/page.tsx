'use client'

import React, { useState, useEffect } from "react";
import { useVorldAuth } from "../providers";
import { ArenaGameService, GameState, GamePlayer } from "../../utils/arenaGameService";
import { viewerService, Viewer } from "../../utils/viewerService";
import { getDroneById } from "../../utils/droneData";
import Link from "next/link";

export default function ArenaPage() {
  const { user, authService } = useVorldAuth();
  const [arenaService] = useState(() => new ArenaGameService());

  const [streamUrl, setStreamUrl] = useState('');
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [isInitializing, setIsInitializing] = useState(false);
  const [error, setError] = useState('');

  // Boost system
  const [selectedPlayer, setSelectedPlayer] = useState('');
  const [boostAmount, setBoostAmount] = useState(50);
  const [boosterUsername, setBoosterUsername] = useState(user?.username || '');
  const [isBoosting, setIsBoosting] = useState(false);

  // Event monitoring
  const [events, setEvents] = useState<any[]>([]);
  const [countdown, setCountdown] = useState<number | null>(null);
  const [arenaActive, setArenaActive] = useState(false);
  const [boostHistory, setBoostHistory] = useState<any[]>([]);

  // Viewer tracking
  const [connectedViewers, setConnectedViewers] = useState<Viewer[]>([]);

  // Load game state from localStorage on mount
  useEffect(() => {
    const savedGameState = localStorage.getItem('arena_game_state');
    if (savedGameState) {
      try {
        const parsed = JSON.parse(savedGameState);
        setGameState(parsed);
        console.log('📦 Restored game state from localStorage:', parsed);
      } catch (err) {
        console.error('Failed to parse saved game state:', err);
        localStorage.removeItem('arena_game_state');
      }
    }
  }, []);

  // Save game state to localStorage whenever it changes
  useEffect(() => {
    if (gameState) {
      localStorage.setItem('arena_game_state', JSON.stringify(gameState));
      console.log('💾 Saved game state to localStorage');
    } else {
      localStorage.removeItem('arena_game_state');
    }
  }, [gameState]);

  // Poll for connected viewers from our own backend
  useEffect(() => {
    if (!gameState) return;

    const checkViewers = async () => {
      const result = await viewerService.getViewers(gameState.gameId);

      if (result.success && result.data) {
        console.log('✅ Fetched viewers:', result.data);
        setConnectedViewers(result.data);
      } else {
        console.warn('⚠️ Get viewers error:', result.error);
        setConnectedViewers([]);
      }
    };

    // Check immediately
    checkViewers();

    // Then check every 5 seconds
    const interval = setInterval(checkViewers, 5000);

    return () => clearInterval(interval);
  }, [gameState]);

  // Set up WebSocket event listeners
  useEffect(() => {
    if (!arenaService) return;

    arenaService.onArenaCountdownStarted = (data) => {
      console.log('Arena countdown started:', data);
      addEvent('arena_countdown_started', data);
    };

    arenaService.onCountdownUpdate = (data) => {
      setCountdown(data.secondsRemaining);
      addEvent('countdown_update', data);
    };

    arenaService.onArenaBegins = (data) => {
      console.log('Arena begins:', data);
      setArenaActive(true);
      addEvent('arena_begins', data);
    };

    arenaService.onPlayerBoostActivated = (data) => {
      console.log('Player boost activated:', data);
      setBoostHistory(prev => [data, ...prev.slice(0, 9)]);
      addEvent('player_boost_activated', data);
    };

    arenaService.onBoostCycleComplete = (data) => {
      console.log('Boost cycle complete:', data);
      addEvent('boost_cycle_complete', data);
    };

    arenaService.onPackageDrop = (data) => {
      console.log('Package drop:', data);
      addEvent('package_drop', data);
    };

    arenaService.onImmediateItemDrop = (data) => {
      console.log('Immediate item drop:', data);
      addEvent('immediate_item_drop', data);
    };

    arenaService.onEventTriggered = (data) => {
      console.log('Event triggered:', data);
      addEvent('event_triggered', data);
    };

    arenaService.onPlayerJoined = (data) => {
      console.log('Player joined:', data);
      addEvent('player_joined', data);
      // Refresh game details to update player count
      if (gameState) {
        handleRefreshGameDetails();
      }
    };

    arenaService.onGameCompleted = (data) => {
      console.log('Game completed:', data);
      addEvent('game_completed', data);
    };

    return () => {
      arenaService.disconnect();
    };
  }, [arenaService, gameState]);

  const addEvent = (type: string, data: any) => {
    setEvents(prev => [{
      type,
      data,
      timestamp: new Date()
    }, ...prev.slice(0, 19)]);
  };

  const handleInitializeGame = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsInitializing(true);

    try {
      const token = authService.getAccessToken();
      if (!token) {
        setError('Not authenticated. Please login again.');
        setIsInitializing(false);
        return;
      }

      // Basic Twitch URL validation
      if (!streamUrl || !streamUrl.includes('twitch.tv')) {
        setError('Please enter a valid Twitch stream URL (e.g., https://www.twitch.tv/username)');
        setIsInitializing(false);
        return;
      }

      const result = await arenaService.initializeGame(streamUrl, token);

      if (result.success && result.data) {
        setGameState(result.data);
        console.log('🎮 Game initialized:', result.data);
        console.log('📊 Initial players from API:', result.data.evaGameDetails?.players);
      } else {
        setError(result.error || 'Failed to initialize game');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to initialize game');
    } finally {
      setIsInitializing(false);
    }
  };

  const handleRefreshGameDetails = async () => {
    if (!gameState) return;

    try {
      const token = authService.getAccessToken();
      if (!token) {
        console.error('No token available');
        return;
      }

      // Ensure token is set in arena service
      arenaService.setUserToken(token);

      const result = await arenaService.getGameDetails(gameState.gameId);
      console.log('🔄 Full refresh result:', JSON.stringify(result, null, 2));
      if (result.success && result.data) {
        console.log('✅ Refreshed game details:', result.data);
        console.log('📊 evaGameDetails:', result.data.evaGameDetails);
        console.log('📊 Players array:', result.data.evaGameDetails?.players);
        setGameState(result.data);
      } else {
        console.error('Failed to refresh:', result.error);
      }
    } catch (err: any) {
      console.error('Refresh error:', err);
    }
  };

  const handleClearGame = () => {
    if (confirm('Are you sure you want to clear the current game? This will allow you to create a new room.')) {
      setGameState(null);
      setConnectedViewers([]);
      setBoostHistory([]);
      setEvents([]);
      setArenaActive(false);
      setCountdown(null);
      localStorage.removeItem('arena_game_state');
      console.log('🗑️ Game state cleared');
    }
  };

  const handleBoostPlayer = async () => {
    if (!gameState || !selectedPlayer) return;

    setIsBoosting(true);
    try {
      const result = await arenaService.boostPlayer(
        gameState.gameId,
        selectedPlayer,
        boostAmount,
        boosterUsername
      );

      if (result.success) {
        console.log('Boost successful:', result.data);
      } else {
        alert('Boost failed: ' + result.error);
      }
    } catch (err: any) {
      alert('Boost failed: ' + err.message);
    } finally {
      setIsBoosting(false);
    }
  };

  const getEventColor = (eventType: string) => {
    const colors: Record<string, string> = {
      'arena_countdown_started': 'text-blue-400',
      'countdown_update': 'text-blue-300',
      'arena_begins': 'text-green-400',
      'player_boost_activated': 'text-purple-400',
      'boost_cycle_complete': 'text-yellow-400',
      'package_drop': 'text-cyan-400',
      'immediate_item_drop': 'text-pink-400',
      'event_triggered': 'text-orange-400',
      'game_completed': 'text-red-400',
    };
    return colors[eventType] || 'text-gray-300';
  };

  if (!user) {
    return (
      <main className="flex min-h-screen flex-col items-center justify-center p-8">
        <div className="text-center">
          <h1 className="text-3xl font-bold text-white mb-4">Arena Arcade</h1>
          <p className="text-gray-300 mb-6">Please login to access Arena Arcade</p>
          <Link href="/" className="px-6 py-3 bg-cyan-600 hover:bg-cyan-700 text-white rounded-lg">
            Go to Login
          </Link>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen p-8 bg-gray-900">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-4xl font-bold text-purple-400 mb-2 font-mek">ARENA ARCADE</h1>
            <p className="text-gray-400">Real-time streaming integration & viewer interaction</p>
          </div>
          <Link href="/" className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white rounded-lg">
            ← Back to Menu
          </Link>
        </div>

        {!gameState ? (
          /* Game Initialization Form */
          <div className="max-w-2xl mx-auto">
            <div className="p-8 bg-gray-800 rounded-lg border-2 border-purple-500/30">
              <h2 className="text-2xl font-bold text-purple-300 mb-6">Initialize Game Session</h2>

              {error && (
                <div className="mb-4 p-3 bg-red-900/50 border border-red-500 rounded text-red-200 text-sm">
                  {error}
                </div>
              )}

              <form onSubmit={handleInitializeGame} className="space-y-4">
                <div>
                  <label className="block text-gray-300 mb-2">Stream URL</label>
                  <input
                    type="url"
                    value={streamUrl}
                    onChange={(e) => setStreamUrl(e.target.value)}
                    className="w-full p-3 bg-gray-900 border border-purple-500/30 text-white rounded focus:border-purple-400 focus:outline-none"
                    placeholder="https://twitch.tv/your_channel"
                    required
                    disabled={isInitializing}
                  />
                  <p className="text-gray-500 text-sm mt-1">
                    Enter your Twitch stream URL (e.g., https://twitch.tv/your_channel)
                  </p>
                  <div className="mt-2 p-3 bg-yellow-900/20 border border-yellow-600/30 rounded">
                    <p className="text-yellow-400 text-sm font-semibold">⚠️ Important:</p>
                    <p className="text-yellow-300/80 text-xs mt-1">
                      Make sure your stream is LIVE before creating the game. Viewers will be watching your stream during gameplay.
                    </p>
                  </div>
                </div>

                <button
                  type="submit"
                  disabled={isInitializing}
                  className="w-full py-3 bg-purple-600 hover:bg-purple-700 disabled:bg-gray-600 text-white font-bold rounded transition-colors"
                >
                  {isInitializing ? 'INITIALIZING...' : 'START ARENA SESSION'}
                </button>
              </form>

              <div className="mt-6 p-4 bg-gray-900/50 rounded">
                <h3 className="text-sm font-semibold text-purple-300 mb-2">What happens next?</h3>
                <ul className="text-sm text-gray-400 space-y-1">
                  <li>• Game session will be created with unique ID</li>
                  <li>• 60-second countdown before arena activation</li>
                  <li>• Viewers can boost players and drop items</li>
                  <li>• Real-time events streamed via WebSocket</li>
                </ul>
              </div>
            </div>
          </div>
        ) : (
          /* Active Game Dashboard */
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Left Column - Game Info & Boost */}
            <div className="space-y-6">
              {/* Game Status Card */}
              <div className="p-6 bg-gray-800 rounded-lg border border-purple-500/30">
                <h3 className="text-xl font-bold text-purple-300 mb-4">Game Status</h3>
                <div className="space-y-3">
                  <div>
                    <p className="text-gray-400 text-sm">Game ID</p>
                    <p className="text-white font-mono text-sm">{gameState.gameId}</p>
                  </div>
                  <div>
                    <p className="text-gray-400 text-sm">Status</p>
                    <p className="text-white capitalize">{gameState.status}</p>
                  </div>
                  <div>
                    <p className="text-gray-400 text-sm">Arena Active</p>
                    <p className={gameState.arenaActive ? 'text-green-400' : 'text-red-400'}>
                      {gameState.arenaActive ? 'LIVE' : 'Waiting'}
                    </p>
                  </div>
                  {/* {gameState.currentBoostCycle && (
                    <div>
                      <p className="text-gray-400 text-sm">Boost Cycle</p>
                      <p className="text-cyan-400 text-xl font-bold">#{gameState.currentBoostCycle}</p>
                    </div>
                  )}
                  {gameState.currentObjective && (
                    <div>
                      <p className="text-gray-400 text-sm">Objective</p>
                      <p className="text-white text-sm">{gameState.currentObjective}</p>
                    </div>
                  )} */}
                  {countdown !== null && (
                    <div>
                      <p className="text-gray-400 text-sm">Countdown</p>
                      <p className="text-cyan-400 text-2xl font-bold">{countdown}s</p>
                    </div>
                  )}
                  <button
                    onClick={handleRefreshGameDetails}
                    className="w-full py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm rounded mt-3"
                  >
                    🔄 Refresh Game Details
                  </button>

                  <button
                    onClick={handleClearGame}
                    className="w-full py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded mt-3"
                  >
                    🗑️ Clear Game & Create New Room
                  </button>

                  {/* Launch Game Button */}
                  {connectedViewers.length > 0 ? (
                    <a
                      href={`/unity?gameId=${gameState.gameId}&scene=8&role=streamer`}
                      className="block w-full py-3 bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 text-white font-bold text-center rounded mt-3 transition-all transform hover:scale-105"
                    >
                      🎮 LAUNCH GAME (STREAMER)
                    </a>
                  ) : (
                    <button
                      disabled
                      className="w-full py-3 bg-gray-600 text-gray-400 font-bold text-center rounded mt-3 cursor-not-allowed"
                    >
                      🔒 WAITING FOR VIEWERS
                    </button>
                  )}
                  <p className="text-gray-400 text-xs text-center mt-2">
                    {connectedViewers.length > 0
                      ? 'Launch Unity as the Mech pilot'
                      : 'At least 1 viewer required to start'
                    }
                  </p>
                </div>
              </div>

            </div>
            <div className="p-6 bg-gray-800 rounded-lg border border-cyan-500/30">
              <h3 className="text-xl font-bold text-cyan-400 mb-4">
                Connected Viewers ({connectedViewers.length})
              </h3>
              <div className="space-y-3">
                {/* Room Code */}
                <div className="p-3 bg-cyan-900/20 rounded-lg border border-cyan-500/30">
                  <p className="text-cyan-300 font-semibold text-xs mb-1">📋 Room Code:</p>
                  <p className="text-white font-mono text-xl text-center bg-gray-900 py-2 rounded">
                    {gameState.gameId}
                  </p>
                </div>

                {/* Viewer List */}
                <div className="space-y-2">
                  {connectedViewers.length > 0 ? (
                    connectedViewers.map((viewer, index) => (
                      <div
                        key={viewer.userId}
                        className="p-3 bg-gray-700/50 rounded-lg border border-cyan-500/20 flex items-center justify-between"
                      >
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 bg-cyan-600 rounded-full flex items-center justify-center text-white font-bold">
                            {index + 1}
                          </div>
                          <div>
                            <p className="text-white font-semibold">{viewer.username}</p>
                            <p className="text-gray-400 text-xs">
                              {viewer.selectedDroneType
                                ? `🚁 ${getDroneById(viewer.selectedDroneType)?.name || 'Drone'}`
                                : `Joined ${new Date(viewer.joinedAt).toLocaleTimeString()}`
                              }
                            </p>
                          </div>
                        </div>
                        <div className="text-green-400 text-sm font-bold">
                          🟢 ONLINE
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="text-center py-4">
                      <p className="text-gray-400 text-sm mb-2">No viewers connected yet</p>
                      <p className="text-gray-500 text-xs">Share the room code above!</p>
                    </div>
                  )}
                </div>

                {/* Instructions */}
                <details className="mt-3">
                  <summary className="text-cyan-400 text-sm cursor-pointer hover:text-cyan-300">
                    How viewers join →
                  </summary>
                  <div className="mt-2 text-gray-300 text-xs space-y-1 pl-4">
                    <p>1. Go to <span className="text-cyan-400 font-mono">/arena/viewer</span></p>
                    <p>2. Enter the room code</p>
                    <p>3. Click &quot;JOIN GAME&quot;</p>
                    <p>4. They&apos;ll stay on viewer portal to watch</p>
                  </div>
                </details>
              </div>
            </div>
          </div>
        )}
      </div>
    </main>
  );
}
