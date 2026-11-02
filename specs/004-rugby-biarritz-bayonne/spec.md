# Feature Specification: Biarritz et Bayonne aux couleurs du rugby

**Feature Branch**: `004-rugby-biarritz-bayonne`

**Created**: 2026-11-02

**Status**: Draft

**Input**: User description: "Biarritz et Bayonne aux couleurs du rugby : décors, boss, obstacles et tenues à débloquer. […] Stade Aguiléra, Hôtel du Palais, Rocher de la Vierge ; boss LE PILIER (niveau 2) et LE TALONNEUR (niveau 7) ; tenues de rugby à débloquer ; mouette redessinée et ballon de rugby en obstacle volant ; le tutoriel dit « mouettes »."

**Écart au jeu web (constitution, principe I)** : nouveaux boss, nouveaux décors, nouvel obstacle
et nouvelles récompenses, à la demande du propriétaire. Les règles de base (saut, figures, combat
de boss par dents et sorts) ne changent pas.

**Marques (principe V)** : aucun logo, écusson, sigle ni nom de club. Les joueurs et le stade ne
portent que des couleurs ; le tableau du stade affiche « BIARRITZ vs BAYONNE » (ou « DERBY »).

**Références visuelles** :
- `Art/apercus/apercu-biarritz.png` : décor du niveau 2 ;
- `Art/apercus/apercu-obstacles.png` : mouette actuelle, mouette redessinée, ballon ;
- `Art/apercus/apercu-boss-rugby.png` : LE PILIER et ses actions ;
- `Art/apercus/apercu-boss-bayonne.png` : LE TALONNEUR ;
- `Art/apercus/tenues-rugby.png` : les deux tenues ;
- `Art/apercus/apercu-idees-basques.png` : Rocher de la Vierge.

Sur les aperçus, le tableau affiche des sigles de clubs : c'est le texte de la spec qui fait foi,
pas l'image.

## Clarifications

### Session 2026-11-02

- Q: Battre un boss de rugby débloque-t-il aussi un sort ? → A: non, il ne donne que sa tenue ; les 4 sorts restent aux boss des niveaux 3, 6, 9 et 12.
- Q: Le niveau 2 a-t-il des ballons volants dans son parcours ? → A: oui, quelques ballons volants, avec le conseil « glisse vers le bas pour te baisser », déplacé du niveau 7 au niveau 2.
- Q: Une touche par une attaque de boss de rugby fait-elle chuter tout de suite ? → A: oui, une touche = une chute, comme pour tous les boss.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Affronter LE PILIER à Biarritz (Priority: P1)

Au bout du niveau 2 (Biarritz), le joueur affronte un rugbyman. Le Pilier trottine, puis enchaîne
trois attaques qu'il faut lire et esquiver. Le joueur le blesse avec ses figures et ses sorts,
comme les autres boss.

Ses trois attaques :
- **la passe** : un ballon lancé à hauteur de tête. On l'évite en se baissant.
- **la chandelle** : un ballon qui retombe devant le skateur et rebondit vers lui. On l'évite en
  sautant.
- **le plaquage** : le boss plonge au ras du sol. On l'évite en sautant.

**Why this priority**: c'est le cœur de la fonctionnalité ; il donne au niveau 2 un premier boss tôt
dans l'aventure et un nouveau type de combat.

**Independent Test**: jouer le niveau 2 jusqu'à la fin : LE PILIER arrive. Survivre à chacune de
ses trois attaques avec le bon geste, le battre : le niveau est réussi.

**Acceptance Scenarios**:

