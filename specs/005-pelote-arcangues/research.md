# Research: Pelote basque à Arcangues

## R1. LE PELOTARI dans le Core

- **Decision**: ajouter `BossKind.Pelotari` à la fin de l'enum. `Levels.SpellOf` rend −1 pour lui, et
  le niveau 8 reçoit `Boss = Pelotari`.
- **Vie** : 5800, entre le Talonneur (5500) et le Sanglier (6000), comme le veut FR-006.
- **Corps** : `Body` = (0, 0), car il reste à distance et ne touche jamais lui-même. `HitWidth` = 40
  et `Height` = 60, comme pour les boss de rugby.
- **Cycle** : `UpdatePelotari`, sur le modèle de `UpdateRugby` (spec 004).
  - **Prise d'élan** : à `Ground`, pendant `PelotaAttackEvery` pas.
  - **Annonce** : `Rising` commence par `RugbyWindup` (36 pas, 0,6 s), où le chistera est armé.
  - **Le lancer** : une `ObstacleKind.Pelote` à hauteur de tête (sol + 46, boîte 12 × 12), qui fonce
    vers le skateur (`Vx = vitesse - PelotaThrowSpeed`).
  - **Le rebond** : une `Pelote` lancée vers le sol devant le boss, qui rebondit vers le skateur en
    petits bonds (hauteur max 22), à sauter. Elle reprend la balistique des ballons (`MoveBall`,
    `Bounces`).
  - Les deux attaques alternent : lancer, rebond, lancer…

## R2. Mode « planche en main » (FR-008 à FR-015)

- **Decision**: `Run.OnFoot` est vrai tant qu'un boss `Pelotari` est là et n'est pas mort. L'état est
  dans le Core, avec des tests :
  - `ShieldUp` et `ShieldSince` : le pas où le bouclier a été levé ;
  - `Boomerang` : position X et Y, phase aller ou retour, boss déjà touché ou non.
- **Gestes** :
  - **`Press()` à pied** : le bouclier se lève tout de suite (`ShieldSince = StepCount`), sauf si la
    planche vole. Il n'y a pas d'ollie à l'appui.
  - **`Release()` à pied** : le bouclier se baisse. Si le doigt est resté moins de `TapSteps`
    (10 pas, 0,17 s) et que le skateur est au sol, il **saute**. C'est le « toucher = sauter » ; et
    comme le saut part au relâché, il ne peut pas partir pendant que le bouclier est levé.
  - **`Swipe(dx, dy)` à pied** :
    - vers le bas : il se baisse, comme d'habitude ;
    - vers le boss (`dx > 0`, plutôt horizontal) : boomerang, si la planche est dans la main ;
    - les autres directions ne font rien (pas de figure).
- **Parade et blocage** (dans `Collide`) : quand une `Pelote` du boss à hauteur de tête touche le
  skateur et que le bouclier est levé :
  - **parade**, si `StepCount - ShieldSince ≤ ParryWindow` (9 pas = 0,15 s, clarification 3) : la
    pelote repart vers le boss (`Vx` = vitesse + 8, `Reflected` = vrai) et le blesse de `ParryDamage`
    si elle le touche. L'événement `Cue.Parry` affiche « PARADE ! » ;
  - **blocage** sinon : la pelote disparaît (`Cue.Block`).
  - Une pelote de rebond (basse) n'est jamais bloquée : il faut sauter.
- **Boomerang** : la planche part droit devant, à hauteur de buste, à `BoomerangSpeed` de plus que le
  défilement. Elle repart dans l'autre sens au bout de `BoomerangOut` pas (40), ou dès qu'elle touche
  le boss (`BoomerangDamage` une fois par lancer), puis elle revient vers la main. Elle est rattrapée
  quand elle repasse à hauteur du skateur, en moins de 90 pas (1,5 s, SC-003).
- **À pied** : pas de figure, pas de grind, donc pas de dent. Les sorts marchent (clarification 2).
  L'ollie à pied garde la hauteur de l'ollie, pour sauter les rebonds.
- **Fin** : à la mort du boss, `OnFoot` redevient faux et le skateur est de nouveau sur sa planche.
  Une chute pendant le combat finit la partie comme d'habitude.
- **Mode infini** : `OnFoot` suit le boss en cours ; le Pelotari tiré au sort se joue donc aussi
  planche en main.

## R3. Pelote volante (FR-017 à FR-019)

