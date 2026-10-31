# Research: Mode histoire, mode infini et figures à débloquer

Pas de point « NEEDS CLARIFICATION » dans le contexte technique : on reste sur la pile de la spec 001.
Les décisions ci-dessous fixent ce que la spec laisse ouvert.

## 1. Où décrire les niveaux

- **Decision**: une table C# dans PalmStreet.Core (`Levels.All`, 12 entrées en dur).
- **Rationale**: les tests EditMode la lisent sans Unity (principe IV) ; 12 niveaux tiennent dans un écran
  de code ; rien n'a besoin d'être réglé dans l'éditeur.
- **Alternatives considered**: ScriptableObject par niveau (dépend d'UnityEngine, inutilisable dans Core,
  12 fichiers d'assets) ; JSON dans Resources (lecture à l'exécution, erreurs découvertes trop tard).

## 2. Fin d'un niveau

- **Decision**: une distance d'arrivée par niveau, calculée pour ~75 s à la vitesse moyenne du niveau ;
  ligne d'arrivée dessinée sur la route ; dans un niveau à boss, le boss arrive à cette distance.
- **Rationale**: un niveau a toujours la même longueur, quelle que soit la façon de jouer (SC-002) ; les
  points ne servent pas à finir, seulement au meilleur score.
- **Alternatives considered**: objectif de score (on pourrait « finir » trop vite avec des combos ou ne
  jamais finir) ; temps fixe (moins lisible qu'une ligne d'arrivée).

## 3. Vitesse

- **Decision**: en histoire, la vitesse monte linéairement de la vitesse de départ à la vitesse maximale
  du niveau sur sa distance ; en infini, elle monte avec le temps de jeu (+0,012 par seconde depuis 1,7,
  jusqu'à 5,5).
- **Rationale**: FR-007 (difficulté croissante par niveau) et FR-018 (infini lié au temps) ; avec
  +0,012/s, la vitesse vaut 3,14 après 2 min, soit 1,85 fois le départ (SC-005).
- **Alternatives considered**: garder la vitesse liée au score (contraire à FR-018) ; paliers (FR-018 dit
  sans palier).

## 4. Boss différents

- **Decision**: un seul état de boss (vie, dents, phases) avec `BossKind` ; chaque sorte a ses mouvements :
  - Crabe : reste au sol devant le skateur et, toutes les 5 s, bondit vers lui puis revient ; le skateur
    doit le sauter quand il passe (collision = chute).
  - Goéland : vole ; toutes les 6 s il pique à hauteur de tête (s'accroupir ou sauter) ; entre deux
    piqués, il lâche 3 coquillages (petits plots qui tombent).
  - Sanglier : toutes les 6 s, il fonce au ras du sol vers le skateur (à sauter), puis repart ; il pose des
    barrières basses entre deux charges.
  - Violet : le boss de 001 (envol toutes les 10 s, 4 plots).
- **Rationale**: réutilise les dents, la barre de vie, le BOOM et les tests de 001 ; chaque boss oblige à
  un geste différent (sauter un obstacle qui arrive, s'accroupir, sauter une charge).
- **Alternatives considered**: une classe par boss (plus de code pour un état presque identique).

## 5. Figures débloquées

- **Decision**: `Progress` déduit les figures des niveaux réussis (1 → FLIP, 3 → SHOVE-IT, 6 → 360°,
  9 → 360° INVERSÉ) ; `Run` reçoit la liste et fait tourner les figures dans cet ordre, en sautant les
  inconnues.
- **Rationale**: une seule source de vérité (les niveaux réussis) ; pas de risque d'incohérence entre
  figures et niveaux ; annonce = différence entre avant et après la réussite.

## 6. Décors des 8 nouvelles villes

- **Decision**: chaque ville a sa palette (ThemeLook) et quatre plans dessinés par le code, avec un
  élément propre :
  Bidart (falaise et église blanche), Guéthary (petit port et grosse vague de surf), Saint-Jean-de-Luz
  (baie, maisons à colombages rouges), Hendaye (deux rochers jumeaux dans la mer), Bayonne (deux flèches
  de cathédrale, fleuve, façades colorées), Espelette (maisons blanches et guirlandes de piments rouges),
  Ascain (montagne et petit train à crémaillère), Cambo-les-Bains (villa et jardins à la française).
- **Rationale**: même technique que les 4 décors existants (Paint, plans en parallaxe, fondu) ; aucune
  image importée (principe V, droits).

## 7. Carte

- **Decision**: écran uGUI avec ScrollRect vertical ; fond de côte dessiné (océan à gauche, terre à
  droite) ; route en segments ; 12 pastilles (verrou, numéro, coche, meilleur score, tête de boss).
- **Rationale**: uGUI est déjà utilisé pour toute l'interface ; la carte défile au doigt sans code de
  caméra.
- **Alternatives considered**: carte dans le monde (caméra et touchers à gérer à part) ; UI Toolkit (une
  deuxième technique d'interface dans le projet).

## 8. Sauvegarde et anciennes données

- **Decision**: PlayerPrefs `progress` en JSON (`JsonUtility`) : `Cleared` (bool[12]), `Best` (int[12]),
  `EndlessBest`, `SeenObstacles`, `Worn` (5 objets portés), `Skin`, `HairStyle`, `HairColor` ; à la première
  ouverture, `EndlessBest` = ancien `best`, et la peau et les cheveux sont repris de l'ancienne clé `outfit`
  (les couleurs de vêtements libres de 001 disparaissent).
- **Rationale**: comme `outfit` (001) ; un seul enregistrement ; migration simple.
