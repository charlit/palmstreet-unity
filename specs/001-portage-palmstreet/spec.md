# Feature Specification: Portage de PalmStreet sur iOS et Android

**Feature Branch**: `001-portage-palmstreet`

**Created**: 2026-10-30

**Status**: Clarified

**Input**: User description: "Refaire le jeu PalmStreet (github.com/charlit/PalmStreet) avec Unity et l'avoir sous iOS et Android."

Référence : le jeu web `public/index.html` du dépôt PalmStreet (commit 52f79e2). Les valeurs
chiffrées ci-dessous en viennent. Le jeu web compte en images à 60 images/s et en pixels d'un
écran logique de 390 × 620 : les durées sont données ici en secondes et les distances dans ces
mêmes unités logiques, à mettre à l'échelle de l'écran.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Rouler, sauter, tomber (Priority: P1)

Le joueur ouvre le jeu, voit son skateur, touche START pour partir. Le skateur roule seul
vers la droite sur un sol avec des bosses ; le joueur touche l'écran pour faire un ollie au-dessus
des plots. Le score monte avec la distance. Toucher un plot fait chuter : écran « WIPEOUT ! »
avec le score, puis on rejoue d'un toucher.

**Why this priority**: c'est la boucle de jeu entière ; sans elle il n'y a pas de jeu.

**Independent Test**: lancer une partie sur un téléphone, sauter 10 plots, en percuter un : la
partie s'arrête, le score s'affiche, un toucher relance une partie.

**Acceptance Scenarios**:

1. **Given** l'écran d'accueil, **When** le joueur touche START, **Then** la partie démarre, la
   musique joue et le skateur roule.
2. **Given** le skateur au sol, **When** le joueur touche l'écran, **Then** il saute (ollie) et
   retombe sur le sol en suivant le relief.
3. **Given** un plot devant le skateur, **When** il le touche, **Then** la planche s'échappe en
   rebondissant, l'écran de fin affiche « WIPEOUT ! » et le score.
4. **Given** un score supérieur au meilleur score enregistré, **When** la partie se termine,
   **Then** le meilleur score (BEST) est mis à jour et gardé après fermeture de l'app.

---

### User Story 2 - Figures, grinds, éclairs et combos (Priority: P2)

En l'air, un deuxième toucher fait une figure (FLIP, SHOVE-IT, 360°, 360° INVERSÉ).
Atterrir sur un rail lance un grind automatique. Des rangées d'éclairs jaunes rapportent des
points. Chaque figure ou grind réussi augmente le combo, qui multiplie les points.

**Why this priority**: c'est ce qui fait le score et le plaisir du jeu web, après la boucle de base.

**Independent Test**: dans une partie, faire une figure, grinder un rail et ramasser une rangée
d'éclairs : les points affichés suivent les barèmes de FR-010 à FR-014.

**Acceptance Scenarios**:

1. **Given** le skateur en l'air, **When** le joueur touche l'écran,
   **Then** une figure s'anime, le texte « +points NOM ! » s'affiche et le combo augmente.
2. **Given** le skateur qui retombe sur le dessus d'un rail, **When** ses pieds touchent le rail,
   **Then** il grinde sans chuter jusqu'au bout du rail, puis reçoit le bonus « GRIND! ».
3. **Given** un combo x3, **When** le joueur ramasse un éclair, **Then** il gagne 30 points.

---

### User Story 3 - Décors et boss (Priority: P3)

Le décor change avec le score : Largenté (palmiers, océan et surfeurs), puis BTZ, puis Arcangues,
en boucle. À 15 000 points arrive le boss : le sol devient plat, le skateur lui tire des dents
automatiquement, le boss s'envole régulièrement et lâche des plots à sauter. Le battre rapporte
5 000 points, affiche « BOOM » et « TU ES UN VRAI PST ! », puis le jeu continue.

**Why this priority**: le contenu qui donne envie de rejouer, une fois le jeu de base solide.

**Independent Test**: atteindre 5 000 puis 10 000 points (changements de décor), puis 15 000
(boss) et le battre ; ou utiliser le raccourci caché du soleil sur l'écran d'accueil.

**Acceptance Scenarios**:

1. **Given** un score qui passe 5 000, **When** le jeu continue, **Then** le décor passe à BTZ et
   le bandeau « FULL SPEED » s'affiche.
2. **Given** un score qui atteint 15 000, **When** le boss arrive, **Then** le bandeau « BOSS ! »
   s'affiche, sa barre de vie apparaît et les obstacles normaux cessent.
3. **Given** la vie du boss à 0, **When** la dernière dent le touche, **Then** +5 000 points,
   « BOOM », puis un long plat suivi d'une très longue barre de nose grind.

---

