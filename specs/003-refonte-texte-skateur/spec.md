# Feature Specification: Refonte graphique du texte de l'interface et du skateur

**Feature Branch**: `003-refonte-texte-skateur`

**Created**: 2026-11-02

**Status**: Draft

**Input**: User description: "Refonte graphique du texte de l'interface et du skateur. 1. Police : remplacer la police système (Avenir Next Condensed, Futura…) par Rubik Bold (Google Fonts, licence OFL), chargée depuis Assets/Resources/Fonts/Rubik-Bold.ttf, sur tout le texte de l'interface (Hud.cs). Supprimer l'ombre portée de tous les textes : elle rend le texte flou sur certains boutons. […] 2. Skateur redessiné (RiderView.cs), même squelette et mêmes poses […]. Référence visuelle : Art/apercus/skateur-avant-apres.png et skateur-genoux.png."

**Écart au jeu web (constitution, principe I)** : les graphismes sont refaits, plus riches que le jeu
web, avec les mêmes ingrédients (principe III) : même skateur, mêmes tenues, mêmes poses, même
univers. Seuls le dessin des pièces du skateur et la police de l'interface changent.

**Références visuelles** : `Art/apercus/skateur-avant-apres.png` (skateur actuel et redessiné),
`Art/apercus/skateur-genoux.png` (genou continu, debout et accroupi). Design system PalmStreet
(claude.ai) : police `display` = Rubik 700, aucune ombre de texte, couleurs `cream` et `night`.

## Clarifications

### Session 2026-11-02

- Q: Au milieu de l'écran, sur les ciels de jour, les bandeaux et les points sont-ils écrits en sombre ou posés sur un fond sombre ? → A: écrits en sombre (night) le jour ; le soir et la nuit, ils gardent leurs couleurs actuelles.
- Q: Le contour de 3 px garde-t-il la même épaisseur à l'écran ou suit-il l'échelle du skateur ? → A: il suit l'échelle du skateur (3 px à la taille de jeu, plus fin sur la carte, plus épais dans FIGURES).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Lire un texte net partout (Priority: P1)

Le joueur lit tous les textes du jeu (accueil, carte, profil, réglages, tutoriel, score en partie,
bandeaux de figures, écrans de réussite et de chute) dans une même police grasse et nette, sans
halo ni flou, sur iPhone comme sur Android.

**Why this priority**: le texte flou est un défaut visible sur chaque écran, et la police système
change d'un téléphone à l'autre (Avenir sur iPhone, Roboto sur Android) : c'est le changement le
plus visible pour le plus petit effort.

**Independent Test**: ouvrir chaque écran sur un iPhone et sur Android : tout le texte est en
Rubik Bold, sans ombre, identique sur les deux appareils.

**Acceptance Scenarios**:

1. **Given** l'accueil, **When** il s'affiche, **Then** le titre, les boutons et les libellés sont
   en Rubik Bold, sans ombre portée.
2. **Given** un iPhone et un téléphone Android, **When** on compare le même écran, **Then** la police
   est la même sur les deux (plus de police système différente).
3. **Given** le profil (onglet FIGURES) et les boutons de l'accueil, **When** on les regarde de près,
   **Then** aucun texte n'est flou ni doublé.
4. **Given** un texte avec des accents ou « œ » (« au cœur du Pays basque », GUÉTHARY, RÉGLAGES),
   **When** il s'affiche, **Then** tous les caractères sont dans la police, sans carré ni glyphe de
   remplacement.

---

### User Story 2 - Lire le texte sur tous les ciels (Priority: P1)

En partie, dans chacune des 12 villes, le joueur lit le score, les bandeaux (figures, combo,
records, défis) et les points gagnés, même sans ombre, y compris sur les ciels clairs de jour.

**Why this priority**: retirer l'ombre sans compenser rendrait certains textes illisibles sur les
ciels de jour ; les deux changements vont ensemble.

**Independent Test**: jouer (ou capturer) un instant de partie dans chaque ville avec un bandeau de
figure et des points affichés : chaque texte se lit sans effort.

**Acceptance Scenarios**:

1. **Given** n'importe laquelle des 12 villes, **When** le score et les boutons du haut de l'écran
   s'affichent, **Then** ils restent en crème et sont lisibles.
2. **Given** une ville de jour (Biarritz, Bidart, Guéthary, Bayonne, Espelette, Cambo), **When** un
   bandeau ou des points s'affichent au milieu de l'écran, **Then** ils sont écrits en sombre (night),
   sans fond ajouté.
3. **Given** une ville de soir ou de nuit, **When** un bandeau s'affiche au milieu de l'écran,
   **Then** il garde ses couleurs actuelles et reste lisible.

---

### User Story 3 - Un skateur plus détaillé (Priority: P2)

