# Research: Refonte graphique du texte de l'interface et du skateur

## R1. Chargement de la police

- **Decision**: `Resources.Load<Font>("Fonts/Rubik-Bold")` dans `Hud.Init`, à la place de
  `Font.CreateDynamicFontFromOSFont(...)`. Si le chargement renvoie `null`, on garde l'appel
  actuel à la police système (cas limite de la spec).
- **Rationale**: tout le texte de l'interface passe par une seule méthode, `Hud.Label`
  (`Hud`, `HudExtras` et `HudLevels` sont la même classe partielle ; `Splash` reçoit un `Text` créé
  par `Hud`). Changer la police à un seul endroit suffit à couvrir FR-001.
- **Fichier** : Rubik Bold statique (graisse 700) de Google Fonts, déjà dans
  `Assets/Resources/Fonts/Rubik-Bold.ttf`. Le fichier variable `Rubik[wght].ttf` a été écarté : le
  `Text` d'Unity ne choisit pas la graisse d'une police variable.
- **Licence** : OFL 1.1, copiée dans `Assets/Resources/Fonts/OFL.txt` (FR-006). L'OFL permet
  d'embarquer la police dans une application vendue (principe V).
- **Alternatives considered**: TextMeshPro (rendu SDF plus net) — rejeté : il faudrait générer un
  atlas, remplacer tous les `Text` et ajouter un paquet ; le flou venait de l'ombre, pas du rendu.

## R2. Suppression de l'ombre

- **Decision**: retirer l'ajout du composant `Shadow` dans `Hud.Label`, puis supprimer le code
  devenu inutile : le `Destroy(...GetComponent<Shadow>())` de JOUER (`Hud.cs`) et la méthode
  `Sharp()` de `HudLevels.cs` avec ses appels.
- **Rationale**: une seule source d'ombre ; la retirer à la source supprime aussi les rustines.

## R3. Texte trop long (FR-005)

- **Decision**: garder `horizontalOverflow = Overflow` (les textes multilignes et les textes centrés
  en dépendent) et vérifier chaque écran par les captures. Seuls les libellés qui débordent de leur
  bouton ou de leur cadre reçoivent `resizeTextForBestFit` (taille min. = 70 % de la taille d'origine).
- **Rationale**: Rubik est plus large qu'Avenir Next Condensed, mais la plupart des textes ont de la
  marge. Un ajustement global changerait la mise en page de textes qui tiennent déjà.
- **Alternatives considered**: `resizeTextForBestFit` sur tous les `Text` — rejeté : il réduirait
  aussi les textes multilignes et le titre.

## R4. Texte sombre sur les ciels de jour (FR-004, clarification 1)

- **Decision**: la liste des villes de jour vit dans le Core : `Levels.IsDay(Theme)` (Biarritz,
  Bidart, Guéthary, Bayonne, Espelette, Cambo), couverte par un test en mode Edit (principe IV).
  Le `Hud` l'utilise pour les textes du milieu de l'écran :
  - bandeau et sous-bandeau (`Banner`) ;
  - figure annoncée (`Shout`) ;
  - points flottants (`Float`).
  Ville de jour : couleur `Night`. Sinon : couleur actuelle.
- **Pas concernés** :
  - le haut de l'écran (score, BEST, COMBO, nom de la ville, boutons) reste crème (FR-003) ;
  - le message du bas (`toast`) est déjà sur fond sombre ;
  - les écrans de fin et les fenêtres sont sur voile sombre.
- **Mode infini** : les villes défilent ; la couleur est choisie au moment où le texte apparaît,
  selon la ville en cours (`run.Theme`).

## R5. Contour du skateur et genou continu (FR-012, FR-013)

- **Decision**: chaque pièce est dessinée deux fois à partir d'une même fonction de silhouette :
  1. une fois gonflée de l'épaisseur du contour, en couleur d'encre (`#1B1028`) ;
  2. une fois à taille normale, en couleurs (2 à 3 tons et détails).
  Les maillages de contour de toutes les pièces d'un même membre ont un ordre de tri plus bas que
  toutes ses couleurs. Le contour de la jambe entière est donc posé avant les couleurs, et aucun
  trait n'apparaît entre la cuisse et le tibia.
- **Genou** : la cuisse finit par un demi-disque de même rayon que le haut du tibia, centré sur
  l'articulation (le tibia commence déjà par un demi-disque). À tout angle, les deux disques se
  recouvrent exactement, et la couleur du tibia couvre le contour de la cuisse.
  La capsule de la cuisse devient effilée (largeur à la hanche, largeur du tibia au genou) : on
  ajoute `Paint.Taper(x0, y0, x1, y1, w0, w1, col)`, une capsule à bouts ronds de largeurs
  différentes.
- **Épaisseur** : une constante `Outline` en unités du skateur, réglée sur capture pour donner 3 px
  à la taille de jeu sur l'écran de référence. Comme le contour fait partie du maillage, il suit
  l'échelle du skateur (clarification 2) : plus fin sur la carte, plus épais dans FIGURES.
- **Rationale**: aucun shader ni post-traitement, rien de nouveau dans le pipeline. Les maillages
  restent construits une seule fois par tenue, comme aujourd'hui (60 images/s, principe II).
- **Alternatives considered**:
  - shader de contour (Sprite Outline) : rejeté, il trace un trait autour de chaque pièce, donc
    aussi au genou ;
  - rendu dans une texture puis contour : rejeté, plus lourd et plus flou.

## R6. Squelette inchangé (FR-007, SC-003)

- **Decision**: ne toucher ni aux méthodes de pose (`Pose`, `Solve`, `Board`, `Ride`, `Demo`,
  `Idle`, `Crash`, `Fall`), ni aux constantes `Thigh` / `Shin`, ni aux positions d'attache
  (`head.localPosition`, `armB/armF.localPosition`). Seules changent les méthodes qui dessinent les
  pièces : `Build` (buste), `MakeLeg`, `Arm`, `Head`.
- **Vérification** : le diff ne touche aucune de ces méthodes, et les captures avant/après ont les
  articulations au même endroit. La planche (`BuildBoard`) garde son dessin.

## R7. Détails du dessin (FR-008 à FR-011)

Référence : `Art/apercus/skateur-avant-apres.png`, côté APRÈS.

- **Tête** :
  - profil avec front, nez, lèvres, menton (un polygone remplace le cercle de profil) ;
  - oreille avec un creux ;
  - joue rose (alpha) ;
  - œil : blanc, iris et reflet ;
  - sourcil ;
  - cheveux en dôme qui suit le crâne, avec mèches pour COURTS et boucles pour BOUCLES ;
  - les couvre-chefs gardent leur dessin, repris sur le nouveau crâne.
- **Bras** :
  - manche puis avant-bras ;
  - coude marqué par un disque ;
  - main avec paume, pouce et doigts repliés (3 petites capsules).
- **Buste** :
  - épaules arrondies ;
  - ombre côté dos et lumière côté poitrine (Lerp −25 % / +20 %) ;
  - 2 plis.
  - Les motifs de `TopDesign` et le logo restent posés par-dessus.
- **Jambes** :
  - pli à l'aine et au genou ;
  - motifs actuels du bas : jean usé, hibiscus, cargo, chaussettes du short.
- **Chaussures** : embout, lacets, bande, semelle, plus le motif propre à chaque paire.
- **Tons** : base, ombre (base −25 % vers le noir) et lumière (base +20 % vers le blanc), comme le
  dit le design system.
