#!/bin/zsh
# Version App Store : build Unity sans Development (rien de débloqué), archive signée « App Store », .ipa,
# puis envoi chez Apple avec --upload. Usage : Tools/release-ios.sh <numéro de build> [--upload]
# Le numéro de build doit augmenter à chaque envoi. Unity doit être fermé sur ce projet.
set -e
BUILD=${1:?numéro de build}
U=/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity
P=${0:a:h:h}
export DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer
export PALMSTREET_BUILD=$BUILD
"$U" -batchmode -nographics -projectPath $P -buildTarget iOS -executeMethod PalmStreet.EditorTools.BuildTools.IOSRelease -logFile $P/Logs/release-ios.log -quit || { grep -E "error|Exception" $P/Logs/release-ios.log | head -20; exit 1; }
A=$P/Builds/release/PalmStreet-$BUILD.xcarchive
rm -rf $A $P/Builds/release/ipa-$BUILD
xcodebuild -project $P/Builds/iOS-release/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -destination generic/platform=iOS -archivePath $A archive -quiet
cat > $P/Builds/release/ExportOptions.plist <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>method</key><string>app-store-connect</string>
  <key>destination</key><string>export</string>
  <key>teamID</key><string>32AK299L44</string>
  <key>signingStyle</key><string>manual</string>
  <key>signingCertificate</key><string>Apple Distribution</string>
  <key>provisioningProfiles</key><dict><key>com.charlit.palmstreet</key><string>PalmStreet App Store</string></dict>
  <key>uploadSymbols</key><true/>
</dict></plist>
PLIST
xcodebuild -exportArchive -archivePath $A -exportPath $P/Builds/release/ipa-$BUILD -exportOptionsPlist $P/Builds/release/ExportOptions.plist -quiet
IPA=$(ls $P/Builds/release/ipa-$BUILD/*.ipa)
ls -la $IPA
if [[ "$2" == "--upload" ]]; then
  ISS=$(python3 -c "import json;print(json.load(open('$HOME/.appstoreconnect/config.json'))['issuer_id'])")
  xcrun altool --upload-app -f $IPA -t ios --apiKey VP7K7G6G3U --apiIssuer $ISS
fi
