# Quickstart: vérifier la spec 003

Prérequis : éditeur Unity fermé avant les commandes en lot ; `Progress.UnlockAll = true` en local.

1. **Tests de logique** : `Tools/run-tests.sh`.
   - Attendu : tous les tests en mode Edit passent, dont le nouveau test de `Levels.IsDay`
     (6 villes de jour, 6 autres).
2. **Captures** : `Tools/capture.sh`. Regarder dans les captures :
   - **Accueil, carte, profil (FIGURES), réglages, tutoriel** : texte en Rubik Bold, sans ombre,
     aucun texte qui déborde de son bouton.
   - **`2-depart-zoom`, `3-grind-zoom`, `3-manual`, `4-fakie-*`** :
     - comparer le skateur avec `Art/apercus/skateur-avant-apres.png` (APRÈS) ;
     - aucun trait au genou ;
     - le skateur est au même endroit et dans la même pose qu'avant.
   - **Profil, toutes les catégories** : chaque objet, chaque peau, coupe et couleur de cheveux
     s'affiche sur le nouveau skateur.
   - **Une partie à Biarritz (jour) et à Anglet (soir)** : à Biarritz, bandeaux, figures annoncées
     et points sont en sombre ; à Anglet, ils gardent leurs couleurs.
3. **Appareils** : `Tools/build-ios.sh`, puis `Tools/build-android.sh`.
   - Attendu : même police sur les deux appareils, partie fluide (60 images/s).
