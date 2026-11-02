# Implementation Plan: Pelote basque à Arcangues

**Branch**: `005-pelote-arcangues` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-pelote-arcangues/spec.md`

## Summary

**LE PELOTARI** finit le niveau 8. Il prend son élan, puis alterne deux attaques, chacune annoncée
0,6 s à l'avance :
- **le lancer** : une pelote à hauteur de tête ;
- **le rebond** : une pelote qui rebondit au sol vers le skateur.

Pendant ce combat, le skateur passe en mode **planche en main**. L'état et les règles de ce mode
vivent dans le Core (`Run.OnFoot`), avec des tests :
- **doigt maintenu** : bouclier ;
- **bouclier levé ≤ 0,15 s avant l'impact** : parade, la pelote repart vers le boss ;
- **toucher bref** : saut, déclenché au relâché ;
- **glissé vers le boss** : boomerang, qui blesse et revient en moins de 1,5 s ;
- **sorts** : ils marchent ; les figures et les grinds sont coupés.

La **pelote** sert aussi d'obstacle volant aux niveaux 8 à 11. Elle fait 12 × 12 et vole deux fois
plus vite que la mouette.

Le **décor d'Arcangues** gagne un fronton ocre en couleurs, un clocher plus marqué et des platanes
taillés.

## Technical Context

**Language/Version**: C# (Unity 6000.3.27f1)

**Primary Dependencies**: rendu 2D URP, maillages `Paint`, uGUI ; aucun paquet nouveau.

**Storage**: aucun changement.

**Testing**: Unity Test Framework (mode Edit pour les règles, mode Play pour les captures),
`Tools/run-tests.sh`, `Tools/capture.sh <scratchpad>`.

**Target Platform**: iOS 15+, Android minSdk 24

**Project Type**: jeu mobile (une scène)

**Performance Goals**: 60 images/s pendant le combat. Les pantins, la pelote et la planche sont
construits une fois.

**Constraints**:
- les gestes existants restent les mêmes hors de ce combat ;
- la fenêtre de parade est de 0,15 s (clarification 3) ;
- les sorts restent utilisables (clarification 2).

**Scale/Scope**:
- Core : `Levels`, `Boss`, `Run` (gestes à pied, cycle du Pelotari, pelote), `Tuning` ;
- Game : nouveau `PelotariView.cs`, `RiderView` (branche à pied), `Props`, `Scenery`, `Game`,
  `Hud` ;
- tests en mode Edit et captures.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Vérification | Statut |
|---|---|---|
| I. Fidélité au jeu web | Écart noté dans la spec (nouveau boss, obstacle, mode de combat) | OK (écart justifié) |
| II. Mobile d'abord | Gestes tactiles simples (maintenir, toucher, glisser) ; annonce ≥ 0,5 s ; maillages construits une fois | OK |
| III. Simplicité | La pelote réutilise le ballon de la spec 004 (balistique, rebonds) ; le boss réutilise le cycle du rugby ; un seul nouveau fichier de dessin | OK |
| IV. Logique testable | Bouclier, parade, boomerang, saut à pied et attaques sont dans le Core, avec des tests en mode Edit | OK |
| V. Prêt pour les stores | Aucune marque : tenue de pelote générique, numéro 7 | OK |

Re-check après la phase 1 : inchangé.

## Project Structure

### Documentation (this feature)

```text
specs/005-pelote-arcangues/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/pelote.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
Assets/Scripts/Core/
├── Levels.cs     # BossKind.Pelotari ; niveau 8 : boss
├── Boss.cs       # vie, corps, IsPelotari
├── Run.cs        # UpdatePelotari, OnFoot, bouclier, parade, boomerang, saut au relâché, pelote volante
└── Tuning.cs     # constantes de la pelote, de la parade, du boomerang
Assets/Scripts/Game/
├── PelotariView.cs   # NOUVEAU : pantin du Pelotari et son chistera
├── RiderView.cs      # branche à pied : course, bouclier, planche sous le bras, boomerang
├── Props.cs          # Pelotari, pool de pelotes
├── Scenery.cs        # Arcangues : fronton en couleurs, clocher, platanes taillés
├── Game.cs, Hud*.cs  # nom et conseil du boss, PARADE !, bulle du boomerang
Assets/Tests/EditMode/PeloteTests.cs   # NOUVEAU
Assets/Tests/PlayMode/CaptureTests.cs  # captures du niveau 8 et du combat
```

**Structure Decision**: on garde la structure actuelle ; deux fichiers nouveaux, `PelotariView.cs` et
`PeloteTests.cs`.

## Complexity Tracking

Aucune violation à justifier.
