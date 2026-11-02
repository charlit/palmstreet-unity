# Contract: Texte de l'interface et dessin du skateur

## Texte

| Où | Police | Ombre | Couleur |
|---|---|---|---|
| Tous les `Text` créés par `Hud.Label` | Rubik Bold (police système si absente) | aucune | inchangée, sauf la ligne ci-dessous |
| Bandeau, sous-bandeau, figure annoncée, points flottants (milieu de l'écran) | Rubik Bold | aucune | `Night` si `Levels.IsDay(run.Theme)`, sinon couleur actuelle |
| Haut de l'écran (score, BEST, COMBO, ville, boutons) | Rubik Bold | aucune | crème, dans les 12 villes |
| JOUER | Rubik Bold | aucune (déjà le cas) | night sur orange |

## Core

```csharp
bool day = Levels.IsDay(Theme.Biarritz);   // true
bool day2 = Levels.IsDay(Theme.Anglet);    // false
```

## Skateur

```csharp
RiderView.Create(look, parent, order, layer)   // signature inchangée
rider.Ride(...); rider.Demo(...); rider.Idle(...); rider.Crash(...); rider.Fall(...)   // inchangées
```

Garanties :
- mêmes articulations et mêmes poses qu'avant ;
- un contour d'encre continu autour de chaque membre, sans trait au genou ;
- le contour suit l'échelle du skateur ;
- tout objet de `Items`, toute peau, coupe et couleur de cheveux s'affiche.
