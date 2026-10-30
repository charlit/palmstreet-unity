# Data Model: Portage de PalmStreet

Toutes les entités vivent dans `PalmStreet.Core` (C# pur). Unités : unités logiques du jeu web
(écran 390 × 620, sol de base à y = 560), temps en pas de simulation de 1/60 s.

## GameState (partie)

| Champ | Type | Règle |
|---|---|---|
| Phase | Ready / Running / Over | Ready → Running au toucher START ; Running → Over à la chute |
| Score | int | +1 par pas ; bonus de FR-010 à FR-013, FR-019 |
| Best | int | chargé au démarrage ; mis à jour en Over si Score > Best |
| Speed | float | min(4,3 ; 1,7 + ⌊Score/100⌋ × 0,045) × multiplicateur de terrain |
| TotalDistance | float | + Speed à chaque pas |
| Combo | int ≥ 1 | +1 par figure ou grind ; revient à 1 après 130 pas |
| ComboTimer | int | 130 à chaque hausse du combo |
| NextSpawnIn | float | pas avant le prochain élément (150 au départ) |
| BigRailThreshold | int | 0 au départ, +5 000 à chaque grand rail |
| MilestoneNext | int | 5 000, puis +5 000 (« FULL SPEED ») |
| NextBossScore | int | 15 000, puis score + 15 000 après chaque victoire |
| NoSpawnUntil | float | distance avant laquelle rien n'apparaît (plat après le boss) |
| Theme | Largente / Btz / Arcangues | Score mod 20 000 : <5 000, <10 000, sinon Arcangues |

## Skater

| Champ | Type | Règle |
|---|---|---|
| Character | Freestill / Keketsk8 / Guigui / Maxwell | choisi à l'accueil, gardé |
| X | float | fixe à l'écran |
| Y, VY | float | gravité 0,5 ; saut −9,6 ; sortie de grind −9,6 × 0,85 |
| State | Ground / Air / Grind / Crashed | voir transitions |
| Ducking | bool | toucher maintenu > 13 pas (0,22 s) au sol |
| CanTrick | bool | vrai au décollage, faux après une figure |
| Trick | Kickflip / Shuvit / ThreeSixty / ThreeSixtyRev, temps écoulé | 22 pas (30 pour les 360) |
| GrindRail | Obstacle? | rail en cours de grind |

Transitions : Ground → Air (toucher) ; Air → Ground (pieds au sol) ; Air → Grind (pieds sur le
dessus d'un rail en descente) ; Grind → Air (fin du rail ou toucher) ; Ground/Air → Crashed
(collision hors grind).

Hauteur : debout 58, accroupi 34 ; boîte de collision réduite (x + 9, largeur − 18, haut + 12).

## Obstacle

| Champ | Type | Règle |
|---|---|---|
| Kind | Cone / Rail | |
| Size | Small (15×24) / Normal (22×34) / Rail (70×14) / BigRail (210×28) / NoseGrind (1 050×28) | |
| X, Y | float | défile de Speed par pas ; Y suit le sol |
| Falling, VY | bool, float | plots lâchés par le boss : gravité × 0,9 jusqu'au sol |

## Coin (éclair)

Rangée de 3 à 5, espacés de 26, hauteur 20, 60 ou 95 au-dessus du sol, rayon 9. Valeur 10 × Combo.

## TerrainSegment

| Champ | Type | Règle |
|---|---|---|
| Kind | Flat / Up / Down | Flat → Up → Down → Flat… |
| Start, End | float | Flat 240–500 ; Up et Down 130–240 ; premier plat 0–420 |
| ElevStart, ElevEnd | float | Up monte à 45–100 ; Down revient à 0 |

Multiplicateur de vitesse : jusqu'à ×1,55 en descente, ×0,78 en montée (selon la pente).

## Boss

| Champ | Type | Règle |
|---|---|---|
| Hp | int | 8 000 ; −100 par dent |
| Phase | Ground / Rising / Airborne / Descending | Ground 600 pas → Rising 30 → Airborne 220 → Descending 30 |
| AirOffset | float | jusqu'à −230 |
| DropsDone | int | 4 plots par envol, un tous les 55 pas |
| ToothCooldown | int | une dent tous les 22 pas (40 au début), vitesse 7,5 |

## Persistance (PlayerPrefs)

| Clé | Valeur |
|---|---|
| `best` | meilleur score (int) |
| `muted` | son coupé (0/1) |
| `character` | dernier skateur choisi |