Le joueur voit un skateur redessiné : visage de profil expressif, cheveux qui suivent le crâne,
mains avec doigts, chaussures de skate détaillées, vêtements avec ombre, lumière et plis, et un
même contour sombre tout autour. Il bouge exactement comme avant.

**Why this priority**: c'est le personnage vu en permanence ; il donne la qualité du jeu, mais le
jeu reste jouable sans ce changement.

**Independent Test**: comparer une capture de la pose d'accueil avec
`Art/apercus/skateur-avant-apres.png` (côté APRÈS) : mêmes proportions et même position, dessin
détaillé.

**Acceptance Scenarios**:

1. **Given** l'accueil, **When** le skateur s'affiche, **Then** il montre de profil un front, un nez,
   des lèvres, un menton, une oreille, une joue, un sourcil et un œil (blanc, iris et reflet).
2. **Given** le skateur, **When** on regarde ses mains, **Then** chacune a un pouce et des doigts, et
   le coude est marqué.
3. **Given** le skateur, **When** on regarde ses chaussures, **Then** elles ont un embout, des lacets,
   une bande et une semelle.
4. **Given** chaque pièce (tête, buste, bras, jambes, chaussures), **When** elle s'affiche, **Then**
   elle a 2 à 3 tons et le même contour sombre de 3 px que les autres.
5. **Given** toutes les animations (poussée, ollie, figures, grinds, manual, nose manual, fakie,
   baissé, chute), **When** elles se jouent, **Then** les positions des articulations sont les mêmes
   qu'avant le redessin.

---

### User Story 4 - Un genou sans couture (Priority: P2)

Quand le skateur plie les jambes (accroupi, ollie, réception, baissé), le genou reste une forme
continue : aucun trait, aucune encoche, aucun trou entre la cuisse et le tibia, à tout angle.

**Why this priority**: le défaut se voit à chaque saut ; il fait partie du redessin du skateur.

**Independent Test**: capturer le skateur debout, accroupi et baissé, et comparer avec
`Art/apercus/skateur-genoux.png` : aucun trait au genou.

**Acceptance Scenarios**:

1. **Given** le skateur debout, **When** on regarde le genou, **Then** la jambe forme une seule
   silhouette continue.
2. **Given** le skateur accroupi ou baissé (genou très plié), **When** on regarde le genou, **Then**
   aucun contour ne passe à l'intérieur de la jambe et le genou est arrondi.

---

### User Story 5 - Toute la garde-robe sur le nouveau skateur (Priority: P2)

Le joueur équipe n'importe quel objet de la garde-robe (têtes, hauts, bas, chaussures, planches),
n'importe quelle peau, coupe et couleur de cheveux : le nouveau dessin les affiche tous, avec leurs
motifs actuels.

**Why this priority**: les objets sont la récompense des niveaux ; aucun ne doit disparaître ou
mal s'afficher.

**Independent Test**: dans le profil, faire défiler chaque objet de chaque catégorie, puis chaque
peau, coupe et couleur de cheveux : le skateur les montre tous correctement.

**Acceptance Scenarios**:

1. **Given** chaque objet de la garde-robe, **When** on l'équipe, **Then** le skateur le porte avec
   sa couleur et son motif (étoile, bandana, hibiscus, cargo, lettres de planche…).
2. **Given** les 5 peaux, les 2 coupes et les 5 couleurs de cheveux, **When** on les choisit,
   **Then** le visage et les cheveux s'affichent correctement.
3. **Given** la carte, l'onglet FIGURES du profil et les quatre skateurs de l'accueil, **When** ils
   montrent le skateur, **Then** c'est le nouveau dessin.

---

### Edge Cases

- Si la police Rubik ne peut pas être chargée, l'interface utilise la police système actuelle
  plutôt que de n'afficher aucun texte.
- Un texte long (nom de ville, nom de figure, défi) garde sa taille de boîte actuelle : Rubik est un
  peu plus large qu'Avenir Next Condensed, le texte peut donc réduire sa taille pour tenir, mais ne
  doit pas déborder ni être coupé.
- Le skateur mis à l'échelle (petit sur la carte, ×1,7 dans FIGURES) garde un contour proportionné :
  son épaisseur suit l'échelle du skateur.
- Le skateur en fakie (dessin retourné) garde son visage, ses mains et ses chaussures correctement
  orientés.
- Le redessin ne doit pas faire baisser la fluidité en partie (60 images/s, principe II).

## Requirements *(mandatory)*

### Functional Requirements

**Texte de l'interface**

- **FR-001**: Tout le texte de l'interface (accueil, carte, aperçu de ville, profil, réglages,
  tutoriel, partie, écrans de réussite et de chute, animation de lancement) MUST utiliser la police
  Rubik Bold fournie avec le jeu, au lieu de la police système.
