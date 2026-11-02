---

description: "Task list for 003 — refonte graphique du texte de l'interface et du skateur"
---

# Tasks: Refonte graphique du texte de l'interface et du skateur

**Input**: Design documents from `/specs/003-refonte-texte-skateur/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-text-and-rider.md, quickstart.md

**Tests**: la constitution (principe IV) impose un test en mode Edit pour la seule règle nouvelle
(`Levels.IsDay`). Le dessin est vérifié par les captures (`Tools/capture.sh`).

**Organization**: les tâches sont groupées par user story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: peut être faite en parallèle (fichiers différents, aucune dépendance)
- **[Story]**: user story de la spec (US1 à US5)

---

## Phase 1: Setup

- [X] T001 Vérifier que `Assets/Resources/Fonts/Rubik-Bold.ttf` (Rubik 700 statique) et `Assets/Resources/Fonts/OFL.txt` sont présents, puis ouvrir le projet en lot une fois pour qu'Unity crée les `.meta` (import en police dynamique)
- [X] T002 Faire des captures de référence AVANT tout changement (`Tools/capture.sh`) et les copier dans `/private/tmp/…/scratchpad/avant-003/`, pour comparer les poses (SC-003)

---

## Phase 2: Foundational

- [X] T003 [P] Ajouter la primitive `Taper(x0, y0, x1, y1, w0, w1, col)` (capsule à bouts ronds de largeurs différentes) dans `Assets/Scripts/Game/Paint.cs`

---

## Phase 3: User Story 1 - Lire un texte net partout (Priority: P1) 🎯 MVP

**Goal**: tout le texte de l'interface est en Rubik Bold, sans ombre, identique sur iPhone et Android.

**Independent Test**: captures de l'accueil, de la carte, du profil (FIGURES), des réglages et du tutoriel : police Rubik, aucune ombre, aucun texte qui déborde.

- [X] T004 [US1] Dans `Hud.Init` (`Assets/Scripts/Game/Hud.cs`), charger `Resources.Load<Font>("Fonts/Rubik-Bold")` ; si `null`, garder `Font.CreateDynamicFontFromOSFont(...)` actuel
- [X] T005 [US1] Dans `Hud.Label` (`Assets/Scripts/Game/Hud.cs`), supprimer l'ajout du composant `Shadow` ; supprimer le `Destroy(...GetComponent<Shadow>())` du libellé de JOUER
- [X] T006 [P] [US1] Supprimer la méthode `Sharp()` et ses appels dans `Assets/Scripts/Game/HudLevels.cs`
- [X] T007 [US1] Lancer `Tools/capture.sh`, regarder chaque écran ; pour chaque libellé qui déborde de son bouton ou de son cadre, activer `resizeTextForBestFit` (min. 70 % de la taille) à l'endroit où il est créé (`Hud.cs`, `HudExtras.cs`, `HudLevels.cs`)
- [X] T008 [US1] Vérifier dans les captures les textes avec accents et « œ » (« au cœur du Pays basque », GUÉTHARY, RÉGLAGES) et le titre animé du lancement (`Splash`)

**Checkpoint**: US1 complète et vérifiable seule.

---

## Phase 4: User Story 2 - Lire le texte sur tous les ciels (Priority: P1)

**Goal**: sur les 6 ciels de jour, bandeaux, figures annoncées et points sont en `Night` ; ailleurs, couleurs actuelles ; le haut de l'écran reste crème.

**Independent Test**: test `IsDay` vert ; captures d'une partie à Biarritz (texte sombre) et à Anglet (couleurs actuelles).

- [X] T009 [P] [US2] Écrire le test en mode Edit `IsDay` dans `Assets/Tests/EditMode/LevelTests.cs` : vrai pour Biarritz, Bidart, Guethary, Bayonne, Espelette, Cambo ; faux pour les 6 autres thèmes (le test échoue avant T010)
- [X] T010 [US2] Ajouter `public static bool IsDay(Theme t)` dans `Assets/Scripts/Core/Levels.cs`
- [X] T011 [US2] Dans `Assets/Scripts/Game/Hud.cs`, quand un bandeau s'affiche (`banners` dans la boucle de mise à jour) et dans `Float`, utiliser `Night` pour le texte et le sous-bandeau si `Levels.IsDay(run.Theme)`, sinon la couleur demandée
- [X] T012 [US2] Dans `Shout` (`Assets/Scripts/Game/HudExtras.cs`), utiliser `Night` si la ville en cours est de jour (le `Hud` garde le thème en cours à la mise à jour)
- [X] T013 [US2] Ajouter dans `Assets/Tests/PlayMode/CaptureTests.cs` une capture d'une partie à Biarritz avec bandeau, figure annoncée et points affichés ; vérifier aussi qu'à Anglet les couleurs ne changent pas et que le haut de l'écran reste crème
- [X] T014 [US2] Lancer `Tools/run-tests.sh` : tous les tests en mode Edit passent

**Checkpoint**: US1 + US2 = texte terminé.

---

## Phase 5: User Story 4 - Un genou sans couture (Priority: P2)

Fait avant US3, car il pose la technique du contour en 2 passes que US3 réutilise pour chaque pièce.

**Goal**: contour d'encre de la jambe entière posé avant ses couleurs ; aucun trait au genou à tout angle.

**Independent Test**: capture du skateur debout, accroupi et baissé : aucun trait au genou (comparer `Art/apercus/skateur-genoux.png`).

- [X] T015 [US4] Dans `Assets/Scripts/Game/RiderView.cs`, ajouter la constante `Outline` (unités du skateur) et la couleur d'encre `#1B1028` ; dans `MakeLeg`, construire pour chaque pièce (cuisse, tibia, chaussure) un maillage de contour (silhouette gonflée de `Outline`, en encre) à l'ordre `order` et le maillage des couleurs à `order + 1`, la chaussure à `order + 2` ; ajuster les ordres des autres membres dans `Build` pour garder jambe arrière < bras arrière < buste < jambe avant < tête < bras avant
- [X] T016 [US4] Dans `MakeLeg`, dessiner la cuisse avec `Taper` : largeur de hanche en haut, largeur du tibia au genou, pour que les deux demi-disques au genou aient le même rayon et le même centre
- [X] T017 [US4] Ajouter dans `Assets/Tests/PlayMode/CaptureTests.cs` un gros plan du skateur accroupi (réception d'ollie) et baissé (`Duck`) ; vérifier debout, accroupi et baissé : aucun trait au genou

**Checkpoint**: genou continu sur l'ancien dessin des jambes.

---

## Phase 6: User Story 3 - Un skateur plus détaillé (Priority: P2)

**Goal**: tête de profil, mains, coude, buste ombré, plis, chaussures détaillées, 2 à 3 tons et le même contour partout ; mêmes poses.

**Independent Test**: capture de la pose d'accueil comparée à `Art/apercus/skateur-avant-apres.png` (APRÈS) ; captures de chaque animation comparées aux captures de T002 : articulations au même endroit.

- [X] T018 [US3] Redessiner `Head` dans `Assets/Scripts/Game/RiderView.cs` :
  - contour + couleurs ;
  - profil avec front, nez, lèvres, menton ;
  - oreille avec creux, joue, sourcil ;
  - œil : blanc, iris, reflet ;
  - cheveux qui suivent le crâne pour COURTS et BOUCLES ;
  - casquette, bonnet et bandana repris sur le nouveau crâne.
- [X] T019 [US3] Redessiner `Arm` dans `Assets/Scripts/Game/RiderView.cs` :
  - contour + couleurs ;
  - manche courte (t-shirt) ou longue (sweat avec poignet) ;
  - coude marqué ;
  - main avec paume, pouce et doigts.
- [X] T020 [US3] Redessiner le buste dans `Build` (`Assets/Scripts/Game/RiderView.cs`) :
  - contour + couleurs ;
  - épaules arrondies, ombre côté dos, lumière côté poitrine, 2 plis ;
  - bassin de la couleur du bas ;
  - le logo et `TopDesign` restent par-dessus.
- [X] T021 [US3] Compléter `MakeLeg` dans `Assets/Scripts/Game/RiderView.cs` :
  - plis à l'aine et au genou, ombre et lumière sur la cuisse et le tibia ;
  - chaussure avec embout, lacets, bande et semelle ;
  - motifs propres à chaque paire (`start-shoes`, `shoes-yellow`, `shoes-pink`) et à chaque bas (jean, hibiscus, cargo, short avec chaussettes) conservés.
- [X] T022 [US3] Régler `Outline` sur la capture `2-depart-zoom` pour obtenir environ 3 px à la taille de jeu ; vérifier que le contour suit l'échelle sur la carte et dans FIGURES (×1,7)
- [X] T023 [US3] Comparer les captures de chaque animation à celles de T002 : poussée, ollie, figures, grinds, manual, nose manual, fakie, baissé, chute. Les articulations doivent être au même endroit. Vérifier aussi par le diff que `Pose`, `Solve`, `Board`, `Ride`, `Demo`, `Idle`, `Crash`, `Fall`, `Thigh`, `Shin` et les positions d'attache sont inchangés.

**Checkpoint**: nouveau skateur complet avec la tenue de départ.

---

## Phase 7: User Story 5 - Toute la garde-robe sur le nouveau skateur (Priority: P2)

**Goal**: chaque objet, peau, coupe et couleur de cheveux s'affiche sur le nouveau dessin.

**Independent Test**: captures du profil pour chaque catégorie.

- [X] T024 [US5] Ajouter dans `Assets/Tests/PlayMode/CaptureTests.cs` une planche de captures :
  - le skateur portant chaque objet de `Items` (têtes, hauts, bas, chaussures, planches) ;
  - les 5 peaux, les 2 coupes et les 5 couleurs de cheveux.
- [X] T025 [US5] Corriger dans `Assets/Scripts/Game/RiderView.cs` chaque objet mal affiché : motif décalé, couleur perdue ou pièce manquante.
- [X] T026 [US5] Vérifier dans les captures existantes que les quatre skateurs de l'accueil, le petit skateur de la carte et la démonstration de FIGURES utilisent le nouveau dessin.

---

## Phase 8: Polish & Cross-Cutting

- [X] T027 Lancer `Tools/run-tests.sh` et `Tools/capture.sh` : tous les tests passent, captures relues
- [X] T028 Lancer `Tools/build-ios.sh` puis `Tools/build-android.sh` et installer sur l'iPhone et l'émulateur ou le téléphone Android. Vérifier :
  - la même police sur les deux ;
  - une partie fluide (60 images/s, SC-006) ;
  - aucun texte flou.
- [ ] T029 Mettre à jour le README du design system si besoin : la police est désormais dans le dépôt. Puis demander au propriétaire avant tout commit ou push.

---

## Dependencies & Execution Order

- Phase 1 → Phase 2 → stories.
- US1 et US2 (texte) sont indépendantes de US3, US4 et US5 (skateur).
- US2 dépend de US1 seulement pour la relecture des captures.
- US4 → US3 → US5 : même fichier (`RiderView.cs`), et la technique du contour de US4 est reprise par US3.
- Phase 8 après toutes les stories.

## Parallel Opportunities

- T003 (`Paint.cs`), T006 (`HudLevels.cs`) et T009 (`LevelTests.cs`) peuvent être faites en même temps.
- Les phases texte (US1, US2) et skateur (US4, US3, US5) touchent des fichiers différents ; seules les captures sont communes.

## Implementation Strategy

1. **MVP** : US1, le texte net. C'est le gain le plus visible, en peu de lignes.
2. US2 : lisibilité de jour, avec son test.
3. US4, puis US3, puis US5 : le skateur.
4. Builds iOS et Android, puis vérification sur appareil.
