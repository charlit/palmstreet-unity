# Contracts: Core et services

## 1. PalmStreet.Core.Run

```csharp
var run = new Run(seed: 0);          // 0 : graine aléatoire ; autre : partie rejouable (tests)
run.Start(Rider.Guigui);             // ou Start(Rider.Freestill, bossNow: true) pour le raccourci du soleil
run.Press();                         // toucher : ollie au sol, OUT! en grind, figure au 2e toucher rapide en l'air
run.Release();                       // fin du toucher (fin de l'accroupi)
run.Step();                          // un pas de 1/60 s, seulement en Phase.Playing
foreach (var e in run.Events) { … }  // Cue, Points, X, Y, Text
run.Events.Clear();                  // c'est l'affichage qui vide
```

Garanties : Run ne dépend pas d'UnityEngine ; même graine et mêmes appels donnent la même partie ;
Press() pendant Phase ≠ Playing ne fait rien.

## 2. PalmStreet.Services.Leaderboard

```csharp
Leaderboard.SignIn();                       // au lancement (iOS : Game Center, silencieux)
Leaderboard.Report(score);                  // à chaque fin de partie, si connecté
Leaderboard.Show(message => hud.Toast(message));   // TOP 10 : écran natif, ou raison de l'indisponibilité
```

Id du classement : `palmstreet.top`. Android : indisponible tant que Google Play Games n'est pas branché (T037).

## 3. Touchers (Game.cs)

| Geste | Effet |
|---|---|
| Toucher (hors bouton) en partie | `Run.Press()` ; relâcher : `Run.Release()` |
| Maintien > 0,22 s au sol | accroupi (géré par Run) |
| Toucher le soleil à l'accueil | `StartRun(boss: true)` (FR-020) |
| Toucher après WIPEOUT (0,35 s après l'écran) | nouvelle partie avec le même skateur |
| Toucher en pause | reprise |
| Clavier (éditeur) | Espace ou ↑ = toucher, Échap = pause |
