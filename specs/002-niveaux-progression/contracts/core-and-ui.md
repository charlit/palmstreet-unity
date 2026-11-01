# Contracts: Core et interface

## 1. PalmStreet.Core

```csharp
LevelDef def = Levels.Get(3);              // 1 à 12 ; Levels.All, Levels.Count
var tricks = progress.Tricks;              // figures connues, déduites des niveaux réussis

run.StartLevel(def, tricks);               // mode histoire
run.StartEndless(cities, bosses, tricks, allowed, bossNow);   // mode infini : villes ouvertes, boss des niveaux réussis,
                                           // obstacles permis des niveaux ouverts (Game les déduit de Progress)
run.Start(bossNow: true);                  // raccourci du soleil (001), en mode infini

run.Phase == Phase.Cleared;                // ligne d'arrivée passée, ou boss du niveau battu
run.Events : Cue.Finish, Cue.Cleared, Cue.NewObstacle (Text = explication), + les Cue de 001

var (trick, item) = progress.Clear(def.Number, run.Score);   // figure (index, −1 si aucune) et objet (null si aucun)
                                                            // gagnés pour la première fois
bool open = progress.IsOpen(n);
Item? gift = Items.ForLevel(n);            // objet donné par le niveau (FR-025)
progress.Owns(itemId); progress.Wear(itemId);   // Wear refuse un objet non gagné
progress.Tricks; progress.NextLevel; progress.IsCleared(n); progress.WornIn(slot);
JsonUtility.ToJson(progress) / FromJson<Progress>(json) puis progress.Repair()   // côté Game, clé `progress`
```

Garanties : sans figure connue, un glissé en l'air ne fait rien ; en histoire, la vitesse ne dépend que de
la distance parcourue ; en infini, que du temps ; un niveau à boss n'est réussi qu'à la défaite du boss.

## 2. Écrans et boutons

| Écran | Contenu | Boutons |
|---|---|---|
| Lancement | planche qui fait un flip au ralenti, dessous « P. ST. », puis titre (≤ 2,5 s) | toucher : passer |
| Accueil | titre, BEST INFINI (plus de grand skateur ni de TOP 10) | JOUER, MODE INFINI, PROFIL (haut droite) |
| Carte | côte, route, 12 niveaux (verrou / ouvert / réussi + meilleur score / tête de boss), prochain niveau mis en avant, petit skateur posé dessus (roule jusqu'au suivant après une réussite) | un niveau ouvert : le lance ; RETOUR |
| Départ d'un niveau | bandeau « NIVEAU n · VILLE » ; bulle d'aide si nouvel obstacle | — |
| En partie | score, combo, RÉGLAGES (001) ; en histoire : barre de progression vers l'arrivée | RÉGLAGES |
| Niveau réussi | « NIVEAU RÉUSSI », score, meilleur score du niveau, « NOUVEAU : objet » avec son dessin, « NOUVELLE FIGURE : … » si déblocage | NIVEAU SUIVANT, CARTE |
| Échec en histoire | « WIPEOUT ! », score | RECOMMENCER, CARTE |
| Échec en infini | « WIPEOUT ! », score, BEST INFINI | toucher pour rejouer, TOP 10, ACCUEIL |
| Fin de l'aventure (niveau 12) | « TU ES UN VRAI PST ! », fin de l'aventure | MODE INFINI, CARTE |
| Profil | onglets TENUE (par emplacement : objets possédés à enfiler, objets verrouillés avec « Niveau n · Ville » ; peau, coiffure, cheveux libres), FIGURES (connues / à débloquer + niveau), RÉGLAGES | RETOUR |
