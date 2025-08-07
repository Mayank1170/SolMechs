'use client'

import React, { useMemo } from "react";
import {
  ConnectionProvider,
  WalletProvider,
} from "@solana/wallet-adapter-react";
import {
  PhantomWalletAdapter,
  SolflareWalletAdapter,
} from "@solana/wallet-adapter-wallets";
import {
  WalletModalProvider,
} from "@solana/wallet-adapter-react-ui";
import { CivicAuthProvider } from "@civic/auth-web3";
import { MultichainWalletProvider, MultichainConnectButton } from "@civic/multichain-connect-react-core";
import { SolanaWalletAdapterConfig } from "@civic/multichain-connect-react-solana-wallet-adapter";

import "@solana/wallet-adapter-react-ui/styles.css";

// Civic Auth configuration
const CIVIC_CLIENT_ID = "928989d4-93f5-4aa2-adbe-8f20c1a26ca7";

export function WalletProviders({ children }: { children: React.ReactNode }) {
  const network = "https://rpc.test.honeycombprotocol.com";
  const endpoint = useMemo(() => network, [network]);

  const wallets = useMemo(
    () => [
      new PhantomWalletAdapter(),
      new SolflareWalletAdapter(),
    ],
    []
  );

  // Solana chains configuration for Civic
  const solanaChains = [
    {
      name: "Honeycomb Testnet",
      rpcEndpoint: network,
    },
  ];

  return (
    <CivicAuthProvider 
      clientId={CIVIC_CLIENT_ID}
      autoCreateWallet={true}
      autoConnectEmbeddedWallet={true}
    >
      <MultichainWalletProvider>
        <SolanaWalletAdapterConfig 
          chains={solanaChains}
          adapters={wallets}
        >
          <ConnectionProvider endpoint={endpoint}>
            <WalletProvider wallets={wallets} autoConnect>
              <WalletModalProvider>
                <div className="flex gap-2 mb-4">
                  <MultichainConnectButton />
                </div>
                {children}
              </WalletModalProvider>
            </WalletProvider>
          </ConnectionProvider>
        </SolanaWalletAdapterConfig>
      </MultichainWalletProvider>
    </CivicAuthProvider>
  );
}