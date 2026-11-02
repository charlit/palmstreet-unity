# Quickstart: vérifier la spec 005

Prérequis : éditeur Unity fermé ; captures dans le dossier scratchpad (Unity vide `Temp/` en quittant).

1. **Tests** : `Tools/run-tests.sh`. Tout passe, dont les tests du Pelotari, du bouclier, de la parade,
   du boomerang, du saut à pied et de la pelote volante.
2. **Captures** : `Tools/capture.sh <dossier>`. Regarder :
   - le niveau 8 en cours de route : fronton, clocher, platanes, et une pelote volante ;
   - le combat :
     - le skateur à pied, la planche à la main ;
     - le bouclier levé ;
     - la parade (pelote qui repart) ;
     - le boomerang en vol ;
     - les annonces du lancer et du rebond ;
   - après la victoire : le skateur de nouveau sur sa planche.
3. **Appareils** : `Tools/build-ios.sh`, puis `Tools/build-android.sh`. Jouer le niveau 8 jusqu'au
   Pelotari et vérifier au doigt :
   - le bouclier (doigt maintenu) ;
   - la parade (0,15 s) ;
   - le boomerang (glissé vers la droite) ;
   - le saut (toucher bref).
