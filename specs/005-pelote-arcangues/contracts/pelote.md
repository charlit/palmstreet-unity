# Contract: Pelotari, mode planche en main, pelote

## Core

```csharp
Levels.Get(8).Boss == BossKind.Pelotari;   Levels.SpellOf(BossKind.Pelotari) == -1;
run.OnFoot;                // vrai pendant le combat contre le Pelotari (boss arrivé, pas mort)
run.ShieldUp;              // doigt maintenu à pied
run.Boomerang;             // null si la planche est dans la main

// à pied :
run.Press();               // lève le bouclier (sauf planche en vol)
run.Release();             // baisse le bouclier ; tenu < TapSteps et au sol → saut
run.Swipe(1, 0);           // vers le boss → boomerang (si planche en main)
run.Swipe(0, -1);          // se baisser
run.Cast(i);               // sorts : inchangés
```

| Pelote du boss | Réponse | Résultat |
|---|---|---|
| Lancer (tête) | bouclier levé depuis ≤ 9 pas | PARADE : renvoyée, dégâts au boss |
| Lancer (tête) | bouclier levé depuis plus longtemps | bloquée |
| Lancer (tête) | baissé | passe au-dessus |
| Lancer (tête) | rien | chute |
| Rebond (sol) | saut | passe dessous |
| Rebond (sol) | rien ou bouclier | chute |

La pelote volante (niveaux 8 à 11) se comporte comme la mouette, à 2 fois sa vitesse.

## Écrans

| Où | Ce qui change |
|---|---|
| Carte, aperçu | niveau 8 marqué BOSS, « LE PELOTARI » |
| Début du combat | « LE PELOTARI » + « MAINTIENS LE DOIGT : BOUCLIER » ; le skateur descend de sa planche |
| Pendant le combat | « PARADE ! » ; bulle unique « GLISSE VERS LE BOSS : BOOMERANG » |
| Niveau 8 | fronton ocre (arc, liserés rouges, bande de métal), clocher, platanes taillés |
