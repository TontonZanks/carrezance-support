# Procédure de release - Carrezance Support

Ce document décrit la procédure standard pour préparer, construire et publier une version stable de Carrezance Support.

## Workflow Git recommandé

Branches :

- `main` : branche stable / production.
- `develop` : branche de test / intégration.

Règle générale :

- travailler sur `develop` ;
- lancer un build Release x64 ;
- tester localement ;
- committer sur `develop` ;
- pousser `develop` ;
- valider manuellement ;
- merger `develop` vers `main` ;
- créer le tag depuis `main` ;
- publier la GitHub Release depuis `main`.

Créer `develop` si nécessaire :

```powershell
git checkout -b develop
git push -u origin develop
```

Basculer sur `develop` :

```powershell
git checkout develop
```

Commit sur `develop` :

```powershell
git add .
git commit -m "..."
git push origin develop
```

Merge validé vers `main` :

```powershell
git checkout main
git pull origin main
git merge develop
git push origin main
```

Créer le tag :

```powershell
git tag vX.X.X
git push origin vX.X.X
```

## Prérequis développeur

- Windows 10 ou Windows 11 x64.
- SDK .NET 8 installé.
- Workload Windows Desktop/WPF disponible.
- Git configuré.
- Accès au dépôt GitHub.

Vérification :

```powershell
dotnet --list-sdks
git status
```

## Vérifier la version

Avant de publier, vérifier que la version est cohérente dans :

- `Carrezance.Support.App\Helpers\AppInfo.cs`
- `Carrezance.Support.App\Carrezance.Support.App.csproj`
- `README.md`
- `CHANGELOG.md`

Exemple attendu pour v1.3.4 :

```text
1.3.4
```

## Build Release

Depuis le dossier `Carrezance.Support` :

```powershell
dotnet build .\Carrezance.Support.sln -c Release -p:Platform=x64
```

Le build doit se terminer avec :

```text
0 Avertissement(s)
0 Erreur(s)
```

## Publication portable

Depuis le dossier `Carrezance.Support` :

```powershell
dotnet publish .\Carrezance.Support.App\Carrezance.Support.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false
```

Chemin de l'exécutable généré :

```text
Carrezance.Support.App\bin\Release\net8.0-windows\win-x64\publish\Carrezance Support.exe
```

## Politique de release

À chaque GitHub Release stable, publier deux assets.

### A. EXE utilisateur

Nom :

```text
CarrezanceSupport-vX.X.X-win-x64.exe
```

Contenu :

- l'exécutable single-file publié `Carrezance Support.exe` ;
- renommé uniquement pour la release en `CarrezanceSupport-vX.X.X-win-x64.exe`.

Usage :

- téléchargement principal pour utilisateur final.
- asset utilisé en priorité par le système de vérification des mises à jour de Carrezance Support.

### B. ZIP technicien

Nom :

```text
CarrezanceSupport-vX.X.X-win-x64.zip
```

Contenu :

- `Carrezance Support.exe`
- `README.md`
- `CHANGELOG.md`
- `RELEASE.md`

Usage :

- archive complète pour technicien ;
- maintenance ;
- archivage.

Ces fichiers ne doivent jamais être committés dans Git. Ils doivent uniquement être attachés aux GitHub Releases.

## Création des assets de release

Méthode recommandée :

```powershell
.\scripts\Create-ReleasePackage.ps1 -Version 1.3.4
```

Sans paramètre, le script lit la version depuis `AppInfo.cs` :

```powershell
.\scripts\Create-ReleasePackage.ps1
```

Le script crée :

```text
artifacts\vX.X.X\CarrezanceSupport-vX.X.X-win-x64.exe
artifacts\vX.X.X\CarrezanceSupport-vX.X.X-win-x64.zip
```

## Vérification du contenu du ZIP

Vérifier que le ZIP contient :

```text
Carrezance Support.exe
README.md
CHANGELOG.md
RELEASE.md
```

Vérifier aussi :

- l'exécutable se lance sans installation ;
- la version affichée dans l'interface est correcte ;
- le diagnostic rapide fonctionne ;
- l'export HTML fonctionne ;
- aucune dépendance externe visible n'est requise.

## Commit Git

Avant commit :

```powershell
git status
git diff
```

Commit recommandé sur `develop` :

```powershell
git add .
git commit -m "Prepare release vX.X.X"
git push origin develop
```

Ne merger vers `main` qu'après validation manuelle.

## Tag Git

Le tag doit être créé uniquement après merge validé sur `main` :

```powershell
git checkout main
git pull origin main
git tag vX.X.X
git push origin vX.X.X
```

## Publication GitHub Release

Dans l'onglet `Releases` du dépôt GitHub :

- créer une nouvelle release ;
- choisir le tag `vX.X.X` ;
- titre recommandé : `Carrezance Support vX.X.X` ;
- joindre `CarrezanceSupport-vX.X.X-win-x64.exe` ;
- joindre `CarrezanceSupport-vX.X.X-win-x64.zip` ;
- copier les points importants depuis `CHANGELOG.md`.

Les releases GitHub sont créées uniquement depuis `main`.

## Fichiers à ne jamais committer

Ne pas committer :

- `bin/`
- `obj/`
- `publish/`
- `release/`
- `artifacts/`
- `*.zip`
- `CarrezanceSupport.log`
- rapports TXT ou HTML générés localement ;
- exécutables ou ZIP de release.

Vérifier que `.gitignore` couvre ces fichiers avant chaque release.

## Notes futures pour le système de mise à jour

Pour une future mise à jour automatique ou semi-automatique, prévoir :

- un manifeste de version publié avec la release ;
- une signature ou un hash SHA256 du ZIP et de l'EXE ;
- une vérification de signature avant exécution ;
- aucune exécution de script distant ;
- un canal stable et un canal test si nécessaire ;
- une documentation claire pour les techniciens.
