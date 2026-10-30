# Research: Portage de PalmStreet sur iOS et Android

## 1. Moteur et rendu

- **Decision**: Unity 6000.3.27f1 (6.3 LTS), Universal Render Pipeline avec le Renderer 2D
  (lumières 2D, post-traitement bloom/vignette/étalonnage couleur), caméra orthographique.
- **Rationale**: déjà installé avec les modules iOS et Android ; URP 2D donne la lumière du
  soleil, les halos et le bloom demandés par la refonte (FR-024) sans asset tiers.
- **Alternatives considered**: Built-in Render Pipeline (pas de lumières 2D) ; rendu 3D avec
  caméra fixe (inutile pour un runner de profil, plus lourd sur mobile).

## 2. Simulation : pas fixe de 1/60 s

- **Decision**: les règles tournent dans une simulation à pas fixe de 1/60 s (accumulateur de
  temps), avec les valeurs du jeu web telles quelles (vitesses en unités/image, gravité 0,5,
  saut 9,6, timers en images). L'affichage interpole entre deux pas.
- **Rationale**: reprend le jeu web à l'identique (principe I) et donne exactement le même jeu à
  30, 60 ou 120 images/s (principe II) ; les tests rejouent des parties image par image.
- **Alternatives considered**: tout convertir en secondes avec `Time.deltaTime` (risque d'écarts
  de saut et de collisions selon l'appareil, et chaque valeur à retraduire) ; physique Unity
  (Rigidbody2D) : comportement différent du jeu web, plus difficile à rendre identique.

## 3. Logique de jeu hors moteur

- **Decision**: assembly `PalmStreet.Core` en C# pur, sans `UnityEngine` (noAutoReferenced,
  noEngineReferences) : terrain, apparitions, score/combo, skateur, boss. Le hasard passe par
  une interface `IRandom` (graine fixe en test). L'assembly `PalmStreet.Game` contient les
  MonoBehaviour qui affichent l'état et transmettent les touchers.
- **Rationale**: principe IV (tests en mode Edit sans scène) ; la logique est un portage direct
  des fonctions `update()`, `spawnObstacle()`, `updateBossFight()` du jeu web.
- **Alternatives considered**: logique dans les MonoBehaviour (non testable sans lancer le jeu).

## 4. Graphismes refaits (FR-024, FR-025)

- **Decision**: illustrations vectorielles (SVG) dessinées pour le projet, découpées en plans de
  parallaxe et en parties de corps, puis converties en PNG haute définition (×3) par un script
  (`Tools/art/render.sh`, Chrome sans interface, déjà présent sur le Mac) et importées comme
  sprites. Les skateurs sont des pantins articulés (tête, buste, bras, jambes, planche) animés
  par des poses décrites en données (angles par partie) et interpolées en code. Effets : lumières
  2D (soleil, lampadaires), Particle System (poussière, étincelles de grind, éclairs, BOOM),
  bloom et dégradés de ciel en shader simple.
- **Rationale**: aucun graphisme sous licence d'un tiers (principe V) ; les SVG se versionnent et
  se retouchent ; les poses en données évitent l'éditeur d'animation et se testent.
- **Alternatives considered**: package Vector Graphics de Unity (encore en préversion) ; 2D
  Animation avec os et PSD (demande des fichiers Photoshop) ; packs d'assets ou illustrateur
  externe (possible plus tard pour une passe artistique, les sprites se remplacent sans code).

## 5. Classement : Game Center (iOS)

- **Decision**: plugin officiel Apple « Apple.GameKit » (github.com/apple/unityplugins), compilé
  en tarball et ajouté par le Package Manager. Classement `palmstreet.top` (déjà utilisé par l'app
  WebView), format entier.
- **Rationale**: l'API Social de Unity et `GameCenterPlatform` sont dépréciées depuis Unity 6 et
  seront retirées ; Apple recommande son plugin.
- **Alternatives considered**: API Social (dépréciée) ; plugin payant de l'Asset Store.

## 6. Classement : Google Play Games (Android)

- **Decision**: « Google Play Games plugin for Unity » (playgameservices, version 2.x, PGS v2 :
  connexion automatique), classement « Meilleur score » créé dans la Play Console.
- **Rationale**: plugin officiel Google, gratuit.
- **Alternatives considered**: aucun classement sur Android (contraire à FR-022).

Les deux plugins sont cachés derrière une interface `ILeaderboard` (voir contracts/) ; dans
l'éditeur et sans connexion, une implémentation locale répond « indisponible ».

## 7. Son et musique

- **Decision**: la musique (mélodie et basse à 132 bpm, notes du jeu web) et les 5 bruitages
  sont générés une fois en WAV par un script Python sans dépendance (`Tools/audio/generate.py`),
  à partir des mêmes fréquences et enveloppes que l'`AudioContext` du jeu web ; lecture par
  AudioSource. Son coupé gardé dans PlayerPrefs.
- **Rationale**: même son que le jeu web, pas de synthèse en temps réel à maintenir.
- **Alternatives considered**: synthèse en direct avec `OnAudioFilterRead` (plus de code, plus
  de risques de craquements sur Android).

## 8. Identifiant de l'app

- **Decision**: `com.charlit.palmstreet` sur iOS et Android, nom affiché « PalmStreet ».
- **Rationale**: l'app WebView actuelle n'est pas publiée ; garder l'identifiant réutilise son
  classement Game Center `palmstreet.top`. La version Unity remplace la WebView.
- **Alternatives considered**: nouvel identifiant (classement à recréer, deux apps à gérer).

## 9. Écrans et texte

- **Decision**: UI Toolkit pour l'accueil, le choix du skateur, l'écran de fin, le HUD et les
  bandeaux ; police libre (Google Fonts, licence OFL) incluse dans le projet.
- **Rationale**: interface déclarative, s'adapte aux zones sûres et aux tailles d'écran.
- **Alternatives considered**: uGUI (plus de réglages manuels pour un résultat équivalent).

## Sources

- Dépréciation de l'API Social : https://discussions.unity.com/t/deprecation-of-social-api/1542110
- GameCenterPlatform (Unity 6.6) : https://docs.unity.cn/6000.6/Documentation/ScriptReference/SocialPlatforms.GameCenter.GameCenterPlatform.html
- Plugin Google Play Games pour Unity : https://developer.android.com/games/pgs/unity/overview
