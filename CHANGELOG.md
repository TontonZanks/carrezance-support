# Changelog

Toutes les modifications notables de Carrezance Support seront documentées ici.

Utiliser ce format pour les futures versions :

## vX.X.X - YYYY-MM-DD

### Ajouté
- ...

### Modifié
- ...

### Corrigé
- ...

### Sécurité
- ...

### Technique
- ...

## v1.3.4 - 2026-05-28

### Ajouté
- Diagnostic rapide et diagnostic complet.
- Rapport HTML professionnel avec synthèse, ID de rapport et conclusion technicien.
- Détection des logiciels importants avec statut, source, chemin/ID et confiance.
- Actions de réparation guidées : DNS, impression, Outlook et nettoyage TEMP.
- Historique des actions dans l'interface, le rapport HTML et le rapport TXT.
- Relance en administrateur via UAC pour les actions nécessitant des droits élevés.

### Modifié
- Amélioration de la lisibilité des logs.
- Séparation claire entre Outlook classique, Nouveau Outlook et Courrier / Calendrier Windows.
- Amélioration du tableau des logiciels importants dans le rapport HTML.
- Amélioration du nommage des rapports HTML.

### Corrigé
- Correction de la détection Windows 10 / Windows 11.
- Correction du faux positif Ciel lié au mot "Logiciel".
- Correction des crashs liés aux bindings WPF et aux mises à jour de collections hors thread UI.
- Correction du dialogue de relance administrateur.
- Correction de l'encodage des sorties console Windows.
- Correction des compteurs du nettoyage TEMP.

### Sécurité
- Aucune action système sensible sans confirmation.
- Vérification des droits administrateur avant réparation impression et vidage file impression.
- Nettoyage limité au dossier TEMP utilisateur.
- Aucun identifiant ou mot de passe stocké.

### Technique
- Application WPF .NET 8 win-x64.
- Publication self-contained single-file.
- Architecture MVVM avec services separes.
- Logs locaux non bloquants.
- Procédure de release enrichie avec génération de SHA256 pour l'EXE et le ZIP.
