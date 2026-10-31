# Implementation Plan: Portage de PalmStreet sur iOS et Android

**Branch**: `001-portage-palmstreet` | **Date**: 2026-10-30 (révisé le même jour) | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-portage-palmstreet/spec.md`

## Summary

Refaire le runner de skate PalmStreet sous Unity pour iOS et Android, **à partir du seul texte de la
spec** : on ne relit ni le code du jeu web ni ses images. Les règles chiffrées de la spec vivent dans une
simulation C# pure à pas fixe de 1/60 s, testée en mode Edit. Unity affiche l'état, entièrement en formes
dessinées par le code (maillages à couleurs de sommets) : ciel, soleil, décors en parallaxe, skateurs
articulés à genoux, planche, obstacles et boss. Unity transmet aussi les touchers.

Ce qui n'est pas chiffré dans la spec est choisi pour le jeu et marqué « choix » dans `Tuning.cs` : tailles
des plots et des rails, hauteurs des éclairs, couleurs, tenues, boss. La musique et les bruitages sont
synthétisés au lancement. Classement : Game Center par l'API Social d'Unity sur iOS ; Google Play Games
reste à brancher sur Android.

Révision du 2026-10-30 : le premier plan portait le jeu web à l'identique (fonctions du jeu web, SVG,
plugin Apple.GameKit, sons générés en WAV). Le jeu a été refait de zéro d'après la spec seule ; ce plan
décrit la version refaite (commit a646645).

## Technical Context

**Language/Version**: C# (Unity 6000.3.27f1, .NET Standard 2.1)

**Primary Dependencies**: URP (Renderer 2D), uGUI, Input System, Unity Test Framework. Aucun asset ni
plugin tiers.

**Storage**: PlayerPrefs (`best`, `total`, `outfit` en JSON, `mute`, `vibrate`)

**Testing**: Unity Test Framework : tests EditMode sur PalmStreet.Core (une règle de la spec par test), et
un test PlayMode qui fait des captures d'écran pour vérifier le rendu sans téléphone.

**Target Platform**: iOS 15+ (iPhone, iPad en portrait), Android 7.0+ (minSdk 25 dans les réglages), target API 36

**Project Type**: mobile-app (jeu 2D)

**Performance Goals**: 60 images/s sur iPhone 12 et Android milieu de gamme 2021 ; démarrage < 3 s

**Constraints**: hors ligne complet (seul le classement a besoin du réseau) ; aucune donnée collectée ;
taille de l'app < 150 Mo

**Scale/Scope**: 1 scène, 1 skateur personnalisable, 3 décors, 1 boss, 32 exigences

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Vérification | État |
|---|---|---|
| I. Fidélité au jeu web | Toutes les valeurs chiffrées de la spec (qui viennent du jeu web) sont reprises. Le code et les images du jeu web ne sont plus la référence : voir Complexity Tracking. | ⚠️ écart justifié |
| II. Mobile d'abord | Portrait, touchers, zones sûres, pas fixe de 1/60 s. L'affichage n'interpole pas entre deux pas : voir Complexity Tracking. | ⚠️ écart justifié |
| III. Simplicité | Aucun asset ni plugin tiers ; 1 scène ; graphismes et sons faits par le code | ✅ |
| IV. Logique testable | PalmStreet.Core sans UnityEngine, 23 tests EditMode, graine fixe | ✅ |
| V. Prêt pour les stores | Pas de données, pas de pub, graphismes et sons créés pour le projet, polices du système | ✅ |

## Project Structure

### Documentation (this feature)

```text
specs/001-portage-palmstreet/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/core-and-services.md
└── tasks.md
Art/cahier-des-charges-skateurs.md   # dessins des skateurs à faire faire (pièces détachées, PNG)
```

### Source Code (repository root)

```text
Assets/
├── Scenes/Game.unity                # caméra, Global Light 2D ; tout le reste est créé par Game.cs
├── Scripts/
│   ├── Core/                        # PalmStreet.Core.asmdef — C# pur, aucun UnityEngine
│   │   ├── Tuning.cs                # toutes les valeurs de la spec (et les « choix »)
│   │   ├── Ground.cs                # segments plat / montée / descente, hauteur, pente, vitesse
│   │   ├── Run.cs                   # une partie : touchers, pas de simulation, obstacles, éclairs, points, événements
│   │   ├── Boss.cs                  # état du boss et des dents
│   │   └── Unlocks.cs               # prix des vêtements en points cumulés
│   ├── Game/                        # PalmStreet.Game.asmdef — affichage Unity
│   │   ├── Game.cs                  # démarrage, boucle à pas fixes, touchers, événements, pause, PlayerPrefs, caméra
│   │   ├── Paint.cs                 # formes (polygones, cercles, capsules, dégradés) → Mesh ; Fader pour les fondus
│   │   ├── Look.cs                  # couleurs des décors et tenues des skateurs
│   │   ├── Scenery.cs               # ciel, soleil, reflet, 4 plans de parallaxe par décor, route
│   │   ├── RiderView.cs             # skateur articulé (jambes en cinématique inverse), planche, figures, chute
│   │   ├── Props.cs                 # plots, rails, éclairs, ombres, boss, dents (réserves d'objets)
│   │   ├── Fx.cs                    # particules : poussière, étincelles, éclats, confettis
│   │   ├── Sound.cs                 # musique 132 bpm et bruitages synthétisés, son coupé
│   │   └── Hud.cs                   # uGUI : accueil, score, combo, bandeaux, barre du boss, WIPEOUT, pause
│   └── Services/                    # PalmStreet.Services.asmdef
│       ├── Leaderboard.cs           # Game Center (iOS) ; message d'indisponibilité ailleurs
│       └── Haptics.cs               # vibrations courtes (iOS : Plugins/iOS/PalmHaptics.mm ; Android : Vibrator)
├── Editor/                          # BuildTools (iOS, Android), OpenGame (jouer en format téléphone)
└── Tests/
    ├── EditMode/RunTests.cs         # une règle de la spec par test
    └── PlayMode/CaptureTests.cs     # captures des moments clés et gros plans du skateur
