import { Connection, PublicKey, LAMPORTS_PER_SOL } from '@solana/web3.js'

// Honeycomb protocol RPC endpoint
const HONEYCOMB_RPC_URL = 'https://rpc.test.honeycombprotocol.com'

// Minimum SOL balance threshold (0.1 SOL)
const MIN_SOL_BALANCE = 0.1 * LAMPORTS_PER_SOL

// Default airdrop amount (1 SOL)
const DEFAULT_AIRDROP_AMOUNT = 1 * LAMPORTS_PER_SOL

export class SolAirdropService {
  private connection: Connection

  constructor() {
    this.connection = new Connection(HONEYCOMB_RPC_URL, 'confirmed')
  }

  /**
   * Check if wallet needs SOL (balance is below minimum threshold) - silent operation
   */
  async checkIfNeedsAirdrop(walletAddress: string): Promise<{ needsAirdrop: boolean; currentBalance: number }> {
    try {
      const publicKey = new PublicKey(walletAddress)
      const balance = await this.connection.getBalance(publicKey)
      
      // console.log(`[Background] Wallet balance: ${(balance / LAMPORTS_PER_SOL).toFixed(4)} SOL`)
      
      return {
        needsAirdrop: balance < MIN_SOL_BALANCE,
        currentBalance: balance
      }
    } catch (error) {
      // console.log('[Background] Error checking wallet balance:', error)
      return {
        needsAirdrop: false,
        currentBalance: 0
      }
    }
  }

  /**
   * Request SOL airdrop for a wallet address (silent background operation)
   */
  async requestAirdrop(walletAddress: string, amount: number = DEFAULT_AIRDROP_AMOUNT): Promise<{ success: boolean; signature?: string; error?: string }> {
    try {
      // Silent logging - no user-facing messages
      // console.log(`[Background] Requesting ${amount / LAMPORTS_PER_SOL} SOL for wallet balance maintenance`)
      
      const publicKey = new PublicKey(walletAddress)
      
      // Request airdrop from Honeycomb testnet
      const signature = await this.connection.requestAirdrop(publicKey, amount)
      
      // console.log(`[Background] Airdrop requested, signature: ${signature}`)
      
      // Use a shorter timeout for background operations
      const confirmationResult = await this.waitForConfirmationWithTimeout(signature, 30000) // 30 seconds
      
      if (confirmationResult.confirmed) {
        // console.log(`[Background] Airdrop confirmed successfully`)
        return {
          success: true,
          signature
        }
      } else if (confirmationResult.timeout) {
        // Transaction might still succeed, but we timed out - that's okay for background operation
        // console.log(`[Background] Airdrop confirmation timed out, transaction may still succeed`)
        
        return {
          success: true, // Return success for background operations even on timeout
          signature
        }
      } else {
        // console.log('[Background] Airdrop transaction failed, will retry later if needed')
        return {
          success: false,
          error: 'Background airdrop failed'
        }
      }
      
    } catch (error) {
      // console.log('[Background] Error during balance maintenance:', error instanceof Error ? error.message : 'Unknown error')
      
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Background operation failed'
      }
    }
  }

  /**
   * Wait for transaction confirmation with custom timeout
   */
  private async waitForConfirmationWithTimeout(signature: string, timeoutMs: number): Promise<{ 
    confirmed: boolean; 
    timeout: boolean; 
    error?: any 
  }> {
    return new Promise((resolve) => {
      let timeoutId: NodeJS.Timeout
      let resolved = false

      // Set timeout
      timeoutId = setTimeout(() => {
        if (!resolved) {
          resolved = true
          resolve({ confirmed: false, timeout: true })
        }
      }, timeoutMs)

      // Try to confirm transaction
      this.connection.confirmTransaction(signature, 'confirmed')
        .then((confirmation) => {
          clearTimeout(timeoutId)
          if (!resolved) {
            resolved = true
            if (confirmation.value.err) {
              resolve({ confirmed: false, timeout: false, error: confirmation.value.err })
            } else {
              resolve({ confirmed: true, timeout: false })
            }
          }
        })
        .catch((error) => {
          clearTimeout(timeoutId)
          if (!resolved) {
            resolved = true
            resolve({ confirmed: false, timeout: false, error })
          }
        })
    })
  }

  /**
   * Auto-airdrop SOL if wallet balance is low (silent background operation)
   */
  async autoAirdropIfNeeded(walletAddress: string): Promise<{ airdropped: boolean; signature?: string; error?: string }> {
    try {
      const { needsAirdrop, currentBalance } = await this.checkIfNeedsAirdrop(walletAddress)
      
      if (!needsAirdrop) {
        // console.log(`[Background] Wallet has sufficient balance: ${(currentBalance / LAMPORTS_PER_SOL).toFixed(4)} SOL`)
        return { airdropped: false }
      }
      
      // console.log(`[Background] Low balance detected (${(currentBalance / LAMPORTS_PER_SOL).toFixed(4)} SOL), performing background maintenance...`)
      
      const result = await this.requestAirdrop(walletAddress)
      
      return {
        airdropped: result.success,
        signature: result.signature,
        error: result.error
      }
    } catch (error) {
      console.log('[Background] Error in balance maintenance:', error)
      return {
        airdropped: false,
        error: error instanceof Error ? error.message : 'Background maintenance failed'
      }
    }
  }

  /**
   * Get current wallet balance in SOL
   */
  async getBalance(walletAddress: string): Promise<number> {
    try {
      const publicKey = new PublicKey(walletAddress)
      const balance = await this.connection.getBalance(publicKey)
      return balance / LAMPORTS_PER_SOL
    } catch (error) {
      console.error('❌ Error getting balance:', error)
      return 0
    }
  }

  /**
   * Check transaction status manually
   */
  async checkTransactionStatus(signature: string): Promise<{ confirmed: boolean; error?: string }> {
    try {
      // console.log(`🔍 Checking transaction status: ${signature}`)
      const status = await this.connection.getSignatureStatus(signature)
      
      if (status?.value?.confirmationStatus === 'confirmed' || status?.value?.confirmationStatus === 'finalized') {
        // console.log(`✅ Transaction confirmed: ${signature}`)
        return { confirmed: true }
      } else if (status?.value?.err) {
        // console.log(`❌ Transaction failed: ${signature}`, status.value.err)
        return { confirmed: false, error: 'Transaction failed' }
      } else {
        // console.log(`⏳ Transaction still processing: ${signature}`)
        return { confirmed: false, error: 'Transaction still processing' }
      }
    } catch (error) {
      console.error('❌ Error checking transaction status:', error)
      return { confirmed: false, error: error instanceof Error ? error.message : 'Unknown error' }
    }
  }
}

// Export singleton instance
export const solAirdropService = new SolAirdropService()

// Export utility functions
export const checkWalletBalance = (walletAddress: string) => 
  solAirdropService.checkIfNeedsAirdrop(walletAddress)

export const requestSolAirdrop = (walletAddress: string, amount?: number) => 
  solAirdropService.requestAirdrop(walletAddress, amount)

export const autoAirdropSol = (walletAddress: string) => 
  solAirdropService.autoAirdropIfNeeded(walletAddress)

export const checkTransactionStatus = (signature: string) =>
  solAirdropService.checkTransactionStatus(signature)

// Debug helper - expose to window for console debugging
if (typeof window !== 'undefined') {
  ;(window as any).debugSolAirdrop = {
    checkBalance: checkWalletBalance,
    requestAirdrop: requestSolAirdrop,
    checkTransaction: checkTransactionStatus,
    service: solAirdropService
  }
}