1. **Given** le niveau 2, **When** le skateur arrive au bout du parcours, **Then** LE PILIER apparaît avec sa barre de vie et son nom.
2. **Given** une passe à hauteur de tête, **When** le skateur est baissé, **Then** le ballon passe au-dessus de lui sans le faire tomber.
3. **Given** une passe, **When** le skateur reste debout ou saute, **Then** le ballon le touche et il chute.
4. **Given** une chandelle, **When** le ballon rebondit vers le skateur et que celui-ci saute, **Then** le ballon passe sous lui.
5. **Given** un plaquage, **When** le skateur saute au bon moment, **Then** le boss passe dessous ; **When** il ne saute pas, **Then** il chute.
6. **Given** les dents et les sorts du joueur, **When** ils touchent LE PILIER, **Then** sa vie baisse ; à zéro, il est battu et le niveau est réussi.
7. **Given** les attaques, **When** elles arrivent, **Then** chacune est annoncée par une pose reconnaissable assez tôt pour réagir.

---

### User Story 2 - Gagner la tenue complète d'un boss de rugby (Priority: P2)

En battant LE PILIER pour la première fois, le joueur gagne sa tenue complète, 5 pièces, dans la
garde-robe :
- bandeau blanc à liseré rouge ;
- maillot rouge à cerceau blanc ;
- short blanc et chaussettes rouges ;
- baskets noires à bande rouge ;
- planche rouge à roues blanches.

Il en va de même avec LE TALONNEUR et sa tenue bleu ciel :
- casque de mêlée bleu ciel à bandes blanches ;
- maillot bleu ciel à cerceau blanc ;
- short blanc et chaussettes bleu ciel ;
- baskets noires à bande bleu ciel ;
- planche bleu ciel.

**Why this priority**: c'est la récompense qui donne envie de battre ces boss, et elle se voit sur le skateur en permanence.

**Independent Test**: depuis une installation neuve, battre LE PILIER : l'écran de réussite annonce la tenue ; dans le profil, les 5 pièces sont disponibles et se portent.

**Acceptance Scenarios**:

1. **Given** LE PILIER jamais battu, **When** le joueur le bat, **Then** l'écran de réussite annonce la tenue gagnée et les 5 pièces apparaissent dans la garde-robe.
2. **Given** LE PILIER déjà battu, **When** le joueur le bat à nouveau, **Then** rien de nouveau n'est annoncé.
3. **Given** les 5 pièces gagnées, **When** le joueur les porte, **Then** le skateur porte la tenue complète, comme sur `tenues-rugby.png`.
4. **Given** les boss Crabe, Goéland, Sanglier et Violet, **When** le joueur les bat, **Then** ils ne donnent aucune tenue.
5. **Given** une pièce de tenue de rugby pas encore gagnée, **When** le joueur la regarde dans le profil, **Then** elle indique le boss à battre pour l'avoir.

---

### User Story 3 - Affronter LE TALONNEUR à Bayonne (Priority: P2)

Le niveau 7 (Bayonne), qui n'avait pas de boss, se termine désormais par LE TALONNEUR. Il a les
mêmes attaques que LE PILIER. Il porte une tenue bleu ciel et blanc, avec un crâne rasé et une barbe.

**Why this priority**: donne au derby Biarritz-Bayonne son deuxième match ; même comportement que le Pilier, donc peu de travail une fois la US1 faite.

**Independent Test**: jouer le niveau 7 jusqu'à la fin : LE TALONNEUR apparaît, se joue comme LE PILIER, et le battre réussit le niveau.

**Acceptance Scenarios**:

1. **Given** le niveau 7, **When** le skateur arrive au bout du parcours, **Then** LE TALONNEUR apparaît, en bleu ciel et blanc, crâne rasé et barbe.
2. **Given** LE TALONNEUR, **When** il attaque, **Then** il utilise la passe, la chandelle et le plaquage, qui s'esquivent comme ceux du Pilier.
3. **Given** la carte, **When** le joueur la regarde, **Then** les niveaux 2 et 7 sont marqués BOSS comme les autres niveaux à boss.

---

### User Story 4 - Biarritz reconnaissable : stade, palace et rocher (Priority: P2)