Tools/
├── run-tests.sh                     # tests EditMode en batchmode
├── capture.sh                       # captures PNG (test PlayMode)
└── build-ios.sh                     # build, signature et installation sur l'iPhone
```

**Structure Decision**: un seul projet Unity à la racine du dépôt, trois assemblies : Core (règles,
testées sans Unity), Game (affichage), Services (plateforme). Tout l'affichage est construit en code au
chargement de la scène « Game » : pas de prefab, pas d'image importée.

## Décisions de conception

- **Unités** : unités logiques de la spec (écran 390 × 620), y vers le haut, sol de base à 0 ;
  100 unités = 1 m Unity. La caméra garde au moins 390 de large et 620 de haut, le skateur est à 90 du bord
  gauche, et les marges des écrans plus hauts ou plus larges montrent plus de décor.
- **Simulation** : `Run.Step()` avance de 1/60 s ; `Press()` et `Release()` reçoivent les touchers ; les
  événements (`Cue`) s'accumulent dans `Run.Events` et l'affichage les vide (sons, particules, textes).
- **Choix hors spec** : départ plat de 420 compté depuis le bord droit de l'écran, pour avoir le temps de
  réagir avant le grand rail ; une figure se fait en 2 touchers (sauter, puis toucher en l'air), au lieu du double toucher en l'air de la spec d'origine ; un rail normal attend
  le plat suivant plutôt que de devenir un plot ; la figure dure le temps qui reste avant la retombée.
- **Rendu** : matériau sprite du pipeline sous une lumière globale blanche ; formes en couleurs de sommets
  (converties en espace linéaire) ; pas de post-traitement ; halos en disques dégradés.
- **Skateur** : pantin en pièces (cuisse, tibia, chaussure, buste, tête, bras) ; les semelles sont posées
  sur le grip et les genoux calculés par cinématique inverse ; figures FLIP et SHOVE-IT (la planche tourne
  sous les pieds levés, avec ses vues de dessus et de dessous), 360° et 360° INVERSÉ (saltos groupés).
  Les pièces pourront être remplacées par des dessins (Art/cahier-des-charges-skateurs.md).
- **Son** : AudioClip créés au lancement (ondes carrée, triangle, sinus et bruit) ; pas de fichier son.
- **Classement** : `Social.ReportScore` / `ShowLeaderboardUI` sur `palmstreet.top` (iOS) ; Google Play
  Games demandera son plugin (seul plugin tiers prévu, imposé par FR-022).

## Phasage (pour /speckit-tasks)

1. Projet Unity, règles et tests (US1 à US3) : fait.
2. Rendu en formes, skateurs, son, interface, pause (FR-021, FR-023, FR-024, FR-025) : fait.
3. Classement Game Center : fait ; Google Play Games : à faire.
4. Dessins des skateurs (optionnel, selon les dessins reçus).
5. Polish, performances, builds de distribution, publication (SC-002, SC-003, SC-005).

## Complexity Tracking

| Écart | Pourquoi | Alternative plus simple écartée |
|---|---|---|
| Principe I : le code et les images du jeu web ne sont plus la référence, seulement les valeurs de la spec | Demande du propriétaire : « refaire le jeu juste avec la spec ». Les valeurs de gameplay restent celles du jeu web. | Porter le code web ligne à ligne : abandonné à la demande. La constitution est à amender (/speckit-constitution). |
| Principe II : pas d'interpolation de l'affichage entre deux pas | À 60 images/s (targetFrameRate), un pas par image : rien à interpoler | Interpoler : du code en plus pour rien à 60 Hz ; à faire si des écrans 120 Hz saccadent |
