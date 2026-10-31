# Contracts: Core et interface

## 1. PalmStreet.Core

```csharp
LevelDef def = Levels.Get(3);              // 1 à 12 ; Levels.All, Levels.Count
var tricks = progress.Tricks;              // figures connues, déduites des niveaux réussis

run.StartLevel(def, tricks);               // mode histoire
run.StartEndless(progress, tricks);        // mode infini (villes et boss ouverts)
run.Start(bossNow: true);                  // raccourci du soleil (001), en mode infini

run.Phase == Phase.Cleared;                // ligne d'arrivée passée, ou boss du niveau battu
run.Events : Cue.Finish, Cue.Cleared, Cue.NewObstacle (Text = explication), + les Cue de 001

Trick? unlocked = progress.Clear(def.Number, run.Score);   // figure à annoncer, ou null
bool open = progress.IsOpen(n);
string json = progress.ToJson();  Progress.FromJson(json, legacyBest);
```

Garanties : sans figure connue, un toucher en l'air ne fait rien ; en histoire, la vitesse ne dépend que de
la distance parcourue ; en infini, que du temps ; un niveau à boss n'est réussi qu'à la défaite du boss.

## 2. Écrans et boutons

| Écran | Contenu | Boutons |
|---|---|---|
| Accueil | titre, skateur, BEST INFINI | JOUER, INFINI, PROFIL (haut droite), TOP 10 |
| Carte | côte, route, 12 niveaux (verrou / ouvert / réussi + meilleur score / tête de boss), prochain niveau mis en avant | un niveau ouvert : le lance ; RETOUR |
| Départ d'un niveau | bandeau « NIVEAU n · VILLE » ; bulle d'aide si nouvel obstacle | — |
| En partie | score, combo, RÉGLAGES (001) ; en histoire : barre de progression vers l'arrivée | RÉGLAGES |
| Niveau réussi | « NIVEAU RÉUSSI », score, meilleur score du niveau, « NOUVELLE FIGURE : … » si déblocage | NIVEAU SUIVANT, CARTE |
| Échec en histoire | « WIPEOUT ! », score | RECOMMENCER, CARTE |
| Échec en infini | « WIPEOUT ! », score, BEST INFINI | toucher pour rejouer, ACCUEIL |
| Fin de l'aventure (niveau 12) | « TU ES UN VRAI PST ! », fin de l'aventure | MODE INFINI, CARTE |
| Profil | onglets TENUE, FIGURES (connues / à débloquer + niveau), RÉGLAGES | RETOUR |
