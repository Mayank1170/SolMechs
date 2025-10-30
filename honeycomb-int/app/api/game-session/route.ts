

import { NextRequest, NextResponse } from 'next/server';
import { SimpleGameSession } from '@/utils/simpleGameSession';

export async function POST(request: NextRequest) {
  try {
    const { walletAddress } = await request.json();

    if (!walletAddress) {
      return NextResponse.json({
        success: false,
        error: 'walletAddress is required'
      }, { status: 400 });
    }

    const sessionId = SimpleGameSession.startSession(walletAddress);

    return NextResponse.json({
      success: true,
      data: { sessionId }
    });

  } catch (error) {
    console.error('Error starting game session:', error);
    return NextResponse.json({
      success: false,
      error: 'Failed to start game session'
    }, { status: 500 });
  }
}

export async function PATCH(request: NextRequest) {
  try {
    const { sessionId, won, score } = await request.json();

    if (!sessionId || won === undefined || score === undefined) {
      return NextResponse.json({
        success: false,
        error: 'sessionId, won, and score are required'
      }, { status: 400 });
    }

    const result = SimpleGameSession.endSession(sessionId, won, score);

    if (!result) {
      return NextResponse.json({
        success: false,
        error: 'Session not found'
      }, { status: 404 });
    }

    return NextResponse.json({
      success: true,
      data: result
    });

  } catch (error) {
    console.error('Error ending game session:', error);
    return NextResponse.json({
      success: false,
      error: 'Failed to end game session'
    }, { status: 500 });
  }
}

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const sessionId = searchParams.get('sessionId');
    const wallet = searchParams.get('wallet');

    if (sessionId) {
      const session = SimpleGameSession.getSession(sessionId);
      if (!session) {
        return NextResponse.json({
          success: false,
          error: 'Session not found'
        }, { status: 404 });
      }
      return NextResponse.json({
        success: true,
        data: session
      });
    }

    if (wallet) {
      const sessions = SimpleGameSession.getWalletSessions(wallet);
      return NextResponse.json({
        success: true,
        data: sessions
      });
    }

    return NextResponse.json({
      success: false,
      error: 'Provide either sessionId or wallet parameter'
    }, { status: 400 });

  } catch (error) {
    console.error('Error getting session:', error);
    return NextResponse.json({
      success: false,
      error: 'Failed to get session'
    }, { status: 500 });
  }
}
