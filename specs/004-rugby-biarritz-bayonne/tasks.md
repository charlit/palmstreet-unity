---

description: "Task list for 004 — Biarritz et Bayonne aux couleurs du rugby"
---

# Tasks: Biarritz et Bayonne aux couleurs du rugby

**Input**: Design documents from `/specs/004-rugby-biarritz-bayonne/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/rugby.md, quickstart.md

**Tests**: la constitution (principe IV) impose des tests en mode Edit pour les règles. Ils sont listés
dans research R8 et écrits avant le code de chaque story. Le dessin est vérifié par captures.

**Organization**: les tâches sont groupées par user story.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : la tâche peut être faite en parallèle (fichiers différents, aucune dépendance).
- **[Story]** : la user story de la spec concernée (US1 à US5).

---

## Phase 1: Setup

- [X] T001 Relire `Art/apercus/apercu-boss-rugby.png`, `apercu-boss-bayonne.png`, `apercu-biarritz.png`, `apercu-obstacles.png`, `tenues-rugby.png` et `apercu-idees-basques.png`, et noter les couleurs et proportions à reprendre

---

## Phase 2: Foundational (bloque les stories)

- [X] T002 Ajouter `Pilier` et `Talonneur` à la fin de `enum BossKind` dans `Assets/Scripts/Core/Levels.cs`
- [X] T003 Dans `Assets/Scripts/Core/Progress.cs`, limiter `Spells` aux boss Crabe à Violet (`l.Boss >= BossKind.Crab && l.Boss <= BossKind.Purple`)
- [X] T004 Ajouter `ObstacleKind.Ball`, le champ `Vx` d'`Obstacle` et `IsFlying` (`Gull` ou `Ball`) dans `Assets/Scripts/Core/Run.cs`. Faire bouger les obstacles selon `Vx` quand il est non nul. Traiter `IsFlying` comme la mouette dans `Collide` et partout où `ObstacleKind.Gull` sert de cas particulier.
- [X] T005 Ajouter les constantes dans `Assets/Scripts/Core/Tuning.cs` :
  - `RugbyWindup` = 36 ;
  - `PassSpeed` ;
  - `ChandelleLand` = 140 ;
  - `ChandelleBounce` = 26 ;
  - `FlyingRate` = 0.28 (taux par défaut des obstacles volants).

**Checkpoint**: le projet compile, et les 68 tests existants passent.

---

## Phase 3: User Story 1 - Affronter LE PILIER à Biarritz (Priority: P1) 🎯 MVP

**Goal**: le niveau 2 finit par LE PILIER (trottine, passe, chandelle, plaquage annoncés), qu'on bat avec dents et sorts.

**Independent Test**: tests des esquives verts ; capture du Pilier en course et en annonce de chaque attaque ; battre le Pilier réussit le niveau 2.

- [X] T006 [P] [US1] Écrire les tests en mode Edit dans `Assets/Tests/EditMode/RugbyTests.cs` :
  - le niveau 2 finit par le Pilier ;
  - la vie du Pilier est plus basse que celle du Crabe ;
  - les attaques tournent dans l'ordre passe → chandelle → plaquage ;
  - chaque attaque est annoncée au moins 30 pas avant de partir ;
  - passe : baissé, pas de chute ; debout, chute ;
  - chandelle : sauter au rebond, pas de chute ; rester au sol, chute ;
  - plaquage : saut, pas de chute ; au sol, chute ;
  - les dents et les sorts blessent le Pilier ;
  - à 0 de vie, le niveau est réussi ;
  - `Spells` ne contient pas de sort pour le Pilier.
- [X] T007 [US1] Dans `Assets/Scripts/Core/Boss.cs` :
  - vie du Pilier = 3000 ;
  - ajouter `IsRugby`, `Attack` et `Windup` ;
  - `Body` = (56, 20) pendant le plaquage, (0, 0) sinon ;
  - `Height` = 60 et `HitWidth` = 40 pour les boss de rugby.
- [X] T008 [US1] Écrire `UpdateRugby(Boss b)` dans `Assets/Scripts/Core/Run.cs`, appelée depuis `UpdateBoss` pour les boss de rugby :
  - **trot** : il trottine au sol à `Distance`, sans bondir ;
  - **annonce** : `RugbyWindup` pas, avant chaque action ;
  - **passe** : un `Ball` à hauteur de tête (`GullHeight`), qui fonce vers le skateur (`Vx = -PassSpeed`) ;
  - **chandelle** : un `Ball` lancé haut, qui retombe à `ChandelleLand` devant le skateur, puis rebondit vers lui sous `ChandelleBounce`, 3 rebonds au plus ;
  - **plaquage** : charge à `Lift = 0` jusqu'à `BehindSkater`, puis retour d'un bond par-dessus le skateur.
- [X] T009 [US1] Dans `Assets/Scripts/Core/Levels.cs`, mettre le niveau 2 à `Boss = Pilier`
- [X] T010 [P] [US1] Créer `Assets/Scripts/Game/RugbyView.cs`, un pantin `Paint` avec un contour d'encre comme le skateur de la spec 003 :
  - pièces : tête, bras, jambes, buste, ballon dans les mains ;
  - couleurs du Pilier : maillot rouge à cerceau et col blancs, short blanc, chaussettes rouges à bandes blanches, bandeau blanc, cheveux bruns ;
  - poses : course, passe (annonce puis lancer), chandelle (annonce puis coup de pied), plaquage (à l'horizontale), chute.
- [X] T011 [US1] Dans `Assets/Scripts/Game/Props.cs` :
  - créer le `RugbyView` du Pilier et l'afficher pour `BossKind.Pilier`, avec la pose selon `State`, `Attack`, `Windup` et `Timer` ;
  - ajouter un pool `balls` : ellipse blanche à bandes rouges et coutures, qui tourne sur elle-même, avec une ombre au sol ;
  - y dessiner les obstacles `Ball`.
- [X] T012 [US1] Dans `Assets/Scripts/Game/Game.cs` :
  - `BossName` : `Pilier` → « LE PILIER » ;
  - `BossTip` : « Baisse-toi pour la passe, saute la chandelle et le plaquage ! » ;
  - vérifier que la carte et l'aperçu (`HudLevels.cs`, `HudExtras.cs`) marquent le niveau 2 BOSS avec ce nom.
- [X] T013 [US1] Ajouter dans `Assets/Tests/PlayMode/CaptureTests.cs` les captures du Pilier : en course, en annonce de passe, de chandelle et de plaquage, ballon en vol. Lancer `Tools/run-tests.sh` et `Tools/capture.sh`.

**Checkpoint**: le niveau 2 se joue jusqu'au Pilier et se gagne.

---

## Phase 4: User Story 3 - Affronter LE TALONNEUR à Bayonne (Priority: P2)

**Goal**: le niveau 7 finit par LE TALONNEUR, qui se joue comme le Pilier, en bleu ciel, crâne rasé et barbe.

**Independent Test**: test « niveau 7 = Talonneur, vie entre Goéland et Sanglier » vert ; capture du Talonneur.

- [X] T014 [P] [US3] Ajouter dans `Assets/Tests/EditMode/RugbyTests.cs` les tests :
  - le niveau 7 finit par le Talonneur ;
  - sa vie est entre celle du Goéland et celle du Sanglier ;
  - il fait les mêmes attaques que le Pilier ;
  - les vies suivent l'ordre Pilier < Crabe < Goéland < Talonneur < Sanglier < Violet.
- [X] T015 [US3] Mettre la vie du Talonneur à 5500 dans `Assets/Scripts/Core/Boss.cs`, et `Boss = Talonneur` au niveau 7 dans `Assets/Scripts/Core/Levels.cs`
- [X] T016 [US3] Dans `Assets/Scripts/Game/RugbyView.cs`, ajouter la variante Talonneur : bleu ciel et blanc, crâne rasé, barbe. Dans `Props.cs`, l'afficher pour `BossKind.Talonneur`. Dans `Game.cs`, `BossName` → « LE TALONNEUR ».
- [X] T017 [US3] Ajouter la capture du Talonneur dans `Assets/Tests/PlayMode/CaptureTests.cs`

---

## Phase 5: User Story 2 - Gagner la tenue complète d'un boss de rugby (Priority: P2)

**Goal**: battre un boss de rugby pour la première fois donne ses 5 pièces, annoncées à l'écran de réussite et portables.

**Independent Test**: tests `OutfitFor` et `Owns` verts ; captures du profil avec les deux tenues complètes.

- [X] T018 [P] [US2] Écrire les tests en mode Edit dans `Assets/Tests/EditMode/RugbyTests.cs` :
  - `Items.OutfitFor(2)` et `OutfitFor(7)` rendent 5 pièces chacune, une par emplacement ;
  - aucune pièce de rugby n'est possédée avant la réussite, toutes le sont après ;
  - `Items.ForLevel(2)` rend toujours l'objet habituel (`hoodie-ocean`) ;
  - `ForLevel(7)` rend toujours `band-red` ;
  - les niveaux sans boss de rugby (`OutfitFor(3)`, `OutfitFor(6)`) ne rendent rien.
- [X] T019 [US2] Dans `Assets/Scripts/Core/Items.cs`, ajouter le champ `Item.Boss` et les 10 pièces de rugby, déclarées après les objets existants :
  - Pilier : `band-pilier`, `jersey-pilier`, `shorts-pilier`, `boots-pilier`, `deck-pilier` ;
  - Talonneur : `scrum-cap`, `jersey-talonneur`, `shorts-talonneur`, `boots-talonneur`, `deck-talonneur` ;
  - ajouter aussi `OutfitFor(int level)`.
- [X] T020 [US2] Dans `Assets/Scripts/Game/Look.cs` :
  - ajouter `Hat.Scrum` ;
  - couleurs : `ItemColor` et `ItemDetail` des 10 pièces ;
  - `Wardrobe.Build` : bandeau à liseré, casque de mêlée, maillot, short + chaussettes (`Shorts`, `Accent`), baskets noires, planche.
- [X] T021 [US2] Dans `Assets/Scripts/Game/RiderView.cs`, dessiner les motifs :
  - bandeau blanc à liseré rouge ;
  - casque de mêlée bleu ciel à bandes blanches (`Hat.Scrum`) ;
  - maillot à cerceau et col (`TopDesign`) ;
  - baskets noires à bande de couleur ;
  - planche de couleur à roues blanches.
- [X] T022 [US2] Annoncer la tenue sur l'écran de réussite, la première fois seulement (« NOUVELLE TENUE : LE PILIER » / « LE TALONNEUR »). Fichiers : `Assets/Scripts/Game/Game.cs` (réussite d'un niveau) et `Assets/Scripts/Game/HudLevels.cs`.
- [X] T023 [US2] Dans le profil (`Assets/Scripts/Game/Hud.cs`), afficher « BATS LE PILIER » ou « BATS LE TALONNEUR » pour une pièce de rugby non gagnée
- [X] T024 [US2] Ajouter dans `Assets/Tests/PlayMode/CaptureTests.cs` les captures du profil avec la tenue complète du Pilier, puis celle du Talonneur. Comparer avec `tenues-rugby.png`.

---

## Phase 6: User Story 4 - Biarritz reconnaissable : stade, palace et rocher (Priority: P2)

**Goal**: le niveau 2 montre le stade Aguiléra avec son tableau « BIARRITZ vs BAYONNE », l'Hôtel du Palais et le Rocher de la Vierge, sans aucun logo.

**Independent Test**: captures du niveau 2 à trois moments du parcours.

- [X] T025 [US4] Dessiner l'Hôtel du Palais dans la couche du milieu (`Decor.Btz`) de `Assets/Scripts/Game/Scenery.cs`, à x 10–250, à la place des maisons :
  - brique et pierre ;
  - toits d'ardoise très pentus ;
  - trois pavillons ;
  - fenêtres éclairées ;
  - terrasse.
- [X] T026 [US4] Dessiner le stade Aguiléra dans la même couche, à x 290–630 :
  - tribune aux sièges rouges et blancs ;
  - toit en porte-à-faux ;
  - pylônes allumés avec halo ;
  - poteaux en H.
- [X] T027 [US4] Ajouter le tableau du stade : un `TextMesh` en Rubik, « BIARRITZ » rouge, « vs », « BAYONNE » bleu ciel, sur un fond sombre.
  - Il est enfant des deux copies de la tuile, avec un ordre de tri au-dessus de la couche.
  - Il est masqué quand le décor de Biarritz n'est pas affiché ; son alpha suit le fondu.
- [X] T028 [US4] Dessiner le Rocher de la Vierge dans la couche lointaine (`Decor.Btz`), à x ≈ 150 : rocher dans l'océan, statue blanche sur socle, passerelle en treillis reliée à la falaise. Le phare existant est gardé.
- [X] T029 [US4] Ajouter dans `Assets/Tests/PlayMode/CaptureTests.cs` trois captures du niveau 2 : le stade, le palace, le rocher. Vérifier qu'aucun logo ni sigle n'apparaît et que le skateur et les obstacles restent lisibles.

---

## Phase 7: User Story 5 - Mouettes redessinées et ballons de rugby (Priority: P3)

**Goal**: mouette redessinée, sans halo le jour ; ballons volants aux niveaux 2 et 12 ; le tutoriel dit « mouettes ».

**Independent Test**: tests des ballons volants verts ; captures du niveau 2, du niveau 12 et du tutoriel.

- [X] T030 [P] [US5] Écrire les tests en mode Edit dans `Assets/Tests/EditMode/RugbyTests.cs` :
  - le niveau 2 fait naître des ballons volants, et aucune mouette ;
  - le niveau 12 fait naître des ballons et des mouettes ;
  - un ballon volant : baissé, pas de chute ; debout, chute ;
  - le conseil du niveau 2 parle de se baisser ;
  - le niveau 7 n'a plus ce conseil.
- [X] T031 [US5] Dans `Assets/Scripts/Core/Levels.cs` et `Assets/Scripts/Core/Run.cs` :
  - niveau 2 : `Allow.Gulls` et `FlyingRate` 0.12 ;
  - les thèmes `Biarritz` et `BiarritzNuit` tirent un ballon une fois sur deux à la place de la mouette (`PlaceNeon`) ;
  - conseils : niveau 2 « Ballons : glisse vers le bas pour te baisser », conseil du rail déplacé au niveau 3, celui du niveau 7 retiré.
- [X] T032 [US5] Redessiner la mouette (pool `gulls`) dans `Assets/Scripts/Game/Props.cs` :
  - dos gris perle, dessous blanc ;
  - bouts d'ailes noirs ;
  - bec jaune à tache rouge ;
  - halo rose seulement si `!Levels.IsDay(theme)`.
- [X] T033 [US5] Dans `Assets/Scripts/Game/HudExtras.cs`, le tutoriel dit « (mouettes) » au lieu de « (oiseaux) » ; vérifier qu'aucun autre texte ne dit « oiseaux »
- [X] T034 [US5] Ajouter dans `Assets/Tests/PlayMode/CaptureTests.cs` les captures :
  - un ballon volant au niveau 2 ;
  - un ballon et une mouette au niveau 12 ;
  - une mouette de jour, sans halo.

---

## Phase 8: Polish & Cross-Cutting

- [X] T035 Lancer `Tools/run-tests.sh` (tout vert) et `Tools/capture.sh <scratchpad>`, et relire les captures selon `quickstart.md`
- [X] T036 Régler la vie du Pilier et du Talonneur et le rythme des attaques, en jouant ou par test, pour viser SC-003 : le Pilier en moins de 60 s, le Talonneur en moins de 90 s, en esquivant bien
- [X] T037 Lancer `Tools/build-ios.sh` puis `Tools/build-android.sh`, installer sur l'iPhone et sur l'émulateur, puis vérifier le niveau 2 complet et une partie fluide
- [X] T038 Demander au propriétaire avant tout commit et tout push

---

## Dependencies & Execution Order

- Les phases 1 et 2 passent avant tout le reste.
- US1 (P1) est la base : US3 réutilise son code ; US2 a besoin des boss pour être gagnée en jeu, mais ses tests et le dessin des tenues sont indépendants.
- US4 (décor) et US5 (obstacles) ne dépendent que de la phase 2.
- `Run.cs` est touché par T004, T008 et T031, `Props.cs` par T011, T016 et T032 : ces tâches se font l'une après l'autre.

## Parallel Opportunities

- T006, T010 et T018 peuvent être faites en même temps (fichiers de test et nouveau fichier de dessin).
- T014 et T030 peuvent aussi être faites en même temps.
- Le décor (T025 à T028, `Scenery.cs`) avance en parallèle des boss (`Run.cs`, `RugbyView.cs`).

## Implementation Strategy

1. **MVP** : phases 1 et 2, puis US1 (le Pilier jouable au niveau 2).
2. US3 (le Talonneur, presque gratuit), puis US2 (les tenues).
3. US4 (le décor), puis US5 (mouettes et ballons volants).
4. Réglage, builds iOS et Android, vérification sur appareil.
