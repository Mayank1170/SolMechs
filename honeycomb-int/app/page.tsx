'use client'

import React, { useState, useEffect } from "react";
import { useUser, useWallet, SignInButton, SignOutButton } from "@civic/auth-web3/react";
import { sendClientTransactions } from "@honeycomb-protocol/edge-client/client/walletHelpers";
import { client, PROJECT_ADDRESS } from "../constants/client";
import { autoAirdropSol } from "../utils/airdrop";
import Image from "next/image";
import UnityGame from "../components/UnityGame";

// Game flow states
type GameState = 'CONNECTING' | 'CHECKING_USER' | 'CREATING_WALLET' | 'CREATING_PROFILE' | 'PLAYING';

export default function Home() {
  const user = useUser();
  const solanaWallet = useWallet({ type: "solana" });
  const [isLoading, setIsLoading] = useState(false);
  const [gameState, setGameState] = useState<GameState>('CONNECTING');
  const [userExists, setUserExists] = useState(false);
  const [profileExists, setProfileExists] = useState(false);
  const [projectAddress, setProjectAddress] = useState(PROJECT_ADDRESS);
  // Main flow logic when user authentication status changes
  useEffect(() => {
    const handleUserFlow = async () => {
      if (!user?.isAuthenticated) {
        setGameState('CONNECTING');
        return;
      }

      // User is authenticated with Civic
      if (!solanaWallet.address) {
        // User authenticated but no wallet - check if they need to create one or if they have an existing Honeycomb account
        await checkIfUserNeedsWallet();
      } else {
        // User has wallet - proceed with normal flow
        await checkUserAndProfile();
      }
    };

    handleUserFlow();
  }, [user?.isAuthenticated, solanaWallet.address]);

  // Check if user needs wallet creation or if they can proceed directly
  const checkIfUserNeedsWallet = async () => {
    if (!user?.isAuthenticated) return;
    
    setIsLoading(true);
    setGameState('CHECKING_USER');
    
    try {
      // For new users or users without wallets, auto-create wallet
      if ('createWallet' in user) {
        // console.log("🆕 New user detected - auto-creating wallet...");
        await autoCreateWallet();
      } else {
        // This shouldn't normally happen, but handle gracefully
        // console.log("❌ Cannot create wallet for this user");
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

  // Auto-create wallet for new users
  const autoCreateWallet = async () => {
    if (!user || !('createWallet' in user)) return;
    
    setIsLoading(true);
    setGameState('CREATING_WALLET');
    
    try {
      // console.log("🏛️ Creating Civic wallet automatically...");
      await user.createWallet();
      // console.log("✅ Civic wallet created successfully");
      
      // Wait a moment for wallet to be available, then silently check balance
      setTimeout(async () => {
        if (solanaWallet.address) {
          // Silent background balance maintenance
          autoAirdropSol(solanaWallet.address);
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
    // Get address from Civic Solana wallet
    if (!solanaWallet.address) return;
    
    setIsLoading(true);
    setGameState('CHECKING_USER');
    
    try {
      // Silent background balance maintenance for existing wallets
      autoAirdropSol(solanaWallet.address);

      // Check if user exists
      const usersResult = await client.findUsers({
        wallets: [solanaWallet.address]
      });
      const honeycombUser = (usersResult as any)?.[0] || null;
      
      if (honeycombUser) {
        setUserExists(true);
        //  console.log("✅ User exists:", honeycombUser);
        
        // Check if user has profile for this project
        const profilesResult = await client.findProfiles({
          projects: [projectAddress],
          addresses: [solanaWallet.address]
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
    if (!solanaWallet.address) {
      alert("Please connect your wallet first");
      return;
    }
    // Use default name based on wallet address
    const defaultName = "Player_" + solanaWallet.address.slice(-4);

    setIsLoading(true);
    try {
      // Create user + profile in one transaction (first time user)
      // console.log("🆕 Creating new user with profile...");
      const {
        createNewUserWithProfileTransaction: txResponse
      } = await client.createNewUserWithProfileTransaction({
        project: projectAddress,
        wallet: solanaWallet.address,
        payer: solanaWallet.address,
        profileIdentity: "main",
        userInfo: {
          name: defaultName,
          bio: "SolMechs Player",
          pfp: "https://api.dicebear.com/7.x/avataaars/svg?seed=" + encodeURIComponent(defaultName),
        },
      });

      // Use Civic wallet for signing
      const result = await sendClientTransactions(
        client,
        solanaWallet.wallet,
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
    if (!solanaWallet.address) {
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
        payer: solanaWallet.address,
        identity: "main",
      });

      // Use Civic wallet for signing
      const result = await sendClientTransactions(
        client,
        solanaWallet.wallet,
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
        walletAddress={solanaWallet.address || ''}
      />
    );
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-8">
      <div className="w-full max-w-md">
        {/* Header */}
        <div className="flex flex-col items-center mb-8">
          <Image src="/images/logo.svg" alt="SolMechs Logo" width={100} height={100} className="w-full h-full" /> 
        </div>
        </div>

        {/* Wallet Connection Status */}
        <div className="mb-6 p-8 h-[80%] flex flex-col items-center justify-center lg:w-[50vw] md:w-[80vw] w-[300px] bg-contain bg-center bg-no-repeat" style={{backgroundImage: "url('/images/frame.png')", minHeight: '550px'}}>
          {!user?.isAuthenticated ? (
            <div className="text-center flex flex-col items-center">
              <div className="text-4xl mb-4">
                <Image src="/images/connect_logo.png" alt="Link" width={100} height={100} className="w-[40px] h-[40px]" /> 
              </div>
              <h2 className="text-3xl font-bold text-white mb-2">Welcome to SolMechs</h2>
              <p className="text-gray-300 mb-4">Sign in to start playing</p>
              <div className="flex justify-center relative">
                {/* Custom button overlay */}
                <div className="relative hover:scale-105 transition-transform duration-200 z-10">
                  <Image 
                    src="/images/Button1.png" 
                    alt="Sign In" 
                    width={200} 
                    height={60} 
                    className="w-auto h-auto"
                  />
                  <span className="absolute inset-0 flex items-center justify-center text-white font-bold text-xl pointer-events-none font-mek">
                    SIGN IN 
                  </span>
                </div>
                {/* Hidden actual SignInButton positioned behind */}
                <SignInButton className="absolute inset-0 opacity-0 z-20 cursor-pointer" />
              </div>
            </div>
          ) : solanaWallet.address && gameState !== 'CREATING_PROFILE' ? (
            <div className="text-center">
              <div className="text-4xl mb-4">✅</div>
              <h2 className="text-xl font-bold text-green-400 mb-2">Ready to Play</h2>
              <p className="text-gray-300 mb-4">Your wallet is connected and ready</p>

              <div className="mt-3 flex gap-2 justify-center relative">
                {/* Custom button overlay */}
                <div className="relative hover:scale-105 transition-transform duration-200 z-10">
                  <Image 
                    src="/images/Button1.png" 
                    alt="Sign Out" 
                    width={160} 
                    height={50} 
                    className="w-auto h-auto"
                  />
                  <span className="absolute inset-0 flex items-center justify-center text-white font-bold text-xl pointer-events-none">
                    SIGN OUT
                  </span>
                </div>
                {/* Hidden actual SignOutButton positioned behind */}
                <SignOutButton className="absolute inset-0 opacity-0 z-20 cursor-pointer" />
              </div>
            </div>
          ) : gameState === 'CREATING_PROFILE' ? (
            <div className="text-center">
              <div className="animate-spin text-4xl mb-4">⚙️</div>
              <h2 className="text-xl font-bold text-white mb-2">Setting Up Your Profile...</h2>
              <p className="text-gray-300 mb-4">
                {userExists 
                  ? 'Creating your game profile' 
                  : 'Creating your account and profile'}
              </p>
              <p className="text-sm text-gray-400">
                Please confirm the transaction in your wallet
              </p>
            </div>
          ) : (
            <div className="text-center">
              <div className="animate-spin text-4xl mb-4">⚙️</div>
              <h2 className="text-xl font-bold text-white mb-2">Setting Up Your Account</h2>
              <p className="text-gray-300">Please wait while we prepare your resources...</p>
            </div>
          )}
</div>
    </main>
  );
}