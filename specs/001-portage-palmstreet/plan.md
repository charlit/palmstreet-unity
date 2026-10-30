# Implementation Plan: Portage de PalmStreet sur iOS et Android

**Branch**: `001-portage-palmstreet` | **Date**: 2026-10-30 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-portage-palmstreet/spec.md`

## Summary

Refaire le runner de skate PalmStreet (jeu web en un fichier) sous Unity pour iOS et Android,
avec les mêmes règles chiffrées et des graphismes refaits plus riches. Les règles sont portées
telles quelles dans une simulation C# pure à pas fixe de 1/60 s, testée en mode Edit ; Unity
affiche l'état (URP 2D, lumières, particules, parallaxe, skateurs articulés en SVG dessinés pour
le projet) et transmet les touchers. Classements : Game Center (plugin Apple.GameKit) et Google
Play Games (plugin officiel), derrière une même interface.

## Technical Context

**Language/Version**: C# (Unity 6000.3.27f1, .NET Standard 2.1)

**Primary Dependencies**: URP (Renderer 2D), UI Toolkit, Unity Test Framework, Apple.GameKit
(iOS), Google Play Games plugin for Unity 2.x (Android)

**Storage**: PlayerPrefs (meilleur score, son coupé, dernier skateur)

**Testing**: Unity Test Framework, tests EditMode sur l'assembly PalmStreet.Core

**Target Platform**: iOS 15+ (iPhone, iPad en portrait), Android 7.0+ (API 24), target API 36

**Project Type**: mobile-app (jeu 2D)

**Performance Goals**: 60 images/s sur iPhone 12 et Android milieu de gamme 2021 ; démarrage < 3 s

**Constraints**: hors ligne complet (seul le classement a besoin du réseau) ; aucune donnée
collectée ; taille de l'app < 150 Mo

**Scale/Scope**: 1 scène de jeu, 4 skateurs, 3 décors, 1 boss, ~25 exigences

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Vérification | État |
|---|---|---|
| I. Fidélité au jeu web | Valeurs reprises telles quelles, simulation à 60 pas/s comme le jeu web | ✅ |
| II. Mobile d'abord | Portrait, touchers, zones sûres, pas fixe indépendant des images/s | ✅ |
| III. Simplicité | 2 plugins (classements, imposés par FR-022), aucun autre asset tiers ; 1 scène | ✅ |
| IV. Logique testable | PalmStreet.Core sans UnityEngine, tests EditMode, graine fixe | ✅ |
| V. Prêt pour les stores | Pas de données, pas de pub, graphismes faits pour le projet, police OFL | ✅ |

Re-vérifié après la phase 1 (data-model, contracts) : aucun écart.

## Project Structure

### Documentation (this feature)

```text
specs/001-portage-palmstreet/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/core-and-services.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
Assets/
├── Scenes/Game.unity
├── Scripts/
│   ├── Core/                    # PalmStreet.Core.asmdef — C# pur, aucun UnityEngine
│   │   ├── Simulation.cs        # boucle de pas, commandes, événements
│   │   ├── Skater.cs            # saut, gravité, grind, figures, accroupi
│   │   ├── Terrain.cs           # segments plat / montée / descente, multiplicateur de vitesse
│   │   ├── Spawner.cs           # plots, rails, grand rail, éclairs
│   │   ├── Scoring.cs           # score, combo, paliers, thèmes
│   │   ├── Boss.cs              # phases, dents, plots lâchés
│   │   └── IRandom.cs
│   ├── Game/                    # PalmStreet.Game.asmdef — MonoBehaviour
│   │   ├── GameRunner.cs        # accumulateur 1/60 s, interpolation, pause en arrière-plan
│   │   ├── TouchInput.cs        # tap / double tap / maintien, clavier dans l'éditeur
│   │   ├── WorldView.cs         # sol, obstacles, éclairs, boss (pools d'objets)
│   │   ├── SkaterRig.cs         # pantin articulé et poses
│   │   ├── Parallax.cs          # plans de décor par thème
│   │   ├── Fx.cs                # particules, textes flottants, BOOM, secousse caméra
│   │   ├── AudioPlayer.cs       # musique, bruitages, son coupé
│   │   └── Ui/                  # accueil, HUD, fin, bandeaux (UI Toolkit)
│   └── Services/                # PalmStreet.Services.asmdef
│       ├── ILeaderboard.cs
│       ├── GameKitLeaderboard.cs     # #if UNITY_IOS
│       ├── PlayGamesLeaderboard.cs   # #if UNITY_ANDROID
│       └── LocalLeaderboard.cs
├── Art/                         # PNG générés depuis Tools/art/*.svg
├── Audio/                       # WAV générés par Tools/audio/generate.py
├── Fonts/                       # police OFL
└── Tests/EditMode/              # PalmStreet.Tests.EditMode.asmdef
Tools/
├── art/                         # SVG sources + render.sh (SVG → PNG ×3)
└── audio/generate.py            # musique et bruitages du jeu web en WAV
Packages/  ProjectSettings/
```

**Structure Decision**: un seul projet Unity à la racine du dépôt. Trois assemblies séparent la
logique pure (Core, testée), l'affichage (Game) et les services de plateforme (Services), pour
que les tests EditMode compilent sans scène ni plugin natif.

## Phasage (pour /speckit-tasks)

1. Projet Unity (URP 2D, portrait, identifiants, asmdef) + Core avec tests : P1.
2. Affichage provisoire en formes simples et touchers : P1 jouable de bout en bout.
3. Figures, grinds, éclairs, combos : P2.
4. Décors, boss : P3.
5. Graphismes refaits (SVG, pantins, lumières, particules), son : FR-024, FR-025, FR-021.
6. Classements, pause, réglages, builds iOS et Android : P4, SC-005.

## Complexity Tracking

Aucun écart à la constitution.