- **FR-002**: Aucun texte de l'interface MUST NOT avoir d'ombre portée.
- **FR-003**: Les textes du haut de l'écran (score, record, boutons de partie) MUST rester en crème
  dans les 12 villes.
- **FR-004**: Sur les ciels de jour (Biarritz, Bidart, Guéthary, Bayonne, Espelette, Cambo), les
  bandeaux et les points affichés au milieu de l'écran MUST être écrits en sombre (night), sans fond
  ajouté ; le soir et la nuit, ils MUST garder leurs couleurs actuelles.
- **FR-005**: Les tailles de texte et les emplacements actuels MUST être conservés ; un texte qui ne
  tient plus dans sa boîte MUST réduire sa taille plutôt que déborder.
- **FR-006**: La licence de la police (OFL) MUST être fournie avec le projet.

**Skateur**

- **FR-007**: Le skateur MUST garder le même squelette : longueurs de la cuisse (14,5) et du tibia
  (14), mêmes points d'attache, même cinématique des jambes et mêmes poses pour toutes les
  animations.
- **FR-008**: La tête MUST être dessinée de profil avec front, nez, lèvres, menton, oreille, joue,
  sourcil et œil (blanc, iris, reflet) ; les cheveux MUST suivre la forme du crâne pour les deux
  coupes.
- **FR-009**: Les mains MUST avoir un pouce et des doigts, et le coude MUST être marqué.
- **FR-010**: Le buste MUST avoir des épaules arrondies, une ombre et une lumière, et les vêtements
  des plis.
- **FR-011**: Les chaussures MUST montrer un embout, des lacets, une bande et une semelle.
- **FR-012**: Chaque pièce MUST avoir 2 à 3 tons (base, ombre, lumière) et un même contour sombre
  de 3 px (à la taille de jeu) tout autour du skateur ; le contour suit l'échelle du skateur.
- **FR-013**: Au genou, la cuisse et le tibia MUST finir par un demi-disque de même rayon centré sur
  l'articulation, et le contour de la jambe entière MUST être posé avant ses couleurs : aucun trait
  visible au genou, quel que soit l'angle.
- **FR-014**: Tous les objets de la garde-robe, les 5 peaux, les 2 coupes et les 5 couleurs de
  cheveux MUST s'afficher sur le nouveau dessin avec leurs couleurs et motifs actuels.
- **FR-015**: Le nouveau dessin MUST être utilisé partout où le skateur apparaît : partie, accueil,
  carte, profil (garde-robe et démonstration des figures).

### Key Entities

- **Police de l'interface** : Rubik Bold, un seul fichier fourni avec le jeu, avec sa licence OFL.
- **Pièce du skateur** : tête, cheveux, buste, bras, main, cuisse, tibia, chaussure, planche ; chaque
  pièce a une articulation (inchangée), 2 à 3 tons et le contour commun.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100 % des écrans du jeu affichent leur texte en Rubik Bold, sans ombre, sur iPhone et
  sur Android.
- **SC-002**: Dans chacune des 12 villes, le score, un bandeau de figure et les points sont lisibles
  sur une capture de partie (contraste d'au moins 3:1 pour les textes de 16 px et plus).
- **SC-003**: Pour chaque animation du skateur, les articulations sont aux mêmes positions qu'avant le
  redessin (écart nul à l'image près).
- **SC-004**: Aucun trait n'est visible au genou sur des captures debout, accroupi et baissé.
- **SC-005**: Chaque objet de la garde-robe, chaque peau, coupe et couleur de cheveux s'affiche
  correctement sur le nouveau skateur (vérifié par une capture de chaque catégorie).
- **SC-006**: La partie reste à 60 images/s sur iPhone et sur Android avec le nouveau skateur.

## Assumptions

- La police est le fichier Rubik Bold statique (graisse 700) de Google Fonts, déjà copié dans
  `Assets/Resources/Fonts/Rubik-Bold.ttf` ; elle contient les accents français et « œ ».
- « Tout le texte de l'interface » couvre tous les textes dessinés par l'interface du jeu (accueil,
  carte, profil, tutoriel, partie, animation de lancement), pas seulement l'écran principal.
- Le libellé de JOUER, qui n'avait déjà pas d'ombre, garde son style (texte night sur orange).
- Le soir et la nuit, les couleurs actuelles des bandeaux restent lisibles sans ombre ; seuls les
  ciels de jour imposent le texte sombre ou le fond sombre au milieu de l'écran.
- Le redessin ne change ni la boîte de collision, ni les règles de jeu, ni les tests de logique
  (principe IV) ; les captures automatiques servent à la vérification visuelle.
- Les quatre skateurs de l'accueil (FREESTILL, KEKETSK8, GUIGUI, MAXWELL) utilisent le même dessin
  avec leurs tenues.
