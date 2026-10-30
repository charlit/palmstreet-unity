<!--
Sync Impact Report
- Version change: 1.0.0 → 2.0.0 (principe III redéfini : refonte graphique au lieu de reprise à l'identique)
- Principes ajoutés : I. Fidélité au jeu web, II. Mobile d'abord, III. Simplicité,
  IV. Logique de jeu testable, V. Prêt pour les stores
- Sections ajoutées : Contraintes techniques, Façon de travailler, Governance
- Sections retirées : aucune
- TODO : TODO(BUNDLE_ID) — garder com.charlit.palmstreet (remplace l'app WebView actuelle) ou un nouvel identifiant
-->
# PalmStreet Unity Constitution

## Core Principles

### I. Fidélité au jeu web

Le jeu web PalmStreet (github.com/charlit/PalmStreet, `public/index.html`) est la référence.
Les règles, les valeurs de gameplay (vitesses, gravité, saut, scores, paliers, boss) et
l'ambiance (4 skateurs, décors Largenté / BTZ / Arcangues, musique, textes) sont reprises à
l'identique au départ. Tout écart MUST être noté dans la spec avec sa raison.

Pourquoi : le jeu existe et plaît ; le but est de le porter, pas d'en inventer un autre.

### II. Mobile d'abord

- Format portrait, plein écran, zones sûres (encoche, barre d'accueil) respectées.
- Commandes tactiles d'abord (tap = ollie, double tap = figure) ; clavier en option pour tester.
- 60 images/s stables sur un iPhone 12 et un Android milieu de gamme de 2021.
- Physique et vitesses indépendantes du nombre d'images par seconde (pas de temps fixe).

Pourquoi : iOS et Android sont les seules cibles.

### III. Simplicité

- Pas de paquet ou d'asset tiers sans besoin réel, noté dans le plan.
- Une seule scène de jeu tant que possible ; pas d'architecture « pour plus tard ».
- Les graphismes sont refaits, plus riches que le jeu web, mais avec ses ingrédients (skate,
  palmiers, soleil couchant, couleurs des décors) : on enrichit, on ne change pas d'univers.

Pourquoi : un petit jeu, un seul développeur ; chaque couche en plus coûte en maintenance.

### IV. Logique de jeu testable

Les règles (score, combo, terrain, apparition des obstacles, boss) vivent dans des classes C#
simples, sans dépendre des MonoBehaviour, et sont couvertes par des tests en mode Edit
(Unity Test Framework). Un bug de règle corrigé reçoit un test.

Pourquoi : les règles se vérifient sans lancer le jeu sur un téléphone.

### V. Prêt pour les stores

- Aucune donnée personnelle collectée, pas de pub, pas de suivi.
- Respect des règles App Store et Google Play (pas de marque ou contenu de tiers sans droit).
- Classements : Game Center sur iOS, Google Play Games sur Android.

Pourquoi : la publication est le but, et les refus de review coûtent des semaines.

## Contraintes techniques

- Moteur : Unity 6000.3 LTS (Unity 6.3), C#, rendu 2D (URP 2D).
- Cibles : iOS 15 et plus (iPhone, iPad en portrait), Android minSdk 24, targetSdk 36.
- Équipe Apple : 32AK299L44. Identifiant : TODO(BUNDLE_ID).
- Le projet Unity vit à la racine de ce dépôt (Assets/, Packages/, ProjectSettings/).

## Façon de travailler

- Spec Kit : constitution → specify → plan → tasks → implement, une fonctionnalité par spec.
- Git : messages de commit en français ; on demande avant chaque commit et chaque push.
- Chaque fonctionnalité est vérifiée en jouant (éditeur, puis appareil) avant d'être dite finie.

## Governance

Cette constitution passe avant les autres habitudes du projet. Toute modification est faite
avec /speckit-constitution, explique sa raison et change la version (MAJOR : principe retiré
ou redéfini, MINOR : principe ou section ajouté, PATCH : formulation). Chaque plan vérifie
qu'il respecte ces principes ; un écart est justifié dans le plan.

**Version**: 2.0.0 | **Ratified**: 2026-10-30 | **Last Amended**: 2026-10-30
