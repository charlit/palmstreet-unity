# Contracts: logique de jeu et services

## 1. Simulation (PalmStreet.Core → PalmStreet.Game)

La couche Game appelle la simulation ; la simulation ne connaît pas Unity.

```text
Simulation(IRandom random, int best)
  Start(Character c)                 // Ready/Over → Running
  StartAtBoss(Character c)           // raccourci du soleil (FR-020) : score 15 000 + boss
  Tap()                              // ollie, sortie de grind, ou relance en Ready/Over
  DoubleTap()                        // figure si en l'air et CanTrick
  SetDuck(bool on)
  Step()                             // avance d'un pas de 1/60 s
  GameState State { get; }           // lecture seule pour l'affichage
  event Action<GameEvent> Events     // voir ci-dessous
```

Événements (pour son, particules, textes flottants, bandeaux) :

| Événement | Données |
|---|---|
| Jumped | — |
| TrickDone | kind, points |
| GrindStarted / GrindEnded | points (fin) ; noseGrind |
| GrindOut | +15 |
| CoinCollected | x, y, points |
| ThemeChanged | theme |
| Milestone | « FULL SPEED » |
| BossStarted / BossHit / BossDefeated | hp ; +5 000 |
| ConeDropped | x |
| Crashed | score final, nouveau record (bool) |

Garanties testées : même graine + même suite de commandes → même partie, pas par pas.

## 2. Classement (ILeaderboard)

```text
ILeaderboard
  bool IsAvailable { get; }
  Task SignInAsync()                 // silencieux ; échec = IsAvailable false
  Task SubmitAsync(long score)       // ignoré si indisponible
  void ShowNative()                  // écran Game Center / Play Games
```

| Plateforme | Implémentation | Identifiant du classement |
|---|---|---|
| iOS | Apple.GameKit | `palmstreet.top` |
| Android | Google Play Games plugin | ID généré par la Play Console (constante `PlayGamesIds.Top`) |
| Éditeur | LocalLeaderboard | — (toujours indisponible) |

Contexte du score (iOS) : numéro du skateur, comme l'app WebView actuelle.

## 3. Entrées (Game)

| Geste | Commande |
|---|---|
| Toucher (relâché ou non) | Tap() |
| Deuxième toucher en moins de 0,32 s, en l'air | DoubleTap() à la place de Tap() |
| Doigt maintenu > 0,22 s au sol | SetDuck(true) jusqu'au relâchement |
| Clavier (éditeur) | Espace/↑ = Tap, ←/→ = DoubleTap, ↓ = accroupi |
