'use client'

import React, { useMemo } from "react";
import { CivicAuthProvider } from "@civic/auth-web3/react";
import { ConnectionProvider } from '@solana/wallet-adapter-react';
import { WalletAdapterNetwork } from '@solana/wallet-adapter-base';
import { clusterApiUrl } from '@solana/web3.js';
import { UnifiedWalletProvider } from '@jup-ag/wallet-adapter';
import { useWrappedReownAdapter } from '@jup-ag/jup-mobile-adapter';
import { Adapter } from '@solana/wallet-adapter-base';
import {
  PhantomWalletAdapter,
  SolflareWalletAdapter,
  CoinbaseWalletAdapter,
  TrustWalletAdapter,
} from '@solana/wallet-adapter-wallets';
import { IWalletNotification } from "@jup-ag/wallet-adapter/dist/types/contexts/WalletConnectionProvider";

// Civic Auth configuration
const CIVIC_CLIENT_ID = "928989d4-93f5-4aa2-adbe-8f20c1a26ca7";

export function WalletProviders({ children }: { children: React.ReactNode }) {
  // Configure Solana network
  const network = WalletAdapterNetwork.Devnet;
  const endpoint = useMemo(() => clusterApiUrl(network), [network]);
  const projectId = process.env.NEXT_PUBLIC_REOWN_PROJECT_ID || "default";
  
  const { reownAdapter, jupiterAdapter } = useWrappedReownAdapter({
    appKitOptions: {
      metadata: {
        name: "SolMechs",
        description: "SolMechs Game - Multiplayer Solana Battle Arena",
        url: "https://solmechs.com",
        icons: ["/images/logo.svg"],
      },
      projectId: projectId,
      features: {
        analytics: false,
        socials: ["google", "x", "apple"],
        email: false,
      },
      enableWallets: false,
    },
  });

  const wallets: Adapter[] = useMemo(() => {
    const baseWallets: Adapter[] = [
      new PhantomWalletAdapter(),
      new SolflareWalletAdapter(),
      new CoinbaseWalletAdapter(),
      new TrustWalletAdapter(),
    ];
    
    if (reownAdapter) baseWallets.push(reownAdapter as Adapter);
    if (jupiterAdapter) baseWallets.push(jupiterAdapter as Adapter);

    return baseWallets.filter((item) => item && item.name && item.icon);
  }, [reownAdapter, jupiterAdapter]);

  return (
    <ConnectionProvider endpoint={endpoint}>
      <UnifiedWalletProvider
        wallets={wallets}
        config={{
          autoConnect: true,
          env: 'devnet',
          metadata: {
            name: 'SolMechs',
            description: 'SolMechs Game',
            url: 'https://solmechs.com',
            iconUrls: ['/images/logo.svg']
          },
          notificationCallback: {
            onConnect: (props: IWalletNotification) => {
              console.log('🚀 Jupiter wallet connected:', props);
              console.log('💰 Wallet Address:', props.publicKey?.toString());
              window.dispatchEvent(new CustomEvent('jupiterWalletConnected', { 
                detail: { wallet: (props as any).name || 'Unknown', publicKey: props.publicKey } 
              }));
            },
            onDisconnect: () => console.log('Jupiter wallet disconnected'),
            onConnecting: (props: IWalletNotification) => console.log('Connecting to wallet...', props),
            onNotInstalled: (props: IWalletNotification) => console.log('Wallet not installed:', props),
          },
          walletlistExplanation: {
            href: 'https://station.jup.ag/docs/additional-topics/wallet-list',
          },
          theme: 'jupiter',
          lang: 'en',
        }}
      >
        <CivicAuthProvider 
          clientId={CIVIC_CLIENT_ID}
          autoCreateWallet={true}
          autoConnectEmbeddedWallet={true}
        >
          {children}
        </CivicAuthProvider>
      </UnifiedWalletProvider>
    </ConnectionProvider>
  );
}