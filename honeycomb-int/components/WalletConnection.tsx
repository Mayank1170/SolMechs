'use client'

import React from 'react';
import { useUnifiedWallet, UnifiedWalletButton } from '@jup-ag/wallet-adapter';
import { useUser, SignInButton } from "@civic/auth-web3/react";
import Image from 'next/image';
import CyberpunkButton from './CyberpunkButton';

export default function WalletConnection() {
  const { connected, publicKey, disconnect } = useUnifiedWallet();
  const user = useUser();

  return (
    <div className="flex flex-col items-center space-y-4 w-full">      
      {/* Civic Auth Option */}
      <div className="w-full max-w-xs sm:max-w-sm md:max-w-md">
        {/* {!user?.isAuthenticated ? (
          <div className="relative">
            <div className="relative hover:scale-105 transition-transform duration-200 z-10">
              <Image 
                src="/images/Button1.png" 
                alt="Sign In with Civic" 
                width={250} 
                height={60} 
                className="w-auto h-auto"
              />
              <span className="absolute inset-0 flex items-center justify-center text-white font-bold text-lg pointer-events-none">
                CONNECT WITH CIVIC
              </span>
            </div>
            <SignInButton className="absolute inset-0 opacity-0 z-20 cursor-pointer" />
          </div>
        ) : (
          <div className="text-center p-4 bg-green-500/20 rounded-lg border border-green-500/30">
            <p className="text-green-400 font-semibold">✅ Connected with Civic</p>
          </div>
        )} */}
      </div>

      {/* Divider */}
      {/* <div className="w-full max-w-md flex items-center my-6">
        <div className="flex-1 border-t border-gray-600"></div>
        <span className="px-4 text-gray-400 text-sm">OR</span>
        <div className="flex-1 border-t border-gray-600"></div>
      </div> */}

      {/* Jupiter Unified Wallet Option */}
      <div className="w-full max-w-xs sm:max-w-sm md:max-w-md">
        {!connected ? (
          <div className="w-full flex justify-center">
            <UnifiedWalletButton 
              buttonClassName="!bg-transparent !border-0 !p-0"
              currentUserClassName=""
              overrideContent={
                <div className="button-frame relative hover:scale-105 transition-transform duration-200">
                  <Image 
                    src="/images/Button1.png" 
                    alt="Connect Jupiter Wallet" 
                    width={200} 
                    height={50} 
                    className="w-full h-auto min-w-[160px] max-w-[200px] sm:max-w-[220px] md:max-w-[250px]"
                  />
                  <span className="absolute inset-0 flex items-center justify-center text-white font-bold text-xs sm:text-sm md:text-base lg:text-lg px-2">
                    CONNECT WALLET
                  </span>
                </div>
              }
            />
          </div>
        ) : (
          <div className="text-center ">
          </div>
        )}
      </div>
    </div>
  );
}