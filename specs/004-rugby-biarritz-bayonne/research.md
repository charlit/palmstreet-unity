# Research: Biarritz et Bayonne aux couleurs du rugby

## R1. Deux nouveaux boss dans le Core

- **Decision**: ajouter `BossKind.Pilier` et `BossKind.Talonneur` **après** `Purple` dans l'enum.
  - `Progress.Spells` déduit le sort de la valeur de l'enum (`(int)Boss - 1`) ; il ne doit garder
    que les boss Crabe à Violet (FR-012b).
  - Les niveaux 2 et 7 reçoivent leur boss dans `Levels.All`.
  - La carte, l'aperçu, la fin de niveau et le mode infini lisent déjà `Def.Boss` : ils suivent sans
    autre changement.
- **Vie (FR-006)** :

  | Boss | Vie |
  |---|---|
  | Pilier | 3000 |
  | Crabe | 4000 |
  | Goéland | 5000 |
  | Talonneur | 5500 |
  | Sanglier | 6000 |
  | Violet | 8000 |

  Le Pilier est le plus fragile, et le Talonneur se place entre le Goéland (niveau 6) et le Sanglier
  (niveau 9). Ces valeurs sont réglées en jouant.
- **Rationale**: on réutilise le même cycle que les autres boss (`Boss.State` : `Ground` → `Rising`
  → `Falling`), avec un compteur d'attaques. Ni classe ni hiérarchie nouvelle.

## R2. Cycle d'un boss de rugby (FR-002 à FR-005b)

`UpdateRugby(Boss b)` dans `Run`, à côté de `UpdateBeast` :

- **Ground (trottine)** : petit balancement au sol, à `Distance`. Il ne bondit pas comme le Crabe :
  il doit rester lisible. Au bout de `AttackEvery` pas, l'attaque suivante démarre, dans l'ordre
  passe → chandelle → plaquage → passe…
- **Annonce (FR-004, SC-002)** : `Rising` commence par `RugbyWindup` = 36 pas (0,6 s ≥ 0,5 s), où le
  boss prend la pose de l'attaque (bras armé, jambe armée ou buste penché). L'action ne part qu'à la
  fin de l'annonce.
- **La passe** : un `Obstacle` de type `Ball`, lancé à hauteur de tête
  (`Y = sol + Tuning.GullHeight`), avec la boîte de la mouette (30 × 16). Il vole vers le skateur
  (`Vx` négatif, vitesse de passe). Il se percute comme une mouette : baissé, le skateur passe
  dessous ; debout ou en l'air, il chute.
