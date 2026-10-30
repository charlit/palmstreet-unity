# PalmStreet — dessins des skateurs et des planches

Cahier des charges pour dessiner les 4 skateurs et leurs planches. Le jeu les anime lui-même : chaque partie
du corps est un dessin séparé que le jeu fait tourner autour de son point d'attache (hanche, genou, cheville,
épaule, cou). Il n'y a donc **aucune animation à dessiner**, seulement des pièces détachées.

Références : `reference-4-skateurs.png` (l'accueil actuel) et `reference-pose-actuelle.png` (gros plan).
Les proportions actuelles sont bonnes ; c'est le dessin qu'on veut plus beau.

## Style

- Jeu de skate sur la côte basque au coucher du soleil : palmiers, océan, ambiance chaude.
- Dessin plat (vectoriel ou peint propre), 2 à 3 tons par partie : couleur de base, ombre, lumière.
  Pas de dégradé photo, pas de texture fine (le skateur fait environ 200 px de haut à l'écran).
- Contour : soit aucun, soit un contour foncé de 3 px partout. Le même pour toutes les pièces.
- **Tous les skateurs regardent vers la droite**, de profil ou de trois-quarts.
- Fond transparent.

## Format de livraison

- Un PNG par pièce, fond transparent, **à la taille exacte de toile indiquée**.
- Échelle : 8 px = 1 unité du jeu. Le skateur debout mesure environ 560 px, pieds au grip.
- Le **point d'attache** est donné en pixels depuis le coin haut-gauche de la toile. La pièce tourne autour de
  ce point : il doit tomber pile au centre de l'articulation.
- Les bouts qui s'articulent (haut de cuisse, haut de tibia, épaule, cou) sont **arrondis** et **débordent un
  peu** au-delà du point d'attache. Ainsi, aucun trou n'apparaît quand le membre tourne.
- Une seule jambe et un seul bras par skateur : le jeu assombrit lui-même la jambe et le bras arrière.
- Noms des fichiers : `<skateur>_<pièce>.png`, en minuscules. Exemple : `guigui_cuisse.png`.

## Pièces du corps (pour chaque skateur)

| Pièce | Toile (px) | Point d'attache (x, y) | Contenu |
|---|---|---|---|
| `tete` | 240 × 240 | (130, 190) : bas du cou | Tête vue de profil, visage à droite. Le centre de la tête est vers (130, 110) et son diamètre fait environ 120 px. Cheveux, casquette ou bonnet compris. Le cou descend jusqu'au point d'attache. |
| `buste` | 160 × 224 | (70, 208) : la hanche, au milieu du bas du haut | Sweat ou t-shirt, sans les bras. Les épaules sont en haut, vers (74, 64) et (90, 64). Le buste penche légèrement en avant. |
| `bras` | 64 × 152 | (32, 24) : l'épaule | Bras pendant vers le bas, manche puis main. La main est vers (32, 140). |
| `cuisse` | 96 × 160 | (48, 24) : la hanche | Cuisse verticale, pantalon ou short. Le genou est en bas, vers (48, 140). |
| `tibia` | 88 × 152 | (44, 20) : le genou | Tibia vertical, bas du pantalon ou jambe et chaussette. La cheville est en bas, vers (44, 132). |
| `chaussure` | 128 × 64 | (40, 16) : la cheville | Chaussure de skate de profil, pointe à droite. La semelle est plate, en bas à y = 48. Le talon est vers x = 10, la pointe vers x = 118. |

## Pièces de la planche (une planche par skateur)

| Pièce | Toile (px) | Point d'attache (x, y) | Contenu |
|---|---|---|---|
| `planche_profil` | 480 × 96 | (240, 40) : milieu du dessus du plateau | Planche de profil, sans les roues. Plateau de 456 px, épais d'environ 26 px, avec les kicktails relevés de 34 px aux deux bouts. Grip noir dessus. Les deux trucks pendent dessous, centrés à x = 128 et x = 352. |
| `roue` | 64 × 64 | (32, 32) : le centre | Roue de profil d'environ 58 px, avec un roulement visible. Elle doit montrer qu'elle tourne (un détail asymétrique). |
| `planche_dessus` | 416 × 128 | (208, 64) : le centre | Planche vue du dessus (400 × 112) : grip noir, bord de la couleur du bois, 8 vis. On la voit pendant les flips. |
| `planche_dessous` | 416 × 128 | (208, 64) : le centre | Planche vue du dessous : graphisme du skateur, les 2 trucks et les 4 roues. On la voit pendant les flips. |

Total : 6 pièces de corps et 4 de planche par skateur, soit 40 PNG.

## Les 4 skateurs

Chacun doit se reconnaître de loin, à sa silhouette et à ses couleurs. Les couleurs ci-dessous sont celles du jeu
actuel ; elles peuvent changer si l'ensemble est plus beau.

| Skateur | Tête | Haut | Bas | Chaussures | Planche |
|---|---|---|---|---|---|
| **FREESTILL** | Peau claire, cheveux blonds, casquette rouge portée à l'envers | T-shirt blanc cassé, détail rouge | Pantalon bleu nuit | Rouges | Rouge, roues crème |
| **KEKETSK8** | Peau mate, bonnet vert canard avec pompon | Sweat orange, détail crème | Jean bleu | Blanches | Noire, roues orange |
| **GUIGUI** | Peau claire hâlée, cheveux bruns bouclés | T-shirt vert, détail jaune | Short beige, chaussettes jaunes | Noires | **Jaune** (obligatoire), roues blanches |
| **MAXWELL** | Peau foncée, cheveux noirs courts, bandeau rose | Veste violette, détail lilas | Pantalon gris | Jaunes | Bleue, roues jaunes |

Codes couleur : FREESTILL rouge `#E63946` ; KEKETSK8 orange `#FF8C1A` et vert canard `#2A9D8F` ;
GUIGUI vert `#3BB273` et jaune `#FFD400` ; MAXWELL violet `#7B2CBF` et rose `#FF4D6D`.

## Droits

Les dessins doivent être des créations originales, cédées pour un usage commercial dans le jeu (App Store et
Google Play). Pas de marque réelle visible : logo de marque de skate, de chaussures ou de vêtements.

## Ensuite (côté jeu)

Les PNG vont dans `Assets/Resources/Skaters/<skateur>/`. Le jeu remplace alors les formes actuelles par ces
dessins, en gardant les mêmes articulations, les pieds collés au grip et les figures.
