# Quickstart: vérifier le portage

## Prérequis

- Unity 6000.3.27f1 avec iOS Build Support et Android Build Support.
- Xcode ; l'iPhone « CHARLES » branché ou sur le même Wi-Fi, déverrouillé.
- Les scripts en batchmode demandent que l'éditeur Unity soit fermé sur ce projet.

## 1. Règles (sans lancer le jeu)

```bash
Tools/run-tests.sh
```

Attendu : 23 réussis (une règle de la spec par test, de FR-003 à FR-020).

## 2. Rendu (sans téléphone)

```bash
Tools/capture.sh /chemin/des/captures
```

Attendu : accueil, départ, grind sur le grand rail, les 4 figures en gros plan à trois moments, BTZ,
Arcangues, boss, BOOM, WIPEOUT.

## 3. Dans l'éditeur

Menu PalmStreet › « Jouer dans l'éditeur (format téléphone) », puis au clavier :

| Scénario | Attendu |
|---|---|
| START, Espace devant chaque plot | ollies, score qui monte de 60 par seconde |
| Percuter un plot | WIPEOUT !, le skateur tombe, la planche rebondit, BEST mis à jour |
| Espace, puis Espace en l'air | FLIP ! +30, COMBO x2 |
| Retomber sur le grand rail du départ | grind, étincelles, +20 × combo GRIND! en sortie |
| Cliquer le soleil à l'accueil | boss immédiat, barre de vie, plots violets lâchés en vol |

## 4. Sur l'iPhone

```bash
Tools/build-ios.sh
```

Vérifier : 60 images/s pendant le boss, pause en arrière-plan, son coupé gardé au relancement, connexion
Game Center et score dans le TOP 10.
