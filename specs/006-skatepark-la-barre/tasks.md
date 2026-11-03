# Tasks: Le skatepark de la Barre à Anglet

**Input**: spec.md, plan.md. Les tests sont en mode Edit (principe IV), et le dessin est vérifié par captures.

## Phase 1: Core (bloque tout)

- [X] T001 Dans `Assets/Scripts/Core/Levels.cs`, ajouter `Theme.Skatepark` à la fin de l'enum. Ajouter `LevelDef.Park` et `Levels.Park` (numéro 13, ANGLET · LA BARRE), hors de `Levels.All`.
- [X] T002 Dans `Assets/Scripts/Core/Tuning.cs`, ajouter les constantes :
  - `ParkSteps` = 5400 ;
  - `ParkRampTop` = 280 et `ParkRampLength` = 700 ;
  - `ParkComboFrom` = 5 et `ParkBonus` ;
  - `ParkScoreGoal` = 12000 et `ParkComboGoal` = 8 ;
  - `BowlDepth`, `CurbHeight`, `FunboxHeight` et `FunboxWidth`.
- [X] T003 Dans `Assets/Scripts/Core/Ground.cs`, rendre la rampe de départ réglable et ajouter le mode `Park` : du plat et des bowls (montée et descente de hauteur négative), sans bosse ni trou.
- [X] T004 Dans `Assets/Scripts/Core/Run.cs`, faire tourner le skatepark :
  - ajouter `ObstacleKind.Curb`, `Funbox` et `Quarter`. `IsRail` couvre `Curb` et `Funbox` ; `Quarter` agit comme le tremplin ;
  - `Run.Park` : grande rampe et « BOOST ! » ;
  - `PlacePark` pose les modules en boucle, avec un quarter à chaque sortie de bowl ;
  - aucune chute (FR-006) ;
  - bonus de combo au-delà de x5 ;
  - fin de session à `ParkSteps`.
- [X] T005 Dans `Assets/Scripts/Core/Progress.cs`, ajouter :
  - `ParkBest`, `ParkCombo` et `ParkStars` ;
  - `ParkOpen`, vrai quand Anglet est réussi ;
  - `ClearPark(score, combo)` ;
  - les étoiles du skatepark dans `TotalStars` ;
  - `Repair` pour les anciennes sauvegardes.
- [X] T006 [P] Écrire les tests dans `Assets/Tests/EditMode/ParkTests.cs` :
  - le skatepark n'est ni dans la carte ni dans le mode infini ;
  - la rampe fait au moins le double de la rampe normale, et le skateur en sort au moins 1,5 fois plus vite ;
  - pendant 90 s, aucun plot, obstacle volant ou trou ;
  - aucune chute ;
  - la session finit à 90 s, à 1 s près ;
  - chaque type de module apparaît au moins une fois toutes les 30 s ;
  - le bonus de combo s'applique ;
  - le skatepark s'ouvre après Anglet et ses étoiles sont comptées.

## Phase 2: Jeu

- [X] T007 Dans `Game.cs`, ajouter :
  - `StartPark()` ;
  - la fin de session : progression, sauvegarde et écran « SESSION TERMINÉE » avec REJOUER et CARTE ;
  - le suivant de l'écran de fin qui relance le skatepark.
- [X] T008 Mettre à jour les écrans :
  - carte (`HudLevels.cs`) : un nœud « LA BARRE » près d'Anglet, une fois Anglet réussi ;
  - aperçu (`HudExtras.cs`) : niveau libre, meilleur score, meilleur combo, étoiles ;
  - partie : temps restant et meilleur combo à la place du défi ;
  - accueil : étoiles sur 39.
- [X] T009 Ajouter le décor : `Theme.Skatepark` dans `Look.cs` et `Decor.Park` dans `Scenery.cs`, avec océan et plage, modules en béton à coping métallique et graffitis sans marque.
- [X] T010 Dans `Props.cs`, dessiner le curb (bloc de béton à coping), la funbox (bloc et rampes) et le quarter (courbe en béton à coping).
- [X] T011 Ajouter les captures dans `CaptureTests.cs` : départ sur la grande rampe, chaque module, fin de session, nœud sur la carte.

## Phase 3: Finitions

- [X] T012 Lancer `Tools/run-tests.sh` et les captures, puis builds iOS et Android ; demander avant tout commit ou push.
