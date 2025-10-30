import { Connection, PublicKey, LAMPORTS_PER_SOL } from '@solana/web3.js'

const HONEYCOMB_RPC_URL = 'https://rpc.test.honeycombprotocol.com'

const MIN_SOL_BALANCE = 0.1 * LAMPORTS_PER_SOL

const DEFAULT_AIRDROP_AMOUNT = 1 * LAMPORTS_PER_SOL

export class SolAirdropService {
  private connection: Connection

  constructor() {
    this.connection = new Connection(HONEYCOMB_RPC_URL, 'confirmed')
  }

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

  async requestAirdrop(walletAddress: string, amount: number = DEFAULT_AIRDROP_AMOUNT): Promise<{ success: boolean; signature?: string; error?: string }> {
    try {
      const publicKey = new PublicKey(walletAddress)
      const signature = await this.connection.requestAirdrop(publicKey, amount)
      
      const confirmationResult = await this.waitForConfirmationWithTimeout(signature, 30000) // 30 seconds
      
      if (confirmationResult.confirmed) {
        // console.log(`[Background] Airdrop confirmed successfully`)
        return {
          success: true,
          signature
        }
      } else if (confirmationResult.timeout) {
        return {
          success: true,
          signature
        }
      } else {
        return {
          success: false,
          error: 'Background airdrop failed'
        }
      }
      
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Background operation failed'
      }
    }
  }

  private async waitForConfirmationWithTimeout(signature: string, timeoutMs: number): Promise<{ 
    confirmed: boolean; 
    timeout: boolean; 
    error?: any 
  }> {
    return new Promise((resolve) => {
      let timeoutId: NodeJS.Timeout
      let resolved = false

      timeoutId = setTimeout(() => {
        if (!resolved) {
          resolved = true
          resolve({ confirmed: false, timeout: true })
        }
      }, timeoutMs)

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

  async autoAirdropIfNeeded(walletAddress: string): Promise<{ airdropped: boolean; signature?: string; error?: string }> {
    try {
      const { needsAirdrop, currentBalance } = await this.checkIfNeedsAirdrop(walletAddress)
      
      if (!needsAirdrop) {
        return { airdropped: false }
      }
      
      const result = await this.requestAirdrop(walletAddress)
      
      return {
        airdropped: result.success,
        signature: result.signature,
        error: result.error
      }
    } catch (error) {
      return {
        airdropped: false,
        error: error instanceof Error ? error.message : 'Background maintenance failed'
      }
    }
  }

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

  async checkTransactionStatus(signature: string): Promise<{ confirmed: boolean; error?: string }> {
    try {
      const status = await this.connection.getSignatureStatus(signature)
      
      if (status?.value?.confirmationStatus === 'confirmed' || status?.value?.confirmationStatus === 'finalized') {
        return { confirmed: true }
      } else if (status?.value?.err) {
        return { confirmed: false, error: 'Transaction failed' }
      } else {
        return { confirmed: false, error: 'Transaction still processing' }
      }
    } catch (error) {
      console.error('❌ Error checking transaction status:', error)
      return { confirmed: false, error: error instanceof Error ? error.message : 'Unknown error' }
    }
  }
}

export const solAirdropService = new SolAirdropService()

export const checkWalletBalance = (walletAddress: string) => 
  solAirdropService.checkIfNeedsAirdrop(walletAddress)

export const requestSolAirdrop = (walletAddress: string, amount?: number) => 
  solAirdropService.requestAirdrop(walletAddress, amount)

export const autoAirdropSol = (walletAddress: string) => 
  solAirdropService.autoAirdropIfNeeded(walletAddress)

export const checkTransactionStatus = (signature: string) =>
  solAirdropService.checkTransactionStatus(signature)

if (typeof window !== 'undefined') {
  ;(window as any).debugSolAirdrop = {
    checkBalance: checkWalletBalance,
    requestAirdrop: requestSolAirdrop,
    checkTransaction: checkTransactionStatus,
    service: solAirdropService
  }
}