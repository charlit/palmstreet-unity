# Feature Specification: Mode histoire en niveaux, mode infini et figures à débloquer

**Feature Branch**: `002-niveaux-progression`

**Created**: 2026-10-31

**Status**: Draft

**Input**: User description: "Mode histoire en niveaux et progression : une interface de carte des niveaux où, dès qu'on finit un niveau, on passe au suivant ; chaque niveau se passe dans une ville (par exemple Biarritz, Anglet, Bayonne…) ; tous les 3 niveaux, un boss aux caractéristiques différentes. Un mode infini où la vitesse augmente avec le temps. Au premier lancement, le skateur ne sait faire qu'un ollie et un grind ; les autres figures se débloquent au fur et à mesure qu'on avance dans le jeu."

Cette fonctionnalité s'ajoute à la spec 001 (portage de PalmStreet). Les règles de 001 restent valables
(saut, grind, éclairs, combo, boss à dents, profil, vêtements), sauf là où cette spec les remplace :
l'écran d'accueil (FR-002 de 001), les figures disponibles (FR-010 de 001), l'enchaînement des décors et
des boss (FR-015 à FR-019 et FR-029 de 001), la tenue et ses déblocages (FR-026 et FR-027 de 001).

Révision du 2026-10-31 (demandes du propriétaire) : accueil sans TOP 10 ni grand skateur, petit skateur
sur la carte, animation de lancement avec la planche « P. ST. », vêtements gagnés en finissant les
niveaux au lieu des couleurs libres (User Stories 5 et 6, FR-022 à FR-028).

**Écart au jeu web (constitution, principe I)** : le jeu web PalmStreet est une course sans fin, sans
niveaux, sans carte, sans boss différents, sans figures ni vêtements à débloquer et sans animation de
lancement. Tout ce que décrit cette spec s'en écarte, à la demande du propriétaire du jeu (2026-10-31),
pour donner une progression et des récompenses. Les règles de base (saut, grind, éclairs, combo, points des
figures, boss à dents) restent celles de la spec 001, donc du jeu web.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Jouer les niveaux sur la carte (Priority: P1)

Le joueur ouvre le jeu et touche JOUER. Une carte de la côte basque montre une route de 12 niveaux, un par
ville. Le premier niveau, Anglet, est ouvert, les autres sont verrouillés. Il touche Anglet : la partie
démarre dans le décor de la ville. Il roule jusqu'à la ligne d'arrivée. L'écran « NIVEAU RÉUSSI » montre
son score et propose NIVEAU SUIVANT. Le niveau 2, Biarritz, s'ouvre sur la carte.

**Why this priority**: c'est la structure du nouveau jeu ; sans elle, ni boss tous les 3 niveaux ni
progression des figures.

**Independent Test**: depuis une installation neuve, finir le niveau 1 : le niveau 2 s'ouvre sur la carte
et NIVEAU SUIVANT le lance directement.

**Acceptance Scenarios**:

1. **Given** une installation neuve, **When** le joueur ouvre la carte, **Then** seul le niveau 1 est
   jouable, les autres sont verrouillés avec leur nom de ville visible.
2. **Given** un niveau en cours, **When** le skateur passe la ligne d'arrivée, **Then** l'écran
   « NIVEAU RÉUSSI » affiche le score du niveau, le meilleur score de ce niveau et les boutons
   NIVEAU SUIVANT et CARTE.
3. **Given** l'écran « NIVEAU RÉUSSI », **When** le joueur touche NIVEAU SUIVANT, **Then** le niveau
   suivant démarre sans repasser par la carte.
4. **Given** une chute pendant un niveau, **When** l'écran de fin s'affiche, **Then** le joueur peut
   recommencer ce niveau depuis le début ou revenir à la carte ; le niveau reste non réussi.
5. **Given** des niveaux déjà réussis, **When** le joueur ouvre la carte, **Then** il peut rejouer
   n'importe lequel d'entre eux pour améliorer son score.

---

### User Story 2 - Un boss différent tous les 3 niveaux (Priority: P2)

Les niveaux 3, 6, 9 et 12 se terminent par un boss. Chaque boss a son allure, sa résistance et ses
attaques. Le niveau n'est réussi que quand le boss est battu.

**Why this priority**: les boss rythment l'aventure et marquent les grandes étapes (et les figures
débloquées).

