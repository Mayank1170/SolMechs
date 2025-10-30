
const activeSessions = new Map<string, {
  sessionId: string;
  walletAddress: string;
  startTime: number;
  status: 'playing' | 'completed';
  score?: number;
  won?: boolean;
}>();

export class SimpleGameSession {


  static startSession(walletAddress: string): string {
    const sessionId = `game_${Date.now()}_${Math.random().toString(36).substring(7)}`;

    activeSessions.set(sessionId, {
      sessionId,
      walletAddress,
      startTime: Date.now(),
      status: 'playing'
    });

    console.log('🎮 Game session started:', sessionId);
    return sessionId;
  }

  static endSession(sessionId: string, won: boolean, score: number) {
    const session = activeSessions.get(sessionId);

    if (!session) {
      console.error('❌ Session not found:', sessionId);
      return null;
    }

    const duration = Date.now() - session.startTime;

    session.status = 'completed';
    session.won = won;
    session.score = score;

    console.log('🏆 Game session completed:', {
      sessionId,
      wallet: session.walletAddress,
      won,
      score,
      duration: `${(duration / 1000).toFixed(1)}s`
    });

    return {
      sessionId: session.sessionId,
      walletAddress: session.walletAddress,
      won: session.won,
      score: session.score,
      duration,
      timestamp: Date.now()
    };
  }

  static getSession(sessionId: string) {
    return activeSessions.get(sessionId);
  }

  static getWalletSessions(walletAddress: string) {
    return Array.from(activeSessions.values())
      .filter(s => s.walletAddress === walletAddress);
  }
}
