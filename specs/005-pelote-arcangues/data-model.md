# Data Model: Pelote basque à Arcangues

## Core

- **`BossKind.Pelotari`** : ajouté à la fin de l'enum.
  - Vie 5800.
  - `Body` (0, 0), `HitWidth` 40, `Height` 60.
  - `Attack` : 0 = lancer, 1 = rebond, en alternance.
  - `Windup` : vrai pendant l'annonce, comme les boss de rugby.
- **`ObstacleKind.Pelote`** : une balle de 12 × 12, qui utilise `Vx`, `Vy` et `Bounces` (spec 004).
  - `Reflected` : nouveau champ d'`Obstacle`, vrai pour une pelote parée qui revient vers le boss.
  - `FromBoss` : nouveau champ, vrai pour un projectile du boss (les pelotes volantes ne se parent pas).
  - `IsFlying` inclut `Pelote`.
- **`Run`** (état du mode planche en main) :
  - `bool OnFoot` : vrai tant qu'un boss Pelotari est là et n'est pas mort ;
  - `bool ShieldUp` et `int ShieldSince` ;
  - `Boomerang` (classe ou null), avec `X`, `Y`, `Vx`, `Returning`, `HitBoss` et `Steps`.
- **Nouveaux événements (`Cue`)** : `Parry` (« PARADE ! »), `Block`, `Boomerang`.
- **`Tuning`** :
  - `ParryWindow` = 9 (0,15 s) et `TapSteps` = 10 ;
  - `PelotaAttackEvery`, `PelotaThrowSpeed`, `PelotaFlySpeed` = 3,2 (mouette : 1,6) ;
  - `ParryDamage`, `BoomerangDamage`, `BoomerangSpeed`, `BoomerangOut` = 40.
- **`Levels`** : le niveau 8 reçoit `Boss = Pelotari`, et garde son conseil sur les tremplins.

## Game

- **`PelotariView`** : pantin du boss avec chistera et poses.
- **Pool `pelotes`** (`Props`) : pelote cousue et traits de vitesse.
- **`RiderView`** : une branche à pied dans `Ride`, avec course, planche en bouclier ou sous le bras,
  et planche en boomerang à la position donnée par le Core.
- **`Scenery`** (Arcangues) : fronton en couleurs, clocher, platanes taillés.

Pas de nouvelle donnée sauvegardée.