- **Decision**: `ObstacleKind.Pelote` sert aussi d'obstacle volant.
  - Elle a une boîte de 12 × 12, et `Y` = sol + 46 : un skateur baissé (hauteur 40) passe dessous, un
    skateur debout (60) est touché.
  - Elle vole avec `Vx = -PelotaFlySpeed` (3,2), deux fois la vitesse de la mouette (1,6).
  - Dans `PlaceNeon`, pour les thèmes Arcangues, Espelette, Ascain et Cambo, un tirage sur deux donne
    une pelote à la place de la mouette.
  - Les niveaux 8 à 11 autorisent déjà les obstacles volants (`Allow.Gulls`).
- `IsFlying` inclut `Pelote` : collisions et mouvement comme les autres obstacles volants.
- Une pelote volante n'est jamais parée, même bouclier levé : il n'y a pas de bouclier hors du combat.

## R4. Dessin

- **`PelotariView`** (nouveau fichier `Game/PelotariView.cs`) : un pantin comme `RugbyView`, avec
  `RiderView.Piece` et le contour d'encre.
  - Tenue : maillot rouge avec un numéro (des chiffres en traits blancs : « 7 »), pantalon blanc,
    casque blanc à bande rouge.
  - Chistera : une longue gouttière d'osier recourbée, prolongeant le bras avant, avec des traits
    d'osier et la lanière au gant.
  - Poses : élan (court sur place), lancer (chistera armé haut derrière, puis fouetté devant), rebond
    (chistera armé bas, puis lancé vers le sol), chute.
- **Pelote** : pool `pelotes` dans `Props`. C'est un disque blanc cousu de 6 de rayon, avec des
  coutures grises et trois traits de vitesse derrière, dans le sens du mouvement.
- **Skateur à pied (`RiderView.OnFoot`)** :
  - les jambes courent : les semelles alternent au sol, par la même cinématique `Solve`, avec des
    cibles de pied qui bougent ;
  - la planche n'est plus sous les pieds. Bouclier levé, elle est tenue debout devant le buste, grip
    vers le boss, avec le bras avant tendu. Bouclier baissé, elle est portée sous le bras.
  - En boomerang, la planche est placée à la position monde du boomerang et tourne sur elle-même.
  - Les méthodes de pose existantes ne changent pas : c'est une nouvelle branche de `Ride`, prise
    quand `run.OnFoot` est vrai.
- **Interface** :
  - au début du combat, le bandeau dit « MAINTIENS LE DOIGT : BOUCLIER » ;
  - après le premier blocage ou la première parade, une bulle dit une fois
    « GLISSE VERS LE BOSS : BOOMERANG » ;
  - « PARADE ! » s'affiche en point flottant.

## R5. Décor d'Arcangues (FR-016)

Le décor d'Arcangues est le cas `default` de `Mid` et `Near` dans `Scenery.cs`.

- **Couche du milieu** : la silhouette du fronton (x 420–530) est remplacée par un fronton en couleurs
  sur x 380–600 :
  - mur ocre `#C9A46A` au sommet arrondi en arc ;
  - liseré rouge `#B8202E` qui suit l'arc, et deux lignes rouges horizontales ;
  - bande de métal gris `#8A949E` en bas.
  L'église (nef et clocher pointu) est gardée en silhouette, avec un clocher plus haut et son
  abat-son éclairé.
- **Premier plan** : les arbres ronds deviennent des platanes taillés : un tronc clair tacheté, des
  moignons de branches, une couronne plate et compacte.
- **Rationale**: le fronton est au premier plan du combat et doit se lire en couleur, comme le palace
  et le stade de la spec 004 ; le reste du village garde ses silhouettes.

## R6. Tests (principe IV)

En mode Edit :
- le niveau 8 finit par le Pelotari ;
- l'ordre des vies Talonneur < Pelotari < Sanglier ;
- le Pelotari ne donne pas de sort ;
- au début du combat le skateur est à pied (`OnFoot`), et il est de nouveau sur sa planche après la
  victoire ;
- lancer :
  - bouclier levé tôt : bloqué, pas de chute, pas de dégât ;
  - bouclier levé dans les 9 pas : parade, la pelote revient et blesse le boss ;
  - rien : chute ;
- rebond : saut (toucher) → pas de chute ; rien → chute ;
- toucher bref : saut ; doigt maintenu : pas de saut ;
- boomerang : il blesse le boss une fois, revient dans la main en moins de 90 pas, et le bouclier est
  impossible pendant son vol ;
- à pied, un glissé en diagonale ne fait pas de figure ;
- pelote volante : plus rapide que la mouette ; baissé, évitée ; debout, chute ; on en voit aux
  niveaux 8 à 11.
