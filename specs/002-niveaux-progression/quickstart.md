# Quickstart: vérifier les niveaux, l'infini et les figures

Prérequis et outils : ceux de la spec 001 ([quickstart 001](../001-portage-palmstreet/quickstart.md)).
L'éditeur Unity doit être fermé pour les scripts en batchmode.

## 1. Règles

```bash
Tools/run-tests.sh
```

Attendu : tous les tests passent, dont LevelTests :
- table des 12 niveaux (boss aux niveaux 3, 6, 9, 12 ; durée 59 à 75 s ; un nouvel obstacle par niveau
  annoncé) ;
- progression (niveau 1 seul ouvert au départ, ouverture du suivant, figures déduites, migration de `best`) ;
- histoire (arrivée → Phase.Cleared ; chute → Over ; boss → Cleared à sa défaite ; aucune figure sans
  déblocage) ;
- infini (vitesse ≥ 1,5 × départ après 2 min ; ne dépend pas du score) ;
- boss (Crabe, Goéland, Sanglier : vie et attaque propres).

## 2. Rendu

```bash
Tools/capture.sh /chemin/des/captures
```

Attendu : carte (installation neuve et progression avancée), départ de chaque ville, ligne d'arrivée,
écran NIVEAU RÉUSSI avec nouvelle figure, les 4 boss, le profil onglet FIGURES.

## 3. Parcours sur téléphone

| Étape | Attendu |
|---|---|
| Installation neuve, JOUER | carte, seul Anglet ouvert |
| Jouer Anglet, toucher en l'air | ollie seulement, pas de figure |
| Passer l'arrivée | NIVEAU RÉUSSI, « NOUVELLE FIGURE : FLIP » ; NIVEAU SUIVANT lance Biarritz |
| Chuter à Biarritz | RECOMMENCER relance Biarritz du début ; CARTE : Biarritz toujours ouvert, non réussi |
| Niveau 3 | le Crabe arrive à la fin ; le battre réussit le niveau, annonce SHOVE-IT |
| INFINI pendant 2 min | la vitesse monte nettement ; décor d'Anglet (et des villes ouvertes) |
| Fermer et rouvrir l'app | carte et figures retrouvées |
