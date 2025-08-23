'use client'

import { useEffect, useRef } from 'react';

interface UnityGameProps {
  onBackToMenu: () => void;
  walletAddress: string;
}

export default function UnityGame({ onBackToMenu, walletAddress }: UnityGameProps) {
  const iframeRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => {
    // Set up Unity WebGL communication bridge
    if (typeof window !== 'undefined') {
      // Expose Web3 functions to Unity iframe
      (window as any).web3Bridge = {
        mintReward: async (amount: number) => {
          console.log(`🏆 Unity called: Mint ${amount} reward tokens`);
          // Show notification in the game
          alert(`🎉 Victory! You earned ${amount} SolMechs tokens!\n\nWallet: ${walletAddress.slice(0, 8)}...${walletAddress.slice(-8)}\n\n(Real blockchain minting will be integrated here)`);
          
          // TODO: Integrate with your existing Honeycomb mintWinReward function
          // Example: await mintWinReward(amount);
          
          return true;
        },
        
        getBalance: async () => {
          console.log('💰 Unity called: Get user balance');
          // TODO: Integrate with your existing checkPointBalance function
          // Example: return await checkPointBalance();
          
          // For now, return mock balance
          const mockBalance = Math.floor(Math.random() * 500) + 100;
          console.log(`💰 Returning balance: ${mockBalance} tokens`);
          return mockBalance;
        },
        
        getWalletAddress: () => {
          console.log('🔗 Unity called: Get wallet address');
          return walletAddress;
        },
        
        onGameWin: async () => {
          console.log('🎉 Unity: Player won the game!');
          return await (window as any).web3Bridge.mintReward(10);
        },
        
        onGameLose: () => {
          console.log('😔 Unity: Player lost the game');
          return false;
        },
        
        // Additional Web3 functions Unity can call
        showWalletInfo: () => {
          alert(`🔗 Wallet Info:\n\nAddress: ${walletAddress}\nNetwork: Solana\nStatus: Connected\n\nYou can earn tokens by winning battles!`);
        }
      };

      console.log('✅ Web3 Bridge initialized for Unity communication');
    }

    return () => {
      // Cleanup
      if (typeof window !== 'undefined') {
        delete (window as any).web3Bridge;
      }
    };
  }, [walletAddress]);

  return (
    <div className="flex flex-col min-h-screen">
      {/* Game Header */}
      <div className="w-full flex justify-between items-center z-10 p-4 bg-contain bg-center bg-no-repeat">
        <div>
          <h1 className="text-2xl font-bold text-white font-mek">🎮 SolMechs Arena</h1>
          <p className="text-gray-300 text-sm font-mek">
            Player: {walletAddress.slice(0, 8)}...{walletAddress.slice(-8)}
          </p>
        </div>
        <button
          onClick={onBackToMenu}
          className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 transition-colors font-bold font-mek"
        >
          ← Exit Game
        </button>
      </div>

      {/* Unity WebGL Game Container */}
      <div className="flex-1 relative">
        <iframe
          ref={iframeRef}
          src="/unity/index.html"
          className="w-full h-full border-0"
          style={{ height: 'calc(100vh - 80px)' }}
          title="SolMechs Unity WebGL Game"
          allow="accelerometer; gyroscope; microphone; camera"
        />
      </div>
      
      {/* Web3 Status Bar */}
      <div className="w-full text-center text-green-100 text-sm p-2 bg-contain bg-center bg-no-repeat font-mek" style={{backgroundImage: "url('/images/frame.png')", minHeight: '60px'}}>
        <div className="flex items-center justify-center h-full">
          <span className="font-bold">🌐 Web3 Bridge Active</span> - Unity can mint rewards and interact with blockchain
        </div>
      </div>
    </div>
  );
}