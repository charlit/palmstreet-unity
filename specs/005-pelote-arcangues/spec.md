# Feature Specification: Pelote basque à Arcangues

**Feature Branch**: `005-pelote-arcangues`

**Created**: 2026-11-02

**Status**: Draft

**Input**: User description: "Pelote basque à Arcangues : un boss, un obstacle et le mode « planche en main ». […] Boss LE PELOTARI (niveau 8 ARCANGUES, devant le fronton) ; obstacle volant : la pelote basque (12 × 12, plus rapide que la mouette) ; mode spécial « planche en main » pendant le combat contre le Pelotari : bouclier, parade, boomerang, saut."

**Écart au jeu web (constitution, principe I)** : nouveau boss, nouvel obstacle et nouveau mode de
combat, à la demande du propriétaire. Dans les autres combats, rien ne change : le skateur reste sur
sa planche et se bat avec les figures et les sorts.

**Références visuelles** :
- `Art/apercus/apercu-idees-basques.png` : LE PELOTARI, le fronton d'Arcangues, la pelote ;
- `Art/apercus/apercu-planche-en-main.png` : le bouclier et la parade, le boomerang.

## Clarifications

### Session 2026-11-02

- Q: Garde-t-on trois boss d'affilée (niveaux 7, 8, 9) ? → A: oui, Talonneur, Pelotari et Sanglier se suivent ; les combats sont assez différents.
- Q: Les sorts marchent-ils pendant le combat planche en main ? → A: oui, comme contre les autres boss.
- Q: Combien de temps avant l'impact peut-on lever le bouclier pour réussir une parade ? → A: 0,15 s (parade difficile, pour joueurs précis).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Affronter LE PELOTARI devant le fronton (Priority: P1)

Au bout du niveau 8 (Arcangues), le joueur affronte un joueur de cesta punta. Le Pelotari porte un
maillot rouge numéroté, un pantalon blanc et un casque blanc à bande rouge, avec son chistera. Il
prend son élan, puis enchaîne deux attaques :
- **le lancer** : une pelote à hauteur de tête ;
- **le rebond** : une pelote qui rebondit au sol vers le skateur.

**Why this priority**: c'est le cœur de la fonctionnalité : un nouveau boss au niveau 8, qui n'en
avait pas.

**Independent Test**: jouer le niveau 8 jusqu'à la fin : LE PELOTARI arrive. Survivre à ses deux
attaques et le battre : le niveau est réussi.

**Acceptance Scenarios**:

1. **Given** le niveau 8, **When** le skateur arrive au bout du parcours, **Then** LE PELOTARI apparaît devant le fronton, avec sa barre de vie et son nom.
2. **Given** un lancer, **When** la pelote arrive à hauteur de tête, **Then** elle fait chuter le skateur, sauf s'il est protégé (US2).
3. **Given** un rebond, **When** la pelote arrive au sol, **Then** elle fait chuter le skateur, sauf s'il saute.
4. **Given** les attaques, **When** elles arrivent, **Then** chacune est annoncée par une pose reconnaissable assez tôt pour réagir.
5. **Given** la vie du Pelotari à zéro, **When** il est battu, **Then** le niveau 8 est réussi.

---

### User Story 2 - Combattre « planche en main » (Priority: P1)

Pendant le combat contre le Pelotari, le skateur descend de sa planche et la tient à la main. Le
combat se joue alors avec des gestes nouveaux :
- **maintenir le doigt** : la planche devient un **bouclier** et bloque les pelotes à hauteur de tête.
  On ne peut pas sauter pendant ce temps.
- **parer** : maintenir le bouclier juste au moment de l'impact renvoie la pelote au boss, ce qui le
  blesse.
- **glisser vers le boss** : le skateur lance sa planche en **boomerang**. Elle blesse le boss, puis
  revient dans sa main. Il n'a pas de bouclier pendant qu'elle vole.
- **toucher** : il saute, pour éviter les rebonds.

**Why this priority**: c'est ce qui rend ce combat unique, et c'est le seul moyen de blesser le
Pelotari.

**Independent Test**: au début du combat, le skateur tient sa planche. Bloquer un lancer avec le
bouclier, réussir une parade qui blesse le boss, lancer le boomerang qui le touche et revient, et
sauter un rebond.

**Acceptance Scenarios**:

