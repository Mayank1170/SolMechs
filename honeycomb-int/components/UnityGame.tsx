'use client'

import { useEffect, useRef, useState } from 'react';

interface UnityGameProps {
  onBackToMenu: () => void;
  walletAddress: string;
}

export default function UnityGame({ onBackToMenu, walletAddress }: UnityGameProps) {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [copiedAddress, setCopiedAddress] = useState(false);
  const [gameSessionId, setGameSessionId] = useState<string | null>(null);

  const handleCopyAddress = async (address: string) => {
    try {
      await navigator.clipboard.writeText(address);
      setCopiedAddress(true);
      setTimeout(() => setCopiedAddress(false), 2000);
    } catch (error) {
      console.error('Failed to copy address:', error);
    }
  };

  useEffect(() => {
    const startGameSession = async () => {
      try {
        console.log('🎮 Starting game session for wallet:', walletAddress);

        const response = await fetch('/api/game-session', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ walletAddress })
        });

        const result = await response.json();

        if (result.success) {
          setGameSessionId(result.data.sessionId);
          console.log('✅ Game session started:', result.data.sessionId);
        } else {
          console.error('❌ Failed to start game session:', result.error);
        }
      } catch (error) {
        console.error('❌ Error starting game session:', error);
      }
    };

    startGameSession();
  }, [walletAddress]);

  useEffect(() => {
    if (typeof window !== 'undefined') {
      (window as any).web3Bridge = {
        mintReward: async (amount: number) => {
       
          alert(`🎉 Victory! You earned ${amount} SolMechs tokens!\n\nWallet: ${walletAddress.slice(0, 8)}...${walletAddress.slice(-8)}\n\n(Real blockchain minting will be integrated here)`);

          return true;
        },

        getBalance: async () => {
        
          const mockBalance = Math.floor(Math.random() * 500) + 100;
          return mockBalance;
        },

        getWalletAddress: () => {
          return walletAddress;
        },

        onGameWin: async () => {
          console.log('🎉 Unity: Player won the game!');

          if (gameSessionId) {
            try {
              const response = await fetch('/api/game-session', {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                  sessionId: gameSessionId,
                  won: true,
                  score: 1000
                })
              });

              const result = await response.json();
              console.log('📊 Game session ended:', result.data);
            } catch (error) {
              console.error('❌ Error ending game session:', error);
            }
          }

          return await (window as any).web3Bridge.mintReward(10);
        },

        onGameLose: async () => {
          console.log('😔 Unity: Player lost the game');

          if (gameSessionId) {
            try {
              const response = await fetch('/api/game-session', {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                  sessionId: gameSessionId,
                  won: false,
                  score: 0
                })
              });

              const result = await response.json();
              console.log('📊 Game session ended:', result.data);
            } catch (error) {
              console.error('❌ Error ending game session:', error);
            }
          }

          return false;
        },

        showWalletInfo: () => {
          alert(`🔗 Wallet Info:\n\nAddress: ${walletAddress}\nNetwork: Solana\nStatus: Connected\n\nYou can earn tokens by winning battles!`);
        }
      };

    }

    return () => {
      // Cleanup
      if (typeof window !== 'undefined') {
        delete (window as any).web3Bridge;
      }
    };
  }, [walletAddress, gameSessionId]);

  return (
    <div className="flex flex-col min-h-screen">
      {/* Game Header */}
      <div className="w-full flex justify-between items-center z-10 p-4 bg-contain bg-center bg-no-repeat">
        <div className="flex items-center gap-3 bg-black/50 backdrop-blur-sm rounded-lg px-3 py-2 border border-gray-700">
          <div className="text-gray-300 text-xs font-mek">
            <div className="font-mono text-sm">
              {walletAddress.slice(0, 8)}...{walletAddress.slice(-8)}
            </div>
          </div>
          <button
            onClick={() => handleCopyAddress(walletAddress)}
            className="p-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors text-sm"
            title="Copy full wallet address"
          >
            {copiedAddress ? '✅' : '📋'}
          </button>
          {copiedAddress && (
            <span className="text-green-400 text-xs font-bold animate-pulse">
              Copied!
            </span>
          )}
        </div>
        <button
          onClick={onBackToMenu}
          className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 transition-colors font-bold font-mek"
        >
          ← Exit Game
        </button>
      </div>

      <div className="flex-1 relative">
        <iframe
          ref={iframeRef}
          src="/unity"
          className="w-full h-full border-0"
          style={{ height: 'calc(100vh)' }}
          title="SolMechs Unity WebGL Game"
          allow="accelerometer; gyroscope; microphone; camera"
        />
      </div>
      
      {/* Web3 Status Bar */}
      {/* <div className="w-full text-center text-green-100 text-sm p-2 bg-contain bg-center bg-no-repeat font-mek" style={{backgroundImage: "url('/images/frame.png')", minHeight: '60px'}}>
        <div className="flex items-center justify-center h-full">
          <span className="font-bold">🌐 Web3 Bridge Active</span> - Unity can mint rewards and interact with blockchain
        </div>
      </div> */}
    </div>
  );
}