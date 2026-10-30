#!/bin/zsh
# Lance les tests EditMode en batchmode et résume le résultat (Unity doit être fermé sur ce projet).
U=/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity
P=${0:a:h:h}
R=${TMPDIR:-/tmp}/palmstreet-tests.xml
rm -f $R
"$U" -batchmode -nographics -projectPath $P -runTests -testPlatform EditMode -testResults $R -logFile ${R%.xml}.log
code=$?
if [ ! -f $R ]; then echo "Pas de résultat : voir ${R%.xml}.log"; grep -E "error CS|Error" ${R%.xml}.log | head -20; exit 1; fi
python3 - "$R" <<'PY'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
print(f"{r.get('passed')} réussis, {r.get('failed')} échoués, {r.get('skipped')} ignorés sur {r.get('total')}")
for c in r.iter('test-case'):
    if c.get('result') == 'Failed':
        m = c.find('failure/message')
        print('ÉCHEC', c.get('fullname'), '-', (m.text or '').strip()[:300] if m is not None else '')
PY
exit $code
