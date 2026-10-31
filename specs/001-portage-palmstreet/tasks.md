---

description: "Tâches du portage de PalmStreet sous Unity (iOS et Android), version refaite d'après la spec seule"
---

# Tasks: Portage de PalmStreet sur iOS et Android

**Input**: Design documents from `/specs/001-portage-palmstreet/`

**Prerequisites**: plan.md (révisé), spec.md, data-model.md, contracts/core-and-services.md, quickstart.md

**Tests**: demandés par la constitution (principe IV) : chaque règle de PalmStreet.Core a son test EditMode
dans Assets/Tests/EditMode/RunTests.cs (nommé d'après l'exigence : `Fr010_…`).

**Organization**: une phase par user story de spec.md (P1 → P4), plus le rendu et le son, puis la
publication. Les tâches cochées sont faites dans le commit a646645 (jeu refait d'après la spec seule).

Référence des valeurs : spec.md uniquement. Ce qui n'y est pas chiffré est dans Tuning.cs, marqué « choix ».
Toutes les durées sont en pas de 1/60 s.

## Phase 1: Setup

- [x] T001 Projet Unity 6000.3.27f1 à la racine (modèle 2D, URP Renderer 2D, Input System, Test Framework)
- [x] T002 ProjectSettings : PalmStreet, charlit, com.charlit.palmstreet, portrait seul, équipe 32AK299L44, IL2CPP, targetSdk 36, Input System seul
- [x] T003 [P] Assemblies PalmStreet.Core (noEngineReferences), PalmStreet.Game, PalmStreet.Services, PalmStreet.Tests.EditMode, PalmStreet.Tests.PlayMode, PalmStreet.Editor
- [x] T004 [P] Scène Assets/Scenes/Game.unity (caméra orthographique, Global Light 2D) seule dans les Build Settings ; couches de tri Sky, Default, Front, Ui
- [x] T005 [P] Tools/run-tests.sh, Tools/capture.sh (captures PNG par le test PlayMode), Tools/build-ios.sh (build, signature, installation sur l'iPhone)

---

## Phase 2: Foundational

- [x] T006 Assets/Scripts/Core/Tuning.cs : toutes les valeurs de la spec, et les « choix » (rail 100 × 30, plots 22 × 28 et 30 × 42, éclairs à 30/75/120, skateur à 90 du bord, 24 × 60)
- [x] T007 Assets/Scripts/Core/Ground.cs : segments plat / montée / descente en demi-cosinus, Height, Slope, SpeedFactor, IsFlat, InDescentStart, FlatUntil (plat forcé), TrimAfter
- [x] T008 Assets/Scripts/Core/Run.cs : Phase, Start(rider, bossNow), Press(), Release(), Step(), Events (Cue + points + position + texte), graine fixe pour les tests

---

## Phase 3: User Story 1 - Rouler, sauter, tomber (Priority: P1) 🎯 MVP

### Tests for User Story 1

- [x] T009 [P] [US1] Tests FR-003 (vitesse), FR-004 (1 point par pas), FR-005 (départ plat, bornes des segments, vitesse en pente), FR-006 (ollie de 87 environ, 38 pas en l'air, accroupi au maintien)
- [x] T010 [P] [US1] Tests FR-007 (que des plots sous 2 000, puis 2 plots pour 1 rail, rails sur le plat, jamais superposés), FR-008 (premier obstacle = grand rail × 3), FR-009 (plot → Over, Crash, Best)

### Implementation for User Story 1

- [x] T011 [US1] Saut, gravité, atterrissage sur le relief, accroupi après 13 pas de maintien dans Run.cs
- [x] T012 [US1] Apparition des obstacles (un à la fois, écart selon la vitesse, pas au début d'une descente, rail sur le plat suivant), collisions et fin de partie
- [x] T013 [US1] Game.cs : boucle à pas fixes (accumulateur, max 0,1 s par image), touchers (pointeur et clavier, boutons d'interface prioritaires), caméra, PlayerPrefs `best` et `rider`
- [x] T014 [US1] Hud.cs : accueil (4 cartes de skateurs, START, TOP 10, BEST), score et BEST en partie, WIPEOUT ! avec score, record et « Changer de skateur », toucher pour rejouer, zones sûres
- [ ] T015 [US1] Jouer une partie complète sur l'iPhone selon quickstart.md §3 et noter les réglages de difficulté à revoir

**Checkpoint**: MVP jouable de bout en bout (installé sur l'iPhone, à confirmer en jouant).

---

## Phase 4: User Story 2 - Figures, grinds, éclairs et combos (Priority: P2)

### Tests for User Story 2

- [x] T016 [P] [US2] Tests FR-010 (ordre et points des 4 figures × combo, une par saut, un toucher au sol = ollie seul, le toucher suivant en l'air = figure), FR-011 (grind +2 par pas, GRIND! 20 × combo), FR-012 (OUT! +15, saut à 85 %)
- [x] T017 [P] [US2] Tests FR-013 (10 × combo, rangées hors obstacles, 3 hauteurs), FR-014 (combo remis à 1 après 132 pas)

### Implementation for User Story 2

- [x] T018 [US2] Figures par un toucher en l'air (2 touchers en tout, écart de 2026-10-30 à la spec d'origine), durée d'animation ajustée au temps restant avant la retombée
- [x] T019 [US2] Grind sur le dessus des rails, sortie en bout de rail ou par OUT!
- [x] T020 [US2] Éclairs : rangées de 3 à 5, toujours sur montée ou début de descente, sinon 38 % ; ramassage
- [x] T021 [US2] Affichage : rails (normal, arc-en-ciel, nose grind), éclairs, COMBO xN, textes flottants « +30 FLIP ! », « +20 GRIND! », « +15 OUT! »

---

## Phase 5: User Story 3 - Décors et boss (Priority: P3)

### Tests for User Story 3

- [x] T022 [P] [US3] Tests FR-015 et FR-016 (cycle de 20 000, FULL SPEED tous les 5 000)
- [x] T023 [P] [US3] Tests FR-017 (boss à 15 000, 8 000 PV, sol plat, plus d'obstacles normaux), FR-018 (une dent tous les 22 pas, envol à 10 s, 4 plots), FR-019 (+5 000, nose grind × 15, plat de 2 500, prochain boss à score + 15 000), FR-020 (raccourci du soleil), plot lâché posé sur une pente

### Implementation for User Story 3

- [x] T024 [US3] Thèmes, paliers et grand rail tous les 5 000 dans Run.cs
- [x] T025 [US3] Boss.cs et son déroulé dans Run.cs (arrivée, au sol, montée, en l'air avec 4 plots, descente, mort), dents qui visent le boss
- [x] T026 [US3] Affichage du boss (boule violette ailée, œil qui suit le skateur, ombre), des dents, des plots violets qui tombent, de la barre de vie, des bandeaux BOSS !, BOOM et TU ES UN VRAI PST !, confettis ; soleil touchable à l'accueil

---

## Phase 6: Rendu et son (FR-021, FR-024, FR-025)

- [x] T027 Paint.cs : formes en maillages à couleurs de sommets (polygones convexes, cercles, capsules, courbes, dégradés, halos) et Fader pour les fondus
- [x] T028 Scenery.cs et Look.cs : ciel en dégradé, soleil et halo, reflet sur l'océan, 4 plans de parallaxe par décor (Largenté : caps, océan et surfeurs, palmiers, promenade ; BTZ : phare, villas, réverbères ; Arcangues : Pyrénées, collines, église et fronton, arbres), fondu entre décors, route avec trottoir
- [x] T029 RiderView.cs : skateur articulé (tenue construite par Wardrobe.Build), jambes en cinématique inverse et semelles collées au grip, poussée, accroupi, ollie, grind, chute avec planche qui rebondit
- [x] T030 RiderView.cs : planche redessinée (profil, dessus, dessous) ; FLIP et SHOVE-IT sous les pieds levés, 360° et 360° INVERSÉ en saltos groupés
- [x] T031 Props.cs et Fx.cs : plots, rails, éclairs, ombres douces ; poussière, étincelles de grind, éclats, confettis
- [x] T032 Sound.cs : musique 132 bpm (la mineur, fa, do, sol), bruitages saut, retombée, figure, éclair, grind en boucle, chute, boss, BOOM ; bouton SON/MUET gardé dans PlayerPrefs `mute`
- [ ] T033 [P] Un bruitage différent pour chaque figure (FLIP, SHOVE-IT, 360°, 360° INVERSÉ)
- [x] T046 [FR-026] Profil : plus de choix entre 4 skateurs ; Outfit et Wardrobe dans Look.cs (coiffures, peaux, cheveux, palette de 14 couleurs), écran PROFIL à onglets (TÊTE, PEAU, HAUT, BAS, PIEDS, PLANCHE) avec aperçu animé, tenue gardée dans PlayerPrefs `outfit`, bouton PROFIL à l'accueil et « CHANGER MA TENUE » à la fin
- [x] T047 [FR-002] Nouveau bouton START (pilule en relief, icône lecture, respiration), PROFIL en haut à droite, TOP 10 seul en bas
- [x] T048 [FR-027] Unlocks.cs (prix des couleurs, coiffures, coupes) et son test ; points cumulés dans PlayerPrefs `total` ; pastilles et choix verrouillés avec leur prix dans le profil ; annonce des nouveautés à la fin d'une partie
- [x] T049 [FR-021, FR-028] Onglet RÉGLAGES du profil (son, vibrations) ; Haptics.cs (Taptic Engine par Assets/Plugins/iOS/PalmHaptics.mm, Vibrator sur Android) ; PlayerPrefs `vibrate` ; bouton SON seulement en partie
- [x] T050 [FR-026] Profil en deux onglets : TENUE (aperçu, puis TÊTE, PEAU, HAUT, BAS, PIEDS, PLANCHE) et RÉGLAGES (son, vibrations)
- [x] T051 [US5] Niveau 2 NUIT NÉON (FR-029 à FR-032) : Level, Theme.Neon, vitesse 5,0 ; trous (Ground.Gaps, Abyss, chute et bord), mouettes, tremplins et MÉGA AIR à deux figures ; décor néon (étoiles, grande roue, ville, palmiers, enseignes, réverbères), bords de trous lumineux, feux d'artifice ; 5 tests, dont un contre le blocage d'un obstacle placé après un trou
- [x] T052 [FR-021, FR-028] En partie : le bouton SON devient RÉGLAGES (pause, son, vibrations, REPRENDRE, QUITTER LA PARTIE)
- [ ] T034 Remplacer les pièces dessinées en code par les PNG de Art/cahier-des-charges-skateurs.md quand ils sont livrés (Assets/Resources/Skaters/<skateur>/), mêmes articulations

---

## Phase 7: User Story 4 - Classement et réglages (Priority: P4)

- [x] T035 [US4] Leaderboard.cs : Game Center sur iOS (connexion au lancement, score envoyé à chaque fin de partie, TOP 10 ouvre l'écran natif sur `palmstreet.top`), message d'indisponibilité hors ligne ou sans connexion
- [x] T036 [US4] Pause quand l'app passe en arrière-plan, musique coupée, reprise au toucher (FR-023)
- [ ] T037 [US4] Google Play Games : ajouter le plugin officiel (seul plugin tiers, imposé par FR-022), créer le classement « Meilleur score » dans la Play Console, brancher Leaderboard.cs sous `#if UNITY_ANDROID`
- [ ] T038 [US4] Vérifier sur l'iPhone : son coupé gardé au relancement, score visible dans le TOP 10 Game Center

---

## Phase 8: Polish & publication

- [ ] T039 Amender la constitution (principe I : la spec remplace le jeu web comme référence) avec /speckit-constitution
- [ ] T040 [P] Icône de l'app et écran de lancement dans le style du jeu (l'icône 1024 × 1024 manque pour l'App Store)
- [ ] T041 Profiler sur iPhone et Android pendant le boss : 60 images/s (SC-002) ; supprimer les allocations par image (maillages du ciel, du soleil et de la route reconstruits à chaque image) ; interpolation de l'affichage si les écrans 120 Hz saccadent
- [ ] T042 Mesurer le démarrage (< 3 s, SC-003, la synthèse du son compte) et la taille de l'app (< 150 Mo)
- [ ] T043 Build Android (.aab signé avec la clé hors du dépôt), essai sur l'émulateur ou un téléphone Android
- [ ] T044 Build iOS de distribution ; vérifier que `palmstreet.top` existe dans App Store Connect pour com.charlit.palmstreet
- [ ] T045 Fiches App Store et Google Play, captures (Tools/capture.sh), page de confidentialité ; envoi en review (SC-005)

---

## Dependencies & Execution Order

- Phases 1 à 6 faites, sauf T015 (essai sur téléphone), T033 (sons des figures) et T034 (dessins, quand ils arrivent).
- T037 (Play Games) et T043 (build Android) vont ensemble.
- T039 peut se faire tout de suite.
- T041 et T042 avant T044 et T045.

## Parallel Examples

- T033, T039 et T040 sont indépendantes.
- T037 et T043 (Android) en parallèle de T044 (iOS).

## Implementation Strategy

1. Jouer sur l'iPhone (T015, T038) et régler ce qui gêne.
2. Android : Play Games et build (T037, T043).
3. Icône, performances, builds de distribution, fiches et review (T040 à T045).
4. Dessins des skateurs dès qu'ils sont livrés (T034).
