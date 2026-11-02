# Data Model: Refonte graphique du texte de l'interface et du skateur

Pas de nouvelle donnée sauvegardée : la progression, la garde-robe et les réglages ne changent pas.

## Core (testable, principe IV)

- **`Levels.IsDay(Theme theme) → bool`** :
  - vrai pour Biarritz, Bidart, Guethary, Bayonne, Espelette et Cambo ;
  - faux pour les 6 autres thèmes (Anglet, SaintJeanDeLuz, Hendaye, Arcangues, Ascain,
    BiarritzNuit).

## Game (dessin, non sauvegardé)

- **Police de l'interface** : `Font` chargé une fois par `Hud.Init`. Si le fichier manque, on
  utilise la police système actuelle.
- **Pièce du skateur** : une fonction de silhouette par pièce (tête, buste, bras, cuisse, tibia,
  chaussure), appelée deux fois :
  - `grow = Outline`, en couleur d'encre : le maillage du contour ;
  - `grow = 0`, en couleurs, avec les détails : le maillage des couleurs.
  Les articulations, les longueurs (`Thigh` = 14,5, `Shin` = 14) et les points d'attache sont
  inchangés.
- **Ordre de tri** :
  - pour chaque membre, contour < couleurs ;
  - l'ordre entre les membres reste celui d'aujourd'hui : jambe arrière < bras arrière < buste
    < jambe avant < tête < bras avant.
