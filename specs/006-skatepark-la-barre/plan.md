# Implementation Plan: Le skatepark de la Barre à Anglet

**Branch**: `006-skatepark-la-barre` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

## Summary

Le skatepark est un niveau à part, `Levels.Park` : il n'est pas dans `Levels.All`, donc ni dans la carte des
12 niveaux ni dans le mode infini. Il tourne comme un niveau d'histoire (`Run.StartLevel`), avec une
définition qui porte `Park = true`. Quand `Run.Park` est vrai :

- **Départ** : une rampe de 280 de haut sur 700 de long, contre 110 sur 360 ailleurs. Elle donne le même élan
  qu'en mode infini (« BOOST ! »).
- **Relief** : du plat et des **bowls**, des creux faits d'une montée et d'une descente de hauteur négative, que
  `Ground` sait déjà enchaîner. Pas de bosse ni de trou.
- **Modules**, posés en boucle par `PlacePark` :
  - **rail** : le rail normal ;
  - **curb** : un rail bas et long ;
  - **funbox** : un bloc dont le bord se grinde ;
  - **quarter** : il fait décoller comme le tremplin, avec 2 figures possibles ;
  - **bowl** : il est toujours suivi d'un quarter à la sortie, qui projette en l'air.
- **Obstacles** : aucun plot, obstacle volant ou trou. `Collide` ne fait jamais chuter (FR-006) : un saut raté
  reste un BAIL.
- **Score** : au-delà d'un combo x5, chaque figure ou variante de grind ajoute un bonus de
  `ParkBonus × (combo − 5)` (FR-007).
- **Session** : elle s'arrête au bout de 5400 pas (90 s) avec « SESSION TERMINÉE » (`Phase.Cleared`).

La progression gagne `ParkBest`, `ParkCombo` et `ParkStars[3]`. Les 3 étoiles sont : session finie, combo x8,
score cible 12 000. Elles comptent dans le total des étoiles, désormais sur 39.

Côté jeu :
- **Carte** : un nœud « LA BARRE » à côté d'Anglet, visible une fois Anglet réussi, ouvre un aperçu dédié.
- **Partie** : le haut de l'écran affiche le temps restant et le meilleur combo.
- **Fin de session** : l'écran dit « SESSION TERMINÉE » et propose REJOUER ou CARTE.
- **Décor** : nouveau thème `Theme.Skatepark` / `Decor.Park`, avec océan et plage, modules en béton à coping
  métallique, et graffitis colorés sans marque.

## Technical Context

Mêmes outils que les specs 004 et 005 : C# Unity 6000.3, maillages `Paint`, tests en mode Edit pour toutes
les règles. Aucune dépendance nouvelle et aucun nouveau fichier de code.

## Constitution Check

| Principe | Vérification | Statut |
|---|---|---|
| I. Fidélité au jeu web | Écart noté dans la spec (nouveau niveau) | OK |
| II. Mobile d'abord | Mêmes gestes ; 60 images/s, maillages construits une fois | OK |
| III. Simplicité | Réutilise le niveau d'histoire, le tremplin, les rails et le relief existants | OK |
| IV. Logique testable | Rampe, modules, absence de chute, durée, bonus, étoiles : tout est testé dans le Core | OK |
| V. Prêt pour les stores | Graffitis sans marque ni logo | OK |

## Source Code

```text
Assets/Scripts/Core/Levels.cs    # Theme.Skatepark, LevelDef.Park, Levels.Park
Assets/Scripts/Core/Ground.cs    # rampe de départ réglable, bowls (Park)
Assets/Scripts/Core/Run.cs       # Park : modules, sans chute, bonus de combo, fin à 90 s ; Curb, Funbox, Quarter
Assets/Scripts/Core/Tuning.cs    # constantes du skatepark
Assets/Scripts/Core/Progress.cs  # ParkBest, ParkCombo, ParkStars, ParkOpen, total des étoiles
Assets/Scripts/Game/             # Game (StartPark, fin de session), HudLevels/HudExtras (carte, aperçu, temps),
                                 # Look/Scenery (décor), Props (curb, funbox, quarter)
Assets/Tests/EditMode/ParkTests.cs   # nouveau
```
