'use client'

import { useEffect, Suspense } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';

function GameRedirectContent() {
  const searchParams = useSearchParams();
  const router = useRouter();

  useEffect(() => {
    const gameId = searchParams?.get('gameId');

    if (gameId) {
      // Redirect to Unity page with full parameters
      router.replace(`/unity?gameId=${gameId}&scene=8&role=streamer`);
    } else {
      // No gameId, redirect to arena
      router.replace('/arena');
    }
  }, [searchParams, router]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-900">
      <div className="text-center">
        <div className="animate-spin text-6xl mb-4">⚙️</div>
        <p className="text-white text-xl">Loading game...</p>
      </div>
    </div>
  );
}

export default function GameRedirect() {
  return (
    <Suspense fallback={
      <div className="flex min-h-screen items-center justify-center bg-gray-900">
        <div className="text-center">
          <div className="animate-spin text-6xl mb-4">⚙️</div>
          <p className="text-white text-xl">Loading...</p>
        </div>
      </div>
    }>
      <GameRedirectContent />
    </Suspense>
  );
}