Dans le niveau 2, le joueur passe devant trois monuments de Biarritz :
- **le stade Aguiléra** :
  - tribune couverte aux sièges rouges et blancs, toit en porte-à-faux ;
  - pylônes d'éclairage allumés, poteaux de rugby ;
  - tableau lumineux « BIARRITZ vs BAYONNE ».
- **l'Hôtel du Palais** : palace brique et pierre, toits d'ardoise très pentus, trois pavillons,
  terrasse sur la plage.
- **le Rocher de la Vierge** : sur une autre portion du parcours, un rocher dans l'océan avec sa
  statue blanche et sa passerelle métallique reliée à la falaise.

**Why this priority**: donne au niveau son identité et met en scène le combat contre le rugbyman ; le niveau reste jouable sans.

**Independent Test**: capture du niveau 2 à plusieurs endroits du parcours : le stade, le palace et le rocher sont visibles et reconnaissables.

**Acceptance Scenarios**:

1. **Given** le niveau 2, **When** il défile, **Then** le stade (tribune, éclairage, poteaux, tableau « BIARRITZ vs BAYONNE ») et l'Hôtel du Palais passent au premier plan.
2. **Given** le niveau 2, **When** il défile plus loin, **Then** le Rocher de la Vierge apparaît dans l'océan avec sa statue et sa passerelle.
3. **Given** tout le décor, **When** on le regarde, **Then** aucun logo, écusson, sigle ou nom de club n'apparaît.
4. **Given** le décor au premier plan, **When** il passe derrière le skateur, **Then** le skateur, les obstacles et les textes restent lisibles.

---

### User Story 5 - Mouettes redessinées et ballons de rugby (Priority: P3)

La mouette est redessinée : dos gris perle, bouts d'ailes noirs, bec jaune à tache rouge, sans halo
rose le jour.

À Biarritz (niveaux 2 et 12), des ballons de rugby passent aussi en l'air. Ils sont blancs à bandes
rouges et tournent sur eux-mêmes. Comme les mouettes, on les évite en se baissant.

Le tutoriel parle de « mouettes » et non plus d'« oiseaux ».

**Why this priority**: améliore un obstacle existant et ajoute une variante locale ; le jeu fonctionne sans.

**Independent Test**: jouer le niveau 12 : mouettes redessinées et ballons apparaissent, et se baisser évite les deux. Ouvrir le tutoriel : le mot « mouettes » est écrit.

**Acceptance Scenarios**:

1. **Given** un niveau avec des mouettes, **When** une mouette passe, **Then** elle a le nouveau dessin, sans halo rose le jour.
2. **Given** les niveaux 2 et 12, **When** des obstacles volants apparaissent, **Then** certains sont des ballons de rugby qui tournent sur eux-mêmes.
3. **Given** un ballon de rugby, **When** le skateur est baissé, **Then** il passe au-dessus ; **When** il est debout, **Then** il le touche et le skateur chute, exactement comme une mouette.
4. **Given** le tutoriel, **When** il s'ouvre, **Then** la ligne sur le geste vers le bas dit « mouettes ».

---

### Edge Cases

- Le skateur saute au moment d'une passe : la passe est à hauteur de tête ; comme pour les mouettes, seul un saut très haut et bien placé passe au-dessus, le geste prévu est de se baisser.
- Le ballon de chandelle rebondit pendant que le skateur est déjà en l'air : s'il est au-dessus du ballon, il passe ; s'il redescend dessus, il chute.
- Un sort ou une dent touche le ballon lancé par le boss : le ballon n'est pas détruit (seul le boss prend des dégâts), sauf si le plan en décide autrement pour la lisibilité.
- Le joueur bat LE PILIER puis recommence le niveau 2 : la tenue n'est donnée qu'une fois.
- Le joueur a déjà gagné tous les objets par niveaux et étoiles : la tenue de rugby s'ajoute quand même.
- Mode infini : les boss de rugby, une fois rencontrés en histoire, apparaissent dans la rotation des boss comme les autres.
- Le niveau 2 est le premier niveau à avoir des obstacles volants : il doit expliquer qu'on se baisse avec un glissé vers le bas, comme le fait aujourd'hui le niveau 7 pour les mouettes.
- Avec le mode « tout débloqué » de test, les deux tenues apparaissent dans la garde-robe comme les autres objets.