### User Story 5 - Niveau 2 : NUIT NÉON (Priority: P3)

Le premier boss battu, le joueur entre dans un nouveau monde : la côte de nuit, lune, étoiles, grande
roue et ville en néon, feux d'artifice. La route a des trous à sauter, des mouettes foncent à hauteur de
tête (on s'accroupit ou on saute), des tremplins néon envoient très haut, et la vitesse monte plus.

**Why this priority**: donner une suite au boss, avec des difficultés vraiment nouvelles.

**Independent Test**: battre le boss (ou raccourci du soleil) : bandeau NIVEAU 2, décor de nuit, puis
un trou, une mouette et un tremplin dans les 30 secondes.

**Acceptance Scenarios**:

1. **Given** le premier boss battu, **When** le plat du nose grind se termine, **Then** le décor NUIT NÉON
   reste jusqu'à la fin de la partie et le bandeau « NIVEAU 2 » s'affiche.
2. **Given** un trou devant le skateur, **When** il ne saute pas, **Then** il tombe et la partie s'arrête.
3. **Given** une mouette qui arrive, **When** le joueur garde le doigt appuyé (accroupi), **Then** elle
   passe au-dessus sans le toucher.
4. **Given** un tremplin, **When** le skateur roule dessus, **Then** il s'envole (« MÉGA AIR ! ») et peut
   faire deux figures avant de retomber.

---

### User Story 4 - Classement et réglages (Priority: P4)

Le joueur voit son meilleur score, coupe ou remet le son, et compare ses scores à ceux des autres
joueurs dans un classement en ligne.

**Why this priority**: utile pour la rejouabilité, mais le jeu se tient sans.

**Independent Test**: couper le son (il reste coupé au relancement), terminer une partie et
retrouver son score dans le classement.

**Acceptance Scenarios**:

1. **Given** le son coupé, **When** l'app est relancée, **Then** le son reste coupé.
2. **Given** une partie terminée, **When** le joueur ouvre le classement, **Then** son score y
   apparaît selon FR-022.

---

### Edge Cases

- L'app passe en arrière-plan (appel, notification) pendant une partie : le jeu se met en pause
  et la musique s'arrête ; au retour, la partie reprend après un toucher.
- Écran très haut (iPhone récent) ou large (iPad, tablette Android) : le jeu garde sa zone de jeu
  en portrait sans déformer l'image ; les marges sont remplies par le décor.
- Toucher pendant un grind : sortie de grind avec bonus « +15 OUT! » et un saut un peu plus court.
- Un toucher au sol fait seulement un ollie ; la figure vient du toucher suivant, en l'air (2 touchers en tout).
- Un plot lâché par le boss tombe sur une pente : il se pose sur le sol à cet endroit.
- Téléphone hors ligne : le jeu marche entièrement ; seul le classement en ligne est indisponible
  et le dit.

## Requirements *(mandatory)*

### Functional Requirements

**Boucle de jeu**

- **FR-001**: Le jeu MUST se jouer en portrait, plein écran, au toucher seul.
- **FR-002** *(remplacé par la spec 002 — FR-001)*: L'écran d'accueil MUST montrer le skateur du joueur avec sa tenue, un grand bouton
  START, un bouton PROFIL en haut à droite et un bouton TOP 10. (Révision 2026-10-30 : le choix entre 4 skateurs est remplacé
  par un skateur unique qu'on habille dans le profil, FR-026.)
- **FR-003** *(remplacé par la spec 002 — FR-007 et FR-018)*: Le skateur MUST rouler seul ; la vitesse démarre à 1,7 et gagne 0,045 tous les
  100 points de score, jusqu'à 4,3 (unités logiques par image à 60 images/s).
- **FR-004**: Le score MUST gagner 1 point par image de jeu (60 par seconde).
- **FR-005**: Le sol MUST alterner plat (240 à 500 de long), montée (130 à 240, hauteur 45 à 100)
  et descente qui revient au niveau de base ; la vitesse gagne jusqu'à +55 % en descente et
  perd jusqu'à −22 % en montée. Le départ est plat sur 420.
- **FR-006**: Un toucher au sol MUST faire un ollie (vitesse verticale 9,6 vers le haut,
  gravité 0,5 par image) ; garder le doigt appuyé plus de 0,22 s fait s'accroupir.
- **FR-007**: Les obstacles MUST apparaître un seul à la fois, jamais superposés : sous
  2 000 points, que des petits plots ; ensuite plots et rails (2 plots pour 1 rail). Jamais de
  rail près d'une pente, jamais d'obstacle au tout début d'une descente.
- **FR-008**: Le premier obstacle de la partie, puis un tous les 5 000 points, MUST être un grand
  rail arc-en-ciel (3 fois plus long), remplacé par un plot s'il n'y a pas assez de plat.
- **FR-009**: Toucher un obstacle (hors grind) MUST terminer la partie : son de chute, planche qui
  s'échappe en rebondissant, écran « WIPEOUT ! » avec le score et le meilleur score.

**Points**

- **FR-010** *(remplacé par la spec 002 — FR-013 à FR-016)*: Une figure MUST se faire par un toucher en l'air (le premier toucher fait sauter, le deuxième fait la figure), une seule
  par saut ; les figures tournent dans l'ordre FLIP (30), SHOVE-IT (35), 360° (45),
  360° INVERSÉ (45), multipliés par le combo.
- **FR-011**: Retomber sur le dessus d'un rail MUST lancer un grind : +2 points par image, puis
  à la sortie du rail « +20 × combo GRIND! ».
- **FR-012**: Sauter pendant un grind MUST donner « +15 OUT! » et un saut à 85 % de la hauteur.
- **FR-013**: Les éclairs MUST arriver en rangées de 3 à 5, à 3 hauteurs possibles, et rapporter
  10 × combo chacun ; ils sont seuls (jamais sur un obstacle), toujours sur les montées et les
  débuts de descente, et sinon 38 % du temps.
- **FR-014**: Chaque figure ou grind MUST augmenter le combo de 1 ; le combo revient à x1 après
  environ 2,2 s sans figure ni grind ; il s'affiche « COMBO xN ».

**Décors et boss**

- **FR-015** *(remplacé par la spec 002 — FR-005 et FR-019)*: Le décor MUST suivre un cycle de 20 000 points : Largenté (0–5 000), BTZ
  (5 000–10 000), Arcangues (10 000–20 000), avec leurs couleurs de ciel, soleil et silhouettes
  du jeu web, en défilement parallaxe.
- **FR-016** *(remplacé par la spec 002 — FR-019 : mode infini seulement)*: Le bandeau « FULL SPEED » MUST s'afficher tous les 5 000 points.
- **FR-017** *(remplacé par la spec 002 — FR-011 et FR-019)*: À 15 000 points, puis 15 000 points après chaque boss battu, le boss MUST arriver :
  sol plat, plus d'obstacles normaux, 8 000 points de vie, barre de vie affichée.
- **FR-018**: Pendant le boss, le skateur MUST tirer automatiquement une dent toutes les 0,37 s
  (100 de dégâts) ; toutes les 10 s le boss s'envole, lâche 4 plots à sauter, puis redescend.
- **FR-019** *(remplacé par la spec 002 — FR-012 ; le plat de nose grind reste en infini)*: Le boss battu MUST donner +5 000 points, « BOOM », « TU ES UN VRAI PST ! », puis un
  plat de 2 500 avec une barre de nose grind 15 fois plus longue qu'un rail, avant la reprise.
- **FR-020** *(remplacé par la spec 002 — le raccourci lance le mode infini)*: Toucher le soleil sur l'écran d'accueil MUST lancer directement le boss avec
  le skateur du joueur (raccourci caché du jeu web).

**Son, sauvegarde, classement**

- **FR-021**: Le jeu MUST jouer une musique et des sons (saut, éclair, chute, grind, figure). Le son
  se coupe et se remet dans l'onglet RÉGLAGES du profil, et pendant une partie par le bouton RÉGLAGES en
  haut à droite, qui met le jeu en pause (son, vibrations, REPRENDRE, QUITTER LA PARTIE) ; le choix est
  gardé entre deux lancements.
- **FR-022**: Le meilleur score MUST être gardé sur l'appareil. Le classement en ligne MUST
  passer par Game Center sur iOS et Google Play Games sur Android (un classement « meilleur
  score » par plateforme) ; le bouton TOP 10 ouvre ce classement. Sans connexion au service, le
  jeu reste jouable et le bouton l'explique.
- **FR-023**: L'app MUST se mettre en pause quand elle passe en arrière-plan.
- **FR-024**: Le rendu MUST être refait, plus riche que le jeu web, en gardant son identité
  (skate, palmiers, coucher de soleil, couleurs de chaque décor) : décors en plusieurs plans de
  parallaxe, lumière et halo du soleil, skateurs animés (poussée, ollie, figures, grind, chute),
  particules (poussière, étincelles de grind, éclairs), écrans d'accueil et de fin refaits.
- **FR-025**: Le skateur MUST être dessiné et animé dans le nouveau style, avec sa tenue
  du profil dans toutes les poses.
- **FR-026** *(remplacé par la spec 002 — FR-023 et FR-026)*: L'écran PROFIL MUST avoir deux onglets, TENUE et RÉGLAGES. TENUE MUST permettre de changer la tenue du skateur, avec un aperçu animé
  qui suit chaque changement : coiffure (casquette, bonnet, boucles, bandeau) et sa couleur, couleur
  des cheveux, peau, couleur du sweat et de son détail, coupe (pantalon ou short) et couleur du bas,
  chaussures, couleur du plateau et des roues de la planche. La tenue est gardée sur l'appareil.
  L'écran de fin propose d'y aller (« CHANGER MA TENUE »).
- **FR-027** *(remplacé par la spec 002 — FR-025 et FR-027)*: Les vêtements MUST se débloquer avec les points cumulés (somme des scores de toutes
  les parties, gardée sur l'appareil) : une tenue de départ est libre, puis des couleurs (de 500 à
  20 000 points), le bonnet (3 000), le short (6 000) et le bandeau (10 000). Le profil affiche les
  points cumulés et le prix des éléments verrouillés ; toucher un élément verrouillé dit à combien
  il se débloque ; l'écran de fin annonce les nouveautés débloquées. Peau et cheveux restent libres.
- **FR-029** *(remplacé par la spec 002 — FR-002 : NUIT NÉON devient le niveau 12)*: Le premier boss battu MUST ouvrir le niveau 2 « NUIT NÉON » jusqu'à la fin de la partie :
  décor de nuit (lune, étoiles, grande roue et ville en néon, palmiers et enseignes lumineux, feux
  d'artifice), bandeau « NIVEAU 2 », vitesse maximale portée à 5,0. Les boss suivants continuent tous
  les 15 000 points.
- **FR-030**: Au niveau 2, la route MUST avoir des trous de 50 à 90 (40 % des fins de plat), jamais sous
  un obstacle ni juste avant ou après ; tomber dedans termine la partie, heurter le bord aussi. Des arcs
  d'éclairs passent au-dessus des trous.
- **FR-031**: Au niveau 2, des mouettes MUST voler vers le skateur à 46 au-dessus du sol : debout il les
  touche, accroupi il passe dessous, on peut aussi les sauter.
- **FR-032**: Au niveau 2, des tremplins néon MUST envoyer le skateur très haut (vitesse 13,5) avec
  « +50 × combo MÉGA AIR ! » et deux figures possibles dans ce saut ; ils se placent souvent juste
  avant un trou.
- **FR-028**: Le téléphone MUST vibrer brièvement aux moments forts (figure, début et fin de grind,
  chute, boss battu), avec un réglage VIBRATIONS dans PROFIL › RÉGLAGES et dans les réglages en partie, gardé entre deux lancements.

### Key Entities

- **Skateur** : tenue choisie dans le profil, position, vitesse verticale,
  au sol / en l'air / en grind / accroupi.
- **Obstacle** : plot (petit ou grand) ou rail (normal, grand arc-en-ciel, nose grind).
- **Éclair** : bonus à ramasser, hauteur au-dessus du sol.
- **Segment de terrain** : plat, montée ou descente, début, fin, hauteur.
- **Boss** : points de vie, phase (au sol, monte, en l'air, descend), plots lâchés.
- **Partie** : score, combo, vitesse, décor courant, meilleur score.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un joueur qui connaît le jeu web retrouve les mêmes règles : sur 10 parties de test,
  les points d'une figure, d'un grind, d'un éclair et du boss correspondent au barème à 100 %.
- **SC-002**: Le jeu tourne à 60 images/s sans saccade visible sur un iPhone 12 et un Android
  milieu de gamme de 2021, y compris pendant le boss.
- **SC-003**: Le jeu démarre (de l'icône à l'écran d'accueil) en moins de 3 secondes.
- **SC-004**: Un nouveau joueur lance sa première partie en moins de 10 secondes, sans aide.
- **SC-005**: L'app est acceptée sur l'App Store et sur Google Play.

## Clarifications

### Session 2026-10-30

- Q: Classement en ligne ? → A: Game Center (iOS) et Google Play Games (Android).
- Q: Style graphique ? → A: Refonte plus riche dès la première version (pas de copie à l'identique).

## Assumptions

- Les graphismes sont créés pour le projet (dessinés et animés dans le moteur, ou assets libres de
  droits notés dans le plan) ; aucun graphisme sous licence d'un tiers sans droit.
- Un seul skateur, que le joueur habille ; la tenue par défaut reprend celle de FREESTILL.
- La musique du jeu web (mélodie à 132 bpm, synthétisée) est reproduite à l'identique ou
  enregistrée depuis le jeu web.
- Le jeu est en français comme le jeu web ; pas d'autre langue dans la première version.
- Pas de pub, pas d'achat, pas de compte, aucune donnée collectée.
- Le champ pseudo du jeu web n'est pas repris : le nom vient de Game Center / Play Games.
- L'identifiant de l'app (com.charlit.palmstreet ou un nouveau) se décide au plan technique.
