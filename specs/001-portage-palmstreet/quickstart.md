# Quickstart: vérifier le portage

## Prérequis

- Unity 6000.3.27f1 avec iOS Build Support et Android Build Support (Unity Hub, compte connecté).
- Xcode (iPhone « CHARLES » branché) ; un téléphone Android ou l'émulateur « quiz ».

## 1. Règles (sans lancer le jeu)

```bash
"/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath ~/Developer/PalmStreetUnity -runTests -testPlatform EditMode \
  -testResults /tmp/palmstreet-tests.xml -quit
```

Attendu : tous les tests passent (barèmes FR-010 à FR-014, terrain FR-005, apparitions FR-007,
FR-008, boss FR-017 à FR-019, partie rejouée à l'identique avec la même graine).

## 2. Dans l'éditeur

Ouvrir `Assets/Scenes/Game.unity`, Play, jouer au clavier :

| Scénario | Attendu |
|---|---|
| START, Espace sur 5 plots | 5 ollies, score qui monte de 60 par seconde |
| Percuter un plot | WIPEOUT !, planche qui rebondit, BEST mis à jour |
| Espace puis ← en l'air | FLIP ! +30, COMBO x2 |
| Retomber sur un rail | grind, étincelles, +20 × combo GRIND! en sortie |
| Cliquer le soleil à l'accueil | boss immédiat, barre de vie, plots lâchés en vol |

## 3. Sur téléphone

- iOS : Build Settings › iOS › Build, puis compiler le projet Xcode sur l'iPhone.
- Android : Build Settings › Android › Build And Run.

Vérifier : 60 images/s pendant le boss (overlay de stats), pause en arrière-plan, son coupé
gardé au relancement, connexion Game Center / Play Games et score envoyé à la fin d'une partie.
