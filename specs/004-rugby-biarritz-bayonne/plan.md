# Implementation Plan: Biarritz et Bayonne aux couleurs du rugby

**Branch**: `004-rugby-biarritz-bayonne` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-rugby-biarritz-bayonne/spec.md`

## Summary

On ajoute deux boss de rugby. LE PILIER finit le niveau 2, LE TALONNEUR le niveau 7. Ils
trottinent, puis alternent trois attaques, chacune annoncée 0,6 s à l'avance :
- **la passe** : un ballon à hauteur de tête, qu'on évite en se baissant ;
- **la chandelle** : un ballon qui rebondit, qu'on évite en sautant ;
- **le plaquage** : un plongeon au sol, qu'on évite en sautant.

Les ballons sont des obstacles (`ObstacleKind.Ball`) qui se percutent comme la mouette. On réutilise
ainsi les collisions et le geste pour se baisser. Le même ballon sert d'obstacle volant à Biarritz,
aux niveaux 2 et 12.

Battre un boss de rugby donne sa tenue de 5 pièces. Ce sont des objets de garde-robe liés au niveau
du boss (`Item.Boss`), sans nouvelle sauvegarde. Les boss de rugby ne donnent pas de sort.

Le décor de Biarritz gagne trois monuments :
- **l'Hôtel du Palais** (couche du milieu) ;
- **le stade Aguiléra** (couche du milieu), avec son tableau « BIARRITZ vs BAYONNE » en texte ;
- **le Rocher de la Vierge** (couche lointaine).

Enfin, la mouette est redessinée, sans halo le jour, et le tutoriel dit « mouettes ».

## Technical Context

**Language/Version**: C# (Unity 6000.3.27f1)

**Primary Dependencies**: rendu 2D URP, maillages procéduraux (`Paint`), uGUI, `TextMesh` pour le
tableau du stade. Aucun nouveau paquet.

**Storage**: aucun changement. Les tenues se déduisent des niveaux réussis.

**Testing**:
- Unity Test Framework, en mode Edit pour les règles et en mode Play pour les captures ;
- `Tools/run-tests.sh` et `Tools/capture.sh <dossier scratchpad>`.

**Target Platform**: iOS 15+, Android minSdk 24

**Project Type**: jeu mobile (une scène)

**Performance Goals**: 60 images/s pendant un combat. Le pantin du boss et les ballons sont
construits une fois, puis seulement déplacés et tournés.

**Constraints**:
- aucun logo, sigle ni nom de club (FR-018, principe V) ;
- les index des sorts ne bougent pas ;
- une sauvegarde existante reste valide.

**Scale/Scope**:
- Core : `Levels.cs`, `Boss.cs`, `Run.cs`, `Items.cs`, `Progress.cs`, `Tuning.cs` ;
- Game : `Props.cs`, nouveau `RugbyView.cs`, `RiderView.cs`, `Look.cs`, `Scenery.cs`, `Game.cs`,
  `Hud.cs`, `HudExtras.cs`, `HudLevels.cs` ;
- tests en mode Edit et captures.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Vérification | Statut |
|---|---|---|
| I. Fidélité au jeu web | Écart noté dans la spec : nouveaux boss, décors et récompenses demandés par le propriétaire | OK (écart justifié) |
| II. Mobile d'abord | Gestes existants (glisser vers le bas, toucher) ; annonce ≥ 0,5 s ; maillages construits une fois | OK |
| III. Simplicité | Pas de paquet ni d'asset. Le ballon réutilise le système des obstacles volants, les tenues celui des objets. Un seul nouveau fichier (`RugbyView`) | OK |
| IV. Logique testable | Attaques, esquives, vies, sorts, tenues et ballons volants sont dans le Core, avec des tests en mode Edit | OK |
| V. Prêt pour les stores | Aucun logo, sigle ni nom de club ; le tableau dit « BIARRITZ vs BAYONNE » (noms de villes) | OK |

Re-check après la phase 1 : inchangé.

## Project Structure

### Documentation (this feature)

```text
specs/004-rugby-biarritz-bayonne/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/rugby.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
Assets/Scripts/Core/
├── Levels.cs     # BossKind.Pilier/Talonneur ; niveaux 2 et 7 : boss, obstacles volants, conseils
├── Boss.cs       # vies, Attack, Windup, Body et Height des boss de rugby
├── Run.cs        # UpdateRugby (passe, chandelle, plaquage), ObstacleKind.Ball, ballons volants à Biarritz
├── Tuning.cs     # constantes : annonce, vitesse de passe, chandelle, taux d'obstacles volants
├── Items.cs      # Item.Boss, 10 pièces de rugby, OutfitFor
└── Progress.cs   # Spells limité aux boss Crabe à Violet
Assets/Scripts/Game/
├── RugbyView.cs  # NOUVEAU : pantin du boss de rugby et ses poses
├── Props.cs      # boss de rugby, pool de ballons, mouette redessinée
├── RiderView.cs  # motifs des tenues de rugby (bandeau, casque de mêlée, maillot, short, baskets, planche)
├── Look.cs       # Hat.Scrum, couleurs des nouveaux objets
├── Scenery.cs    # Btz : Hôtel du Palais, stade + tableau TextMesh, Rocher de la Vierge
├── Game.cs       # noms et conseils des boss, annonce de la tenue
├── Hud*.cs       # « NOUVELLE TENUE », « BATS LE PILIER », tutoriel « mouettes »
Assets/Tests/EditMode/  # tests des règles (research R8)
Assets/Tests/PlayMode/CaptureTests.cs   # captures du niveau 2, des boss, des tenues
```

**Structure Decision**: on garde la structure actuelle (Core sans moteur, Game, Tests). Un seul fichier
nouveau, `RugbyView.cs`.

## Complexity Tracking

Aucune violation à justifier.
