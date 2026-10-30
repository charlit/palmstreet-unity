---

description: "Tâches du portage de PalmStreet sous Unity (iOS et Android)"
---

# Tasks: Portage de PalmStreet sur iOS et Android

**Input**: Design documents from `/specs/001-portage-palmstreet/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/core-and-services.md, quickstart.md

**Tests**: demandés par la constitution (principe IV) : chaque règle de PalmStreet.Core a ses
tests EditMode, écrits avant le code et qui échouent d'abord.

**Organization**: une phase par user story de spec.md (P1 → P4), plus la refonte graphique et le son.

Référence pour toutes les valeurs : `public/index.html` du dépôt PalmStreet (commit 52f79e2) et
data-model.md. Toutes les durées sont en pas de 1/60 s.

## Phase 1: Setup

**Purpose**: projet Unity configuré, vide mais qui compile pour iOS et Android.

- [x] T001 Créer le projet Unity 6000.3.27f1 à la racine du dépôt (Assets/, Packages/, ProjectSettings/) avec le modèle « Universal 2D » (URP + Renderer 2D), en batchmode : `Unity -batchmode -createProject ~/Developer/PalmStreetUnity -quit`, puis ajouter com.unity.render-pipelines.universal, com.unity.test-framework et com.unity.inputsystem dans Packages/manifest.json
- [x] T002 Régler ProjectSettings/ProjectSettings.asset : nom « PalmStreet », société « charlit », identifiant com.charlit.palmstreet (iOS et Android), orientation portrait seule, iOS 15.0 minimum, équipe 32AK299L44, Android minSdk 24 / targetSdk 36, IL2CPP + ARM64, nouveau Input System actif
- [x] T003 [P] Créer les assemblies Assets/Scripts/Core/PalmStreet.Core.asmdef (noEngineReferences: true), Assets/Scripts/Game/PalmStreet.Game.asmdef (réf. Core, Services), Assets/Scripts/Services/PalmStreet.Services.asmdef et Assets/Tests/EditMode/PalmStreet.Tests.EditMode.asmdef (réf. Core, éditeur seulement, nunit)
- [x] T004 [P] Créer la scène Assets/Scenes/Game.unity : caméra orthographique cadrant la zone logique 390 × 620 (unités logiques → mètres à 1/100), Global Light 2D, Volume de post-traitement vide ; l'ajouter seule dans EditorBuildSettings
- [x] T005 [P] Ajouter Tools/run-tests.sh qui lance les tests EditMode en batchmode (commande de quickstart.md §1) et affiche le nombre de réussites/échecs depuis le XML

**Checkpoint**: `Tools/run-tests.sh` tourne (0 test) et le projet s'ouvre sans erreur.

---

## Phase 2: Foundational

**Purpose**: socle de la simulation dont dépendent toutes les stories.

- [x] T006 [P] Écrire Assets/Scripts/Core/IRandom.cs (Next01(), Range(min, max)) et SeededRandom (xorshift, graine int) pour des parties rejouables
- [x] T007 [P] Écrire Assets/Scripts/Core/GameEvent.cs : les événements de contracts/core-and-services.md §1 (Jumped, TrickDone, GrindStarted, GrindEnded, GrindOut, CoinCollected, ThemeChanged, Milestone, BossStarted, BossHit, BossDefeated, ConeDropped, Crashed) avec leurs données
- [x] T008 Écrire Assets/Scripts/Core/GameState.cs avec les champs de data-model.md « GameState » et « Persistance » ; Phase « Ready → Running au toucher START ; Running → Over à la chute »
- [x] T009 Écrire Assets/Scripts/Core/Simulation.cs : constructeur (IRandom, best), Start(character), Tap(), DoubleTap(), SetDuck(on), Step(), State, event Events ; Step() vide pour l'instant sauf Score += 1 en Running
- [x] T010 [P] Test Assets/Tests/EditMode/SimulationTests.cs : en Ready, Tap() démarre la partie ; 60 Step() en Running donnent Score = 60 ; deux simulations à même graine et mêmes commandes ont le même état après 3 000 pas

**Checkpoint**: Simulation pas à pas testée.

---

## Phase 3: User Story 1 - Rouler, sauter, tomber (Priority: P1) 🎯 MVP

**Goal**: une partie complète jouable sur téléphone en formes simples.

**Independent Test**: sauter 10 plots, en percuter un, voir WIPEOUT ! et le score, rejouer d'un toucher.

### Tests for User Story 1

- [x] T011 [P] [US1] Test Assets/Tests/EditMode/TerrainTests.cs : départ plat 0–420 ; ordre Flat → Up → Down → Flat ; « Flat 240–500 ; Up et Down 130–240 » ; « Up monte à 45–100 ; Down revient à 0 » ; multiplicateur ≤ 1,55 en descente et ≥ 0,78 en montée (FR-005)
- [x] T012 [P] [US1] Test Assets/Tests/EditMode/SkaterTests.cs : Tap au sol → VY = −9,6 ; gravité 0,5 ; atterrit sur le sol en suivant le relief ; pas de saut accroupi ; « debout 58, accroupi 34 » (FR-006)
- [x] T013 [P] [US1] Test Assets/Tests/EditMode/SpawnerTests.cs : un seul élément à la fois ; sous 2 000 que des petits plots 15×24 ; ensuite plots/rails 2 pour 1 ; pas de rail si pas assez de plat (marge 50) ; pas d'obstacle à moins de 90 du début d'une descente ; premier obstacle = grand rail 210×28 puis tous les 5 000 (FR-007, FR-008)
- [x] T014 [P] [US1] Test Assets/Tests/EditMode/SpeedAndCrashTests.cs : vitesse = min(4,3 ; 1,7 + ⌊Score/100⌋ × 0,045) × terrain ; collision avec un plot → Phase Over, événement Crashed ; Best mis à jour si Score > Best (FR-003, FR-009)

### Implementation for User Story 1

- [x] T015 [P] [US1] Implémenter Assets/Scripts/Core/Terrain.cs (port de resetTerrain, pushNextTerrainSegment, ensureTerrainAhead, terrainInfoAt, terrainSpeedMultiplier, groundYAt)
- [x] T016 [P] [US1] Implémenter Assets/Scripts/Core/Skater.cs (port de jump, setDuck, gravité/atterrissage de update, boîte de collision x + 9, largeur − 18, haut + 12)
- [x] T017 [US1] Implémenter Assets/Scripts/Core/Spawner.cs pour les plots et le grand rail (port de spawnObstacle, isFlatEnoughForRail, tooEarlyInDescent, spawnPointWorldX, calcul de NextSpawnIn) — dépend de T015
- [x] T018 [US1] Brancher Terrain, Skater, Spawner, vitesse, défilement, collisions et fin de partie dans Assets/Scripts/Core/Simulation.cs ; faire passer T011–T014
- [x] T019 [P] [US1] Écrire Assets/Scripts/Game/GameRunner.cs : accumulateur 1/60 s (max 5 pas par image), affichage limité à 60 images/s (interpolation reportée : inutile à 60 Hz), chargement/sauvegarde de best dans PlayerPrefs
- [x] T020 [P] [US1] Écrire Assets/Scripts/Game/TouchInput.cs selon contracts §3 : tap, double tap < 0,32 s en l'air, maintien > 0,22 s = accroupi, clavier dans l'éditeur
- [x] T021 [US1] Écrire Assets/Scripts/Game/WorldView.cs en formes provisoires (sol en LineRenderer suivant le relief, plots et rails en sprites unis, skateur en rectangle), avec pools d'objets
- [x] T022 [US1] Écrire l'UI provisoire (uGUI construit en code, UI Toolkit gardé pour la refonte T046) dans Assets/Scripts/Game/Ui/ : accueil (4 skateurs, START, TOP 10 désactivé), HUD SCORE/BEST, écran WIPEOUT ! avec score et rejouer ; respecter les zones sûres
- [ ] T023 [US1] Vérifier US1 dans l'éditeur puis sur l'iPhone (build iOS de développement) selon quickstart.md §2–3

**Checkpoint**: MVP jouable de bout en bout sur iPhone.

---

## Phase 4: User Story 2 - Figures, grinds, éclairs et combos (Priority: P2)

**Goal**: le système de points du jeu web.

**Independent Test**: une figure, un grind et une rangée d'éclairs donnent les points des FR-010 à FR-014.

### Tests for User Story 2

- [x] T024 [P] [US2] Test Assets/Tests/EditMode/TrickTests.cs : DoubleTap en l'air → FLIP 30, SHOVE-IT 35, 360° 45, 360° INVERSÉ 45 dans cet ordre, × combo ; une seule figure par saut ; rien au sol (FR-010)
- [x] T025 [P] [US2] Test Assets/Tests/EditMode/GrindTests.cs : pieds sur le dessus du rail en descente (−10/+16) → grind ; +2 par pas ; sortie +20 × combo ; Tap pendant le grind → +15 et VY = −9,6 × 0,85 (FR-011, FR-012)
- [x] T026 [P] [US2] Test Assets/Tests/EditMode/CoinAndComboTests.cs : rangées de 3 à 5 espacées de 26 aux hauteurs 20/60/95 ; 10 × combo ; toujours en montée et début de descente, sinon 38 % ; combo +1 par figure ou grind, retour à 1 après 130 pas (FR-013, FR-014)

### Implementation for User Story 2

- [x] T027 [US2] Implémenter figures et combo dans Assets/Scripts/Core/Scoring.cs et Skater.cs (port de doTrick, bumpCombo, timer de combo)
- [x] T028 [US2] Implémenter le grind et les rails dans Skater.cs et Spawner.cs (port du bloc « fallingOntoTop » et de la sortie de grind)
- [x] T029 [US2] Implémenter les éclairs dans Spawner.cs (port de spawnCoinRow et du choix pièces/obstacle) et leur ramassage dans Simulation.cs ; faire passer T024–T026
- [ ] T030 [US2] Afficher dans WorldView.cs et l'UI : rails, éclairs, COMBO xN, textes flottants (« +30 FLIP ! », « +20 GRIND! », « +15 OUT! »)

**Checkpoint**: US1 et US2 marchent ensemble.

---

## Phase 5: User Story 3 - Décors et boss (Priority: P3)

**Goal**: cycle des 3 décors et combat de boss.

**Independent Test**: passer 5 000 et 10 000 points, puis battre le boss (ou raccourci du soleil).

### Tests for User Story 3

- [ ] T031 [P] [US3] Test Assets/Tests/EditMode/ThemeTests.cs : « Score mod 20 000 : <5 000, <10 000, sinon Arcangues » ; Milestone FULL SPEED tous les 5 000 (FR-015, FR-016)
- [ ] T032 [P] [US3] Test Assets/Tests/EditMode/BossTests.cs : boss à 15 000 ; 8 000 PV ; dent tous les 22 pas (40 au départ), −100 ; phases « Ground 600 pas → Rising 30 → Airborne 220 → Descending 30 » ; 4 plots, un tous les 55 pas ; victoire +5 000, plat de 2 500, rail nose grind 1 050×28, prochain boss à score + 15 000 (FR-017 à FR-019)
- [ ] T033 [P] [US3] Test dans BossTests.cs : StartAtBoss() → Score 15 000, boss actif, skateur FREESTILL (FR-020)

### Implementation for User Story 3

- [ ] T034 [US3] Implémenter les thèmes et paliers dans Assets/Scripts/Core/Scoring.cs
- [ ] T035 [US3] Implémenter Assets/Scripts/Core/Boss.cs (port de triggerBossFight, updateBossFight, dropBossCone, defeatBoss) et l'intégrer à Simulation.cs ; faire passer T031–T033
- [ ] T036 [US3] Afficher le boss, les dents, les plots qui tombent, la barre de vie, les bandeaux BOSS !, BOOM et TU ES UN VRAI PST ! (formes provisoires), et le soleil cliquable de l'accueil

**Checkpoint**: tout le gameplay du jeu web est là, en formes simples.

---

## Phase 6: Refonte graphique et son (FR-021, FR-024, FR-025)

**Purpose**: le nouveau style, sans changer les règles.

- [ ] T037 [P] Écrire Tools/art/render.sh : convertit Tools/art/**/*.svg en PNG ×3 dans Assets/Art/ (Chrome sans interface), réglages d'import (Sprite, Point/Bilinear, pas de compression avec perte sur les dégradés)
- [ ] T038 [P] Dessiner les décors en SVG dans Tools/art/themes/{largente,btz,arcangues}/ : ciel dégradé, soleil et halo, 3 plans de parallaxe (lointain, silhouettes, premier plan), océan et surfeurs pour Largenté, avec les couleurs du jeu web (ciel, silhouette, soleil, accent)
- [ ] T039 [P] Dessiner les 4 skateurs en SVG découpés (tête, buste, bras, avant-bras, cuisses, jambes, planche, roues) dans Tools/art/skaters/ d'après les sprites du jeu web ; planche jaune pour GUIGUI
- [ ] T040 [P] Dessiner plots, rails (normal, arc-en-ciel, nose grind), éclair, boss (skate enflammé), dent et éléments d'UI dans Tools/art/props/
- [ ] T041 Écrire Assets/Scripts/Game/SkaterRig.cs : pantin articulé et poses en données (poussée, ollie, atterrissage écrasé, accroupi, 4 figures, grind, nose grind, chute), interpolées en code
- [ ] T042 Écrire Assets/Scripts/Game/Parallax.cs et remplacer les formes provisoires de WorldView.cs par les sprites ; fondu entre décors au changement de thème
- [ ] T043 Écrire Assets/Scripts/Game/Fx.cs : lumières 2D (soleil, halo), poussière des roues, étincelles de grind, éclats d'éclair, BOOM, légère secousse de caméra au crash et au boss ; bloom et vignette dans le Volume
- [ ] T044 [P] Écrire Tools/audio/generate.py (stdlib) : musique MELODY/BASS à 132 bpm et bruitages saut, éclair, chute, grind, figure, reproduits depuis l'AudioContext du jeu web, en WAV dans Assets/Audio/
- [ ] T045 Écrire Assets/Scripts/Game/AudioPlayer.cs : musique en boucle pendant la partie, bruitages sur les événements, bouton son coupé gardé dans PlayerPrefs `muted`
- [ ] T046 Refaire l'UI dans le nouveau style (accueil avec cartes de skateurs animées, HUD, écran de fin, bandeaux) avec une police OFL dans Assets/Fonts/

**Checkpoint**: le jeu a son nouveau look et son son, mêmes règles (tests toujours verts).

---

## Phase 7: User Story 4 - Classement et réglages (Priority: P4)

**Goal**: Game Center, Play Games, pause, son.

**Independent Test**: son coupé gardé au relancement ; score d'une partie visible dans le classement.

- [ ] T047 [P] [US4] Écrire Assets/Scripts/Services/ILeaderboard.cs et LocalLeaderboard.cs (toujours indisponible) selon contracts §2
- [ ] T048 [US4] Compiler le plugin Apple.GameKit (github.com/apple/unityplugins, scripts de build) et l'ajouter en tarball dans Packages/ ; écrire Assets/Scripts/Services/GameKitLeaderboard.cs (#if UNITY_IOS) : connexion silencieuse, score sur `palmstreet.top` avec le skateur en contexte, écran natif
- [ ] T049 [US4] Ajouter le Google Play Games plugin for Unity 2.x ; écrire Assets/Scripts/Services/PlayGamesLeaderboard.cs (#if UNITY_ANDROID) avec PlayGamesIds.Top (ID du classement « Meilleur score » de la Play Console)
- [ ] T050 [US4] Brancher le classement : connexion au lancement, score envoyé à chaque fin de partie, bouton TOP 10 qui ouvre l'écran natif ou explique l'indisponibilité (FR-022)
- [ ] T051 [US4] Pause quand l'app passe en arrière-plan (OnApplicationPause), musique coupée, reprise au toucher (FR-023)

**Checkpoint**: toutes les stories marchent.

---

## Phase 8: Polish & publication

- [ ] T052 [P] Icône de l'app et écran de lancement dans le nouveau style (Tools/art/icon.svg → Assets/Art/Icon/)
- [ ] T053 Profiler sur iPhone et Android pendant le boss : 60 images/s visés (SC-002), corriger les allocations par image
- [ ] T054 Mesurer le démarrage (< 3 s, SC-003) et la taille de l'app (< 150 Mo)
- [ ] T055 [P] Créer le classement Google Play Games et l'app Android dans la Play Console ; vérifier que `palmstreet.top` existe dans App Store Connect pour com.charlit.palmstreet
- [ ] T056 Build iOS de distribution et build Android .aab signé (clé d'upload hors du dépôt), installation sur l'iPhone et l'émulateur, parcours complet de quickstart.md
- [ ] T057 Fiches App Store / Google Play, captures d'écran, page de confidentialité ; envoi en review (SC-005)

---

## Dependencies & Execution Order

- Setup (T001–T005) → Foundational (T006–T010) → US1 (T011–T023).
- US2 et US3 dépendent de US1 (même Simulation) ; US3 peut commencer après T018 en parallèle de US2.
- Phase 6 (graphismes, son) dépend des événements et de l'état de US1–US3 ; les SVG (T038–T040)
  et l'audio (T044) peuvent être faits dès la Phase 2.
- US4 ne dépend que de US1 (fin de partie) ; T048/T049 peuvent avancer en parallèle de la Phase 6.
- Polish après tout le reste.

## Parallel Examples

- US1 : T011, T012, T013, T014 (tests) ensemble ; puis T015 et T016 ensemble ; T019 et T020 ensemble.
- US2 : T024, T025, T026 ensemble.
- Phase 6 : T037, T038, T039, T040, T044 ensemble.

## Implementation Strategy

1. MVP = Phases 1–3 : un PalmStreet jouable en formes simples sur l'iPhone ; on s'arrête et on joue.
2. Ajouter US2 puis US3 : tout le gameplay du jeu web.
3. Refonte graphique et son : le jeu prend son nouveau look.
4. Classements, polish, publication.
