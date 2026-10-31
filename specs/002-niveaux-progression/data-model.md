# Data Model: Mode histoire, mode infini et figures à débloquer

Entités en C# pur (PalmStreet.Core). Unités de la spec 001 : distances logiques, vitesses en unités par
pas de 1/60 s.

## LevelDef (table Levels.All)

| N° | Ville (Theme) | Distance d'arrivée | Vitesse départ → max | Obstacles permis | Boss | Figure débloquée |
|---|---|---|---|---|---|---|
| 1 | Anglet | 9 000 | 1,7 → 2,4 | petits plots, sol plat | — | FLIP |
| 2 | Biarritz | 10 000 | 1,9 → 2,7 | + rails, grand rail | — | — |
| 3 | Bidart | 9 000 | 2,1 → 3,0 | plots, rails | Crabe (4 000) | SHOVE-IT |
| 4 | Guéthary | 12 000 | 2,4 → 3,3 | + grands plots, bosses | — | — |
| 5 | Saint-Jean-de-Luz | 13 000 | 2,6 → 3,5 | idem | — | — |
| 6 | Hendaye | 12 000 | 2,8 → 3,7 | idem | Goéland (6 000) | 360° |
| 7 | Bayonne | 14 000 | 3,0 → 3,9 | + mouettes | — | — |
| 8 | Arcangues | 15 000 | 3,2 → 4,1 | + tremplins | — | — |
| 9 | Espelette | 14 000 | 3,4 → 4,3 | idem | Sanglier (7 000) | 360° INVERSÉ |
| 10 | Ascain | 16 000 | 3,6 → 4,5 | + trous | — | — |
| 11 | Cambo-les-Bains | 17 000 | 3,8 → 4,7 | idem | — | — |
| 12 | Biarritz la nuit | 16 000 | 4,0 → 5,0 | tout | Violet (8 000) | — |

Champs : `Number`, `City` (nom affiché), `Theme`, `Finish` (distance), `StartSpeed`, `MaxSpeed`,
`Allowed` (drapeaux : Rails, BigCones, Hills, Gulls, Pads, Gaps), `Boss` (BossKind ou aucun),
`Unlocks` (figure ou aucune), `NewObstacle` (texte de la bulle d'aide, FR-007).

Règles :
- Vitesse en histoire : `Start + (Max − Start) × min(1, distance parcourue / Finish)`.
- Les éclairs et les règles de points de 001 restent dans tous les niveaux.
- Durée à vitesse moyenne : de 59 s (niveau 12) à 75 s (niveau 3) avant le boss éventuel.

## Endless (mode infini)

| Réglage | Valeur |
|---|---|
| Vitesse | 1,7 + 0,012 × secondes de jeu, plafond 5,5 |
| Décor | change toutes les 4 000 points, parmi les villes ouvertes en histoire |
| Boss | tous les 15 000 points, tiré parmi les boss des niveaux réussis (le Crabe si aucun) |
| Obstacles | tous ceux permis dans les niveaux ouverts |

## BossKind

| Sorte | Vie | Mouvement | Attaque |
|---|---|---|---|
| Crab | 4 000 | au sol, 230 devant ; bond vers le skateur toutes les 5 s puis retour | son corps (à sauter) |
| Gull | 6 000 | en vol à 150 ; piqué à 46 du sol toutes les 6 s | piqué (s'accroupir ou sauter) ; 3 coquillages entre deux piqués |
| Boar | 7 000 | au sol ; charge toutes les 6 s puis retour | charge (à sauter) ; barrières basses (22 de haut) |
| Purple | 8 000 | celui de 001 | envol toutes les 10 s, 4 plots |

Dents : une toutes les 22 pas, 100 de dégâts (001). Victoire : +5 000 points, BOOM, niveau réussi.

## Run (ajouts)

| Champ | Règle |
|---|---|
| Mode | Story ou Endless |
| Level | LevelDef en cours (histoire) |
| Tricks | figures connues, dans l'ordre FLIP, SHOVE-IT, 360°, 360° INVERSÉ |
| Elapsed | pas de jeu écoulés (vitesse de l'infini) |
| Phase | Menu, Playing, Over, **Cleared** (niveau réussi) |
| Cue | + Finish (ligne d'arrivée), Cleared, NewObstacle (bulle d'aide) |

## Item (garde-robe, FR-023 à FR-027)

| Id | Emplacement | Objet | Donné par |
|---|---|---|---|
| start-head | couvre-chef | tête nue | départ |
| start-top | haut | t-shirt blanc | départ |
| start-legs | bas | jean bleu | départ |
| start-shoes | chaussures | baskets blanches | départ |
| start-deck | planche | bois naturel, roues crème | départ |
| cap-red | couvre-chef | casquette rouge | niveau 1 Anglet |
| hoodie-ocean | haut | sweat bleu océan | niveau 2 Biarritz |
| deck-crab | planche | planche rouge « Crabe » | niveau 3 Bidart |
| shorts-surf | bas | short de surf turquoise | niveau 4 Guéthary |
| beanie-stripes | couvre-chef | bonnet rayé marine | niveau 5 Saint-Jean-de-Luz |
| hoodie-gull | haut | sweat blanc « Goéland » | niveau 6 Hendaye |
| band-red | couvre-chef | bandeau rouge | niveau 7 Bayonne |
| pants-green | bas | pantalon vert | niveau 8 Arcangues |
| deck-pepper | planche | planche « Piment » | niveau 9 Espelette |
| shoes-yellow | chaussures | baskets jaunes | niveau 10 Ascain |
| hoodie-purple | haut | sweat violet | niveau 11 Cambo-les-Bains |
| deck-neon | planche | planche « Néon » rose et cyan | niveau 12 Biarritz la nuit |

Libres (pas des objets) : peau (5), coiffure (courts, boucles), couleur des cheveux (5).

## Progress (sauvegarde `progress`)

| Champ | Règle |
|---|---|
| Cleared[12] | niveau réussi au moins une fois |
| Best[12] | meilleur score par niveau |
| EndlessBest | meilleur score infini (= ancien `best` à la migration) |
| SeenObstacles | bulles d'aide déjà montrées |
| Owned | objets gagnés (déduits des niveaux réussis + objets de départ) |
| Worn | objet porté par emplacement (5 ids) ; peau, coiffure, cheveux à part |
| IsOpen(n) | n = 1, ou niveau n−1 réussi |
| Tricks | déduit de Cleared : 1 → FLIP, 3 → SHOVE-IT, 6 → 360°, 9 → 360° INVERSÉ |
| Clear(n, score) | marque réussi, garde le meilleur, renvoie la figure et l'objet nouvellement gagnés (ou aucun) |
