---

description: "Task list for 005 — Pelote basque à Arcangues"
---

# Tasks: Pelote basque à Arcangues

**Input**: Design documents from `/specs/005-pelote-arcangues/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/pelote.md, quickstart.md

**Tests**: la constitution (principe IV) impose des tests en mode Edit pour les règles. Ils sont listés
dans research R6 et écrits avant le code de chaque story. Le dessin est vérifié par captures.

**Organization**: les tâches sont groupées par user story.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : la tâche peut se faire en parallèle (fichiers différents, aucune dépendance).
- **[Story]** : la user story de la spec concernée (US1 à US4).

---

## Phase 1: Setup

- [X] T001 Relire `Art/apercus/apercu-idees-basques.png` et `apercu-planche-en-main.png`, et noter les couleurs, les proportions du chistera et la pose du bouclier

---

## Phase 2: Foundational (bloque les stories)

- [X] T002 Ajouter `BossKind.Pelotari` à la fin de l'enum dans `Assets/Scripts/Core/Levels.cs`. Vérifier que `Levels.SpellOf` rend −1 pour lui.
- [X] T003 Dans `Assets/Scripts/Core/Run.cs` :
  - ajouter `ObstacleKind.Pelote` ;
  - ajouter les champs `Reflected` et `FromBoss` d'`Obstacle` ;
  - faire inclure `Pelote` par `IsFlying` ;
  - faire bouger les pelotes par `MoveBall` (`Vx`, `Bounces`), comme les ballons ;
  - ajouter les événements `Cue.Parry`, `Cue.Block` et `Cue.Boomerang`.
- [X] T004 Ajouter les constantes dans `Assets/Scripts/Core/Tuning.cs` : `ParryWindow` = 9, `TapSteps` = 10, `PelotaAttackEvery`, `PelotaThrowSpeed`, `PelotaFlySpeed` = 3.2, `PelotaBounce` = 22, `ParryDamage`, `BoomerangDamage`, `BoomerangSpeed`, `BoomerangOut` = 40

**Checkpoint**: le projet compile, et les 80 tests existants passent.

---

## Phase 3: User Story 1 - Affronter LE PELOTARI (Priority: P1) 🎯 MVP (avec US2)

**Goal**: le niveau 8 finit par LE PELOTARI ; lancer et rebond annoncés, qui font chuter si on ne réagit pas.

**Independent Test**: tests verts ; capture du Pelotari en élan et en annonce de chaque attaque.

- [X] T005 [P] [US1] Écrire les tests en mode Edit dans `Assets/Tests/EditMode/PeloteTests.cs` :
  - le niveau 8 finit par le Pelotari ;
  - les vies suivent l'ordre Talonneur < Pelotari < Sanglier ;
  - le Pelotari ne donne pas de sort ;
  - les attaques alternent lancer et rebond ;
  - chaque attaque est annoncée au moins 30 pas avant ;
  - lancer sans réaction : chute ; lancer en étant baissé : pas de chute ;
  - rebond sans réaction : chute ; rebond en sautant : pas de chute.
- [X] T006 [US1] Dans `Assets/Scripts/Core/Boss.cs` : vie du Pelotari = 5800, `Body` (0, 0), `HitWidth` 40, `Height` 60. Étendre `Windup` au Pelotari.
- [X] T007 [US1] Dans `Assets/Scripts/Core/Run.cs`, écrire `UpdatePelotari(Boss b)` :
  - prise d'élan au sol ;
  - annonce de `RugbyWindup` pas ;
  - lancer : `Pelote` à hauteur de tête, avec `FromBoss` ;
  - rebond : `Pelote` qui rebondit vers le skateur sous `PelotaBounce`, avec `FromBoss`.
  Mettre `Boss = Pelotari` au niveau 8 dans `Levels.cs`.
- [X] T008 [P] [US1] Créer `Assets/Scripts/Game/PelotariView.cs`, un pantin avec contour d'encre :
  - tenue : maillot rouge numéroté 7, pantalon blanc, casque blanc à bande rouge ;
  - chistera en osier au bras avant ;
  - poses : élan, lancer (armé puis fouetté), rebond (armé bas puis lancé), chute.
- [X] T009 [US1] Dans `Assets/Scripts/Game/Props.cs` :
  - créer et afficher le `PelotariView` pour `BossKind.Pelotari` ;
  - ajouter un pool `pelotes` : balle blanche cousue avec traits de vitesse, qui tourne ;
  - y dessiner les obstacles `Pelote`.
- [X] T010 [US1] Dans `Assets/Scripts/Game/Game.cs` : `BossName` → « LE PELOTARI » ; `BossTip` → « MAINTIENS LE DOIGT : BOUCLIER »

---

## Phase 4: User Story 2 - Combattre « planche en main » (Priority: P1) 🎯 MVP

**Goal**: pendant le combat, le skateur est à pied. Bouclier, parade, boomerang, saut au toucher, sorts ; pas de figure.

**Independent Test**: tests du bouclier, de la parade, du boomerang et du saut verts ; captures du skateur à pied.

- [X] T011 [P] [US2] Ajouter les tests dans `Assets/Tests/EditMode/PeloteTests.cs` :
  - à pied pendant le combat (`OnFoot`), de nouveau sur la planche après la victoire ;
  - bouclier levé tôt : la pelote est bloquée, pas de chute, pas de dégâts ;
  - bouclier levé dans les 9 pas : parade, la pelote revient et le boss perd de la vie ;
  - toucher bref : saut ; doigt maintenu : pas de saut ;
  - boomerang : blesse le boss une fois, revient en moins de 90 pas, bouclier impossible pendant le vol, un seul à la fois ;
  - un glissé en diagonale ne fait pas de figure ;
  - le glissé vers le bas baisse le skateur ;
  - `Cast` marche contre le Pelotari.
- [X] T012 [US2] Dans `Assets/Scripts/Core/Run.cs`, ajouter l'état du mode à pied : `OnFoot`, `ShieldUp`, `ShieldSince` et la classe `Boomerang`
- [X] T013 [US2] Dans `Assets/Scripts/Core/Run.cs`, faire passer les gestes par le mode à pied quand `OnFoot` est vrai :
  - `Press` lève le bouclier (si la planche est dans la main), sans ollie ;
  - `Release` baisse le bouclier et fait sauter si le doigt est resté moins de `TapSteps` et que le skateur est au sol ;
  - `Swipe` vers le boss lance le boomerang ; vers le bas, il se baisse ; les autres directions ne font rien.
- [X] T014 [US2] Dans `Collide` (`Assets/Scripts/Core/Run.cs`), gérer une pelote du boss à hauteur de tête quand le bouclier est levé :
  - parade si le bouclier est levé depuis au plus `ParryWindow` pas : la pelote est renvoyée et un événement `Cue.Parry` est émis ;
  - blocage sinon : la pelote disparaît, avec `Cue.Block`.
  Une pelote renvoyée qui touche le boss lui fait `ParryDamage` de dégâts, et `Defeat` s'il n'a plus de vie.
- [X] T015 [US2] Mettre à jour le boomerang à chaque pas dans `Assets/Scripts/Core/Run.cs` :
  - aller, puis retour au bout de `BoomerangOut` pas ou dès qu'il touche le boss (`BoomerangDamage`, une seule fois) ;
  - rattrapé quand il revient à hauteur du skateur.
- [X] T016 [US2] Dans `Assets/Scripts/Game/RiderView.cs`, ajouter une branche à pied, utilisée quand `run.OnFoot` est vrai :
  - jambes qui courent ;
  - planche en bouclier devant le buste, ou portée sous le bras ;
  - planche en boomerang à la position du Core, qui tourne.
  Les poses existantes ne changent pas.
- [X] T017 [US2] Brancher l'interface dans `Assets/Scripts/Game/Game.cs` et `Hud*.cs` :
  - `Cue.Parry` → « PARADE ! » en point flottant, avec vibration ;
  - après le premier blocage ou la première parade, une bulle « GLISSE VERS LE BOSS : BOOMERANG », une fois par combat ;
  - un son pour le blocage et le boomerang.
- [X] T018 [US2] Ajouter les captures dans `Assets/Tests/PlayMode/CaptureTests.cs` :
  - le skateur à pied en début de combat ;
  - le bouclier levé ;
  - la parade ;
  - le boomerang en vol ;
  - les annonces du lancer et du rebond.

---

## Phase 5: User Story 3 - Arcangues : fronton, clocher, platanes (Priority: P2)

**Goal**: décor du niveau 8 reconnaissable ; combat devant le fronton.

**Independent Test**: captures du niveau 8.

- [X] T019 [US3] Dans `Assets/Scripts/Game/Scenery.cs` (couche du milieu, cas Arcangues), remplacer la silhouette du fronton par un fronton en couleurs sur x 380–600 :
  - mur ocre au sommet en arc ;
  - liseré rouge qui suit l'arc, et deux lignes rouges horizontales ;
  - bande de métal en bas.
  Rendre le clocher plus haut, avec un abat-son éclairé.
- [X] T020 [US3] Au premier plan (cas Arcangues) de `Scenery.cs`, remplacer les arbres ronds par des platanes taillés : tronc tacheté, moignons de branches, couronne plate
- [X] T021 [US3] Ajouter dans `CaptureTests.cs` une capture du niveau 8 où l'on voit le fronton, le clocher et les platanes

---

## Phase 6: User Story 4 - La pelote volante (Priority: P2)

**Goal**: pelotes volantes aux niveaux 8 à 11, 2 fois plus rapides que la mouette, évitées en se baissant.

**Independent Test**: tests verts ; capture d'une pelote volante.

- [X] T022 [P] [US4] Ajouter les tests dans `Assets/Tests/EditMode/PeloteTests.cs` :
  - les niveaux 8 à 11 produisent des pelotes volantes ;
  - une pelote volante va plus vite qu'une mouette ;
  - baissé, le skateur l'évite ; debout, il chute.
- [X] T023 [US4] Dans `PlaceNeon` (`Assets/Scripts/Core/Run.cs`), pour les thèmes Arcangues, Espelette, Ascain et Cambo, tirer une pelote (`Vx = -PelotaFlySpeed`) une fois sur deux à la place de la mouette
- [X] T024 [US4] Ajouter dans `CaptureTests.cs` une capture d'une pelote volante au niveau 8

---

## Phase 7: Polish & Cross-Cutting

- [X] T025 Lancer `Tools/run-tests.sh` (tout vert) et `Tools/capture.sh <scratchpad>`, et relire les captures selon `quickstart.md`
- [X] T026 Régler la vie du Pelotari, les dégâts de la parade et du boomerang et le rythme des attaques, pour viser SC-004 : le battre en moins de 90 s en jouant bien
- [X] T027 Lancer `Tools/build-ios.sh` puis `Tools/build-android.sh`, installer sur l'iPhone et sur l'émulateur, puis vérifier le lancement sans erreur
- [X] T028 Demander au propriétaire avant tout commit et tout push

---

## Dependencies & Execution Order

- Les phases 1 et 2 passent avant tout le reste.
- US1 et US2 forment le MVP ensemble : le Pelotari ne se bat qu'à pied. US2 dépend de US1 (le boss et ses pelotes).
- US3 (décor, `Scenery.cs`) et US4 (pelote volante) ne dépendent que de la phase 2.
- `Run.cs` est touché par T003, T007, T012 à T015 et T023 : ces tâches se font l'une après l'autre.

## Parallel Opportunities

- T005, T008 et T011 peuvent être faites en même temps (tests et nouveau fichier de dessin).
- US3 (`Scenery.cs`) avance en parallèle de US2 (`Run.cs`, `RiderView.cs`).

## Implementation Strategy

1. **MVP** : phases 1 et 2, puis US1 et US2 : le combat planche en main jouable.
2. US4 (pelote volante), puis US3 (décor).
3. Réglage, builds iOS et Android.