1. **Given** le début du combat contre le Pelotari, **When** il commence, **Then** le skateur descend de sa planche, la tient à la main et le conseil « MAINTIENS LE DOIGT : BOUCLIER » s'affiche.
2. **Given** le doigt maintenu, **When** un lancer arrive à hauteur de tête, **Then** la pelote est bloquée et le skateur ne chute pas.
3. **Given** le bouclier levé au tout dernier moment avant l'impact, **When** la pelote arrive, **Then** « PARADE ! » s'affiche, la pelote repart vers le boss et le blesse.
4. **Given** le skateur planche en main, **When** il glisse le doigt vers le boss, **Then** la planche part en boomerang, blesse le boss si elle le touche, puis revient dans sa main.
5. **Given** la planche en vol, **When** un lancer arrive, **Then** le skateur ne peut pas lever le bouclier : il doit se baisser ou être touché.
6. **Given** le bouclier levé, **When** le joueur touche l'écran pour sauter, **Then** le saut ne se fait pas tant que le bouclier est levé.
7. **Given** un rebond qui arrive, **When** le skateur saute, **Then** la pelote passe sous lui.
8. **Given** le Pelotari battu, **When** le combat se termine, **Then** le skateur remonte sur sa planche.

---

### User Story 3 - Arcangues reconnaissable : fronton, clocher, platanes (Priority: P2)

Le niveau 8 montre le décor de la pelote basque :
- un **fronton ocre** au sommet arrondi, avec des filets rouges et une bande de métal en bas ;
- le **clocher de l'église d'Arcangues** ;
- des **platanes taillés**.

Le combat contre le Pelotari se déroule devant le fronton.

**Why this priority**: donne au boss sa scène et au niveau son identité ; le combat reste jouable sans.

**Independent Test**: capture du niveau 8 pendant le parcours et pendant le combat : fronton, clocher et platanes visibles.

**Acceptance Scenarios**:

1. **Given** le niveau 8, **When** il défile, **Then** le fronton, le clocher et les platanes sont visibles et reconnaissables.
2. **Given** le combat, **When** le Pelotari attaque, **Then** le fronton est en arrière-plan.

---

### User Story 4 - La pelote, obstacle volant de l'intérieur (Priority: P2)

À Arcangues et dans les niveaux de l'intérieur, une petite pelote blanche cousue traverse l'écran,
avec des traits de vitesse. Elle va plus vite que la mouette. On l'évite en se baissant.

**Why this priority**: annonce le boss pendant le parcours et apprend le geste ; le jeu fonctionne sans.

**Independent Test**: jouer le niveau 8 : des pelotes passent, plus vite que les mouettes ; se baisser les évite.

**Acceptance Scenarios**:

1. **Given** le niveau 8 ou un autre niveau de l'intérieur, **When** des obstacles volants apparaissent, **Then** certains sont des pelotes.
2. **Given** une pelote volante, **When** le skateur est baissé, **Then** elle passe au-dessus ; **When** il est debout, **Then** elle le touche et il chute.
3. **Given** une pelote et une mouette lancées au même moment, **When** elles volent, **Then** la pelote arrive sur le skateur avant la mouette.

---

### Edge Cases

