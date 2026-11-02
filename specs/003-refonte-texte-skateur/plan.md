# Implementation Plan: Refonte graphique du texte de l'interface et du skateur

**Branch**: `003-refonte-texte-skateur` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-refonte-texte-skateur/spec.md`

## Summary

Deux changements graphiques, sans toucher aux règles du jeu.

1. **Texte de l'interface** :
   - tout le texte passe en Rubik Bold, chargée depuis `Resources` dans `Hud.Init` ;
   - l'ombre est retirée à sa seule source, `Hud.Label` ;
   - sur les 6 ciels de jour, les textes du milieu de l'écran passent en `Night`. La liste de ces
     villes est `Levels.IsDay`, dans le Core, avec un test.
2. **Skateur** : `RiderView` redessine ses pièces (tête, buste, bras, jambes, chaussures).
   - Chaque pièce est une silhouette dessinée deux fois : d'abord gonflée en encre pour le contour,
     puis en couleurs par-dessus.
   - Le contour de chaque membre passe sous toutes ses couleurs, et la cuisse finit au genou par un
     demi-disque du rayon du tibia : aucun trait au genou.
   - Le squelette et les poses ne sont pas touchés.

## Technical Context

**Language/Version**: C# (Unity 6000.3.27f1)

**Primary Dependencies**:
- uGUI `Text` ;
- rendu 2D URP, maillages procéduraux (`Paint`) ;
- aucun nouveau paquet.

**Storage**: aucun changement (PlayerPrefs `progress`)

**Testing**:
- Unity Test Framework : mode Edit pour `Levels.IsDay`, mode Play pour les captures ;
- scripts `Tools/run-tests.sh` et `Tools/capture.sh`.

**Target Platform**: iOS 15+ (iPhone), Android minSdk 24

**Project Type**: jeu mobile (une scène)

**Performance Goals**: 60 images/s en partie (principe II). Les maillages du skateur sont construits
une fois par tenue, comme aujourd'hui.

**Constraints**:
- squelette et poses inchangés (FR-007) ;
- garde-robe complète (FR-014) ;
- aucun texte qui déborde (FR-005).

**Scale/Scope**: 4 fichiers de jeu touchés :
- `Hud.cs`, `HudExtras.cs` et `HudLevels.cs` ;
- `RiderView.cs` ;
- `Paint.cs` (+1 primitive).

Plus `Levels.cs` dans le Core, un test, la police et sa licence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Vérification | Statut |
|---|---|---|
| I. Fidélité au jeu web | Écart noté dans la spec : graphismes plus riches, mêmes ingrédients | OK (écart justifié) |
| II. Mobile d'abord | Maillages construits une fois ; pas de shader ni de rendu en texture ; 60 images/s vérifiées sur appareil | OK |
| III. Simplicité | Pas de paquet. Un seul asset tiers : la police Rubik, demandée par le propriétaire, sous licence OFL. Une seule primitive ajoutée (`Taper`) | OK (asset justifié) |
| IV. Logique testable | La seule règle nouvelle (`Levels.IsDay`) est dans le Core avec un test en mode Edit ; le reste est du dessin, vérifié par captures | OK |
| V. Prêt pour les stores | OFL : police embarquable dans une app vendue, licence fournie ; aucun logo ni marque | OK |

Re-check après la phase 1 : inchangé, aucune violation.

## Project Structure

### Documentation (this feature)

```text
specs/003-refonte-texte-skateur/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/ui-text-and-rider.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
Assets/
├── Resources/Fonts/
│   ├── Rubik-Bold.ttf          # déjà copié
│   └── OFL.txt                 # licence (FR-006)
├── Scripts/Core/Levels.cs      # + IsDay(Theme)
├── Scripts/Game/
│   ├── Hud.cs                  # police Rubik, Label sans ombre, couleur jour des bandeaux et points
│   ├── HudExtras.cs            # Shout : couleur jour
│   ├── HudLevels.cs            # retrait de Sharp()
│   ├── Paint.cs                # + Taper (capsule effilée)
│   └── RiderView.cs            # Build (buste), MakeLeg, Arm, Head redessinés ; contour en 2 passes
└── Tests/
    ├── EditMode/LevelTests.cs  # + test IsDay
    └── PlayMode/CaptureTests.cs  # captures existantes ; + gros plans genou accroupi et garde-robe si besoin
```

**Structure Decision**: on garde la structure actuelle (Core sans moteur / Game / Tests), sans nouveau
fichier de code.

## Complexity Tracking

Aucune violation à justifier.
