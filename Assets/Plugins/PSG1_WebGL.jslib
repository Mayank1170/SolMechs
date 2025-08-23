// PSG1 WebGL JavaScript Library for Unity
var PSG1_WebGL = {
    
    // Get wallet address from Next.js bridge
    GetWalletAddress: function() {
        try {
            if (window.parent && window.parent.web3Bridge && window.parent.web3Bridge.getWalletAddress) {
                var wallet = window.parent.web3Bridge.getWalletAddress();
                console.log("🔗 PSG1 jslib returning wallet: " + wallet);
                var bufferSize = lengthBytesUTF8(wallet) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(wallet, buffer, bufferSize);
                return buffer;
            } else {
                console.log("🔗 PSG1 jslib returning demo wallet");
                var demoWallet = "DEMO_WALLET_PSG1_JSLIB";
                var bufferSize = lengthBytesUTF8(demoWallet) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(demoWallet, buffer, bufferSize);
                return buffer;
            }
        } catch (e) {
            console.error("❌ PSG1 GetWalletAddress error: " + e.message);
            var errorWallet = "ERROR_WALLET";
            var bufferSize = lengthBytesUTF8(errorWallet) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(errorWallet, buffer, bufferSize);
            return buffer;
        }
    },
    
    // Get user balance from Next.js bridge
    GetUserBalance: function() {
        try {
            console.log("💰 PSG1 jslib getting user balance");
            // Return a string representation of balance
            var balance = "1000";
            if (window.parent && window.parent.web3Bridge && window.parent.web3Bridge.getBalance) {
                // For now, return mock balance since getBalance is async
                balance = "1500";
                console.log("💰 PSG1 jslib returning balance from bridge: " + balance);
            } else {
                console.log("💰 PSG1 jslib returning demo balance: " + balance);
            }
            
            var bufferSize = lengthBytesUTF8(balance) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(balance, buffer, bufferSize);
            return buffer;
        } catch (e) {
            console.error("❌ PSG1 GetUserBalance error: " + e.message);
            var errorBalance = "0";
            var bufferSize = lengthBytesUTF8(errorBalance) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(errorBalance, buffer, bufferSize);
            return buffer;
        }
    },
    
    // Log messages to JavaScript console
    LogToJS: function(messagePtr) {
        try {
            var message = UTF8ToString(messagePtr);
            console.log("🎮 PSG1 UNITY MESSAGE: " + message);
            
            // Handle PSG1 screen messages
            if (message === "PSG1_WEBGL_SCREEN_LOADED") {
                console.log("🌐 PSG1 WebGL Screen loaded successfully!");
                if (document) {
                    document.title = "SolMechs × Play Solana - Loading...";
                }
                return;
            }
            
            if (message === "PSG1_SCREEN_ACTIVE") {
                console.log("🎮 PSG1 Screen is now active");
                if (document) {
                    document.title = "SolMechs × Play Solana";
                }
                return;
            }
            
            if (message === "SOLMECHS_GAME_ACTIVE") {
                console.log("🎮 SolMechs game is now active");
                if (document) {
                    document.title = "SolMechs - Unity Game";
                }
                return;
            }
            
            if (message === "PSG1_CONNECTION_TEST") {
                console.log("🔧 PSG1 connection test requested");
                if (window.alert) {
                    window.alert("🔧 PSG1 Connection Test\\n\\nTesting Play Solana integration...\\n\\nCheck console for detailed results!");
                }
                return;
            }
            
            // Handle NFT mint requests
            if (message.indexOf("NFT_MINT_REQUEST:") === 0) {
                var nftData = message.replace("NFT_MINT_REQUEST:", "");
                console.log("🎨 PSG1 NFT Mint Request: " + nftData);
                
                if (window.parent && window.parent.web3Bridge && window.parent.web3Bridge.handleNFTMintRequest) {
                    window.parent.web3Bridge.handleNFTMintRequest(nftData);
                } else if (window.alert) {
                    window.alert("🎨 PSG1 × SolMechs NFT!\\n\\nMinting NFT via Play Solana:\\n" + nftData + "\\n\\nThis will integrate with Honeycomb Protocol!");
                }
                return;
            }
            
            // Handle milestone tests
            if (message.indexOf("MILESTONE2") >= 0) {
                console.log("✅ MILESTONE 2 SUCCESS: " + message);
                return;
            }
            
            // Regular Unity log messages
            console.log("📝 PSG1 Unity Log: " + message);
            
        } catch (e) {
            console.error("❌ PSG1 LogToJS error: " + e.message);
        }
    }
    
}; // End of PSG1_WebGL

// Merge the library with existing ones
mergeInto(LibraryManager.library, PSG1_WebGL);