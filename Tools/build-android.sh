#!/bin/zsh
# Build Android de développement (APK) ; l'installe sur l'appareil ou l'émulateur Android branché s'il y en a un.
set -e
U=/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity
P=${0:a:h:h}
ADB=$HOME/Library/Android/sdk/platform-tools/adb
"$U" -batchmode -nographics -projectPath $P -buildTarget Android -executeMethod PalmStreet.EditorTools.BuildTools.Android -logFile $P/Logs/build-android.log -quit || { grep -E "error|Exception" $P/Logs/build-android.log | head -20; exit 1; }
ls -la $P/Builds/Android/PalmStreet.apk
if $ADB get-state >/dev/null 2>&1; then $ADB install -r $P/Builds/Android/PalmStreet.apk && $ADB shell monkey -p com.charlit.palmstreet -c android.intent.category.LAUNCHER 1 >/dev/null; fi
