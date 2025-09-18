'use client'

import React, { useState, useEffect } from "react";
import { useUser, useWallet, SignInButton, SignOutButton } from "@civic/auth-web3/react";
import { useUnifiedWallet } from '@jup-ag/wallet-adapter';
import { useWallet as useSolanaWallet } from '@solana/wallet-adapter-react';
import { sendClientTransactions } from "@honeycomb-protocol/edge-client/client/walletHelpers";
import { client, PROJECT_ADDRESS } from "../constants/client";
import { autoAirdropSol } from "../utils/airdrop";
import Image from "next/image";
import UnityGame from "../components/UnityGame";
import WalletConnection from "../components/WalletConnection";

// Game flow states
type GameState = 'CONNECTING' | 'CHECKING_USER' | 'CREATING_WALLET' | 'CREATING_PROFILE' | 'PLAYING';

export default function Home() {
  const user = useUser();
  const solanaWallet = useWallet({ type: "solana" });
  const jupiterWallet = useUnifiedWallet();
  
  const rawSolanaWallet = useSolanaWallet();
  const [isLoading, setIsLoading] = useState(false);
  const [gameState, setGameState] = useState<GameState>('CONNECTING');
  const [userExists, setUserExists] = useState(false);
  const [profileExists, setProfileExists] = useState(false);
  const [projectAddress, setProjectAddress] = useState(PROJECT_ADDRESS);


  const activeWallet = user?.isAuthenticated && solanaWallet.address 
    ? solanaWallet 
    : jupiterWallet.connected && jupiterWallet.publicKey 
      ? { address: jupiterWallet.publicKey.toString(), wallet: jupiterWallet }
      : rawSolanaWallet.connected && rawSolanaWallet.publicKey
        ? { address: rawSolanaWallet.publicKey.toString(), wallet: rawSolanaWallet }
        : null;

  useEffect(() => {
   
    console.log('- Game state:', gameState);
  }, [user?.isAuthenticated, solanaWallet.address, jupiterWallet.connected, jupiterWallet.publicKey, activeWallet, gameState, rawSolanaWallet.connected, rawSolanaWallet.publicKey]);
  useEffect(() => {
    if (gameState === 'CONNECTING') {
      const interval = setInterval(() => {
       
        const isConnected = jupiterWallet.connected && jupiterWallet.publicKey;
        const rawConnected = rawSolanaWallet.connected && rawSolanaWallet.publicKey;
        
        if (isConnected || rawConnected) {
          clearInterval(interval);
          setGameState('CHECKING_USER');
          setTimeout(() => checkUserAndProfile(), 500);
        }
      }, 2000);

      return () => clearInterval(interval);
    }
  }, [gameState, jupiterWallet.connected, jupiterWallet.publicKey, rawSolanaWallet.connected, rawSolanaWallet.publicKey]);

  useEffect(() => {
    if (rawSolanaWallet.connected && rawSolanaWallet.publicKey && gameState === 'CONNECTING') {
      
      setGameState('CHECKING_USER');
      setTimeout(() => {
        checkUserAndProfile();
      }, 1000);
    }
  }, [rawSolanaWallet.connected, rawSolanaWallet.publicKey, gameState]);
  useEffect(() => {
    const handleJupiterConnection = (event: CustomEvent) => {
      if (gameState === 'CONNECTING') {
        
        const closeModal = () => {
          const backdrop = document.querySelector('[data-testid="modal-backdrop"]') || 
                           document.querySelector('.unified-wallet-modal-backdrop') ||
                           document.querySelector('[role="dialog"] + div') ||
                           document.querySelector('.modal-backdrop');
          if (backdrop) {
            (backdrop as HTMLElement).click();
          }
          
          const closeButton = document.querySelector('[data-testid="close-button"]') || 
                             document.querySelector('.close-button') ||
                             document.querySelector('[aria-label="Close"]') ||
                             document.querySelector('button[aria-label="close"]');
          if (closeButton) {
            (closeButton as HTMLElement).click();
          }
          
          document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
        };
        
        closeModal();
        setTimeout(() => {
          checkUserAndProfile();
        }, 1000);
      }
    };

    window.addEventListener('jupiterWalletConnected', handleJupiterConnection as EventListener);
    
    return () => {
      window.removeEventListener('jupiterWalletConnected', handleJupiterConnection as EventListener);
    };
  }, [gameState]);

  useEffect(() => {
    // console.log('⚡ Jupiter state monitor:', {
    //   connected: jupiterWallet.connected,
    //   publicKey: jupiterWallet.publicKey?.toString(),
    //   gameState: gameState
    // });

    if (jupiterWallet.connected && jupiterWallet.publicKey) {
      // console.log('🔥 Jupiter wallet state changed - connected!');
      // console.log('💰 Jupiter wallet address:', jupiterWallet.publicKey.toString());
      
      if (gameState === 'CONNECTING') {
        // Force close any open modals
        setTimeout(() => {
          const escapeEvent = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true });
          document.dispatchEvent(escapeEvent);
        }, 100);
        
        // Force game state change and proceed
        // console.log('🚀 FORCING game state change...');
        setGameState('CHECKING_USER');
        
        setTimeout(() => {
          // console.log('🚀 Proceeding to game setup via state change...');
          checkUserAndProfile();
        }, 1000);
      }
    }
  }, [jupiterWallet.connected, jupiterWallet.publicKey, gameState]);

  useEffect(() => {
    const handleUserFlow = async () => {
      // console.log('🔄 handleUserFlow triggered - activeWallet:', activeWallet?.address);
      
      if (!activeWallet) {
        // console.log('❌ No active wallet - staying in CONNECTING state');
        setGameState('CONNECTING');
        return;
      }

      // console.log('✅ Active wallet detected:', activeWallet.address);

      if (user?.isAuthenticated && !solanaWallet.address) {
        console.log('🏛️ Civic user needs wallet creation');
        await checkIfUserNeedsWallet();
      } else {
        console.log('🚀 Proceeding with wallet flow...');
        await checkUserAndProfile();
      }
    };

    handleUserFlow();
  }, [activeWallet?.address, user?.isAuthenticated, solanaWallet.address]);

  const checkIfUserNeedsWallet = async () => {
    if (!user?.isAuthenticated) return;
    
    setIsLoading(true);
    setGameState('CHECKING_USER');
    
    try {
      if ('createWallet' in user) {
        await autoCreateWallet();
      } else {
        alert("Wallet creation not available. Please try again or contact support.");
        setGameState('CONNECTING');
      }
    } catch (error) {
      console.error("❌ Error in user flow:", error);
      setGameState('CONNECTING');
    } finally {
      setIsLoading(false);
    }
  };

]  const autoCreateWallet = async () => {
    if (!user || !('createWallet' in user)) return;
    
    setIsLoading(true);
    setGameState('CREATING_WALLET');
    
    try {
      // console.log("🏛️ Creating Civic wallet automatically...");
      await user.createWallet();
      // console.log("✅ Civic wallet created successfully");
      
      // Wait a moment for wallet to be available, then silently check balance
      setTimeout(async () => {
        if (activeWallet?.address) {
          // Silent background balance maintenance
          autoAirdropSol(activeWallet.address);
          checkUserAndProfile();
        }
      }, 1000);
      
    } catch (error) {
      console.error("❌ Error creating Civic wallet:", error);
      alert("Failed to create wallet automatically. Please try again.");
      setGameState('CONNECTING');
    } finally {
      setIsLoading(false);
    }
  };


  const checkUserAndProfile = async () => {
    // Get address from active wallet
    if (!activeWallet?.address) return;
    
    setIsLoading(true);
    setGameState('CHECKING_USER');
    
    try {
      // Silent background balance maintenance for existing wallets
      autoAirdropSol(activeWallet.address);

      // Check if user exists
      const usersResult = await client.findUsers({
        wallets: [activeWallet.address]
      });
      const honeycombUser = (usersResult as any)?.[0] || null;
      
      if (honeycombUser) {
        setUserExists(true);
        //  console.log("✅ User exists:", honeycombUser);
        
        // Check if user has profile for this project
        const profilesResult = await client.findProfiles({
          projects: [projectAddress],
          addresses: [activeWallet.address]
        });
        const profile = (profilesResult as any)?.[0] || null;
        
        if (profile) {
          setProfileExists(true);
          setGameState('PLAYING');
          // console.log("✅ User profile exists - redirecting to game:", profile);
        } else {
          setProfileExists(false);
          setGameState('CREATING_PROFILE');
          // console.log("❌ No profile found for this project - auto-creating profile...");
          // Auto-create profile for existing user
          setTimeout(() => createProfileForExistingUser(), 100);
        }
      } else {
        setUserExists(false);
        setProfileExists(false);
        setGameState('CREATING_PROFILE');
        // console.log("❌ No user found - auto-creating user and profile...");
        // Auto-create user and profile for new user
        setTimeout(() => createUserWithProfile(), 100);
      }
    } catch (error) {
      console.error("Error checking user/profile:", error);
      // If there's an error, assume we need to create profile
      setGameState('CREATING_PROFILE');
    } finally {
      setIsLoading(false);
    }
  };

  const createUserWithProfile = async () => {
    if (!activeWallet?.address) {
      alert("Please connect your wallet first");
      return;
    }
    // Use default name based on wallet address
    const defaultName = "Player_" + activeWallet.address.slice(-4);

    setIsLoading(true);
    try {
      // Create user + profile in one transaction (first time user)
      // console.log("🆕 Creating new user with profile...");
      const {
        createNewUserWithProfileTransaction: txResponse
      } = await client.createNewUserWithProfileTransaction({
        project: projectAddress,
        wallet: activeWallet.address,
        payer: activeWallet.address,
        profileIdentity: "main",
        userInfo: {
          name: defaultName,
          bio: "SolMechs Player",
          pfp: "https://api.dicebear.com/7.x/avataaars/svg?seed=" + encodeURIComponent(defaultName),
        },
      });

      // Use active wallet for signing
      const result = await sendClientTransactions(
        client,
        activeWallet.wallet,
        txResponse
      );
      // console.log("📋 Transaction result:", result);
      
      // Check if transaction was successful
      const isSuccess = result && result.length > 0 && 
        result[0].responses && result[0].responses.length > 0 &&
        result[0].responses[0].status === "Success";
        
      if (!isSuccess) {
        const errorMsg = result[0]?.responses[0]?.error || "Unknown error";
        throw new Error(errorMsg);
      }
      
      // console.log("✅ New user and profile created successfully!");
      
      // Only update states if transaction was successful
      setUserExists(true);
      setProfileExists(true);
      setGameState('PLAYING');
      
      // console.log("🎉 Profile creation successful! Redirecting to game...");
      
    } catch (error) {
      console.error("❌ Error creating user with profile:", error);
      handleProfileCreationError(error as Error);
    } finally {
      setIsLoading(false);
    }
  };

  const createProfileForExistingUser = async () => {
    if (!activeWallet?.address) {
      alert("Please connect your wallet first");
      return;
    }

    setIsLoading(true);
    try {
      // User exists, just create profile for this project
      // console.log("👤 Creating profile for existing user...");
      const {
        createNewProfileTransaction: txResponse
      } = await client.createNewProfileTransaction({
        project: projectAddress,
        payer: activeWallet.address,
        identity: "main",
      });

      // Use active wallet for signing
      const result = await sendClientTransactions(
        client,
        activeWallet.wallet,
        txResponse
      );
      // console.log("📋 Transaction result:", result);
      
      // Check if transaction was successful
      const isSuccess = result && result.length > 0 && 
        result[0].responses && result[0].responses.length > 0 &&
        result[0].responses[0].status === "Success";
        
      if (!isSuccess) {
        const errorMsg = result[0]?.responses[0]?.error || "Unknown error";
        throw new Error(errorMsg);
      }
      
      // console.log("✅ Profile created for existing user successfully!");
      
      // Only update states if transaction was successful
      setUserExists(true);
      setProfileExists(true);
      setGameState('PLAYING');
      
      // console.log("🎉 Profile creation successful! Redirecting to game...");
      
    } catch (error) {
      // console.error("❌ Error creating profile for existing user:", error);
      handleProfileCreationError(error as Error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleProfileCreationError = (error: Error) => {
    const errorMessage = error.message;
    
    // Check if error is about user already having profile
    if (errorMessage.includes("User already exists with profile")) {
      // console.log("🔄 User already has profile - redirecting to game...");
      // Profile already exists, just go to play state
      setUserExists(true);
      setProfileExists(true);
      setGameState('PLAYING');
    } else if (errorMessage.includes("Attempt to debit an account but found no record of a prior credit")) {
      // Insufficient balance error
      alert("❌ Transaction Failed: Insufficient SOL balance\n\nYou need SOL in your wallet to create a profile. Please add some SOL and try again.");
      setGameState('CONNECTING');
    } else if (errorMessage.includes("Transaction simulation failed")) {
      // General simulation failure
      alert("❌ Transaction Failed\n\nThe transaction could not be completed. This might be due to:\n• Insufficient SOL balance\n• Network issues\n• Wallet configuration\n\nPlease check your wallet and try again.");
      setGameState('CONNECTING');
    } else {
      // Other errors
      alert("❌ Error creating profile:\n\n" + errorMessage);
      setGameState('CONNECTING');
    }
  };

  const handleBackToMenu = () => {
    setGameState('CONNECTING');
    // Reset states when going back
    setUserExists(false);
    setProfileExists(false);
  };

  // Main render logic
  if (gameState === 'PLAYING') {
    return (
      <UnityGame 
        onBackToMenu={handleBackToMenu}
        walletAddress={activeWallet?.address || ''}
      />
    );
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-0 sm:p-6 md:p-8">
      <div className="w-full max-w-sm sm:max-w-md">
        {/* Header */}
        <div className="flex flex-col items-center mb-6 sm:mb-8">
          <Image 
            src="/images/logo.svg" 
            alt="SolMechs Logo" 
            width={80} 
            height={80} 
            className="w-40 h-20 sm:w-20 sm:h-20 md:w-24 md:h-24" 
          /> 
        </div>
      </div>

      {/* Wallet Connection Status */}
      <div className="mb-6 p-4 sm:p-6 md:p-8 w-full sm:max-w-sm md:max-w-md lg:max-w-lg xl:max-w-xl flex flex-col items-center justify-center bg-contain bg-center bg-no-repeat" 
           style={{
             backgroundImage: "url('/images/frame.png')", 
             minHeight: '400px',
             backgroundSize: 'contain'
           }}>
          {!activeWallet ? (
            <div className="text-center flex flex-col items-center px-4">
              <div className="mb-2">
                <Image 
                  src="/images/connect_logo.png" 
                  alt="Link" 
                  width={40} 
                  height={40} 
                  className="w-8 h-8 sm:w-10 sm:h-10" 
                /> 
              </div>
              <h2 className="text-xl sm:text-2xl md:text-3xl font-bold text-white mb-0 text-center">
                Welcome to SolMechs
              </h2>
              <p className="text-gray-300 text-sm sm:text-base text-center">
                Choose your wallet to start playing
              </p>
              <WalletConnection />
            </div>
          ) : activeWallet.address && gameState !== 'CREATING_PROFILE' ? (
            <div className="text-center px-4">
              <div className="text-3xl sm:text-4xl mb-2">✅</div>
              <h2 className="text-lg sm:text-xl font-bold text-green-400 mb-2">Ready to Play</h2>
              <p className="text-gray-300 mb-1 text-sm sm:text-base">Your wallet is connected and ready</p>

              <div className="mt-3 flex gap-2 justify-center relative">
                {user?.isAuthenticated ? (
                  <>
                    {/* Custom button overlay for Civic Sign Out */}
                    <div className="button-frame relative hover:scale-105 transition-transform duration-200 z-10">
                      <Image 
                        src="/images/Button1.png" 
                        alt="Sign Out" 
                        width={140} 
                        height={45} 
                        className="w-full h-auto min-w-[120px] max-w-[140px] sm:max-w-[160px] md:max-w-[180px]"
                      />
                      <span className="absolute inset-0 flex items-center justify-center text-white font-bold text-xs sm:text-sm md:text-base pointer-events-none px-2">
                        SIGN OUT
                      </span>
                    </div>
                    {/* Hidden actual SignOutButton positioned behind */}
                    <SignOutButton className="absolute inset-0 opacity-0 z-20 cursor-pointer" />
                  </>
                ) : (
                  <WalletConnection />
                )}
              </div>
            </div>
          ) : gameState === 'CREATING_PROFILE' ? (
            <div className="text-center px-4">
              <div className="animate-spin text-3xl sm:text-4xl mb-4">⚙️</div>
              <h2 className="text-lg sm:text-xl font-bold text-white mb-2">Setting Up Your Profile...</h2>
              <p className="text-gray-300 mb-4 text-sm sm:text-base">
                {userExists 
                  ? 'Creating your game profile' 
                  : 'Creating your account and profile'}
              </p>
              <p className="text-xs sm:text-sm text-gray-400">
                Please confirm the transaction in your wallet
              </p>
            </div>
          ) : (
            <div className="text-center px-4">
              <div className="animate-spin text-3xl sm:text-4xl mb-4">⚙️</div>
              <h2 className="text-lg sm:text-xl font-bold text-white mb-2">Setting Up Your Account</h2>
              <p className="text-gray-300 text-sm sm:text-base">Please wait while we prepare your resources...</p>
            </div>
          )}
</div>
    </main>
  );
}