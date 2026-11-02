# PalmStreet — aperçus et specs à venir

À copier dans `palmstreet-unity/Art/apercus/`. Images de référence préparées avec Claude (session du 2 novembre 2026).
Design system PalmStreet : https://claude.ai/code/artifact/06fdd57a-1318-46d5-bf5d-fa275e1fccdb

## Les images

| Fichier | Ce qu'il montre |
|---|---|
| skateur-avant-apres.png | Skateur actuel et skateur redessiné (même squelette) |
| skateur-genoux.png | Genou continu entre cuisse et tibia, debout et accroupi |
| tenues-rugby.png | Tenues complètes BO et Aviron, débloquées en battant les boss de rugby |
| apercu-biarritz.png | Niveau 2 : stade Aguiléra au premier plan, Hôtel du Palais |
| apercu-obstacles.png | Mouette actuelle, mouette redessinée, ballon de rugby (boîte 30 × 16) |
| apercu-boss-rugby.png | Boss LE PILIER (Biarritz) et ses quatre actions |
| apercu-boss-bayonne.png | Boss LE TALONNEUR (Bayonne) et le derby |
| apercu-idees-basques.png | Rocher de la Vierge, boss LE PELOTARI au fronton d'Arcangues, pelote basque |
| apercu-planche-en-main.png | Mode spécial « planche en main » : bouclier et boomerang |
| carte-pays-basque.png | Carte du mode histoire redessinée d'après MapArt.cs |

## Premier message à Claude Code (contexte)

```
Contexte : dans une autre session Claude, j'ai préparé plusieurs évolutions de PalmStreet. Lis d'abord la constitution (.specify/memory/constitution.md), les specs 001 et 002, puis Art/apercus/LISEZ-MOI.md et les images de Art/apercus/. Le design system PalmStreet (lien dans LISEZ-MOI.md) sert de référence si tu peux le lire.
On travaille spec par spec avec Spec Kit, en commençant par la spec 003. Ne code rien avant que je lance /speckit-specify. Demande-moi avant chaque commit et chaque push.
```

Avant la spec 003 : copier `static/Rubik-Bold.ttf` (Google Fonts, Rubik) dans `Assets/Resources/Fonts/`.

## Spec 003 — /speckit-specify

```
Refonte graphique du texte de l'interface et du skateur.

1. Police : remplacer la police système (Avenir Next Condensed, Futura…) par Rubik Bold (Google Fonts, licence OFL), chargée depuis Assets/Resources/Fonts/Rubik-Bold.ttf, sur tout le texte de l'interface (Hud.cs). Supprimer l'ombre portée de tous les textes : elle rend le texte flou sur certains boutons. Le libellé de JOUER n'en avait déjà pas. Le texte doit rester lisible sans ombre sur les 12 ciels : en haut de l'écran le crème suffit ; au milieu de l'écran, sur les ciels de jour (Biarritz, Bidart, Guéthary, Bayonne, Espelette, Cambo), écrire les bandeaux et les points en sombre (night) ou les poser sur un fond sombre.

2. Skateur redessiné (RiderView.cs), même squelette et mêmes poses (cuisse 14,5, tibia 14, mêmes points d'attache, même cinématique des jambes), seul le dessin des pièces change : visage de profil (front, nez, lèvres, menton, œil avec blanc, iris et reflet, sourcil, oreille, joue), cheveux qui suivent le crâne, mains avec pouce et doigts, coude marqué, buste aux épaules arrondies avec ombre et lumière, plis, chaussures de skate détaillées (embout, lacets, bande, semelle), 2 à 3 tons par pièce et un même contour sombre de 3 px partout. Au genou, la cuisse et le tibia finissent par un demi-disque de même rayon centré sur l'articulation, et le contour de la jambe entière est posé avant les couleurs : aucun trait au genou, à tout angle. Toute la garde-robe existante doit fonctionner avec le nouveau dessin.

Référence visuelle : Art/apercus/skateur-avant-apres.png et skateur-genoux.png.
Écart au jeu web (principe I) : graphismes refaits, plus riches, mêmes ingrédients (principe III).
```

## Spec 004 — /speckit-specify

