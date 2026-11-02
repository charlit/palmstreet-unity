# Data Model: Biarritz et Bayonne aux couleurs du rugby

## Core

- **`BossKind`** : `None, Crab, Gull, Boar, Purple, Pilier, Talonneur`. Les deux nouveaux sont
  ajoutés à la fin, pour que les index des sorts ne bougent pas.
- **`Boss`** (champs ajoutés ou étendus) :
  - `MaxHp` : Pilier 3000, Talonneur 5500.
  - `bool IsRugby` (Pilier ou Talonneur).
  - `int Attack` : attaque en cours. 0 = passe, 1 = chandelle, 2 = plaquage, en rotation.
  - `bool Windup` : vrai pendant l'annonce (`Rising` et `Timer < RugbyWindup`).
  - `Body` : (0, 0) en trottinant ; (56, 20) pendant le plaquage.
  - `Height` : 60 (cible des dents).
- **`ObstacleKind.Ball`** : un ballon de rugby.
  - `W` = 30, `H` = 16.
  - `Vx` : nouveau champ d'`Obstacle`, vitesse propre (négative = vers le skateur).
  - `Vy` et `Bounces` : chandelle uniquement.
  - `IsFlying` = `Gull` ou `Ball` : mêmes collisions.
- **`LevelDef`** :
  - le niveau 2 a `Boss = Pilier`, `Allow` + `Gulls` (obstacles volants), `FlyingRate = 0.12`, et le
    conseil « Ballons : glisse vers le bas pour te baisser » ;
  - le niveau 7 a `Boss = Talonneur`, sans conseil ;
  - le conseil du rail passe au niveau 3.
- **`Item`** :
  - `+ BossKind Boss` (`None` par défaut) ;
  - 10 objets nouveaux, `Level` = 2 ou 7 et `Boss` = Pilier ou Talonneur ;
  - `Items.OutfitFor(int level)` rend les objets dont `Boss != None && Level == level`.
- **`Progress.Spells`** : ne compte que les boss `Crab` à `Purple`.

## Game

- **`RiderLook`** : `Hat.Scrum` (casque de mêlée), plus les motifs des nouveaux objets, par leur `Id`.
- **`RugbyView`** : pantin du boss (couleurs, tête), avec une pose par état (course, passe,
  chandelle, plaquage, chute).
- **Pool `balls`** (`Props`) : ballons du boss et ballons volants, en rotation.
- **Tuile `Btz`** :
  - couche du milieu : Hôtel du Palais, stade Aguiléra, tableau `TextMesh` ;
  - couche lointaine : Rocher de la Vierge.

Pas de nouvelle donnée sauvegardée : les tenues se déduisent de `Cleared`, comme les autres objets.
