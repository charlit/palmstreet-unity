#!/bin/zsh
# Build iOS de développement et installation sur l'iPhone branché (CHARLES).
set -e
U=/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity
P=${0:a:h:h}
DEVICE=00008140-001838442230801C
export DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer
"$U" -batchmode -nographics -projectPath $P -buildTarget iOS -executeMethod PalmStreet.EditorTools.BuildTools.IOS -logFile $P/Logs/build-ios.log -quit || { grep -E "error|Exception" $P/Logs/build-ios.log | head -20; exit 1; }
ISS=$(python3 -c "import json;print(json.load(open('$HOME/.appstoreconnect/config.json'))['issuer_id'])")
xcodebuild -project $P/Builds/iOS/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Debug -destination "id=$DEVICE" \
  -derivedDataPath $P/Builds/iOS-dd -allowProvisioningUpdates \
  -authenticationKeyPath ~/.appstoreconnect/private_keys/AuthKey_VP7K7G6G3U.p8 -authenticationKeyID VP7K7G6G3U -authenticationKeyIssuerID $ISS \
  DEVELOPMENT_TEAM=32AK299L44 CODE_SIGN_STYLE=Automatic build -quiet
xcrun devicectl device install app --device $DEVICE $P/Builds/iOS-dd/Build/Products/Debug-iphoneos/*.app | tail -2
