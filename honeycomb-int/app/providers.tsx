'use client'

import React, { useMemo } from "react";
import { CivicAuthProvider } from "@civic/auth-web3/react";
import { ConnectionProvider } from '@solana/wallet-adapter-react';
import { WalletAdapterNetwork } from '@solana/wallet-adapter-base';
import { clusterApiUrl } from '@solana/web3.js';

const CIVIC_CLIENT_ID = "928989d4-93f5-4aa2-adbe-8f20c1a26ca7";

export function WalletProviders({ children }: { children: React.ReactNode }) {
  const network = WalletAdapterNetwork.Devnet;
  const endpoint = useMemo(() => clusterApiUrl(network), [network]);

  return (
    <ConnectionProvider endpoint={endpoint}>
      <CivicAuthProvider 
        clientId={CIVIC_CLIENT_ID}
        autoCreateWallet={true}
        autoConnectEmbeddedWallet={true}
      >
        {children}
      </CivicAuthProvider>
    </ConnectionProvider>
  );
}