```
Biarritz et Bayonne aux couleurs du rugby : décors, boss, obstacles et tenues à débloquer.

1. Décor du niveau 2 BIARRITZ : ajouter au premier plan le stade Aguiléra (tribune couverte aux sièges rouges et blancs, toit en porte-à-faux, pylônes d'éclairage allumés, poteaux de rugby, tableau d'affichage lumineux annonçant le derby), l'Hôtel du Palais (palace brique et pierre, toits d'ardoise très pentus, trois pavillons, plan en E, terrasse sur la plage), et sur une autre portion du décor le Rocher de la Vierge (rocher dans l'océan, statue blanche, passerelle métallique en treillis reliée à la falaise). Aucun logo ni écusson de club ; le tableau du stade affiche « BIARRITZ vs BAYONNE » (ou « DERBY ») plutôt que les sigles des clubs (principe V).

2. Boss LE PILIER (niveau 2 BIARRITZ) : joueur de rugby en maillot rouge à cerceau et col blancs, short blanc, chaussettes rouges à bandes blanches, bandeau blanc. Entre deux attaques il trottine ; ses attaques : la passe (ballon lancé à la main à hauteur de tête : se baisser), la chandelle (coup de pied, le ballon retombe devant le skateur et rebondit vers lui : sauter), le plaquage (il plonge au ras du sol : sauter). On le blesse comme les autres boss (dents, sorts). Moins résistant que les boss suivants car c'est le début du jeu.

3. Boss LE TALONNEUR (niveau 7 BAYONNE, qui n'a pas de boss aujourd'hui) : mêmes compétences que le Pilier, tenue bleu ciel et blanc, crâne rasé et barbe pour le distinguer. Même code que le Pilier, seules les couleurs et la tête changent.

4. Tenues à débloquer, pour les deux boss de rugby uniquement : battre un boss de rugby débloque sa tenue complète en 5 pièces dans la garde-robe. Le Pilier : bandeau blanc à liseré rouge, maillot rouge à cerceau blanc, short blanc et chaussettes rouges, baskets noires à bande rouge, planche rouge à roues blanches. Le Talonneur : casque de mêlée bleu ciel à bandes blanches, maillot bleu ciel à cerceau blanc, short blanc et chaussettes bleu ciel, baskets noires à bande bleu ciel, planche bleu ciel. Les autres boss ne donnent pas de tenue.

5. Obstacles volants : redessiner la mouette (dos gris perle, bouts d'ailes noirs, bec jaune à tache rouge, sans halo rose le jour) et ajouter le ballon de rugby (blanc à bandes rouges, tourne sur lui-même) comme obstacle volant à Biarritz (niveaux 2 et 12), avec la même boîte de collision que la mouette (30 × 16) ; on l'évite en se baissant. Le tutoriel dit « mouettes » au lieu d'« oiseaux ».

Référence visuelle : Art/apercus/apercu-biarritz.png, apercu-obstacles.png, apercu-boss-rugby.png, apercu-boss-bayonne.png, tenues-rugby.png, apercu-idees-basques.png (Rocher de la Vierge).
Écart au jeu web (principe I) : nouveaux boss, décors et récompenses, à la demande du propriétaire.
```

## Spec 005 — /speckit-specify

```
Pelote basque à Arcangues : un boss, un obstacle et le mode « planche en main ».

1. Boss LE PELOTARI (niveau 8 ARCANGUES, devant le fronton) : joueur de cesta punta en maillot rouge numéroté, pantalon blanc, casque blanc à bande rouge, avec un chistera (grand panier d'osier recourbé lacé au gant). Attaques : le lancer (pelote à hauteur de tête : se baisser) et le rebond (la pelote rebondit au sol vers le skateur : sauter). Décor : fronton ocre au sommet arrondi, filets rouges et bande de métal en bas, clocher de l'église d'Arcangues, platanes taillés.

2. Obstacle volant : la pelote basque, petite balle blanche cousue avec traits de vitesse, plus rapide que la mouette, boîte de collision 12 × 12 ; on l'évite en se baissant. Utilisée à Arcangues et dans les niveaux de l'intérieur.

3. Mode spécial « planche en main » pendant le combat contre le Pelotari : le skateur descend de sa planche et la tient à la main. Maintenir le doigt = bouclier (la planche bloque les pelotes à hauteur de tête, pas de saut pendant ce temps) ; parer juste au moment de l'impact renvoie la pelote au boss ; glisser vers le boss = lancer la planche en boomerang (dégâts, puis elle revient dans sa main ; pas de bouclier pendant son vol) ; toucher = sauter.

Référence visuelle : Art/apercus/apercu-idees-basques.png et apercu-planche-en-main.png.
Écart au jeu web (principe I) : nouveau boss, nouvel obstacle et nouveau mode de combat, à la demande du propriétaire.
```

## Spec 006 — /speckit-specify

```
Nouveau niveau : le skatepark de la Barre à Anglet.

Un niveau libre pour s'amuser et faire le maximum de points : aucun obstacle volant ni personnage (ni mouettes, ni ballons, ni enfants). Le décor est un skatepark avec ses modules : rails et curbs pour grinder, bowl, quarters, funbox. Au départ du niveau, le skateur descend une immense rampe. Le niveau récompense les enchaînements de figures et les combos.

Écart au jeu web (principe I) : nouveau niveau et nouveau décor, à la demande du propriétaire.
```

## Questions encore ouvertes (pour /speckit-clarify)

- 004 : résistance exacte des boss ; le Pilier débloque-t-il aussi un sort ?
- 005 : planche en main réservée au Pelotari ou à tous les boss à projectiles ? Usure de la planche ? Figures et sorts utilisables à pied ?
- 006 : niveau 13 ou niveau bonus ? Nombre d'étoiles ?
- Carte : Biarritz (niveau 2) est loin de la côte et Biarritz la nuit (niveau 12) en pleine mer dans MapArt.cs — à rapprocher de la côte.
