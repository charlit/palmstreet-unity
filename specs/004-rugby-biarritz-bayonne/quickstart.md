# Quickstart: vérifier la spec 004

Prérequis : éditeur Unity fermé avant les commandes en lot. Les captures vont dans le dossier
scratchpad, parce qu'Unity vide `Temp/` en quittant.

1. **Tests de logique** : `Tools/run-tests.sh`. Tous les tests en mode Edit doivent passer, dont ceux
   des esquives, de l'annonce, de l'ordre des vies, des sorts, des tenues et des ballons volants.
2. **Captures** : `Tools/capture.sh <dossier>`. Regarder :
   - le niveau 2 à trois endroits : le stade avec le tableau « BIARRITZ vs BAYONNE », l'Hôtel du
     Palais, le Rocher de la Vierge, sans aucun logo ni sigle ;
   - LE PILIER et LE TALONNEUR : en course, en annonce de passe, de chandelle et de plaquage ;
   - un ballon volant au niveau 2 et au niveau 12, et une mouette redessinée de jour (sans halo) ;
   - le profil avec la tenue complète du Pilier, puis celle du Talonneur ;
   - le tutoriel, qui doit dire « mouettes ».
3. **Appareils** : `Tools/build-ios.sh`, puis `Tools/build-android.sh`. Jouer le niveau 2 jusqu'au
   boss, esquiver chaque attaque, le battre et vérifier l'annonce de la tenue. Le jeu doit rester
   fluide (60 images/s).
