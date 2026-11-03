# Feature Specification: Le skatepark de la Barre à Anglet

**Feature Branch**: `006-skatepark-la-barre`

**Created**: 2026-11-02

**Status**: Draft

**Input**: User description: "Nouveau niveau : le skatepark de la Barre à Anglet. Un niveau libre pour s'amuser et faire le maximum de points : aucun obstacle volant ni personnage (ni mouettes, ni ballons, ni enfants). Le décor est un skatepark avec ses modules : rails et curbs pour grinder, bowl, quarters, funbox. Au départ du niveau, le skateur descend une immense rampe. Le niveau récompense les enchaînements de figures et les combos."

**Écart au jeu web (constitution, principe I)** : nouveau niveau et nouveau décor, à la demande du
propriétaire. Le jeu web n'a pas de skatepark. Les gestes, les figures, les grinds et les combos sont
ceux du jeu.

## Clarifications

### Session 2026-11-02

- Q: Le skatepark est-il un niveau bonus ou le niveau 13 ? → A: niveau bonus, ouvert dès qu'Anglet (niveau 1) est réussi, placé à côté d'Anglet sur la carte.
- Q: Comment se termine une session ? → A: session chronométrée de 90 s, puis « SESSION TERMINÉE ».
- Q: Que se passe-t-il pour une figure pas finie à la retombée ? → A: pas de chute ; BAIL, le combo est perdu et la session continue.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Rider librement au skatepark (Priority: P1)

Depuis la carte, le joueur ouvre le skatepark de la Barre, à Anglet. Le skateur descend une immense
rampe, prend beaucoup de vitesse, puis enchaîne les modules du skatepark :
- **rails et curbs** pour grinder ;
- **bowl** à traverser ;
- **quarters** pour décoller ;
- **funbox** à sauter.

Il n'y a ni obstacle volant, ni personnage, ni plot qui fasse tomber : le but est de faire le maximum
de points.

**Why this priority**: c'est le niveau lui-même ; sans lui, rien d'autre n'existe.

**Independent Test**: lancer le skatepark : la grande rampe de départ, puis une suite de modules se
déroulent ; on peut grinder, sauter et enchaîner sans jamais croiser de mouette, de ballon ou de pelote.

**Acceptance Scenarios**:

1. **Given** la carte, **When** le joueur touche le skatepark, **Then** l'aperçu présente le niveau libre (« SKATEPARK DE LA BARRE ») et le bouton JOUER le lance.
2. **Given** le départ, **When** le niveau commence, **Then** le skateur descend une rampe bien plus haute que la rampe de départ des autres niveaux, et en sort bien plus vite.
3. **Given** le parcours, **When** il défile, **Then** se succèdent des rails et des curbs à grinder, un bowl, des quarters et des funbox.
4. **Given** tout le parcours, **When** il défile, **Then** aucun obstacle volant, personnage ou plot n'apparaît.
5. **Given** un rail ou un curb, **When** le skateur retombe dessus, **Then** il grinde, avec les 8 variantes de grind par glissé, comme ailleurs.
6. **Given** un quarter, **When** le skateur le monte, **Then** il décolle plus haut qu'avec un ollie et peut enchaîner deux figures.
7. **Given** le bowl, **When** il le traverse, **Then** il descend puis remonte, et la remontée le projette en l'air.

---

### User Story 2 - Des points pour les enchaînements (Priority: P1)

Au skatepark, le score récompense surtout les enchaînements : plus le combo est long, plus chaque
figure rapporte. Une session réussie se juge à son score.

**Why this priority**: c'est la raison d'être du niveau (« faire le maximum de points »).

**Independent Test**: enchaîner 6 figures et grinds sans retomber à plat entre les modules : le
multiplicateur de combo monte et le score dépasse nettement celui d'un passage figure par figure.

**Acceptance Scenarios**:

1. **Given** un combo en cours, **When** le skateur passe d'un module à l'autre sans rouler longtemps entre les deux, **Then** le combo continue.
2. **Given** le skatepark, **When** le combo dépasse x5, **Then** chaque figure rapporte un bonus de skatepark en plus du score habituel.
3. **Given** la fin de la session, **When** l'écran de fin s'affiche, **Then** il montre le score, le meilleur combo de la session et le meilleur score du skatepark.

---

### User Story 3 - Une session qui se termine bien (Priority: P2)

La session au skatepark ne s'arrête pas sur une chute bête : c'est un niveau pour s'amuser. Elle se
termine à la fin du temps de session, et le joueur peut rejouer tout de suite.

**Why this priority**: sans fin claire, le niveau n'a ni score à battre ni étoile ; mais il est jouable sans.

**Independent Test**: rider jusqu'à la fin du temps : « SESSION TERMINÉE » s'affiche avec le score ; REJOUER relance.

**Acceptance Scenarios**:

1. **Given** le skatepark, **When** le temps de session est écoulé, **Then** la session se termine avec « SESSION TERMINÉE », le score et le meilleur combo.
2. **Given** une figure pas finie à la retombée, **When** le skateur retombe, **Then** c'est un BAIL : il vacille et perd son combo, mais la session continue.
3. **Given** la fin de session, **When** le joueur touche REJOUER, **Then** une nouvelle session commence en haut de la grande rampe.
4. **Given** un temps de session, **When** il s'écoule, **Then** le temps restant est visible en haut de l'écran.

---

