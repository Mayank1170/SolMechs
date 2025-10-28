'use client'

import React, { createContext, useContext, useState, useEffect, useMemo } from "react";
import { ConnectionProvider } from '@solana/wallet-adapter-react';
import { WalletAdapterNetwork } from '@solana/wallet-adapter-base';
import { clusterApiUrl } from '@solana/web3.js';
import { VorldAuthService, UserProfile } from "../utils/authService";

// Vorld Auth Context
interface VorldAuthContextType {
  user: UserProfile | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<{ success: boolean; requiresOTP?: boolean; error?: string }>;
  verifyOTP: (email: string, otp: string) => Promise<{ success: boolean; error?: string }>;
  logout: () => Promise<void>;
  refreshProfile: () => Promise<void>;
  authService: VorldAuthService;
}

const VorldAuthContext = createContext<VorldAuthContextType | undefined>(undefined);

export function useVorldAuth() {
  const context = useContext(VorldAuthContext);
  if (!context) {
    throw new Error('useVorldAuth must be used within VorldAuthProvider');
  }
  return context;
}

function VorldAuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const authService = useMemo(() => new VorldAuthService(), []);

  // Load user profile on mount
  useEffect(() => {
    loadUserProfile();
  }, []);

  const loadUserProfile = async () => {
    console.log('Provider: Loading user profile...');
    setIsLoading(true);
    try {
      const isAuth = authService.isAuthenticated();
      console.log('Provider: Is authenticated?', isAuth);

      if (isAuth) {
        const result = await authService.getProfile();
        console.log('Provider: Profile result:', result);
        console.log('Provider: Profile data structure:', JSON.stringify(result.data, null, 2));

        if (result.success && result.data) {
          // Check if profile is nested or at root level
          const userProfile = result.data.profile || result.data;
          console.log('Provider: Setting user:', userProfile);
          setUser(userProfile);
        } else {
          console.log('Provider: Profile fetch failed, setting user to null');
          setUser(null);
        }
      } else {
        console.log('Provider: Not authenticated, setting user to null');
        setUser(null);
      }
    } catch (error) {
      console.error('Provider: Failed to load user profile:', error);
      setUser(null);
    } finally {
      setIsLoading(false);
      console.log('Provider: Profile loading complete');
    }
  };

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    try {
      const result = await authService.loginWithEmail(email, password);

      if (result.success && result.data) {
        if (result.data.requiresOTP) {
          setIsLoading(false);
          return { success: true, requiresOTP: true };
        } else {
          // Login successful, load profile
          await loadUserProfile();
          return { success: true };
        }
      } else {
        setIsLoading(false);
        return { success: false, error: result.error };
      }
    } catch (error: any) {
      setIsLoading(false);
      return { success: false, error: error.message || 'Login failed' };
    }
  };

  const verifyOTP = async (email: string, otp: string) => {
    setIsLoading(true);
    try {
      const result = await authService.verifyOTP(email, otp);

      if (result.success) {
        // OTP verified, load profile
        await loadUserProfile();
        return { success: true };
      } else {
        setIsLoading(false);
        return { success: false, error: result.error };
      }
    } catch (error: any) {
      setIsLoading(false);
      return { success: false, error: error.message || 'OTP verification failed' };
    }
  };

  const logout = async () => {
    await authService.logout();
    setUser(null);
  };

  const refreshProfile = async () => {
    await loadUserProfile();
  };

  const value = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    verifyOTP,
    logout,
    refreshProfile,
    authService
  };

  return (
    <VorldAuthContext.Provider value={value}>
      {children}
    </VorldAuthContext.Provider>
  );
}

export function WalletProviders({ children }: { children: React.ReactNode }) {
  // Configure Solana network
  const network = WalletAdapterNetwork.Devnet;
  const endpoint = useMemo(() => clusterApiUrl(network), [network]);

  return (
    <ConnectionProvider endpoint={endpoint}>
      <VorldAuthProvider>
        {children}
      </VorldAuthProvider>
    </ConnectionProvider>
  );
}
