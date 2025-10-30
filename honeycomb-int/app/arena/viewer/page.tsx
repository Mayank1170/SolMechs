'use client'

import React, { useState, useEffect } from "react";
import { useVorldAuth } from "../../providers";
import { ArenaGameService, GameState, GamePackage } from "../../../utils/arenaGameService";
import { viewerService } from "../../../utils/viewerService";
import DroneSelectionModal from "../../../components/DroneSelectionModal";
import { getDroneById } from "../../../utils/droneData";
import Link from "next/link";

export default function ViewerPortalPage() {
  const { user, authService } = useVorldAuth();
  const [arenaService] = useState(() => new ArenaGameService());

  const [gameId, setGameId] = useState('');
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [isJoining, setIsJoining] = useState(false);
  const [error, setError] = useState('');
  const [viewerUserId, setViewerUserId] = useState<string>(''); // Store the userId we used to join

  // Drone selection
  const [showDroneModal, setShowDroneModal] = useState(false);
  const [selectedDroneType, setSelectedDroneType] = useState<string | null>(null);

  // Boost system
  const [selectedPlayer, setSelectedPlayer] = useState('');
  const [boostAmount, setBoostAmount] = useState(50);
  const [isBoosting, setIsBoosting] = useState(false);

  // Item system
  const [itemsCatalog, setItemsCatalog] = useState<any[]>([]);
  const [selectedItem, setSelectedItem] = useState('');
  const [targetPlayer, setTargetPlayer] = useState('');
  const [isDroppingItem, setIsDroppingItem] = useState(false);

  // Event monitoring
  const [events, setEvents] = useState<any[]>([]);
  const [countdown, setCountdown] = useState<number | null>(null);
  const [arenaActive, setArenaActive] = useState(false);
  const [boostHistory, setBoostHistory] = useState<any[]>([]);

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

    arenaService.onGameCompleted = (data) => {
      console.log('Game completed:', data);
      addEvent('game_completed', data);
    };

    return () => {
      arenaService.disconnect();
    };
  }, [arenaService]);

  // Heartbeat to keep viewer active and check if game is still active
  useEffect(() => {
    if (!gameState || !viewerUserId) return;

    const heartbeat = setInterval(async () => {
      // Send heartbeat
      const result = await viewerService.sendHeartbeat(gameState.gameId, viewerUserId);
      if (!result.success) {
        console.warn('⚠️ Heartbeat failed:', result.error);
      } else {
        console.log('💓 Heartbeat sent successfully');
      }

      // Check if game is still active (only kick out if truly ended)
      const gameCheck = await arenaService.getGameDetails(gameState.gameId);

      console.log('🔍 Game check result:', {
        success: gameCheck.success,
        status: gameCheck.data?.status,
        hasData: !!gameCheck.data
      });

      // Only disconnect if game is completed/cancelled, not just status changes
      if (gameCheck.data?.status === 'completed' || gameCheck.data?.status === 'cancelled') {
        console.log('🚪 Game has ended');
        setError('This game has been completed or cancelled by the streamer.');
        setGameState(null);
        setViewerUserId('');
        clearInterval(heartbeat);
      } else if (!gameCheck.success) {
        // Only show warning, don't disconnect yet - could be temporary network issue
        console.warn('⚠️ Could not check game status, will retry next heartbeat');
      } else {
        console.log('✅ Game is still active, status:', gameCheck.data?.status);
      }
    }, 10000); // Update every 10 seconds

    return () => clearInterval(heartbeat);
  }, [gameState, viewerUserId, arenaService]);

  const addEvent = (type: string, data: any) => {
    setEvents(prev => [{
      type,
      data,
      timestamp: new Date()
    }, ...prev.slice(0, 19)]);
  };

  const handleJoinGame = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // Show drone selection modal first
    setShowDroneModal(true);
  };

  const handleDroneSelected = async (droneId: string) => {
    setSelectedDroneType(droneId);
    setShowDroneModal(false);
    setIsJoining(true);

    try {
      const token = authService.getAccessToken();
      if (!token) {
        setError('Not authenticated. Please login again.');
        setIsJoining(false);
        return;
      }

      // Set the token in arena service for API calls
      arenaService.setUserToken(token);

      // Join as viewer using our own backend
      const username = user?.username || `Viewer_${Date.now()}`;
      const userId = user?.id || `temp_${Date.now()}`;

      console.log('🎮 Attempting to join as viewer:', { username, userId, gameId, droneType: droneId });

      // Register with our viewer service, including selected drone
      const joinResult = await viewerService.joinViewer(gameId, userId, username, droneId);

      if (!joinResult.success) {
        setError(joinResult.error || 'Failed to join as viewer');
        setIsJoining(false);
        return;
      }

      console.log('✅ Successfully joined as viewer:', joinResult.data);
      console.log(`🎮 You are Drone ${joinResult.data?.droneId}`);

      // Store the userId we used for heartbeats
      setViewerUserId(userId);

      // Get game details from Arena Arcade
      const result = await arenaService.getGameDetails(gameId);

      if (result.success && result.data) {
        console.log('✅ Successfully connected to game:', result.data);
        setGameState(result.data);

        // Load items catalog
        loadItemsCatalog();
      } else {
        setError(result.error || 'Game not found. Please check the Game ID.');
      }
    } catch (err: any) {
      console.error('❌ Join game error:', err);
      setError(err.message || 'Failed to join game');
    } finally {
      setIsJoining(false);
    }
  };

  const loadItemsCatalog = async () => {
    try {
      const result = await arenaService.getItemsCatalog();
      if (result.success && result.data) {
        setItemsCatalog(result.data.items || []);
      }
    } catch (err) {
      console.error('Failed to load items catalog:', err);
    }
  };

  const handleBoostPlayer = async () => {
    if (!gameState || !selectedPlayer || !user) return;

    setIsBoosting(true);
    try {
      const result = await arenaService.boostPlayer(
        gameState.gameId,
        selectedPlayer,
        boostAmount,
        user.username
      );

      if (result.success) {
        console.log('Boost successful:', result.data);
        alert(`Successfully boosted player with +${boostAmount} points!`);
        setSelectedPlayer('');
      } else {
        alert('Boost failed: ' + result.error);
      }
    } catch (err: any) {
      alert('Boost failed: ' + err.message);
    } finally {
      setIsBoosting(false);
    }
  };

  const handleDropItem = async () => {
    if (!gameState || !selectedItem || !targetPlayer) return;

    setIsDroppingItem(true);
    try {
      const result = await arenaService.dropImmediateItem(
        gameState.gameId,
        selectedItem,
        targetPlayer
      );

      if (result.success) {
        console.log('Item drop successful:', result.data);
        alert('Item dropped successfully!');
        setSelectedItem('');
        setTargetPlayer('');
      } else {
        alert('Item drop failed: ' + result.error);
      }
    } catch (err: any) {
      alert('Item drop failed: ' + err.message);
    } finally {
      setIsDroppingItem(false);
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
          <h1 className="text-3xl font-bold text-white mb-4">Arena Arcade Viewer</h1>
          <p className="text-gray-300 mb-6">Please login to join a game</p>
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
            <h1 className="text-4xl font-bold text-cyan-400 mb-2 font-mek">VIEWER PORTAL</h1>
            <p className="text-gray-400">Join a streamer&apos;s game and boost your favorite players!</p>
          </div>
          <Link href="/" className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white rounded-lg">
            ← Back to Menu
          </Link>
        </div>

        {!gameState ? (
          /* Join Game Form */
          <div className="max-w-2xl mx-auto">
            <div className="p-8 bg-gray-800 rounded-lg border-2 border-cyan-500/30">
              <h2 className="text-2xl font-bold text-cyan-300 mb-6">Join Game Session</h2>

              {error && (
                <div className="mb-4 p-3 bg-red-900/50 border border-red-500 rounded text-red-200 text-sm">
                  {error}
                </div>
              )}

              <form onSubmit={handleJoinGame} className="space-y-4">
                <div>
                  <label className="block text-gray-300 mb-2">Game ID</label>
                  <input
                    type="text"
                    value={gameId}
                    onChange={(e) => setGameId(e.target.value)}
                    className="w-full p-3 bg-gray-900 border border-cyan-500/30 text-white rounded focus:border-cyan-400 focus:outline-none font-mono"
                    placeholder="Enter the Game ID from streamer"
                    required
                    disabled={isJoining}
                  />
                  <p className="text-gray-500 text-sm mt-1">
                    Get the Game ID from the streamer&apos;s chat or overlay
                  </p>
                </div>

                <button
                  type="submit"
                  disabled={isJoining}
                  className="w-full py-3 bg-cyan-600 hover:bg-cyan-700 disabled:bg-gray-600 text-white font-bold rounded transition-colors"
                >
                  {isJoining ? 'JOINING...' : 'JOIN GAME'}
                </button>
              </form>

              {/* <div className="mt-6 p-4 bg-gray-900/50 rounded">
                <h3 className="text-sm font-semibold text-cyan-300 mb-2">How does it work?</h3>
                <ul className="text-sm text-gray-400 space-y-1">
                  <li>• Get the Game ID from your favorite streamer</li>
                  <li>• Enter it above to join their active game session</li>
                  <li>• Boost players with your Arena coins</li>
                  <li>• Drop power-ups and items into the game</li>
                  <li>• Watch the action unfold in real-time!</li>
                </ul>
              </div> */}
            </div>
          </div>
        ) : (
          /* Active Game Dashboard */
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Left Column - Game Info & Boost */}
            <div className="space-y-6">
              {/* Game Status Card */}
              <div className="p-6 bg-gray-800 rounded-lg border border-cyan-500/30">
                <h3 className="text-xl font-bold text-cyan-300 mb-4">Game Status</h3>
                <div className="space-y-3">
                  <div>
                    <p className="text-gray-400 text-sm">Game ID</p>
                    <p className="text-white font-mono text-sm">{gameState.gameId}</p>
                  </div>
                  <div>
                    <p className="text-gray-400 text-sm">Your Role</p>
                    <p className="text-cyan-400 font-semibold">🎮 Viewer</p>
                  </div>
                  <div>
                    <p className="text-gray-400 text-sm">Username</p>
                    <p className="text-white font-semibold">{user?.username || 'Guest'}</p>
                  </div>
                  <div>
                    <p className="text-gray-400 text-sm">Status</p>
                    <p className="text-green-400 font-bold">✅ CONNECTED</p>
                  </div>
                  {/* <div>
                    <p className="text-gray-400 text-sm">Arena Active</p>
                    <p className={arenaActive ? 'text-green-400' : 'text-red-400'}>
                      {arenaActive ? 'LIVE' : 'Waiting'}
                    </p>
                  </div> */}
                  {countdown !== null && (
                    <div>
                      <p className="text-gray-400 text-sm">Countdown</p>
                      <p className="text-cyan-400 text-2xl font-bold">{countdown}s</p>
                    </div>
                  )}

                  {/* Viewer Info */}

                  {/* Selected Drone Info */}
                  {selectedDroneType && (
                    <div className="mt-3 p-3 bg-purple-900/20 rounded-lg border border-purple-500/30">
                      <p className="text-purple-300 text-sm font-semibold mb-2"> Your Drone</p>
                      <div className="flex items-center gap-3">
                        <div>
                          <p className="text-white font-bold">{getDroneById(selectedDroneType)?.name || 'Unknown Drone'}</p>
                        </div>
                      </div>
                    </div>
                  )}
                  {/* Leave Game Button */}
                  <button
                    onClick={async () => {
                      if (gameState && viewerUserId) {
                        await viewerService.leaveGame(gameState.gameId, viewerUserId);
                      }
                      setGameState(null);
                      setViewerUserId('');
                      setError('');
                    }}
                    className="w-full py-2 bg-red-600 hover:bg-red-700 text-white font-bold rounded mt-3 text-sm"
                  >
                    🚪 LEAVE GAME
                  </button>
                </div>
              </div>


              {/* Recent Boosts */}
              {boostHistory.length > 0 && (
                <div className="p-6 bg-gray-800 rounded-lg border border-cyan-500/30">
                  <h3 className="text-xl font-bold text-yellow-400 mb-4">Recent Boosts</h3>
                  <div className="space-y-2 max-h-64 overflow-y-auto">
                    {boostHistory.map((boost, i) => (
                      <div key={i} className="p-2 bg-gray-900 rounded text-sm">
                        <div className="flex justify-between items-center">
                          <span className="text-white font-semibold">{boost.playerName}</span>
                          <span className="text-green-400">+{boost.currentCyclePoints}</span>
                        </div>
                        <p className="text-gray-400 text-xs">by {boost.boosterUsername || 'Anonymous'}</p>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Drone Selection Modal */}
      <DroneSelectionModal
        isOpen={showDroneModal}
        onSelect={handleDroneSelected}
        onClose={() => setShowDroneModal(false)}
      />
    </main>
  );
}