- **La chandelle** : un `Ball` lancé haut, qui retombe à `ChandelleLand` (≈ 140) devant le skateur.
  Il rebondit ensuite vers lui en petits bonds (hauteur max ≈ 26, sous la hauteur d'un ollie) :
  seul un saut l'évite. Il reste au sol après 3 rebonds puis sort de l'écran.
- **Le plaquage** : comme la charge du Crabe (`Offset` va de `Distance` à `BehindSkater`), avec
  `Lift = 0` et un corps bas (56 × 20) : sauter l'évite. Il revient devant d'un bond par-dessus le
  skateur (`Falling`).
- **Touches (FR-005b)** : le ballon ou le corps fait chuter tout de suite, par les collisions
  existantes. Le corps ne touche que pendant le plaquage ; en trottinant, il est hors de portée.
- **Dégâts reçus (FR-005)** : les dents et les sorts, sans changement (`HitWidth`, `Height` du boss
  debout : 40 × 60).
- **Ballons du boss** : ce sont des obstacles ; les dents et les sorts ne les détruisent pas.
  `Obstacles.RemoveAll` les retire quand ils sont loin derrière.

## R3. Ballon de rugby volant à Biarritz (FR-020, FR-022)

- **Decision**: `ObstacleKind.Ball` sert à la fois pour les ballons du boss et pour les ballons
  volants. Dans `PlaceNeon`, là où naît une mouette, le thème Biarritz (niveaux 2 et 12) tire un
  ballon une fois sur deux. Le ballon a la même boîte que la mouette et le même mouvement vers le
  skateur.
  - Le niveau 12 est `Theme.BiarritzNuit` ; le niveau 2 est `Theme.Biarritz`.
  - En mode infini, le décor de Biarritz peut aussi en avoir.
- **Niveau 2** : on ajoute `Allow.Gulls` (« obstacles volants ») et on garde « quelques » ballons
  avec un tirage réduit : `Def.FlyingRate`, 0,12 au niveau 2 contre 0,28 ailleurs.
- **Conseils** : le niveau 2 dit « Ballons : glisse vers le bas pour te baisser » ; le conseil du
  niveau 7 sur les mouettes est retiré. Le conseil du rail, aujourd'hui au niveau 2, passe au niveau 3,
  qui n'en a pas.
- **Collision** : `Collide` traite `Ball` comme `Gull` (une seule condition `IsFlying`).

## R4. Tenues de rugby (FR-010 à FR-014)

- **Decision**: `Item` reçoit un champ `Boss` (`BossKind`, `None` par défaut). On ajoute 10 objets :

  | Pièce | Pilier (niveau 2) | Talonneur (niveau 7) |
  |---|---|---|
  | Tête | `band-pilier` | `scrum-cap` |
  | Haut | `jersey-pilier` | `jersey-talonneur` |
  | Bas | `shorts-pilier` | `shorts-talonneur` |
  | Chaussures | `boots-pilier` | `boots-talonneur` |
  | Planche | `deck-pilier` | `deck-talonneur` |

  Chaque objet porte `Level` = le niveau de son boss et `Boss` = ce boss.
  - `Owns` marche déjà (`IsCleared(Level)`).
  - `Items.ForLevel` garde l'objet habituel du niveau, car il est déclaré avant.
  - `Items.OutfitFor(n)` rend les 5 pièces du boss du niveau n.
  - `Progress.Clear` ne change pas. La première réussite des niveaux 2 et 7 rend l'objet habituel,
    et l'écran de réussite y ajoute « NOUVELLE TENUE : LE PILIER ».
- **Profil (FR-013)** : une pièce non gagnée affiche « BATS LE PILIER » ou « BATS LE TALONNEUR » au
  lieu de « NIVEAU n · VILLE ».
- **Dessin** : on réutilise le skateur de la spec 003, avec 5 nouveaux motifs.
  - `Hat.Band` : bandeau à liseré.
  - Nouveau `Hat.Scrum` : casque de mêlée à bandes.
  - Haut : maillot à cerceau et col (motif de `TopDesign`).
  - Bas : short + chaussettes, comme `shorts-surf`, couleur des chaussettes = `Accent`.
  - Chaussures : basket noire à bande de couleur.
  - Planche : couleur pleine, roues blanches.
- **Alternatives considered**: un nouveau `Slot` « tenue complète » — rejeté, car la garde-robe se
  compose pièce par pièce.

## R5. Dessin des boss de rugby

- **Decision**: une nouvelle classe `RugbyView` (`Game/RugbyView.cs`). C'est un pantin de pièces
  `Paint`, avec contour d'encre comme le skateur de la spec 003, dans les couleurs du boss. `Props`
  la crée deux fois (Pilier, Talonneur) et l'affiche selon `Boss.Kind`.
- **Poses** : course (jambes en ciseaux, bras qui balancent), passe (bras armé, puis lancer),
  chandelle (jambe armée, puis coup de pied), plaquage (corps à l'horizontale, bras devant), chute
  (Dying).
- **Têtes** : le Pilier a des cheveux bruns et un bandeau blanc ; le Talonneur a le crâne rasé et une
  barbe.
- **Ballon** : un pool `balls` dans `Props`. C'est une ellipse blanche à bandes rouges et coutures,
  qui tourne sur elle-même (rotation selon le temps), avec une ombre au sol.
- **Rationale**: `Props` fait déjà 519 lignes ; un pantin à poses mérite son propre fichier, sur le
  modèle de `RiderView`.

## R6. Décor de Biarritz (FR-015 à FR-018)

Le décor est fait de couches de tuiles de 640 qui défilent en parallaxe (`Scenery`), chaque couche
répétant sa tuile.

- **Couche du milieu (`Decor.Btz`)** : les maisons sont remplacées par l'Hôtel du Palais (x 10 à
  250) et le stade Aguiléra (x 290 à 630).
  - Le dessin est en couleurs : brique, pierre, ardoise ; sièges rouges et blancs, toit, pylônes
    allumés avec halo, poteaux en H.
  - Le tableau est un `TextMesh` en Rubik : « BIARRITZ » en rouge, « vs », « BAYONNE » en bleu ciel.
    Il est enfant des deux copies de la tuile et suit leur fondu de thème (masqué hors de Biarritz).
- **Couche lointaine** : le Rocher de la Vierge est ajouté à x ≈ 150, à côté du phare existant
  (x 470). Il comprend le rocher dans l'océan, la statue blanche sur son socle et une passerelle en
  treillis reliée à la falaise. Comme cette couche défile plus lentement, il passe à un autre moment
  que le stade : c'est « une autre portion du décor ».
- **Lisibilité** : le premier plan (`near`) garde ses réverbères. Le stade est dans la couche du
  milieu, derrière le skateur et les obstacles (ordre de tri inchangé).
- **Alternatives considered**: une tuile de 1280 propre à Biarritz — rejetée, car tout le défilement
  suppose une largeur unique.

## R7. Mouette redessinée (FR-019)

- **Decision**: dans le pool `gulls` de `Props` :
  - dos gris perle `#C9CED6` et dessous blanc ;
  - bouts d'ailes noirs ;
  - bec jaune `#FFD23F` à tache rouge ;
  - œil noir.
  Le halo rose n'est affiché que si `!Levels.IsDay(theme)`. Le Goéland géant (boss) garde son dessin.
- **Tutoriel (FR-021)** : `HudExtras.Gestures[0]` dit « (mouettes) ».

## R8. Tests (principe IV)

En mode Edit, avec `Run` et `Progress` :
- passe : baissé → pas de chute ; debout → chute ;
- chandelle : saut au rebond → pas de chute ; au sol → chute ;
- plaquage : saut → pas de chute ; au sol → chute ;
- annonce : au moins 30 pas (0,5 s) avant chaque action ;
- ordre des vies (`Pilier < Crabe < Goéland < Talonneur < Sanglier < Violet`) ;
- `Spells` sans sort pour les boss de rugby ;
- `OutfitFor(2)` et `OutfitFor(7)` : 5 pièces chacune, possédées après la réussite, une seule fois ;
- niveau 2 : il produit des ballons volants (`Ball`), et un ballon volant se comporte comme une
  mouette ;
- niveaux 2 et 7 : ils finissent par un boss.