**Independent Test**: jouer le niveau 3 jusqu'au boss, le battre : le niveau est réussi ; jouer le
niveau 6 : un boss d'une autre forme, avec d'autres attaques, arrive.

**Acceptance Scenarios**:

1. **Given** le niveau 3, **When** le skateur atteint la fin du parcours, **Then** le boss du niveau
   arrive avec son bandeau et sa barre de vie, et il n'y a plus d'obstacles normaux.
2. **Given** un boss en cours, **When** sa vie tombe à zéro, **Then** « BOOM » s'affiche, des points sont
   gagnés et le niveau est réussi.
3. **Given** deux boss différents, **When** on les compare, **Then** ils diffèrent par leur forme, leur
   vie et au moins une attaque.

---

### User Story 3 - Commencer avec l'ollie et le grind, débloquer les figures (Priority: P2)

Au premier lancement, le skateur sait faire un ollie et grinder, rien d'autre. Les figures (FLIP,
SHOVE-IT, 360°, 360° INVERSÉ) se débloquent en réussissant certains niveaux. Le jeu annonce chaque
nouvelle figure et montre comment la faire.

**Why this priority**: donne une raison d'avancer dans les niveaux et apprend le jeu pas à pas.

**Independent Test**: installation neuve : toucher en l'air ne fait aucune figure ; réussir le niveau 1 :
« NOUVELLE FIGURE : FLIP » s'affiche, puis le toucher en l'air fait un FLIP.

**Acceptance Scenarios**:

1. **Given** une installation neuve, **When** le joueur touche l'écran en l'air, **Then** aucune figure
   ne se fait (pas de points de figure).
2. **Given** le niveau qui débloque une figure, **When** il est réussi pour la première fois, **Then**
   l'écran de réussite annonce la nouvelle figure et la façon de la faire.
3. **Given** plusieurs figures débloquées, **When** le joueur touche en l'air, **Then** les figures
   s'enchaînent d'un saut à l'autre parmi celles débloquées seulement.
4. **Given** des figures débloquées en mode histoire, **When** le joueur lance le mode infini, **Then**
   il dispose des mêmes figures.

---

### User Story 4 - Mode infini (Priority: P3)

Depuis l'accueil, le joueur lance le mode INFINI : une course sans fin où la vitesse augmente avec le
temps. Les décors des villes déjà ouvertes défilent, les boss reviennent régulièrement, le score part dans
le classement TOP 10.

**Why this priority**: la rejouabilité et le classement, une fois l'aventure entamée.

**Independent Test**: lancer INFINI et tenir 2 minutes : la vitesse affichée par le défilement augmente
nettement, sans pause ; à la chute, le score est enregistré comme meilleur score infini.

**Acceptance Scenarios**:

1. **Given** le mode infini, **When** le temps passe, **Then** la vitesse augmente régulièrement, quel que
   soit le score.
2. **Given** une partie infinie terminée, **When** le score dépasse le meilleur score infini, **Then** il
   est enregistré et envoyé au classement.
3. **Given** le mode infini, **When** le joueur n'a encore ouvert qu'une ville, **Then** seul le décor de
   cette ville défile.

---

### User Story 5 - Gagner ses vêtements en finissant les niveaux (Priority: P2)

Le profil ne propose plus de couleurs au choix. Le skateur commence avec une tenue simple. Chaque niveau
réussi pour la première fois offre un vêtement ou une planche (Anglet : une casquette rouge, Biarritz :
un sweat bleu océan, …). Le joueur les enfile dans le profil.

**Why this priority**: une récompense concrète à chaque niveau, qui donne envie d'avancer.

**Independent Test**: installation neuve : le profil n'a que la tenue de départ et des objets verrouillés ;
réussir Anglet annonce « NOUVEAU : CASQUETTE ROUGE », qu'on peut ensuite porter.

**Acceptance Scenarios**:

1. **Given** une installation neuve, **When** le joueur ouvre le profil, **Then** il voit la tenue de
   départ portée et les objets à gagner verrouillés, chacun avec « Niveau n · Ville ».
2. **Given** le niveau 1 réussi pour la première fois, **When** l'écran de réussite s'affiche, **Then** il
   annonce la casquette rouge avec son dessin.
