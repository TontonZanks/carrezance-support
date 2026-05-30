# Procédure de release - Carrezance Support

Ce document decrit la procedure standard pour generer une version portable de Carrezance Support et publier une release GitHub.

## Prerequis developpeur

- Windows 10 ou Windows 11 x64.
- SDK .NET 8 installe.
- Workload Windows Desktop/WPF disponible.
- Git configure.
- Acces au depot GitHub.

Verification :

```powershell
dotnet --list-sdks
git status
```

## Verifier la version

Avant de publier, verifier que la version est coherente dans :

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

Chemin de l'executable genere :

```text
Carrezance.Support.App\bin\Release\net8.0-windows\win-x64\publish\Carrezance Support.exe
```

## Creation du dossier de release

Creer un dossier temporaire de release, par exemple :

```powershell
New-Item -ItemType Directory -Force .\release\CarrezanceSupport-v1.3.4-win-x64
Copy-Item '.\Carrezance.Support.App\bin\Release\net8.0-windows\win-x64\publish\Carrezance Support.exe' '.\release\CarrezanceSupport-v1.3.4-win-x64\'
```

Ajouter uniquement les fichiers utiles a l'utilisateur final.

## Creation du ZIP

Exemple :

```powershell
Compress-Archive -Path .\release\CarrezanceSupport-v1.3.4-win-x64\* -DestinationPath .\release\CarrezanceSupport-v1.3.4-win-x64.zip -Force
```

Nom recommande :

```text
CarrezanceSupport-v1.3.4-win-x64.zip
```

## Verification du contenu du ZIP

Verifier que le ZIP contient au minimum :

```text
Carrezance Support.exe
```

Verifier aussi :

- l'executable se lance sans installation ;
- la version affichee dans l'interface est correcte ;
- le diagnostic rapide fonctionne ;
- l'export HTML fonctionne ;
- aucune dependance externe visible n'est requise.

## Commit Git

Avant commit :

```powershell
git status
git diff
```

Commit recommande :

```powershell
git add README.md CHANGELOG.md RELEASE.md Carrezance.Support.App\Carrezance.Support.App.csproj Carrezance.Support.App\Helpers\AppInfo.cs
git commit -m "Release v1.3.4"
git push
```

Adapter la liste des fichiers si d'autres fichiers ont ete modifies pour la version.

## Tag Git

Creer et pousser le tag :

```powershell
git tag v1.3.4
git push origin v1.3.4
```

## Publication GitHub Release

Dans l'onglet `Releases` du depot GitHub :

- creer une nouvelle release ;
- choisir le tag `v1.3.4` ;
- titre recommande : `Carrezance Support v1.3.4` ;
- joindre le fichier `CarrezanceSupport-v1.3.4-win-x64.zip` ;
- copier les points importants depuis `CHANGELOG.md`.

## Fichiers a ne jamais committer

Ne pas committer :

- `bin/`
- `obj/`
- `publish/`
- `release/`
- `CarrezanceSupport.log`
- rapports TXT ou HTML generes localement ;
- ZIP de release, sauf decision explicite contraire.

Verifier que `.gitignore` couvre ces fichiers avant chaque release.

## Notes futures pour le systeme de mise a jour

Pour une future mise a jour automatique ou semi-automatique, prevoir :

- un manifeste de version publie avec la release ;
- une signature ou un hash SHA256 du ZIP ;
- une verification de signature avant execution ;
- aucune execution de script distant ;
- un canal stable et un canal test si necessaire ;
- une documentation claire pour les techniciens.