### User Story 4 - Le décor du skatepark de la Barre (Priority: P2)

Le skatepark est reconnaissable : modules en béton gris, coping métallique, graffitis colorés sans
marque, l'océan et la plage de la Barre à Anglet derrière.

**Why this priority**: donne au niveau son identité ; il se joue sans.

**Independent Test**: capture du skatepark : modules en béton, graffitis, océan derrière.

**Acceptance Scenarios**:

1. **Given** le skatepark, **When** il défile, **Then** on voit des modules en béton, des graffitis colorés et l'océan derrière.
2. **Given** les graffitis, **When** on les regarde, **Then** aucun ne montre de marque ou de logo réel (principe V).

---

### Edge Cases

- Le skateur tombe dans un trou : le skatepark n'en a aucun.
- Le skateur arrive mal sur un module (de face, trop bas) : il le monte ou le grinde, sans chuter. Seule une figure inachevée à la retombée fait un BAIL.
- Le joueur quitte la session avec la croix : le score n'est pas compté, comme dans les autres niveaux.
- Mode infini : le décor du skatepark n'y entre pas. C'est un niveau à part.
- Avec le mode « tout débloqué » de test, le skatepark est ouvert dès le départ.

## Requirements *(mandatory)*

### Functional Requirements

**Accès**

- **FR-001**: Le skatepark MUST être un niveau bonus, hors de la suite des 12 niveaux. Il est placé sur la carte à côté d'Anglet et ouvert dès que le niveau 1 (Anglet) est réussi.
- **FR-002**: L'aperçu du skatepark MUST l'annoncer comme niveau libre et montrer son meilleur score et son meilleur combo.

**Parcours**

- **FR-003**: Le niveau MUST commencer par une rampe de départ au moins deux fois plus haute que celle des autres niveaux. Le skateur en sort bien plus vite qu'ailleurs.
- **FR-004**: Le parcours MUST enchaîner des modules de skatepark, en boucle :
  - rails et curbs (bas, à grinder) ;
  - bowl (creux qui renvoie en l'air) ;
  - quarters (rampes qui font décoller plus haut qu'un ollie, avec deux figures possibles) ;
  - funbox (bloc à sauter ou à grinder sur son bord).
- **FR-005**: Le parcours MUST NOT contenir d'obstacle volant, de personnage, de plot ni de trou.
- **FR-006**: Aucun module MUST NOT faire chuter le skateur : les arrivées ratées sont des BAIL (combo perdu, la session continue).

**Score**

- **FR-007**: Au skatepark, au-delà d'un combo x5, chaque figure et chaque variante de grind MUST rapporter un bonus en plus du score habituel. Le bonus grandit avec le combo.
- **FR-008**: Le skatepark MUST garder son propre meilleur score et son meilleur combo, sauvegardés avec la progression.

**Session**

- **FR-009**: La session MUST durer 90 secondes, avec le temps restant affiché en haut de l'écran. À la fin, elle MUST se terminer par « SESSION TERMINÉE » avec le score, le meilleur combo et le meilleur score du skatepark. Le joueur peut alors toucher REJOUER ou CARTE.
- **FR-010**: Le skatepark MUST donner jusqu'à 3 étoiles, comptées dans le total des étoiles :
  - finir une session ;
  - atteindre un combo x8 ;
  - atteindre un score cible.

**Décor**

- **FR-011**: Le décor MUST montrer :
  - des modules en béton gris avec coping métallique ;
  - des graffitis colorés sans marque ;
  - l'océan et la plage de la Barre derrière.

### Key Entities

- **Niveau bonus « Skatepark de la Barre »** : nom, ville (ANGLET), règles propres (session à durée fixe, pas de chute, bonus de combo), meilleur score, meilleur combo, 3 étoiles.
- **Module** : rail, curb, bowl, quarter ou funbox. Chacun a une forme, des endroits où grinder et un effet (décoller, renvoyer en l'air).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Sur une session complète, aucun obstacle volant, personnage, plot ni trou n'apparaît (0 dans 100 % des sessions testées).
- **SC-002**: Le skateur sort de la grande rampe de départ au moins 1,5 fois plus vite que d'une rampe de départ normale.
- **SC-003**: Une session dure le temps fixé (90 s), à 1 s près, puis se termine par l'écran de fin.
- **SC-004**: Un enchaînement de 8 figures en combo rapporte au moins 2 fois plus de points que les mêmes 8 figures faites séparément.
- **SC-005**: Chaque type de module (rail, curb, bowl, quarter, funbox) apparaît au moins une fois toutes les 30 secondes de session.
- **SC-006**: La partie reste à 60 images/s sur iPhone et Android.

## Assumptions

- Le skatepark est un **niveau bonus** : il ne fait pas partie de la chaîne des 12 niveaux et n'est pas un niveau 13. Il est débloqué par Anglet, ville où se trouve la Barre.
- La session dure **90 secondes**, comme un niveau normal (60 à 90 s).
- Le skatepark ne débloque ni figure, ni objet, ni sort : ses récompenses sont ses étoiles et son meilleur score. Les étoiles comptent pour les objets à étoiles.
- Les étoiles : 1 pour une session finie, 1 pour un combo x8, 1 pour un score cible fixé au plan.
- Le classement Game Center / Google Play Games du skatepark est hors de cette spec.
- Le spot nommé « LA BARRE » du niveau 1 reste à Anglet ; le skatepark est un lieu à part sur la carte.
