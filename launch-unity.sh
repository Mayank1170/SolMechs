#!/bin/bash

echo "🚀 Launching SolMechs Unity Game..."
echo "📁 Unity Project Path: /home/mayank/SolMechs/"

# Try to find Unity Editor
UNITY_PATH=""
if command -v unity &> /dev/null; then
    UNITY_PATH="unity"
elif [ -f "/usr/bin/unity-editor" ]; then
    UNITY_PATH="/usr/bin/unity-editor"
elif [ -f "/snap/bin/unity" ]; then
    UNITY_PATH="/snap/bin/unity"
elif [ -f "/opt/Unity/Editor/Unity" ]; then
    UNITY_PATH="/opt/Unity/Editor/Unity"
fi

if [ -n "$UNITY_PATH" ]; then
    echo "✅ Found Unity at: $UNITY_PATH"
    echo "🎮 Opening Unity project..."
    cd /home/mayank/SolMechs/
    $UNITY_PATH -projectPath /home/mayank/SolMechs/ &
    echo "🌐 Unity should now be running with Web3 bridge available!"
    echo "💡 In Unity, your game can call Application.ExternalCall() to interact with Web3"
else
    echo "❌ Unity Editor not found in common locations"
    echo "💡 Please open Unity manually:"
    echo "   1. Open Unity Hub"
    echo "   2. Open project: /home/mayank/SolMechs/"
    echo "   3. Press Play to start the game"
fi

echo ""
echo "🔗 Web3 Bridge is ready for Unity communication!"
echo "📋 Available functions:"
echo "   - Application.ExternalCall('web3Bridge.mintReward', 10)"
echo "   - Application.ExternalCall('web3Bridge.getWalletAddress')"
echo "   - Application.ExternalCall('web3Bridge.getBalance')"