- Le skateur garde le doigt posé quand le combat commence : il lève le bouclier tout de suite, sans faire d'ollie.
- Le joueur glisse vers le boss alors que la planche vole déjà : rien ne se passe ; un seul boomerang à la fois.
- La planche revient dans la main au moment où arrive un lancer : il peut lever le bouclier dès qu'elle est revenue.
- Le skateur saute pendant que la planche vole : le saut se fait à pied.
- Une pelote parée qui rate le boss (boss en l'air ou déplacé) disparaît sans effet.
- Le skateur chute pendant le combat : le combat s'arrête comme pour les autres boss (WIPEOUT, RECOMMENCER). Au niveau suivant, il est de nouveau sur sa planche.
- Mode infini : si le Pelotari est tiré, le combat se joue aussi planche en main.
- À pied, le skateur ne peut faire ni figure ni grind. La planche est dans sa main.
- Le glissé vers le bas (se baisser) reste possible à pied.

## Requirements *(mandatory)*

### Functional Requirements

**Boss LE PELOTARI**

- **FR-001**: Le niveau 8 (ARCANGUES) MUST finir par le boss LE PELOTARI ; il est réussi seulement quand le boss est battu.
- **FR-002**: LE PELOTARI MUST porter :
  - un maillot rouge numéroté ;
  - un pantalon blanc ;
  - un casque blanc à bande rouge ;
  - un chistera (grand panier d'osier recourbé lacé au gant).
- **FR-003**: Entre deux attaques, il MUST prendre son élan. Ses attaques MUST alterner :
  - **le lancer** : une pelote à hauteur de tête ;
  - **le rebond** : une pelote qui rebondit au sol vers le skateur.
- **FR-004**: Chaque attaque MUST être précédée d'une pose reconnaissable, assez tôt pour réagir (au moins 0,5 s).
- **FR-005**: Être touché par une pelote du boss MUST faire chuter le skateur, sauf si elle est bloquée par le bouclier ou évitée.
- **FR-006**: La résistance du Pelotari MUST le placer entre LE TALONNEUR (niveau 7) et le Sanglier (niveau 9).
- **FR-007**: La carte, l'aperçu et l'annonce de début de combat MUST traiter le niveau 8 comme un niveau à boss : marque BOSS, nom, conseil.

**Mode « planche en main »**

- **FR-008**: Pendant tout le combat contre le Pelotari, le skateur MUST être à pied, la planche à la main. Il remonte sur sa planche à la fin du combat.
- **FR-009**: Maintenir le doigt MUST lever le bouclier. Le bouclier MUST bloquer les pelotes à hauteur de tête qui arrivent de face. Pendant qu'il est levé, toucher MUST NOT faire sauter.
- **FR-010**: Une pelote qui touche le bouclier moins de 0,15 s après sa levée MUST être parée : elle repart vers le boss et le blesse, et « PARADE ! » s'affiche. Au-delà de 0,15 s, elle est seulement bloquée.
- **FR-011**: Glisser le doigt vers le boss MUST lancer la planche en boomerang. Elle avance vers le boss, le blesse si elle le touche, puis revient dans la main.
- **FR-012**: Pendant le vol de la planche, le bouclier MUST être impossible, et un seul boomerang MUST être en vol à la fois.
- **FR-013**: Toucher MUST faire sauter le skateur à pied, pour éviter les rebonds.
- **FR-014**: Pendant ce combat, les figures, les grinds et les dents de figure MUST être désactivés. Le Pelotari se blesse par la parade, le boomerang et les sorts, qui restent utilisables comme contre les autres boss.
- **FR-015**: Au début du combat, un conseil MUST expliquer le bouclier ; au premier boomerang possible, un conseil MUST expliquer le glissé vers le boss.

**Décor**

- **FR-016**: Le décor du niveau 8 MUST montrer :
  - un fronton ocre au sommet arrondi, avec des filets rouges et une bande de métal en bas ;
  - le clocher de l'église d'Arcangues ;
  - des platanes taillés.
  Le combat se déroule devant le fronton.

**Obstacle volant**

- **FR-017**: La pelote MUST être une petite balle blanche cousue, avec des traits de vitesse. Sa boîte de collision MUST mesurer 12 × 12, et elle MUST voler plus vite que la mouette vers le skateur.
- **FR-018**: Se baisser MUST éviter la pelote volante ; debout, elle fait chuter.
- **FR-019**: Les pelotes volantes MUST apparaître à Arcangues (niveau 8) et dans les niveaux de l'intérieur (9 Espelette, 10 Ascain, 11 Cambo-les-Bains), mêlées aux autres obstacles volants.

### Key Entities

- **Boss LE PELOTARI** : un nouveau type de boss. Il a une vie, deux attaques (lancer, rebond), une pose d'annonce, et il impose le mode planche en main.
- **Pelote** : une balle de 12 × 12. C'est soit un projectile du boss (lancer droit, ou rebond au sol), soit un obstacle volant de l'intérieur. Une pelote parée repart vers le boss.
- **Mode planche en main** : l'état du skateur pendant ce combat. La planche peut être dans la main, levée en bouclier (et alors depuis combien de temps, pour la parade) ou en vol en boomerang.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Dans 100 % des cas testés :
  - un lancer est bloqué quand le bouclier est levé ;
  - un rebond est évité quand le skateur saute ;
  - sinon, le skateur chute.
- **SC-002**: Une parade au bon moment renvoie la pelote et blesse le boss dans 100 % des cas testés. Un bouclier levé trop tôt bloque sans renvoyer.
- **SC-003**: Le boomerang revient dans la main en moins de 1,5 s, et le touche dans 100 % des cas où le boss est au sol devant le skateur.
- **SC-004**: Un joueur qui pare et lance bien bat LE PELOTARI en moins de 90 secondes.
- **SC-005**: Le joueur a au moins 0,5 s entre la pose d'annonce et le moment où il doit réagir.
- **SC-006**: Sur des captures du niveau 8, le fronton, le clocher et les platanes sont visibles.
- **SC-007**: La partie reste à 60 images/s sur iPhone et Android pendant le combat.

## Assumptions

- Le mode « planche en main » ne sert que contre LE PELOTARI ; les autres combats ne changent pas.
- Le Pelotari ne donne ni sort ni tenue. Les sorts restent aux boss des niveaux 3, 6, 9 et 12, les tenues aux boss de rugby.
- La planche ne s'use pas : le boomerang peut être relancé sans limite, dès qu'elle est revenue.
- Pendant le combat, le décor continue de défiler et le skateur court à pied à la même vitesse que sur sa planche.
- Le niveau 8 garde ses tremplins et ses autres obstacles avant le boss. Son conseil sur les tremplins reste.
- Le niveau 8 a désormais un boss, comme les niveaux 7 et 9 : trois combats de suite (Talonneur, Pelotari, Sanglier), confirmé par le propriétaire.
- « Les niveaux de l'intérieur » sont Arcangues, Espelette, Ascain et Cambo-les-Bains (8 à 11).
