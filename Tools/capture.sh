#!/bin/zsh
# Captures d'écran du jeu (test PlayMode CaptureTests) dans $1 (défaut : Temp/Captures). Unity doit être fermé sur ce projet.
U=/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity
P=${0:a:h:h}
export PALMSTREET_CAPTURES=${1:-$P/Temp/Captures}
rm -rf $PALMSTREET_CAPTURES; mkdir -p $PALMSTREET_CAPTURES
"$U" -batchmode -projectPath $P -runTests -testPlatform PlayMode -testFilter CaptureTests -testResults $P/Temp/capture-results.xml -logFile $P/Logs/capture.log
grep -E "error CS|Exception|Failed" $P/Logs/capture.log | head -10
ls $PALMSTREET_CAPTURES