3. **Given** un objet gagné, **When** le joueur le touche dans le profil, **Then** le skateur le porte
   (aperçu et parties) ; un objet verrouillé ne s'enfile pas et rappelle le niveau qui le donne.

---

### User Story 6 - Animation de lancement (Priority: P3)

À l'ouverture de l'application, une planche de skate fait un flip au ralenti au milieu de l'écran ;
en tournant, elle montre son dessous où est écrit « P. ST. », puis le titre PALM STREET apparaît et
l'accueil s'ouvre.

**Why this priority**: l'identité du jeu dès la première seconde.

**Independent Test**: ouvrir l'application : l'animation se joue en moins de 3 secondes, « P. ST. » est
lisible sous la planche, un toucher la passe.

**Acceptance Scenarios**:

1. **Given** l'application fermée, **When** le joueur l'ouvre, **Then** l'animation de la planche se joue,
   puis l'accueil s'affiche.
2. **Given** l'animation en cours, **When** le joueur touche l'écran, **Then** l'accueil s'affiche tout de
   suite.

---

### Edge Cases

- Le joueur quitte l'application en plein niveau : au retour, le niveau est à recommencer ; les niveaux
  déjà réussis restent réussis.
- Le joueur rejoue un niveau déjà réussi et chute : rien n'est perdu.
- Un niveau qui débloque une figure est réussi une deuxième fois : pas de nouvelle annonce.
- Le dernier niveau (12) est réussi : l'écran annonce la fin de l'aventure et propose le mode infini.
- Toutes les figures débloquées : aucune annonce de figure supplémentaire.
- Joueur qui avait déjà une progression de la version précédente : le meilleur score devient le meilleur
  score infini ; la tenue repart de la tenue de départ (les couleurs libres n'existent plus), la peau et
  les cheveux choisis sont gardés ; la carte commence au niveau 1 et les figures sont à débloquer.
- Le joueur porte un objet puis le retire : il retrouve l'objet de départ de cet emplacement.
- Le joueur ouvre la carte sans connexion : tout fonctionne (seul le classement a besoin du réseau).

## Requirements *(mandatory)*

### Functional Requirements

**Accueil et carte**

- **FR-001**: L'accueil MUST proposer le titre, JOUER (mode histoire), INFINI et PROFIL (en haut à
  droite) ; il n'y a plus de bouton TOP 10 ni de grand skateur sur l'accueil.
- **FR-002**: JOUER MUST ouvrir une carte de la côte basque avec 12 niveaux reliés par une route, chacun
  nommé par sa ville, dans cet ordre : 1 Anglet, 2 Biarritz, 3 Bidart (boss), 4 Guéthary,
  5 Saint-Jean-de-Luz, 6 Hendaye (boss), 7 Bayonne, 8 Arcangues, 9 Espelette (boss), 10 Ascain,
  11 Cambo-les-Bains, 12 Biarritz la nuit (boss final).
- **FR-003**: Chaque niveau sur la carte MUST montrer son état : verrouillé, ouvert, réussi (avec son
  meilleur score) ; les niveaux à boss sont signalés. Le prochain niveau à jouer est mis en avant.
- **FR-004**: Au départ seul le niveau 1 MUST être ouvert ; réussir un niveau MUST ouvrir le suivant.
- **FR-022**: La carte MUST montrer un petit skateur (avec la tenue du joueur) posé sur le prochain niveau
  à jouer ; après une réussite, il roule le long de la route jusqu'au niveau suivant à l'ouverture de la
  carte.

**Niveaux**

- **FR-005**: Chaque niveau MUST se dérouler dans le décor de sa ville, reconnaissable par au moins un
  élément propre (monument, paysage ou ambiance), et afficher le nom de la ville au départ.
- **FR-006**: Un niveau sans boss MUST se terminer par une ligne d'arrivée visible après un parcours
  d'environ 60 à 90 secondes ; un niveau à boss MUST se terminer par le combat contre le boss.
