'use client'

import React, { useState } from "react";
import { useVorldAuth } from "./providers";
import Image from "next/image";
import Link from "next/link";

export default function Home() {
  const { user, isAuthenticated, isLoading, login, verifyOTP, logout } = useVorldAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [otp, setOtp] = useState('');
  const [showOtp, setShowOtp] = useState(false);
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setError('');
    setIsSubmitting(true);

    console.log('Login started with email:', email);

    try {
      const result = await login(email, password);
      console.log('Login result:', result);

      if (result.success) {
        if (result.requiresOTP) {
          console.log('OTP required');
          setShowOtp(true);
        } else {
          console.log('Login successful, no OTP required');
        }
        // If login successful without OTP, user will be redirected automatically
      } else {
        console.error('Login failed:', result.error);
        setError(result.error || 'Login failed');
      }
    } catch (err: any) {
      console.error('Login exception:', err);
      setError(err.message || 'Login failed');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOtpVerification = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      const result = await verifyOTP(email, otp);

      if (result.success) {
        setShowOtp(false);
        setOtp('');
        // User will be redirected automatically
      } else {
        setError(result.error || 'OTP verification failed');
      }
    } catch (err: any) {
      setError(err.message || 'OTP verification failed');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleLogout = async () => {
    await logout();
    setEmail('');
    setPassword('');
    setOtp('');
    setShowOtp(false);
    setError('');
  };

  // Show loading state
  if (isLoading && !user && !email) {
    return (
      <main className="flex min-h-screen flex-col items-center justify-center p-8">
        <div className="animate-spin text-4xl mb-4">⚙️</div>
        <p className="text-white">Loading...</p>
      </main>
    );
  }

  // If authenticated, show main menu
  if (isAuthenticated && user) {
    return (
      <main className="flex min-h-screen flex-col items-center justify-center p-8">
        <div className="w-full max-w-4xl">
          {/* Header */}
          <div className="flex flex-col items-center mb-8">
            <Image src="/images/logo.svg" alt="SolMechs Logo" width={200} height={200} className="w-64 h-32" />
          </div>

          {/* User Info Card */}
          <div className="mb-6 p-6 bg-gray-800/80 rounded-lg border border-cyan-500/30">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-2xl font-bold text-cyan-400 mb-2">Welcome, {user.username}!</h2>
                <p className="text-gray-300">Email: {user.email}</p>
                <p className="text-gray-300">Connected Accounts: {user.totalConnectedAccounts}</p>
                <div className="flex gap-2 mt-2">
                  {user.authMethods.map((method) => (
                    <span
                      key={method}
                      className="px-3 py-1 bg-cyan-600/20 border border-cyan-500/50 text-cyan-300 rounded-full text-sm"
                    >
                      {method.toUpperCase()}
                    </span>
                  ))}
                </div>
              </div>
              <button
                onClick={handleLogout}
                className="px-6 py-3 bg-red-600 hover:bg-red-700 text-white rounded-lg transition-colors"
              >
                SIGN OUT
              </button>
            </div>
          </div>

          {/* Main Menu */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Viewer Portal Card */}
            <Link href="/arena/viewer">
              <div className="p-8 bg-gradient-to-br from-blue-900/40 to-indigo-900/40 rounded-lg border-2 border-blue-500/50 hover:border-blue-400 transition-all hover:scale-105 cursor-pointer">
                <div className="text-5xl mb-4">👥</div>
                <h3 className="text-2xl font-bold text-blue-300 mb-2 font-mek">VIEWER PORTAL</h3>
                <p className="text-gray-300">Join a streamer&apos;s game and support them</p>
              </div>
            </Link>

            {/* Arena Arcade Card */}
            <Link href="/arena">
              <div className="p-8 bg-gradient-to-br from-purple-900/40 to-pink-900/40 rounded-lg border-2 border-purple-500/50 hover:border-purple-400 transition-all hover:scale-105 cursor-pointer">
                <div className="text-5xl mb-4">🎪</div>
                <h3 className="text-2xl font-bold text-purple-300 mb-2 font-mek">ARENA ARCADE</h3>
                <p className="text-gray-300">Stream integration & viewer boosts</p>
              </div>
            </Link>

            {/* Profile Card */}
            <Link href="/profile">
              <div className="p-8 bg-gradient-to-br from-green-900/40 to-emerald-900/40 rounded-lg border-2 border-green-500/50 hover:border-green-400 transition-all hover:scale-105 cursor-pointer">
                <div className="text-5xl mb-4">👤</div>
                <h3 className="text-2xl font-bold text-green-300 mb-2 font-mek">PROFILE</h3>
                <p className="text-gray-300">View your stats and settings</p>
              </div>
            </Link>

            {/* About Card */}
            <div className="p-8 bg-gradient-to-br from-gray-800/40 to-gray-900/40 rounded-lg border-2 border-gray-500/50">
              <div className="text-5xl mb-4">ℹ️</div>
              <h3 className="text-2xl font-bold text-gray-300 mb-2 font-mek">ABOUT</h3>
              <p className="text-gray-400">Solana blockchain-powered mech battles</p>
            </div>
          </div>
        </div>
      </main>
    );
  }

  // Login Form
  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-8">
      <div className="w-full max-w-md">
        {/* Header */}
        <div className="flex flex-col items-center mb-8">
          <Image src="/images/logo.svg" alt="SolMechs Logo" width={250} height={250} className="w-64 h-32" />
          {/* <h1 className="text-3xl font-bold text-white mt-4 font-mek">SOLMECHS</h1> */}
        </div>

        {/* Login Box */}
        <div className="p-8 bg-gray-800/90 rounded-lg border-2 border-cyan-500/30 backdrop-blur-sm">
          {!showOtp ? (
            <>
              <h2 className="text-2xl font-bold text-cyan-400 mb-6 text-center">SIGN IN</h2>

              {error && (
                <div className="mb-4 p-3 bg-red-900/50 border border-red-500 rounded text-red-200 text-sm">
                  {error}
                </div>
              )}

              <form onSubmit={handleLogin} className="space-y-4">
                <div>
                  <label className="block text-gray-300 mb-2 text-sm">EMAIL</label>
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="w-full p-3 bg-gray-900 border border-cyan-500/30 text-white rounded focus:border-cyan-400 focus:outline-none"
                    placeholder="your@email.com"
                    required
                    disabled={isSubmitting}
                  />
                </div>

                <div>
                  <label className="block text-gray-300 mb-2 text-sm">PASSWORD</label>
                  <input
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full p-3 bg-gray-900 border border-cyan-500/30 text-white rounded focus:border-cyan-400 focus:outline-none"
                    placeholder="••••••••"
                    required
                    disabled={isSubmitting}
                  />
                </div>

                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="w-full py-3 bg-cyan-600 hover:bg-cyan-700 disabled:bg-gray-600 text-white font-bold rounded transition-colors disabled:cursor-not-allowed"
                >
                  {isSubmitting ? 'SIGNING IN...' : 'SIGN IN'}
                </button>
              </form>

              <div className="mt-6 text-center text-gray-400 text-sm">
                <p>Don&apos;t have an account?</p>
                <a href="https://access.thevorld.com" target="_blank" rel="noopener noreferrer" className="text-cyan-400 hover:text-cyan-300">
                  Create account on Vorld →
                </a>
              </div>
            </>
          ) : (
            <>
              <h2 className="text-2xl font-bold text-cyan-400 mb-6 text-center">VERIFY OTP</h2>

              <p className="text-gray-300 mb-4 text-center text-sm">
                Enter the 6-digit code sent to<br />
                <span className="text-cyan-400">{email}</span>
              </p>

              {error && (
                <div className="mb-4 p-3 bg-red-900/50 border border-red-500 rounded text-red-200 text-sm">
                  {error}
                </div>
              )}

              <form onSubmit={handleOtpVerification} className="space-y-4">
                <div>
                  <input
                    type="text"
                    value={otp}
                    onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                    className="w-full p-4 bg-gray-900 border border-cyan-500/30 text-white rounded text-center text-2xl tracking-widest focus:border-cyan-400 focus:outline-none"
                    placeholder="000000"
                    maxLength={6}
                    required
                    disabled={isSubmitting}
                  />
                </div>

                <button
                  type="submit"
                  disabled={isSubmitting || otp.length !== 6}
                  className="w-full py-3 bg-cyan-600 hover:bg-cyan-700 disabled:bg-gray-600 text-white font-bold rounded transition-colors disabled:cursor-not-allowed"
                >
                  {isSubmitting ? 'VERIFYING...' : 'VERIFY OTP'}
                </button>

                <button
                  type="button"
                  onClick={() => {
                    setShowOtp(false);
                    setOtp('');
                    setError('');
                  }}
                  className="w-full py-2 text-gray-400 hover:text-white transition-colors text-sm"
                >
                  ← Back to login
                </button>
              </form>
            </>
          )}
        </div>
      </div>
    </main>
  );
}