## Requirements *(mandatory)*

### Functional Requirements

**Boss de rugby**

- **FR-001**: Le niveau 2 (BIARRITZ) MUST finir par le boss LE PILIER, et le niveau 7 (BAYONNE) par le boss LE TALONNEUR ; chacun est réussi seulement quand son boss est battu.
- **FR-002**: Entre deux attaques, un boss de rugby MUST trottiner devant le skateur.
- **FR-003**: Un boss de rugby MUST alterner trois attaques :
  - **la passe** : un ballon à hauteur de tête, évité seulement en étant baissé ;
  - **la chandelle** : un ballon qui retombe devant le skateur et rebondit vers lui, évité en sautant ;
  - **le plaquage** : un plongeon au ras du sol, évité en sautant.
- **FR-004**: Chaque attaque MUST être précédée d'une pose reconnaissable, assez tôt pour que le joueur ait le temps de réagir.
- **FR-005**: Les boss de rugby MUST prendre des dégâts des dents et des sorts comme les autres boss.
- **FR-005b**: Être touché par une attaque d'un boss de rugby (ballon ou plaquage) MUST faire chuter le skateur tout de suite, comme avec les autres boss.
- **FR-006**: LE PILIER MUST être le boss le moins résistant du jeu. LE TALONNEUR MUST être plus résistant que lui et moins que le boss du niveau 9 (le Sanglier). La difficulté suit ainsi l'ordre des niveaux.
- **FR-007**: LE PILIER MUST porter :
  - un maillot rouge à cerceau et col blancs ;
  - un short blanc ;
  - des chaussettes rouges à bandes blanches ;
  - un bandeau blanc.
- **FR-008**: LE TALONNEUR MUST porter la même tenue en bleu ciel et blanc, avec un crâne rasé et une barbe. Son comportement est identique à celui du Pilier.
- **FR-009**: La carte, l'aperçu des villes et l'annonce de début de combat MUST traiter les niveaux 2 et 7 comme des niveaux à boss : marque BOSS, nom du boss, conseil d'esquive.

**Tenues de rugby**

- **FR-010**: Battre un boss de rugby pour la première fois MUST débloquer sa tenue complète de 5 pièces (tête, haut, bas, chaussures, planche) dans la garde-robe, telle que décrite dans la US2.
- **FR-011**: L'écran de réussite MUST annoncer la tenue gagnée la première fois ; les fois suivantes, il ne l'annonce pas.
- **FR-012**: Seuls LE PILIER et LE TALONNEUR MUST donner une tenue ; les autres boss n'en donnent pas.
- **FR-012b**: Les boss de rugby MUST NOT débloquer de sort : les 4 sorts restent donnés par les boss des niveaux 3, 6, 9 et 12.
- **FR-013**: Une pièce de rugby non gagnée MUST indiquer dans le profil le boss qui la donne.
- **FR-014**: Les objets déjà donnés par les niveaux 2 et 7 MUST rester donnés comme avant ; la tenue s'ajoute.

**Décor de Biarritz (niveau 2)**

- **FR-015**: Le décor du niveau 2 MUST montrer au premier plan le stade Aguiléra :
  - tribune couverte aux sièges rouges et blancs ;
  - toit en porte-à-faux ;
  - pylônes d'éclairage allumés ;
  - poteaux de rugby ;
  - tableau lumineux « BIARRITZ vs BAYONNE ».
- **FR-016**: Le décor du niveau 2 MUST montrer l'Hôtel du Palais :
  - brique et pierre ;
  - toits d'ardoise très pentus ;
  - trois pavillons ;
  - terrasse sur la plage.