- **FR-007**: La difficulté MUST augmenter d'un niveau à l'autre (vitesse, densité d'obstacles) et
  chaque nouveau type d'obstacle MUST apparaître d'abord dans un niveau précis, avec une courte bulle
  d'explication la première fois : rails (niveau 2), grands plots et bosses (niveau 4), mouettes à
  esquiver accroupi (niveau 7), tremplins (niveau 8), trous dans la route (niveau 10).
- **FR-008**: Passer la ligne d'arrivée ou battre le boss MUST afficher « NIVEAU RÉUSSI » avec le score
  du niveau, le meilleur score du niveau et les boutons NIVEAU SUIVANT et CARTE ; NIVEAU SUIVANT lance
  directement le niveau suivant.
- **FR-009**: Une chute pendant un niveau MUST proposer RECOMMENCER (le niveau depuis le début) et CARTE ;
  le niveau n'est pas réussi.
- **FR-010**: Les vêtements MUST se gagner en finissant les niveaux (FR-025), pas avec des points ;
  le score d'un niveau ne sert qu'à son meilleur score.

**Boss**

- **FR-011**: Les niveaux 3, 6, 9 et 12 MUST finir par un boss, chacun avec sa forme, sa vie et ses
  attaques ; le skateur tire ses dents automatiquement comme dans la spec 001 :
  - Bidart : le Crabe géant, au sol, avance par bonds qu'il faut sauter (4 000 points de vie).
  - Hendaye : le Goéland géant, en vol, pique vers le skateur et lâche des coquillages à sauter
    (6 000 points de vie).
  - Espelette : le Sanglier, charge au ras du sol et pose des barrières à sauter (7 000 points de vie).
  - Biarritz la nuit : le grand boss violet de la spec 001, qui s'envole et lâche des plots
    (8 000 points de vie).
- **FR-012**: Chaque boss battu MUST afficher « BOOM » et rapporter des points, et réussir le niveau.

**Figures à débloquer**

- **FR-013**: Au premier lancement, le skateur MUST savoir faire seulement l'ollie et le grind (avec la
  sortie de grind OUT!) ; un toucher en l'air ne fait aucune figure tant qu'aucune n'est débloquée.
- **FR-014**: Les figures MUST se débloquer en réussissant pour la première fois : niveau 1 → FLIP,
  niveau 3 (boss) → SHOVE-IT, niveau 6 (boss) → 360°, niveau 9 (boss) → 360° INVERSÉ.
