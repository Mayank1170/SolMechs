'use client'

import Script from 'next/script'
import { useEffect, useRef } from 'react'

export default function UnityPage() {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const progressBarRef = useRef<HTMLDivElement>(null)
  const loadingBarRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const initUnity = () => {
      if (!canvasRef.current || !progressBarRef.current || !loadingBarRef.current) return

      const canvas = canvasRef.current
      const loadingBar = loadingBarRef.current
      const progressBarFull = progressBarRef.current

      const buildUrl = "/unity/Build"
      const config = {
        dataUrl: buildUrl + "/webgl-build.data",
        frameworkUrl: buildUrl + "/webgl-build.framework.js",
        codeUrl: buildUrl + "/webgl-build.wasm",
        streamingAssetsUrl: "StreamingAssets",
        companyName: "DefaultCompany",
        productName: "MechBattle",
        productVersion: "1.0",
      }

      loadingBar.style.display = "block"

      // @ts-ignore - Unity's createUnityInstance function
      if (typeof createUnityInstance !== 'undefined') {
        // @ts-ignore
        createUnityInstance(canvas, config, (progress: number) => {
          progressBarFull.style.width = 100 * progress + "%"
        }).then((unityInstance: any) => {
          loadingBar.style.display = "none"
          
          console.log("🎮 PSG1 WebGL Game loaded successfully!")
          console.log("🌐 PSG1 Template active - SolMechs × Play Solana")
          
          // Make Unity instance globally available
          ;(window as any).unityInstance = unityInstance
          
          // PSG1 JavaScript Functions for Unity WebGL
          ;(window as any).GetWalletAddress = function() {
            console.log("🔗 PSG1 Template: Unity requesting wallet address")
            if ((window as any).parent && (window as any).parent.web3Bridge && (window as any).parent.web3Bridge.getWalletAddress) {
              const wallet = (window as any).parent.web3Bridge.getWalletAddress()
              console.log(`🔗 PSG1 Template returning wallet: ${wallet}`)
              return wallet
            }
            console.log("🔗 PSG1 Template returning demo wallet")
            return "PSG1_DEMO_WALLET"
          }
          
          ;(window as any).GetUserBalance = function() {
            console.log("💰 PSG1 Template: Unity requesting user balance")
            if ((window as any).parent && (window as any).parent.web3Bridge && (window as any).parent.web3Bridge.getBalance) {
              console.log("💰 PSG1 Template returning balance: 2000")
              return "2000"
            }
            console.log("💰 PSG1 Template returning demo balance")
            return "1000"
          }
          
          ;(window as any).LogToJS = function(message: string) {
            console.log("🎮 PSG1 TEMPLATE MESSAGE:", message)
            
            // Update page title based on PSG1 state
            if (message === "PSG1_SCREEN_ACTIVE") {
              document.title = "SolMechs × Play Solana (PSG1) - PSG1 Mode"
            } else if (message === "SOLMECHS_GAME_ACTIVE") {
              document.title = "SolMechs × Play Solana (PSG1) - Game Mode"
            }
            
            // Handle NFT mint requests
            if (message.startsWith("NFT_MINT_REQUEST:")) {
              const nftData = message.replace("NFT_MINT_REQUEST:", "")
              console.log("🎨 PSG1 Template NFT Mint:", nftData)
              
              if ((window as any).parent && (window as any).parent.web3Bridge && (window as any).parent.web3Bridge.handleNFTMintRequest) {
                ;(window as any).parent.web3Bridge.handleNFTMintRequest(nftData)
              } else {
                alert(`🎨 PSG1 × SolMechs NFT!\n\nMinting via Play Solana:\n${nftData}`)
              }
            }
          }
          
          console.log("✅ PSG1 Template: All bridge functions ready!")
            
        }).catch((message: string) => {
          console.error("PSG1 WebGL Load Error:", message)
          alert("PSG1 WebGL Load Error: " + message)
        })
      } else {
        console.error("createUnityInstance is not defined")
      }
    }

    // Initialize Unity when the loader script is loaded
    const timer = setTimeout(initUnity, 100)
    return () => clearTimeout(timer)
  }, [])

  return (
    <div style={{ 
      margin: 0, 
      padding: 0, 
      background: "linear-gradient(135deg, #1a1a2e, #16213e, #0f3460)",
      backgroundAttachment: "fixed",
      fontFamily: "Arial, sans-serif",
      display: "flex",
      justifyContent: "center",
      alignItems: "center",
      minHeight: "100vh",
      overflow: "hidden"
    }}>
      <Script
        src="/unity/Build/webgl-build.loader.js"
        strategy="beforeInteractive"
        onError={(e) => {
          console.error("Failed to load Unity loader script:", e)
        }}
      />
      
      <div style={{
        position: "relative",
        width: "1040px",
        height: "880px",
        backgroundImage: "url('/unity/OverlayPSG1.png')",
        backgroundRepeat: "no-repeat",
        backgroundPosition: "center center",
        backgroundSize: "contain",
        display: "flex",
        justifyContent: "center",
        alignItems: "center"
      }}>
        <div style={{
          width: "900px",
          height: "760px",
          background: "#000",
          overflow: "hidden",
          position: "relative"
        }}>
          <div 
            id="unity-container"
            style={{
              width: "100%",
              height: "100%",
              position: "relative"
            }}>
            <canvas 
              ref={canvasRef}
              id="unity-canvas"
              width={420} 
              height={280}
              style={{
                width: "100%",
                height: "100%"
              }}
            />
            <div 
              ref={loadingBarRef}
              id="unity-loading-bar"
              style={{
                position: "absolute",
                left: "50%",
                top: "50%",
                transform: "translate(-50%, -50%)",
                display: "none"
              }}
            >
              <div 
                id="unity-logo"
                style={{
                  width: "154px",
                  height: "130px",
                  backgroundImage: "url('/unity/unity-logo-dark.png')",
                  backgroundRepeat: "no-repeat",
                  backgroundPosition: "center"
                }} />
              <div 
                id="unity-progress-bar-empty"
                style={{
                  width: "141px",
                  height: "18px",
                  marginTop: "10px",
                  marginLeft: "6.5px",
                  backgroundImage: "url('/unity/progress-bar-empty-dark.png')",
                  backgroundRepeat: "no-repeat",
                  backgroundPosition: "center"
                }}>
                <div 
                  ref={progressBarRef}
                  id="unity-progress-bar-full"
                  style={{
                    width: "0%",
                    height: "18px",
                    backgroundImage: "url('/unity/progress-bar-full-dark.png')",
                    backgroundRepeat: "no-repeat",
                    backgroundPosition: "center"
                  }}
                />
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <style jsx global>{`
        @media screen and (max-width: 1280px) {
          .psg1-container {
            transform: scale(0.8);
          }
        }
        
        @media screen and (max-width: 1024px) {
          .psg1-container {
            transform: scale(0.6);
          }
        }
        
        @media screen and (max-width: 768px) {
          .psg1-container {
            transform: scale(0.5);
          }
        }
      `}</style>
    </div>
  )
}