- **FR-017**: Le décor du niveau 2 MUST montrer, sur une autre portion du parcours, le Rocher de la Vierge : rocher dans l'océan, statue blanche, passerelle métallique reliée à la falaise.
- **FR-018**: Aucun élément du jeu MUST NOT montrer de logo, d'écusson, de sigle ou de nom de club.

**Obstacles volants**

- **FR-019**: La mouette MUST être redessinée :
  - dos gris perle ;
  - bouts d'ailes noirs ;
  - bec jaune à tache rouge ;
  - pas de halo rose le jour.
- **FR-020**: Aux niveaux 2 et 12 (Biarritz), les obstacles volants MUST comprendre des ballons de rugby (blancs à bandes rouges, tournant sur eux-mêmes). Ils ont la même taille de collision que la mouette (30 × 16) et le même effet : se baisser les évite, sinon chute.
- **FR-021**: Le tutoriel et les conseils MUST dire « mouettes » au lieu d'« oiseaux ».
- **FR-022**: Le niveau 2 MUST avoir quelques ballons volants dans son parcours et afficher au départ le conseil « glisse vers le bas pour te baisser » ; ce conseil n'est plus au niveau 7.

### Key Entities

- **Boss de rugby** : un type de boss avec deux variantes (LE PILIER, LE TALONNEUR). Chaque variante a :
  - un nom et des couleurs ;
  - une tête (bandeau, ou crâne rasé et barbe) ;
  - une vie ;
  - les trois mêmes attaques (passe, chandelle, plaquage).
- **Ballon** : un projectile du boss (passe ou chandelle) ou un obstacle volant de Biarritz. Il a une hauteur, une trajectoire, des rebonds pour la chandelle, et tourne sur lui-même.
- **Tenue de rugby** : 5 objets de garde-robe liés à un boss. Ils sont gagnés quand ce boss est battu, et pas par un niveau ni par des étoiles.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Chaque attaque d'un boss de rugby s'esquive à 100 % avec le bon geste (baissé pour la passe, saut pour la chandelle et le plaquage), vérifié par des tests de règles.
- **SC-002**: Le joueur a au moins 0,5 s entre le début de la pose annonçant une attaque et le moment où il doit réagir.
- **SC-003**: Un joueur qui esquive bien bat LE PILIER en moins de 60 secondes, et LE TALONNEUR en moins de 90 secondes.
- **SC-004**: Battre un boss de rugby pour la première fois ajoute exactement 5 pièces à la garde-robe, et 0 les fois suivantes.
- **SC-005**: Sur des captures du niveau 2, le stade, l'Hôtel du Palais et le Rocher de la Vierge sont chacun visibles. Aucun logo, sigle ou nom de club n'apparaît.
- **SC-006**: Le ballon de rugby volant et la mouette donnent le même résultat (évités baissé, chute sinon) dans 100 % des cas testés.
- **SC-007**: La partie reste à 60 images/s sur iPhone et Android pendant un combat contre un boss de rugby.

## Assumptions

- Le niveau 7 garde ses obstacles (dont les mouettes) avant le boss ; seul le boss est ajouté à la fin.
- Les dents et les sorts ne détruisent pas les ballons du boss : ils ne visent que le boss.
- Le ballon volant de Biarritz s'ajoute aux mouettes du niveau 12 ; au niveau 2, les ballons sont les seuls obstacles volants.
- La vie exacte des boss et le rythme des attaques sont réglés au plan, puis en jouant, dans l'ordre fixé par FR-006.
- Le moment où le niveau 2 passe du stade au Rocher de la Vierge est un choix de mise en page, réglé au plan.
- Le tableau du stade affiche « BIARRITZ vs BAYONNE », jamais de sigle de club.
- En mode infini, les boss de rugby entrent dans la rotation des boss déjà rencontrés, comme ceux de la spec 002.
