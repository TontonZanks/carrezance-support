# Carrezance Support

Version actuelle : **v1.4.0**

Carrezance Support est une application Windows portable d'assistance, de diagnostic et de reparation simple pour des utilisateurs non techniques.

L'application se lance directement depuis `Carrezance Support.exe`, sans installateur et sans prerequis visible cote client.

## Fonctionnalites principales

- Tableau de bord avec informations rapides : poste, utilisateur, version Windows, disque C: et etat Internet.
- Diagnostic rapide pour les informations essentielles.
- Diagnostic complet avec reseau, securite, identite machine et logiciels importants.
- Detection Windows 10 / Windows 11 / Windows Server via une methode robuste.
- Detection des logiciels importants avec statut, source, chemin/ID et niveau de confiance.
- Mode technicien avec raccourcis vers les consoles Windows courantes.
- Vérification des mises à jour stables via GitHub Releases.
- Journal local `CarrezanceSupport.log`.

## Actions de reparation

Les actions guidees demandent confirmation et ajoutent une entree dans l'historique :

- reparer l'acces aux sites web avec vidage du cache DNS ;
- reparer l'impression avec redemarrage du spouleur ;
- vider la file d'attente impression ;
- fermer Outlook bloque ;
- lancer Outlook classique en mode sans echec si disponible ;
- nettoyer uniquement le dossier TEMP de l'utilisateur courant.

Les actions d'impression qui necessitent des droits eleves proposent une relance en administrateur via l'UAC Windows.

## Mises à jour

Carrezance Support vérifie en arrière-plan si une nouvelle version stable est disponible sur GitHub Releases :

```text
https://github.com/TontonZanks/carrezance-support/releases
```

En v1.4.0, l'application propose seulement de consulter ou télécharger la mise à jour. Elle ne télécharge pas silencieusement, ne remplace pas l'exécutable et ne lance aucun updater automatique.

## Rapports

Carrezance Support peut generer :

- un rapport HTML professionnel, format principal pour l'utilisateur et le technicien ;
- un rapport TXT technique secondaire.

Le rapport HTML contient notamment :

- un ID de rapport ;
- une synthese OK / Attention / Critique ;
- les informations systeme, reseau, disque et memoire ;
- les informations de securite et d'identite ;
- les logiciels importants detectes ;
- l'historique des actions ;
- une section `Conclusion technicien`.

## Securite

- Aucune donnee sensible ni mot de passe n'est stocke.
- Aucun script telecharge depuis Internet n'est execute.
- Aucune action systeme sensible n'est lancee sans confirmation.
- Les droits administrateur sont verifies avant les actions d'impression.
- Le nettoyage est limite au dossier TEMP utilisateur.
- Les logs sont locaux et ne doivent jamais bloquer l'application.

## Build developpeur

Prerequis :

- Windows 10 ou Windows 11 x64 ;
- SDK .NET 8 ;
- workload Windows Desktop/WPF.

Depuis le dossier `Carrezance.Support` :

```powershell
dotnet build .\Carrezance.Support.sln -c Release -p:Platform=x64
```

## Publication

Depuis le dossier `Carrezance.Support` :

```powershell
dotnet publish .\Carrezance.Support.App\Carrezance.Support.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false
```

Executable final :

```text
Carrezance.Support.App\bin\Release\net8.0-windows\win-x64\publish\Carrezance Support.exe
```

## Documentation complementaire

- [CHANGELOG.md](CHANGELOG.md) : historique des versions et modifications notables.
- [RELEASE.md](RELEASE.md) : procedure de build, publication, ZIP, tag Git et GitHub Release.
