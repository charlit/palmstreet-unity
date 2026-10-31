---

description: "Tâches de la spec 002 : mode histoire en 12 niveaux, boss, figures et vêtements à débloquer, mode infini, animation de lancement"
---

# Tasks: Mode histoire en niveaux, mode infini et figures à débloquer

**Input**: Design documents from `/specs/002-niveaux-progression/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/core-and-ui.md, quickstart.md

**Tests**: demandés par la constitution (principe IV) : chaque règle de PalmStreet.Core a ses tests EditMode
(Assets/Tests/EditMode/LevelTests.cs), écrits avant le code. Le rendu se vérifie par captures
(Assets/Tests/PlayMode/CaptureTests.cs, `Tools/capture.sh`).

**Organization**: une phase par user story de spec.md, par priorité : US1 carte et niveaux (P1), US2 boss
(P2), US3 figures à débloquer (P2), US5 vêtements gagnés en finissant les niveaux (P2), US4 mode infini
(P3), US6 animation de lancement (P3).

Valeurs : data-model.md (table des niveaux, boss, infini). Durées en pas de 1/60 s.

## Phase 1: Setup

**Purpose**: préparer les fichiers et le test de la nouvelle fonctionnalité.

- [ ] T001 Créer Assets/Tests/EditMode/LevelTests.cs (classe `LevelTests`, namespace PalmStreet.Tests, mêmes usings que RunTests.cs) vide, et vérifier que `Tools/run-tests.sh` compile toujours (29 tests)

---

## Phase 2: Foundational

**Purpose**: villes, table des niveaux, progression et modes de `Run`, dont dépendent toutes les stories.

- [ ] T002 Dans Assets/Scripts/Core/Run.cs, remplacer `enum Theme { Largente, Btz, Arcangues, Neon }` par les 12 villes dans l'ordre de la carte : `Anglet, Biarritz, Bidart, Guethary, SaintJeanDeLuz, Hendaye, Bayonne, Arcangues, Espelette, Ascain, Cambo, BiarritzNuit` ; adapter `ThemeFor` (cycle de 001 = Anglet, Biarritz, Arcangues) et `CurrentTheme` (niveau néon = BiarritzNuit) pour que les tests FR-015/FR-029 de 001 passent toujours
- [ ] T003 Dans Assets/Scripts/Game/Look.cs, passer `ThemeLook.All` à 12 entrées dans l'ordre de l'énum : Anglet = palette Largenté, Biarritz = BTZ, Arcangues = Arcangues, BiarritzNuit = NUIT NÉON ; pour les 8 autres villes, une palette provisoire copiée d'une proche (Bidart, Guéthary, Saint-Jean-de-Luz, Hendaye ← BTZ ; Bayonne, Ascain, Cambo ← Arcangues ; Espelette ← Arcangues) avec leur nom en majuscules (ex. « SAINT-JEAN-DE-LUZ ») ; dans Assets/Scripts/Game/Scenery.cs, tableaux `layers` et `weight` à `ThemeLook.All.Length` et les `switch (t)` des plans qui retombent sur la ville modèle
- [ ] T004 [P] Écrire Assets/Scripts/Core/Levels.cs : `enum BossKind { None, Crab, Gull, Boar, Purple }`, `[Flags] enum Allow { Rails, BigCones, Hills, Gulls, Pads, Gaps }`, classe `LevelDef` (Number, City, Theme, Finish, StartSpeed, MaxSpeed, Allowed, Boss, Unlocks (index de figure ou −1), NewObstacle (texte de bulle ou null)) et `Levels.All` avec exactement la table de data-model.md (« 1 Anglet 9 000, 1,7 → 2,4, petits plots, sol plat, FLIP » … « 12 Biarritz la nuit 16 000, 4,0 → 5,0, tout, Violet ») ; `Levels.Get(n)` (1 à 12), `Levels.Count` ; réglages de l'infini dans Tuning.cs : `EndlessStart = 1.7f, EndlessSlopePerSecond = 0.012f, EndlessMax = 5.5f, EndlessThemeEvery = 4000`
- [ ] T005 [P] Écrire Assets/Scripts/Core/Progress.cs (sans UnityEngine) : `bool[] Cleared = new bool[12]`, `int[] Best = new int[12]`, `int EndlessBest`, `List<int> SeenObstacles` ; `IsOpen(n)` (« n = 1, ou niveau n−1 réussi ») ; `Tricks` (« déduit de Cleared : 1 → FLIP, 3 → SHOVE-IT, 6 → 360°, 9 → 360° INVERSÉ », liste d'index de figures triée) ; `int? Clear(n, score)` (marque réussi, garde le meilleur, renvoie la figure nouvellement débloquée ou null) ; `NextLevel` (premier niveau ouvert non réussi, ou 12) ; sérialisation JSON simple (champs publics, compatible JsonUtility côté Game)
- [ ] T006 [P] Écrire Assets/Scripts/Core/Items.cs : `enum Slot { Head, Top, Legs, Shoes, Deck }`, classe `Item` (Id, Slot, Name, Level : 0 pour départ, 1 à 12 sinon) et `Items.All` avec exactement la table de data-model.md (5 objets de départ « tête nue, t-shirt blanc, jean bleu, baskets blanches, planche bois naturel aux roues crème » et 1 objet par niveau, de « 1 Anglet → casquette rouge » à « 12 Biarritz la nuit → planche Néon ») ; `Items.ForLevel(n)` ; dans Progress.cs : `Owned` (départ + objets des niveaux réussis), `Worn` (5 ids, départ par défaut), `Wear(id)` refusé si non possédé, `Clear` renvoie aussi l'objet gagné
- [ ] T007 Dans Assets/Scripts/Core/Run.cs, ajouter `enum GameMode { Story, Endless }`, `Phase.Cleared`, `Cue.Finish, Cue.Cleared, Cue.NewObstacle`, champs `Mode`, `LevelDef Def`, `List<int> Tricks`, `int Elapsed` ; `StartLevel(LevelDef def, IList<int> tricks)` et `StartEndless(IList<Theme> cities, IList<BossKind> bosses, IList<int> tricks)` qui appellent l'actuel `Start` puis règlent mode, décor de départ (`def.Theme` ou première ville) et figures ; garder `Start(bossNow)` pour le raccourci du soleil et les tests de 001 (mode Endless, toutes les figures)
- [ ] T008 Adapter Assets/Tests/EditMode/RunTests.cs : `NewRun` démarre avec toutes les figures connues (comportement de 001) ; vérifier que les 29 tests passent

**Checkpoint**: le jeu tourne comme avant (infini par défaut), avec la table des niveaux et la progression disponibles.

---

## Phase 3: User Story 1 - Jouer les niveaux sur la carte (Priority: P1) 🎯 MVP

**Goal**: carte de 12 villes, niveaux avec ligne d'arrivée, NIVEAU RÉUSSI → NIVEAU SUIVANT, chute → RECOMMENCER.

**Independent Test**: installation neuve : seul Anglet est ouvert ; finir Anglet ouvre Biarritz et NIVEAU SUIVANT le lance.

### Tests for User Story 1

- [ ] T009 [P] [US1] Tests dans Assets/Tests/EditMode/LevelTests.cs : table (12 niveaux, boss aux niveaux 3, 6, 9, 12 seulement, distances et vitesses croissantes, durée `Finish / ((Start + Max) / 2) / 60` entre 55 et 80 s) ; Progress (au départ seul 1 ouvert, `Clear(1)` ouvre 2, meilleur score gardé, `NextLevel`, JSON aller-retour)
- [ ] T010 [P] [US1] Tests dans Assets/Tests/EditMode/LevelTests.cs : `StartLevel(Get(1))` → vitesse = `Start + (Max − Start) × min(1, distance / Finish)` ; passer `Finish` → `Phase.Cleared` et `Cue.Finish` ; chute avant → `Phase.Over` ; niveau 1 sans rail ni grand plot ni bosse, niveau 2 avec rails, niveau 7 avec mouettes, niveau 10 avec trous ; `Cue.NewObstacle` émis au départ d'un niveau qui introduit un obstacle

### Implementation for User Story 1

- [ ] T011 [US1] Dans Assets/Scripts/Core/Run.cs : vitesse en mode Story selon T010 ; ligne d'arrivée (pas d'obstacle ni d'éclair au-delà de `Finish − 300`) ; à `Skater.X ≥ Finish` sans boss → `Cue.Finish`, `Phase.Cleared` ; obstacles filtrés par `Def.Allowed` (sans `Rails` : pas de rails ni grand rail ; sans `BigCones` : petits plots ; sans `Hills` : `Ground.FlatUntil = +∞` ; `Gulls`, `Pads`, `Gaps` remplacent la condition `Level >= 2` de 001) ; `Cue.NewObstacle` avec `Def.NewObstacle` au départ ; pas de boss par score en mode Story
- [ ] T012 [P] [US1] Dans Assets/Scripts/Game/Props.cs : ligne d'arrivée à `Def.Finish` (arche avec bandeau damier et « ARRIVÉE », poteaux, petits drapeaux), visible dès qu'elle entre à l'écran
- [ ] T013 [US1] Dans Assets/Scripts/Game/Hud.cs : accueil avec le titre, JOUER (bouton START actuel renommé), INFINI (bouton secondaire), PROFIL en haut à droite et « BEST INFINI » — retirer le bouton TOP 10 et le grand skateur sur son socle (FR-001) ; nouvel écran `map` : ScrollRect vertical, fond de côte (océan à gauche, terre à droite), route en segments, 12 pastilles en zigzag (numéro, nom de ville, verrou si fermé, coche et meilleur score si réussi, petite tête de boss pour 3, 6, 9, 12), prochain niveau mis en avant et défilement initial centré dessus, bouton RETOUR ; méthode `ShowMap(Progress)` ; petit skateur (RiderView avec la tenue portée, échelle ~0,7, ordre au-dessus du canevas) posé sur la pastille du prochain niveau, qui roule le long de la route depuis le niveau réussi quand la carte s'ouvre après une réussite (FR-022)
- [ ] T014 [US1] Dans Assets/Scripts/Game/Hud.cs : écran « NIVEAU RÉUSSI » (score, meilleur du niveau, ligne « NOUVELLE FIGURE : … » optionnelle, NIVEAU SUIVANT, CARTE) ; écran d'échec en histoire (WIPEOUT !, score, RECOMMENCER, CARTE) ; en partie histoire, bandeau de départ « NIVEAU n · VILLE », barre de progression fine vers l'arrivée en haut de l'écran, bulle d'aide (texte de `Cue.NewObstacle`, 3 s)
- [ ] T015 [US1] Dans Assets/Scripts/Game/Game.cs : `Progress` chargé depuis PlayerPrefs `progress` (migration : `EndlessBest` = ancien `best`), sauvegardé à chaque réussite ; navigation accueil → JOUER → carte → niveau ; `Phase.Cleared` → `progress.Clear`, écran de réussite ; NIVEAU SUIVANT → `StartLevel(Get(n+1))` ; échec → RECOMMENCER / CARTE ; niveau 12 réussi → écran de fin d'aventure (« TU ES UN VRAI PST ! », MODE INFINI, CARTE) ; points du niveau ajoutés à `total` (FR-010)
- [ ] T016 [US1] Captures dans Assets/Tests/PlayMode/CaptureTests.cs : carte (installation neuve, puis 5 niveaux réussis), départ d'Anglet avec bandeau, ligne d'arrivée, écran NIVEAU RÉUSSI ; vérifier avec `Tools/capture.sh`

**Checkpoint**: on joue Anglet → Biarritz → … de bout en bout, sans boss ni nouveaux décors (décors provisoires de T003).

---

## Phase 4: User Story 2 - Un boss différent tous les 3 niveaux (Priority: P2)

**Goal**: Crabe (Bidart), Goéland (Hendaye), Sanglier (Espelette), Violet (Biarritz la nuit) ; la défaite réussit le niveau.

**Independent Test**: niveau 3 : le Crabe arrive à la fin, le battre réussit le niveau ; niveau 6 : le Goéland, autre forme et autre attaque.

### Tests for User Story 2

- [ ] T017 [P] [US2] Tests dans Assets/Tests/EditMode/LevelTests.cs : en mode Story, à `Finish` d'un niveau à boss → boss de la bonne sorte (vie « Crab 4 000, Gull 6 000, Boar 7 000, Purple 8 000 »), sol plat, plus d'obstacles ; défaite → `Cue.Boom`, `Phase.Cleared` ; Crabe : bond vers le skateur toutes les 300 pas, collision au sol = chute, le sauter = pas de chute ; Goéland : piqué à 46 du sol toutes les 360 pas (accroupi passe), 3 coquillages entre deux piqués ; Sanglier : charge toutes les 360 pas (à sauter), barrières basses de 22 de haut

### Implementation for User Story 2

- [ ] T018 [US2] Dans Assets/Scripts/Core/Boss.cs et Run.cs : champ `BossKind Kind` ; `StartBoss(kind)` avec la vie de data-model.md ; en mode Story, boss déclenché à `Def.Finish` si `Def.Boss != None` ; défaite en Story → `Phase.Cleared` (plus de plat de nose grind ni de passage au niveau néon) ; mouvements et attaques de chaque sorte selon research.md §4 (corps du Crabe et du Sanglier comme obstacles qui bougent ; piqué du Goéland comme obstacle à hauteur de tête ; coquillages = `DroppedCone` petits ; barrières = obstacle bas `Barrier` 30 × 22)
- [ ] T019 [P] [US2] Dans Assets/Scripts/Game/Props.cs : dessins du Crabe (carapace rouge, pinces qui claquent, yeux sur pédoncules), du Goéland géant (blanc et gris, ailes qui battent, bec jaune), du Sanglier (brun, défenses, poils), des coquillages et des barrières ; même barre de vie, BOOM et confettis que le boss de 001 ; le boss violet garde son dessin
- [ ] T020 [US2] Dans Assets/Scripts/Game/Game.cs et Hud.cs : bandeau d'arrivée du boss avec son nom (« LE CRABE GÉANT », « LE GOÉLAND GÉANT », « LE SANGLIER », « LE GRAND BOSS ») ; captures des 4 boss dans Assets/Tests/PlayMode/CaptureTests.cs

**Checkpoint**: les 12 niveaux sont jouables jusqu'au bout, avec leurs 4 boss.

---

## Phase 5: User Story 3 - Commencer avec l'ollie et le grind, débloquer les figures (Priority: P2)

**Goal**: aucune figure au départ ; FLIP, SHOVE-IT, 360°, 360° INVERSÉ débloqués par les niveaux 1, 3, 6, 9 ; annonce et profil.

**Independent Test**: installation neuve : toucher en l'air ne fait rien ; réussir Anglet annonce FLIP, puis le toucher en l'air fait un FLIP.

### Tests for User Story 3

- [ ] T021 [P] [US3] Tests dans Assets/Tests/EditMode/LevelTests.cs : `Tricks` vide → toucher en l'air sans `Cue.Trick` ; `Tricks = {FLIP, 360°}` → les figures alternent FLIP, 360° (points de 001 × combo) ; `Progress.Clear(1)` renvoie FLIP une seule fois, `Clear(2)` rien, `Clear(3)` SHOVE-IT ; grind et OUT! marchent sans figure

### Implementation for User Story 3

- [ ] T022 [US3] Dans Assets/Scripts/Core/Run.cs : `Trick()` choisit la figure suivante dans `Tricks` (ordre FLIP, SHOVE-IT, 360°, 360° INVERSÉ) ; aucune figure si `Tricks` est vide (« un toucher en l'air ne fait aucune figure tant qu'aucune n'est débloquée »)
- [ ] T023 [US3] Dans Assets/Scripts/Game/Game.cs et Hud.cs : passer `progress.Tricks` à `StartLevel`/`StartEndless` ; sur l'écran de réussite, « NOUVELLE FIGURE : FLIP » et « Saute, puis touche l'écran en l'air » quand `Clear` en renvoie une
- [ ] T024 [US3] Dans Assets/Scripts/Game/Hud.cs : onglet FIGURES du profil (entre TENUE et RÉGLAGES) : OLLIE et GRIND connus, puis les 4 figures, connues ou verrouillées avec « Niveau n · Ville » ; capture dans Assets/Tests/PlayMode/CaptureTests.cs

**Checkpoint**: la progression des figures suit les niveaux.

---

## Phase 6: User Story 5 - Gagner ses vêtements en finissant les niveaux (Priority: P2)

**Goal**: plus de couleurs libres ; tenue de départ ; un objet par niveau réussi, annoncé, à enfiler dans le profil.

**Independent Test**: installation neuve : le profil n'a que la tenue de départ ; réussir Anglet annonce « NOUVEAU : CASQUETTE ROUGE », qui s'enfile ensuite.

### Tests for User Story 5

- [ ] T025 [P] [US5] Tests dans Assets/Tests/EditMode/LevelTests.cs : `Items.All` (5 objets de départ, un objet par niveau 1 à 12, ids uniques) ; Progress neuf : `Owned` = objets de départ, `Worn` = départ ; `Clear(1)` donne `cap-red` une seule fois ; `Wear("cap-red")` refusé avant, accepté après ; JSON aller-retour avec `Owned`/`Worn`

### Implementation for User Story 5

- [ ] T026 [US5] Dans Assets/Scripts/Game/Look.cs : `Wardrobe.Build(worn, skin, hairStyle, hairColor)` qui traduit les 5 objets portés en apparence (couleurs et motifs) : casquette rouge, bonnet rayé marine (rayures), bandeau rouge, sweat bleu océan, sweat blanc « Goéland » (aile grise sur la poitrine), sweat violet, short de surf turquoise (fleurs blanches), jean bleu, pantalon vert, baskets blanches et jaunes, planche bois naturel, « Crabe » (rouge, pince dessous), « Piment » (rouge, piments dessous), « Néon » (rose et cyan, roues lumineuses) ; dans Assets/Scripts/Game/RiderView.cs, motif de dessous de planche selon l'objet (remplace le logo rond)
- [ ] T027 [US5] Dans Assets/Scripts/Game/Hud.cs : onglet TENUE refait — un emplacement par onglet (COUVRE-CHEF, HAUT, BAS, CHAUSSURES, PLANCHE) avec des cartes d'objets (dessin, nom) : possédés à toucher pour les porter (cadre orange sur l'objet porté), verrouillés grisés avec « Niveau n · Ville » ; onglet PEAU & CHEVEUX avec peau (5), coiffure (courts, boucles), couleur des cheveux (5) ; retirer toutes les palettes de couleurs et la ligne « POINTS CUMULÉS » (FR-023, FR-027)
- [ ] T028 [US5] Dans Assets/Scripts/Game/Game.cs : sauvegarde de la tenue dans `progress` (migration : peau et cheveux de l'ancienne clé `outfit` gardés, vêtements remis à la tenue de départ) ; écran de réussite : « NOUVEAU : CASQUETTE ROUGE » avec le dessin de l'objet (FR-026) ; retirer Assets/Scripts/Core/Unlocks.cs, son test `Fr027_…` dans RunTests.cs et l'annonce de vêtements par points en fin de partie
- [ ] T029 [US5] Captures dans Assets/Tests/PlayMode/CaptureTests.cs : profil TENUE (installation neuve, puis 6 niveaux réussis), écran de réussite avec objet gagné

**Checkpoint**: la garde-robe se remplit niveau après niveau.

---

## Phase 7: User Story 4 - Mode infini (Priority: P3)

**Goal**: course sans fin, vitesse liée au temps, villes ouvertes en décor, boss déjà rencontrés, classement.

**Independent Test**: INFINI pendant 2 min : vitesse ≥ 1,5 × départ ; décor des villes ouvertes ; score au TOP 10.

### Tests for User Story 4

- [ ] T030 [P] [US4] Tests dans Assets/Tests/EditMode/LevelTests.cs : en Endless, vitesse = `min(5.5, 1.7 + 0.012 × secondes)` sans dépendre du score (même vitesse à score 0 et 50 000 au même temps) ; ≥ 2,55 après 7 200 pas ; décor qui change toutes les 4 000 points parmi les villes données ; boss tous les 15 000 points tiré parmi les boss donnés (Crabe si liste vide) ; obstacles limités à ceux des niveaux ouverts

### Implementation for User Story 4

- [ ] T031 [US4] Dans Assets/Scripts/Core/Run.cs : mode Endless selon T030 (remplace la vitesse par score et le décor par score de 001 ; le niveau néon automatique de 001 disparaît)
- [ ] T032 [US4] Dans Assets/Scripts/Game/Game.cs : bouton INFINI → `StartEndless(villes ouvertes, boss des niveaux réussis, figures)` ; fin de partie infinie → `EndlessBest`, `Leaderboard.Report`, écran de fin (toucher pour rejouer, TOP 10 qui ouvre `Leaderboard.Show`, ACCUEIL) ; le raccourci du soleil lance le boss en infini

**Checkpoint**: toutes les stories marchent.

---

## Phase 8: User Story 6 - Animation de lancement (Priority: P3)

**Goal**: à l'ouverture, une planche flippe au ralenti, « P. ST. » lisible dessous, puis le titre ; ≤ 2,5 s ; un toucher passe.

**Independent Test**: ouvrir l'app : animation de moins de 3 s, « P. ST. » lisible ; un toucher affiche l'accueil tout de suite.

- [ ] T033 [US6] Écrire Assets/Scripts/Game/Splash.cs : au démarrage, avant l'accueil, fond du coucher de soleil d'Anglet assombri ; une grande planche (formes de RiderView, ~3 fois la taille normale) au centre qui fait un kickflip au ralenti (≈ 1,4 s par tour, faces dessus/dessous comme le FLIP) ; dessous de la planche marqué « P. ST. » en grandes lettres, bien lisible quand il est face à l'écran ; puis « PALM STREET » qui apparaît et fondu vers l'accueil ; durée totale ≤ 2,5 s (FR-028)
- [ ] T034 [US6] Dans Assets/Scripts/Game/Game.cs : jouer Splash à chaque lancement, toucher = passer, puis afficher l'accueil ; capture d'une image au milieu de l'animation dans Assets/Tests/PlayMode/CaptureTests.cs

**Checkpoint**: le jeu s'ouvre sur sa signature.

---

## Phase 9: Décors des villes & polish

**Purpose**: les 8 décors nouveaux, la vérification sur téléphone et la documentation.

- [ ] T035 [P] Dans Assets/Scripts/Game/Look.cs et Scenery.cs : décors de Bidart (falaise, église blanche) et Guéthary (petit port, grosse vague de surf)
- [ ] T036 [P] Dans Assets/Scripts/Game/Look.cs et Scenery.cs : décors de Saint-Jean-de-Luz (baie, maisons à colombages rouges) et Hendaye (deux rochers jumeaux dans la mer)
- [ ] T037 [P] Dans Assets/Scripts/Game/Look.cs et Scenery.cs : décors de Bayonne (deux flèches de cathédrale, fleuve, façades colorées) et Espelette (maisons blanches, guirlandes de piments rouges)
- [ ] T038 [P] Dans Assets/Scripts/Game/Look.cs et Scenery.cs : décors d'Ascain (montagne, petit train à crémaillère) et Cambo-les-Bains (villa, jardins à la française)
- [ ] T039 Captures de départ des 12 villes dans Assets/Tests/PlayMode/CaptureTests.cs et relecture
- [ ] T040 Mettre à jour specs/001-portage-palmstreet/spec.md (FR-002, FR-010, FR-015 à FR-019, FR-026, FR-027, FR-029 remplacés par la spec 002) et specs/001-portage-palmstreet/tasks.md
- [ ] T041 Build iOS (`Tools/build-ios.sh`) et parcours de quickstart.md §3 sur l'iPhone ; noter les réglages de difficulté à revoir (SC-001, SC-002)

---

## Dependencies & Execution Order

- Setup (T001) → Foundational (T002–T008) → US1 carte et niveaux (T009–T016).
- US2 (boss), US3 (figures) et US5 (vêtements) dépendent de US1 (mode histoire, écran de réussite) ; elles
  sont indépendantes entre elles (US2 : Boss.cs, Props.cs ; US3 : Trick() et onglet FIGURES ; US5 :
  Items, Wardrobe, onglet TENUE).
- US4 (infini) dépend de la phase 2 seulement.
- US6 (animation de lancement) est indépendante : elle peut se faire à tout moment après la phase 2.
- Décors des villes : dès T003 ; captures des 12 villes, mise à jour de la spec 001 et essai sur l'iPhone
  en dernier.

## Parallel Examples

- Phase 2 : Levels.cs, Progress.cs et Items.cs ensemble.
- US1 : les deux tâches de tests ensemble ; la ligne d'arrivée pendant la carte.
- US2 : les dessins des boss pendant leurs règles, après les tests.
- US5 et US6 : en parallèle (fichiers différents : Look.cs/Hud.cs TENUE contre Splash.cs).
- Décors : les quatre tâches de villes ensemble.

## Implementation Strategy

1. MVP = Phases 1–3 : accueil épuré, carte avec petit skateur, niveaux avec arrivée (décors provisoires).
2. US3 (figures) et US5 (vêtements) : ce que chaque niveau rapporte.
3. US2 (boss) : les grandes étapes.
4. US4 (infini, TOP 10 sur son écran de fin), US6 (animation de lancement), puis décors des villes et
   essai sur l'iPhone.
