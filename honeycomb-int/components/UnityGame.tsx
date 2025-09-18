'use client'

import { useEffect, useRef, useState } from 'react';

interface UnityGameProps {
  onBackToMenu: () => void;
  walletAddress: string;
}

export default function UnityGame({ onBackToMenu, walletAddress }: UnityGameProps) {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [copiedAddress, setCopiedAddress] = useState(false);
  const [dimensions, setDimensions] = useState({ width: 0, height: 0 });

  // Copy wallet address to clipboard
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
    const updateDimensions = () => {
      const headerHeight = 60; 
      const availableWidth = window.innerWidth;
      const availableHeight = window.innerHeight - headerHeight;
      
      const newDimensions = {
        width: Math.min(availableWidth, window.screen.availWidth),
        height: Math.min(availableHeight, window.screen.availHeight - headerHeight)
      };
      
      console.log('🎮 Unity dimensions calculated:', {
        windowSize: `${window.innerWidth}x${window.innerHeight}`,
        screenSize: `${window.screen.availWidth}x${window.screen.availHeight}`,
        calculated: `${newDimensions.width}x${newDimensions.height}`,
        headerHeight
      });
      
      setDimensions(newDimensions);
    };

    updateDimensions();
    window.addEventListener('resize', updateDimensions);
    window.addEventListener('orientationchange', () => {
      setTimeout(updateDimensions, 100); 
    });

    return () => {
      window.removeEventListener('resize', updateDimensions);
      window.removeEventListener('orientationchange', updateDimensions);
    };
  }, []);

  useEffect(() => {
    // Set up Unity WebGL communication bridge
    if (typeof window !== 'undefined') {
      // Expose Web3 functions to Unity iframe
      (window as any).web3Bridge = {
        mintReward: async (amount: number) => {
          // console.log(`🏆 Unity called: Mint ${amount} reward tokens`);
          // Show notification in the game
          alert(`🎉 Victory! You earned ${amount} SolMechs tokens!\n\nWallet: ${walletAddress.slice(0, 8)}...${walletAddress.slice(-8)}\n\n(Real blockchain minting will be integrated here)`);
          
          // TODO: Integrate with your existing Honeycomb mintWinReward function
          // Example: await mintWinReward(amount);
          
          return true;
        },
          
        getBalance: async () => {
          // console.log('💰 Unity called: Get user balance');
          // TODO: Integrate with your existing checkPointBalance function
          // Example: return await checkPointBalance();
          
          // For now, return mock balance
          const mockBalance = Math.floor(Math.random() * 500) + 100;
          // console.log(`💰 Returning balance: ${mockBalance} tokens`);
          return mockBalance;
        },
        
        getWalletAddress: () => {
          // console.log('🔗 Unity called: Get wallet address');
          return walletAddress;
        },
        
        onGameWin: async () => {
          // console.log('🎉 Unity: Player won the game!');
          return await (window as any).web3Bridge.mintReward(10);
        },
        
        onGameLose: () => {
          // console.log('😔 Unity: Player lost the game');
          return false;
        },
        
        // Additional Web3 functions Unity can call
        showWalletInfo: () => {
          alert(`🔗 Wallet Info:\n\nAddress: ${walletAddress}\nNetwork: Solana\nStatus: Connected\n\nYou can earn tokens by winning battles!`);
        }
      };

      // console.log('✅ Web3 Bridge initialized for Unity communication');
    }

    return () => {
      // Cleanup
      if (typeof window !== 'undefined') {
        delete (window as any).web3Bridge;
      }
    };
  }, [walletAddress]);

  if (dimensions.width === 0 || dimensions.height === 0) {
    return (
      <div className="fixed inset-0 bg-black flex items-center justify-center">
        <div className="text-white text-lg">Loading game...</div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-screen w-screen bg-black overflow-hidden">
      {/* Game Header */}
      <div className="w-full flex justify-between items-center z-10 p-4 bg-black/80 backdrop-blur-sm border-b border-gray-800">
        <div className="flex items-center gap-2 px-3 py-2">
          <div className="text-gray-300 text-xs font-mek">
            <div className="font-mono text-xs truncate">
              <span className="inline sm:hidden">{walletAddress.slice(0, 4)}...{walletAddress.slice(-4)}</span>
              <span className="hidden sm:inline">{walletAddress.slice(0, 6)}...{walletAddress.slice(-6)}</span>
            </div>
          </div>
          <button
            onClick={() => handleCopyAddress(walletAddress)}
            className="p-1.5 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors text-xs flex items-center justify-center"
            title="Copy full wallet address"
          >
            {copiedAddress ? '✅' : '📋'}
          </button>
          {copiedAddress && (
            <span className="text-green-400 text-xs font-bold animate-pulse hidden sm:inline">
              Copied!
            </span>
          )}
        </div>
        <button
          onClick={onBackToMenu}
          className="px-3 py-2 bg-red-600 text-white rounded hover:bg-red-700 transition-colors font-bold font-mek text-xs mr-3"
        >
          <span className="hidden sm:inline">← Exit Game</span>
          <span className="sm:hidden">←</span>
        </button>
      </div>

      <div className="flex-1 w-full bg-black">
        <iframe
          ref={iframeRef}
          src="/unity"
          className="w-full h-full border-0 bg-black"
          title="SolMechs Unity WebGL Game"
          allow="accelerometer; gyroscope; microphone; camera; fullscreen"
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