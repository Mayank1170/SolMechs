'use client'

import React from "react";
import { CivicAuthProvider } from "@civic/auth-web3/react";

// Civic Auth configuration
const CIVIC_CLIENT_ID = "928989d4-93f5-4aa2-adbe-8f20c1a26ca7";

export function WalletProviders({ children }: { children: React.ReactNode }) {
  return (
    <CivicAuthProvider 
      clientId={CIVIC_CLIENT_ID}
      autoCreateWallet={true}
      autoConnectEmbeddedWallet={true}
    >
      {children}
    </CivicAuthProvider>
  );
}