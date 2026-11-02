# Contract: boss de rugby, ballons et tenues

## Core

```csharp
Levels.Get(2).Boss == BossKind.Pilier;   Levels.Get(7).Boss == BossKind.Talonneur;
new Boss(BossKind.Pilier).MaxHp < new Boss(BossKind.Crab).MaxHp;
new Boss(BossKind.Talonneur).MaxHp is > 5000 and < 6000;

// pendant le combat (Phase.Playing, boss arrivé)
boss.Attack;                 // 0 passe, 1 chandelle, 2 plaquage (rotation)
boss.Windup;                 // pose d'annonce, ≥ 30 pas avant l'action
run.Obstacles.Exists(o => o.Kind == ObstacleKind.Ball);   // passe ou chandelle lancée

progress.Spells;             // jamais de sort pour Pilier ou Talonneur
Items.OutfitFor(2).Count() == 5;  Items.OutfitFor(7).Count() == 5;
progress.Owns("jersey-pilier") == progress.IsCleared(2);
```

Garanties :

| Attaque | Esquive | Sinon |
|---|---|---|
| Passe | baissé | chute |
| Chandelle | saut au rebond | chute |
| Plaquage | saut | chute |

Un ballon volant de Biarritz se comporte comme une mouette.

## Écrans

| Où | Ce qui change |
|---|---|
| Carte, aperçu | niveaux 2 et 7 marqués BOSS ; nom et conseil du boss |
| Début du combat | « LE PILIER » / « LE TALONNEUR » + « Baisse-toi pour la passe, saute la chandelle et le plaquage ! » |
| Niveau réussi (1re fois) | objet habituel du niveau + « NOUVELLE TENUE : LE PILIER » (ou LE TALONNEUR) |
| Profil | 10 pièces de rugby ; non gagnées : « BATS LE PILIER » / « BATS LE TALONNEUR » |
| Tutoriel | « GLISSE VERS LE BAS : il se baisse (mouettes) » |
| Décor du niveau 2 | stade Aguiléra + tableau « BIARRITZ vs BAYONNE », Hôtel du Palais, Rocher de la Vierge |
