'use client'

import { useEffect, useRef } from 'react'

// Declare Unity global function
declare global {
  function createUnityInstance(canvas: HTMLCanvasElement, config: any, onProgress?: (progress: number) => void): Promise<any>
}

export default function UnityPage() {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const progressBarRef = useRef<HTMLDivElement>(null)
  const loadingBarRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const loadUnityScript = () => {
      return new Promise<void>((resolve, reject) => {
        // Check if already loaded
        if (typeof (window as any).createUnityInstance === 'function' || 
            typeof createUnityInstance !== 'undefined') {
          // console.log('✅ createUnityInstance already available')
          resolve()
          return
        }

        // console.log('🔄 Loading Unity loader script...')
        // console.log('🌐 Current URL:', window.location.href)
        // console.log('🗂️ Script path:', '/unity/Build/webgl-build.loader.js')
        
        const script = document.createElement('script')
        const scriptSrc = '/unity/Build/webgl-build.loader.js'
        
        // console.log('🔗 Using script source:', scriptSrc)
        script.src = scriptSrc
        
        // Conservative script loading
        script.async = true
        script.defer = false
        
        script.onload = () => {
          // console.log('✅ Unity loader script loaded successfully')
          
          // Wait a bit for script execution and check multiple ways
          setTimeout(() => {
            // console.log('🔍 Checking for createUnityInstance availability...')
            // console.log('- window.createUnityInstance:', typeof (window as any).createUnityInstance)
            // console.log('- global createUnityInstance:', typeof (window as any).createUnityInstance)
            
            // Try to access the function
            try {
              if (typeof createUnityInstance !== 'undefined') {
                // console.log('✅ Found createUnityInstance in global scope')
                resolve()
              } else if (typeof (window as any).createUnityInstance === 'function') {
                // console.log('✅ Found createUnityInstance on window')
                resolve()
              } else {
                console.error('❌ createUnityInstance not found after script load')
                // console.log('Available globals:', Object.keys(window).filter(k => k.includes('Unity') || k.includes('create')))
                reject(new Error('createUnityInstance not available after script load'))
              }
            } catch (e) {
              console.error('❌ Error checking createUnityInstance:', e)
              reject(e)
            }
          }, 200) // Increased timeout for production
        }
        
        script.onerror = (error) => {
          console.error('❌ Failed to load Unity loader script:', error)
          // console.log('🔍 Attempting to fetch script directly to check if it exists...')
          
          // Try to fetch the script to see if it exists
          fetch('/unity/Build/webgl-build.loader.js')
            .then(response => {
              // console.log('📄 Script fetch response:', response.status, response.statusText)
              if (!response.ok) {
                reject(new Error(`Script not found: ${response.status} ${response.statusText}`))
              } else {
                reject(new Error('Script exists but failed to load'))
              }
            })
            .catch(fetchError => {
              console.error('❌ Script fetch failed:', fetchError)
              reject(new Error(`Script not accessible: ${fetchError.message}`))
            })
        }
        
        // console.log('📤 Appending script to head...')
        document.head.appendChild(script)
      })
    }

    const initUnity = async () => {
      if (!canvasRef.current || !progressBarRef.current || !loadingBarRef.current) return

      try {
        // Ensure Unity loader is available
        await loadUnityScript()

        const canvas = canvasRef.current
        const loadingBar = loadingBarRef.current
        const progressBarFull = progressBarRef.current

        // Use full URL in production to avoid path resolution issues
        const isProduction = typeof window !== 'undefined' && 
                            window.location.hostname !== 'localhost' && 
                            window.location.hostname !== '127.0.0.1'
        
        const buildUrl = isProduction 
          ? `${window.location.protocol}//${window.location.host}/unity/Build`
          : "/unity/Build"
        
        const config = {
          dataUrl: buildUrl + "/webgl-build.data",
          frameworkUrl: buildUrl + "/webgl-build.framework.js",
          codeUrl: buildUrl + "/webgl-build.wasm",
          streamingAssetsUrl: "StreamingAssets",
          companyName: "DefaultCompany",
          productName: "MechBattle",
          productVersion: "1.0",
          matchWebGLToCanvasSize: false,
          devicePixelRatio: 1
        }
        
        console.log('🔧 Unity build URL:', buildUrl)
        console.log('🔧 Unity config:', config)
        
        // Test file accessibility before Unity initialization
        const testFiles = async () => {
          try {
            const dataResponse = await fetch(config.dataUrl, { method: 'HEAD' })
            console.log('📄 Data file accessible:', dataResponse.ok, dataResponse.status)
            
            const frameworkResponse = await fetch(config.frameworkUrl, { method: 'HEAD' })
            console.log('📄 Framework file accessible:', frameworkResponse.ok, frameworkResponse.status)
            
            const wasmResponse = await fetch(config.codeUrl, { method: 'HEAD' })
            console.log('📄 WASM file accessible:', wasmResponse.ok, wasmResponse.status)
          } catch (e) {
            console.error('❌ File accessibility test failed:', e)
          }
        }
        
        await testFiles()

        // Minimal setup to avoid conflicts

        loadingBar.style.display = "block"

        // Use the global createUnityInstance function
        // console.log('🎮 Initializing Unity instance...')
        // console.log('🖼️ Canvas element:', canvas)
        // console.log('⚙️ Unity config:', config)
        
        // Double check that createUnityInstance is available
        if (typeof createUnityInstance === 'undefined') {
          throw new Error('createUnityInstance is not defined at initialization time')
        }
        
        // // console.log('✅ createUnityInstance confirmed available, starting Unity...')
        
        // @ts-ignore - createUnityInstance is loaded as a global function by Unity
        console.log('🎮 Starting Unity initialization...')
        createUnityInstance(canvas, config, (progress: number) => {
          console.log('Unity loading progress:', Math.round(progress * 100) + '%')
          progressBarFull.style.width = 100 * progress + "%"
        }).then((unityInstance: any) => {
          loadingBar.style.display = "none"
          
          // console.log("🎮 PSG1 WebGL Game loaded successfully!")
          // console.log("🌐 PSG1 Template active - SolMechs × Play Solana")
          
          // Make Unity instance globally available
          ;(window as any).unityInstance = unityInstance
          
          // Minimal post-load setup
          
          // PSG1 JavaScript Functions for Unity WebGL
          ;(window as any).GetWalletAddress = function() {
            // console.log("🔗 PSG1 Template: Unity requesting wallet address")
            if ((window as any).parent && (window as any).parent.web3Bridge && (window as any).parent.web3Bridge.getWalletAddress) {
              const wallet = (window as any).parent.web3Bridge.getWalletAddress()
              // console.log(`🔗 PSG1 Template returning wallet: ${wallet}`)
              return wallet
            }
            // console.log("🔗 PSG1 Template returning demo wallet")
            return "PSG1_DEMO_WALLET"
          }
          
          ;(window as any).GetUserBalance = function() {
            // console.log("💰 PSG1 Template: Unity requesting user balance")
            if ((window as any).parent && (window as any).parent.web3Bridge && (window as any).parent.web3Bridge.getBalance) {
              // console.log("💰 PSG1 Template returning balance: 2000")
              return "2000"
            }
            // console.log("💰 PSG1 Template returning demo balance")
            return "1000"
          }
          
          ;(window as any).LogToJS = function(message: string) {
            // console.log("🎮 PSG1 TEMPLATE MESSAGE:", message)
            
            // Update page title based on PSG1 state
            if (message === "PSG1_SCREEN_ACTIVE") {
              document.title = "SolMechs × Play Solana (PSG1) - PSG1 Mode"
            } else if (message === "SOLMECHS_GAME_ACTIVE") {
              document.title = "SolMechs × Play Solana (PSG1) - Game Mode"
            }
            
            // Handle NFT mint requests
            if (message.startsWith("NFT_MINT_REQUEST:")) {
              const nftData = message.replace("NFT_MINT_REQUEST:", "")
              // console.log("🎨 PSG1 Template NFT Mint:", nftData)
              
              if ((window as any).parent && (window as any).parent.web3Bridge && (window as any).parent.web3Bridge.handleNFTMintRequest) {
                ;(window as any).parent.web3Bridge.handleNFTMintRequest(nftData)
              } else {
                alert(`🎨 PSG1 × SolMechs NFT!\n\nMinting via Play Solana:\n${nftData}`)
              }
            }
          }
          
          // console.log("✅ PSG1 Template: All bridge functions ready!")
            
        }).catch((message: string) => {
          console.error("PSG1 WebGL Load Error:", message)
          
          // Log additional debug info for EM_ASM errors
          if (message.includes("EM_ASM") || message.includes("TypeError: Cannot read properties of undefined")) {
            // console.error("🔍 Debug Info:")
            // console.error("- Current domain:", window.location.hostname)
            // console.error("- Build URL:", buildUrl)
            // console.error("- WASM URL:", buildUrl + "/webgl-build.wasm")
            
            // Test WASM file accessibility
            fetch(buildUrl + "/webgl-build.wasm")
              .then(response => {
                console.error("- WASM fetch response:", response.status, response.statusText)
                // console.error("- Content-Type:", response.headers.get('content-type'))
                // console.error("- Content-Encoding:", response.headers.get('content-encoding'))
                // console.error("- Content-Length:", response.headers.get('content-length'))
              })
              .catch(e => console.error("- WASM fetch failed:", e))
          }
          
          alert("PSG1 WebGL Load Error: " + message + "\n\nCheck console for debug info.")
        })

      } catch (error) {
        console.error("Failed to initialize Unity:", error)
        alert("Failed to initialize Unity: " + error)
      }
    }

    // Initialize Unity
    initUnity()
  }, [])

  return (
    <div style={{ 
      margin: 0, 
      padding: 0, 
      // background: "linear-gradient(135deg, #1a1a2e, #16213e, #0f3460)",
      backgroundAttachment: "fixed",
      fontFamily: "Arial, sans-serif",
      display: "flex",
      justifyContent: "center",
      alignItems: "center",
      minHeight: "100vh",
      overflow: "hidden"
    }}>
      <div className="max-h-[900px] max-w-[900px]" style={{
        position: "relative",
        width: "100%",
        height: "100%",
        // maxHeight: "94px",
        // backgroundImage: "url('/unity/OverlayPSG1.png')",
        backgroundRepeat: "no-repeat",
        backgroundPosition: "center center",
        backgroundSize: "contain",
        display: "flex",
        justifyContent: "center",
        alignItems: "center"
      }}>
        <div style={{
          width: "100%",
          height: "100%",
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
              width={783} 
              height={683}
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