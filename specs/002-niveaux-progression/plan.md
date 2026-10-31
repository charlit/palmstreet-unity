# Implementation Plan: Mode histoire en niveaux, mode infini et figures à débloquer

**Branch**: `002-niveaux-progression` (travail sur `main`, comme la spec 001) | **Date**: 2026-10-31 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-niveaux-progression/spec.md`

## Summary

Ajouter au jeu existant (spec 001) un mode histoire de 12 niveaux, un par ville de la côte basque, avec
une carte, une ligne d'arrivée, un boss différent tous les 3 niveaux, un mode infini dont la vitesse monte
avec le temps, et des figures qui se débloquent en réussissant des niveaux.

Approche : on garde la simulation `Run` et ses règles. Une table de niveaux en C# pur (`Levels`) décrit
chaque niveau : ville, décor, longueur, vitesses, obstacles permis, boss, figure débloquée. `Run` reçoit
un mode (histoire ou infini) et les figures connues, puis sait finir un niveau. `Progress` (C# pur) tient
les niveaux réussis, les meilleurs scores et en déduit les figures. Les quatre boss partagent l'état
existant (vie, dents) avec un `BossKind` qui change leurs attaques. Côté affichage : carte des niveaux et
écrans de réussite et d'échec dans `Hud`, décors des 8 nouvelles villes dans `Scenery`, ligne d'arrivée
et trois nouveaux boss dans `Props`. Tout est testé en mode Edit, comme les règles de 001.

## Technical Context

**Language/Version**: C# (Unity 6000.3.27f1, .NET Standard 2.1), comme la spec 001

**Primary Dependencies**: URP Renderer 2D, uGUI (ScrollRect pour la carte), Input System, Unity Test Framework. Aucun ajout.

**Storage**: PlayerPrefs ; nouvelle clé `progress` (JSON : niveaux réussis, meilleur score par niveau,
meilleur score infini, bulles d'aide vues, tenue portée, peau, coiffure, cheveux) ; `best` existant repris comme meilleur score infini.

**Testing**: tests EditMode sur PalmStreet.Core (Levels, Progress, Run en mode histoire et infini, boss) ;
captures PlayMode pour la carte, les nouvelles villes, la ligne d'arrivée et les boss.

**Target Platform**: iOS 15+ et Android 7+, portrait

**Project Type**: mobile-app (jeu 2D)

**Performance Goals**: 60 images/s, y compris pendant les nouveaux boss ; ouverture de la carte < 0,5 s

**Constraints**: hors ligne ; aucune donnée collectée ; graphismes dessinés par le code (pas d'image importée)

**Scale/Scope**: 12 niveaux, 12 décors (4 existants, 8 nouveaux), 4 boss, 3 nouveaux écrans (carte, réussite, échec), 28 exigences

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Vérification | État |
|---|---|---|
| I. Fidélité au jeu web | Le jeu web n'a pas de niveaux : cette spec ajoute un mode à la demande du propriétaire. Les règles de base (saut, grind, points) restent celles de 001. | ⚠️ écart justifié (Complexity Tracking) |
| II. Mobile d'abord | Carte et écrans au toucher, zones sûres, pas fixe 1/60 s inchangé | ✅ |
| III. Simplicité | Aucun paquet ; une seule scène ; la carte est un écran uGUI de plus ; les niveaux sont une table de données, pas un éditeur | ✅ |
| IV. Logique testable | Levels, Progress, modes de Run et boss dans PalmStreet.Core, tests EditMode | ✅ |
| V. Prêt pour les stores | Noms de villes sans logo ni marque ; aucune donnée collectée ; classement inchangé (infini) | ✅ |

Re-vérifié après la phase 1 (data-model, contracts) : mêmes résultats, aucun nouvel écart.

## Project Structure

### Documentation (this feature)

```text
specs/002-niveaux-progression/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/core-and-ui.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
Assets/Scripts/
├── Core/
│   ├── Levels.cs        # NOUVEAU : table des 12 niveaux et réglages du mode infini
│   ├── Progress.cs      # NOUVEAU : niveaux réussis, meilleurs scores, figures et objets déduits, tenue portée, JSON
│   ├── Items.cs         # NOUVEAU : objets de garde-robe (départ + 1 par niveau)
│   ├── Run.cs           # modes Story/Endless, figures connues, ligne d'arrivée, Phase.Cleared,
│   │                    # vitesse par niveau ou par temps, obstacles permis par niveau, décor par ville
│   ├── Boss.cs          # BossKind (Crab, Gull, Boar, Purple) : vie et attaques différentes
│   ├── Ground.cs        # option sans bosses (niveaux 1 à 3)
│   └── Tuning.cs        # valeurs des boss et du mode infini
├── Game/
│   ├── Game.cs          # navigation accueil → carte → niveau → réussite/échec ; infini ; sauvegarde
│   ├── Hud.cs           # accueil JOUER/INFINI sans TOP 10 ni grand skateur, carte avec petit skateur,
│   │                    # écrans réussite et échec, bulles d'aide, TENUE par objets, onglet FIGURES
│   ├── Look.cs          # 12 décors (ThemeLook par ville) ; Wardrobe : apparence des objets (couleurs, motifs de planche)
│   ├── Splash.cs        # NOUVEAU : animation de lancement (planche « P. ST. » qui flippe au ralenti)
│   ├── Scenery.cs       # plans des 8 nouvelles villes
│   └── Props.cs         # ligne d'arrivée, crabe, goéland, sanglier, coquillages, barrières
└── Services/Leaderboard.cs   # inchangé (score du mode infini)
Assets/Tests/EditMode/
├── RunTests.cs          # existants, adaptés (figures connues, mode)
└── LevelTests.cs        # NOUVEAU : niveaux, progression, boss, infini
Assets/Tests/PlayMode/CaptureTests.cs   # carte, villes, arrivée, boss
```

**Structure Decision**: même projet et mêmes assemblies que 001. Les niveaux sont des données dans
PalmStreet.Core (pas de ScriptableObject : rien à régler dans l'éditeur, et les tests les lisent
directement).

## Décisions de conception

- **Thème = ville** : l'énum `Theme` passe aux 12 villes (Anglet, Biarritz, Bidart, Guethary,
  SaintJeanDeLuz, Hendaye, Bayonne, Arcangues, Espelette, Ascain, Cambo, BiarritzNuit). Anglet reprend
  Largenté, Biarritz reprend BTZ, Arcangues reste Arcangues, Biarritz la nuit reprend NUIT NÉON.
- **Longueur d'un niveau** : une distance d'arrivée par niveau (environ 75 s à la vitesse moyenne du
  niveau) ; la vitesse monte de la vitesse de départ à la vitesse maximale du niveau sur la distance.
- **Boss** : à la distance d'arrivée d'un niveau à boss, le boss arrive (sol plat, plus d'obstacles) ;
  sa défaite réussit le niveau. Le boss de 001 devient `BossKind.Purple` ; les autres réutilisent la vie,
  les dents et la barre de vie, avec leurs propres mouvements et projectiles.
- **Figures** : `Run` reçoit la liste des figures connues ; le toucher en l'air fait la suivante dans cette
  liste ; aucune figure si la liste est vide. `Progress` déduit la liste des niveaux réussis (FR-014).
- **Mode infini** : vitesse = départ + pente × temps de jeu, plafonnée ; décor qui change toutes les
  4 000 points parmi les villes ouvertes ; boss tous les 15 000 points, tiré parmi ceux déjà rencontrés ;
  tous les types d'obstacles des niveaux ouverts.
- **Carte** : ScrollRect vertical, la côte dessinée du nord (Anglet) au sud puis l'intérieur, 12 pastilles
  reliées par une route ; la carte s'ouvre centrée sur le prochain niveau.
- **Garde-robe** : les objets sont des données de Core (`Items`) ; `Wardrobe.Build` traduit la tenue portée
  en apparence ; `Unlocks.cs` (vêtements par points, 001) est retiré.
- **Animation de lancement** : dessinée avec le même moteur de formes (planche et ses faces), avant
  l'accueil, ≤ 2,5 s, passée au toucher.
- **Ancienne progression** : `best` devient le meilleur score infini ; `total` et `outfit` restent ;
  le niveau 2 automatique de 001 (FR-029) disparaît, NUIT NÉON devient le niveau 12.

## Complexity Tracking

| Écart | Pourquoi | Alternative plus simple écartée |
|---|---|---|
| Principe I : un mode histoire qui n'existe pas dans le jeu web | Demande du propriétaire (spec 002) ; les règles de jeu de base restent celles de 001 | Garder la seule course sans fin : ne répond pas à la demande. La constitution est à amender (tâche T039 de 001). |
