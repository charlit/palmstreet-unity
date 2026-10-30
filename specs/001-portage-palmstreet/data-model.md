# Data Model: Portage de PalmStreet

Toutes les entités vivent dans PalmStreet.Core (C# pur). Unités logiques (écran 390 × 620), y vers le haut,
sol de base à 0. Durées en pas de 1/60 s. Valeurs : Tuning.cs (spec, et « choix » quand la spec ne dit rien).

## Run (une partie)

| Champ | Type | Règle |
|---|---|---|
| Phase | Menu, Playing, Over | Start() → Playing ; collision → Over ; BackToMenu() → Menu |
| Rider | Freestill, Keketsk8, Guigui, Maxwell | choisi à l'accueil ; FREESTILL pour le raccourci du soleil (FR-020) |
| Score | int | +1 par pas (FR-004), + points des figures, grinds, éclairs, boss |
| Best, NewBest | int, bool | mis à jour à la chute ; gardé par l'affichage dans PlayerPrefs `best` |
| Combo, ComboTimer | int | +1 par figure ou grind, retour à 1 après 132 pas sans (FR-014) |
| Speed, CurrentSpeed | float | min(4,3 ; 1,7 + 0,045 × ⌊Score/100⌋) × facteur de pente (FR-003, FR-005) |
| Theme | Largente, Btz, Arcangues | Score mod 20 000 : < 5 000, < 10 000, sinon Arcangues (FR-015) |
| NextBossScore | int | 15 000, puis score à la victoire + 15 000 (FR-017) |
| Ground, Skater, Obstacles, Bolts, Boss, Events | | ci-dessous |

## Skater

| Champ | Règle |
|---|---|
| X | distance parcourue ; la caméra le garde à 90 du bord gauche |
| Y, Vy | hauteur des pieds ; ollie Vy = 9,6, gravité 0,5 par pas (FR-006) |
| Grounded, Ducking | accroupi après 13 pas de doigt maintenu, au sol (hauteur 40 au lieu de 60) |
| Rail | rail en cours de grind (FR-011) |
| Trick, TrickStep, TrickLength, TrickUsed | figure en cours (0 à 3), une par saut ; durée ≤ 24 pas, finie avant la retombée |

## Segment (Ground)

Plat (240–500), montée (130–240, hauteur 45–100), descente (130–240) qui revient à 0, en demi-cosinus.
Départ plat jusqu'à 720 (choix : 420 après le bord droit de l'écran). `FlatUntil` force le plat (boss, plat
d'après boss). Facteur de vitesse : jusqu'à 1,55 en descente, jusqu'à 0,78 en montée, selon la raideur.

## Obstacle

| Kind | Taille (L × H) | Règle |
|---|---|---|
| SmallCone | 22 × 28 | seul type sous 2 000 points |
| BigCone | 30 × 42 | 40 % des plots après 2 000 |
| Rail | 100 × 30 | 1 sur 3 après 2 000, sur le plat avec 10 de marge (FR-007) |
| BigRail | 300 × 30 | premier obstacle, puis tous les 5 000 ; plot s'il n'y a pas de plat (FR-008) |
| NoseRail | 1 500 × 30 | 400 après la victoire sur le boss, dans un plat de 2 500 (FR-019) |
| DroppedCone | 26 × 34 | lâché par le boss, tombe et se pose sur le sol, même en pente |

Un seul obstacle à la fois, écart de 200 à 400 + 40 × vitesse ; jamais dans le premier tiers d'une descente.

## Bolt (éclair)

Rangées de 3 à 5 espacées de 24, à 30, 75 ou 120 au-dessus du sol ; dans l'écart entre deux obstacles
(50 de marge) ; toujours sur une montée ou un début de descente, sinon 38 % (FR-013). 10 × combo.

## Boss

| Champ | Règle |
|---|---|
| State | Arriving (120 pas) → Ground (600) → Rising (50) → Air (4 plots, un tous les 60) → Falling (50) → Ground… ; Dying (90) |
| Hp | 8 000 ; −100 par dent (FR-017, FR-018) |
| X, Lift | 230 devant le skateur ; jusqu'à 150 de haut en vol |
| Teeth | une dent tous les 22 pas, qui file vers le boss |

## GameEvent

Cue (Ollie, Land, Trick, Bolt, GrindStart, GrindEnd, GrindOut, Crash, Theme, FullSpeed, BossIn, BossFly,
ConeDrop, Tooth, BossHit, Boom), Points, X, Y, Text. Ajoutés par Run, lus puis vidés par l'affichage.

## Persistance (PlayerPrefs, côté Game)

`best` (meilleur score), `rider` (dernier skateur), `mute` (son coupé).