- **FR-015**: Chaque déblocage MUST être annoncé sur l'écran de réussite (« NOUVELLE FIGURE : … ») avec
  le geste à faire (sauter, puis toucher en l'air).
- **FR-016**: Un toucher en l'air MUST faire la figure suivante parmi les seules figures débloquées, dans
  l'ordre FLIP, SHOVE-IT, 360°, 360° INVERSÉ ; les points des figures restent ceux de la spec 001.
- **FR-017**: Le profil MUST montrer les figures connues et celles à débloquer, avec le niveau qui les
  débloque.

**Mode infini**

- **FR-018**: INFINI MUST lancer une course sans fin dont la vitesse augmente avec le temps de jeu
  (et non plus avec le score), sans palier ni pause, plafonnée à 5,5 (un saut reste assez long pour passer les obstacles).
- **FR-019**: En mode infini, les décors des villes déjà ouvertes en histoire MUST défiler à tour de
  rôle, et un boss MUST revenir régulièrement, choisi parmi ceux déjà rencontrés en histoire.
- **FR-020**: Le mode infini MUST avoir son propre meilleur score, envoyé au classement ; le bouton
  TOP 10 se trouve sur l'écran de fin du mode infini (il n'est plus sur l'accueil) ; les figures
  disponibles sont celles débloquées en histoire.

**Garde-robe gagnée en jouant**

- **FR-023**: Le profil MUST ne plus proposer de palettes de couleurs pour les vêtements et la planche :
  chaque emplacement (couvre-chef, haut, bas, chaussures, planche) propose seulement les objets possédés.
- **FR-024**: Le skateur MUST commencer avec une tenue de départ : tête nue, t-shirt blanc, jean bleu,
  baskets blanches, planche en bois naturel aux roues crème.
- **FR-025**: Réussir un niveau pour la première fois MUST donner un objet, dans cet ordre :
  1 Anglet → casquette rouge ; 2 Biarritz → sweat bleu océan ; 3 Bidart → planche rouge « Crabe » ;
  4 Guéthary → short de surf turquoise ; 5 Saint-Jean-de-Luz → bonnet rayé marine ;
  6 Hendaye → sweat blanc « Goéland » ; 7 Bayonne → bandeau rouge ; 8 Arcangues → pantalon vert ;
  9 Espelette → planche « Piment » ; 10 Ascain → baskets jaunes ; 11 Cambo-les-Bains → sweat violet ;
  12 Biarritz la nuit → planche « Néon » rose et cyan.
- **FR-026**: L'écran de réussite MUST annoncer l'objet gagné (« NOUVEAU : … ») avec son dessin ; le
  profil MUST montrer les objets possédés à enfiler et les objets à gagner verrouillés avec
  « Niveau n · Ville ».
- **FR-027**: La peau, la coiffure (courts, boucles) et la couleur des cheveux MUST rester au choix libre
  dans le profil ; les points cumulés ne débloquent plus rien et ne sont plus affichés.

**Lancement**

- **FR-028**: À chaque ouverture de l'application, une animation MUST montrer une planche qui fait un
  flip au ralenti au centre de l'écran, son dessous marqué « P. ST. » bien lisible pendant la rotation,
  puis le titre PALM STREET ; elle dure au plus 2,5 secondes et un toucher la passe.

**Sauvegarde**

- **FR-021**: La progression (niveaux réussis, meilleur score par niveau, figures débloquées, objets
  gagnés et portés, meilleur score infini) MUST être gardée sur l'appareil et survivre à la fermeture de l'application.

### Key Entities

- **Niveau** : numéro (1 à 12), ville, décor, longueur du parcours, boss éventuel, figure débloquée
  éventuelle, types d'obstacles autorisés, vitesse de départ et maximale.
- **Boss** : nom, forme, points de vie, attaques, niveau où il apparaît.
- **Progression du joueur** : niveaux réussis, meilleur score par niveau, figures débloquées, objets
  gagnés, tenue portée, meilleur score infini.
- **Figure** : nom, points (spec 001), niveau qui la débloque.
- **Objet de garde-robe** : nom, emplacement (couvre-chef, haut, bas, chaussures, planche), apparence,
  niveau qui le donne (ou « départ »).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un nouveau joueur réussit le niveau 1 en 3 essais au plus dans 80 % des cas.
- **SC-002**: Un niveau sans boss dure entre 60 et 90 secondes sans chute ; un niveau à boss, au plus
  2 min 30.
- **SC-003**: De l'accueil au départ d'un niveau, il faut au plus 2 touchers (JOUER, puis le niveau).
- **SC-004**: Après « NIVEAU RÉUSSI », le niveau suivant démarre en 1 toucher.
- **SC-005**: En mode infini, la vitesse après 2 minutes de jeu est au moins 1,5 fois celle du départ.
- **SC-006**: 100 % des déblocages de figures sont annoncés une seule fois, au bon niveau.
- **SC-007**: Toute la progression est retrouvée après fermeture et réouverture de l'application.
- **SC-008**: De l'ouverture de l'application à l'accueil, au plus 3 secondes animation comprise.
- **SC-009**: 100 % des objets sont donnés au bon niveau, une seule fois, et annoncés.

## Assumptions

- 12 niveaux et 4 boss pour cette version ; d'autres villes pourront s'ajouter plus tard.
- Les villes sont nommées pour leur ambiance ; les décors sont dessinés pour le jeu (pas de marque ni de
  photo, pas de logo de ville).
- Le niveau 12 reprend le décor « NUIT NÉON » et ses difficultés (spec 001, FR-029 à FR-032) ; le
  passage automatique au niveau néon après le premier boss disparaît au profit de la carte.
- Les décors existants servent de base : Largenté pour Anglet, BTZ pour Biarritz, Arcangues pour
  Arcangues ; les autres villes reçoivent un décor nouveau.
- Le mode infini est disponible dès le premier lancement, avec au moins la ville d'Anglet.
- Pas d'étoiles ni d'objectifs secondaires par niveau dans cette version : réussir = finir le niveau.
- Les vêtements ne se gagnent plus par points (spec 001, FR-027) mais par niveaux (FR-025) ; la peau et
  les cheveux restent libres parce qu'ils décrivent le joueur, pas sa tenue.
- Le classement TOP 10 porte sur le mode infini seulement et s'ouvre depuis son écran de fin.
- L'animation de lancement reste courte et se joue à chaque ouverture (pas seulement la première).
- La progression est locale à l'appareil (pas de sauvegarde en ligne) ; une désinstallation la perd.
