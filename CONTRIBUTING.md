# Contribution - Carrezance Support

Ce document définit la méthode de travail du dépôt Carrezance Support.

## Branches

- `main` : branche stable / production.
- `develop` : branche de test / intégration.

La branche `main` doit toujours rester publiable. Elle représente la dernière version validée manuellement.

La branche `develop` reçoit les évolutions, corrections et préparations de release avant validation.

## Règles de travail

- Toutes les évolutions doivent être faites sur `develop`.
- Codex travaille uniquement sur `develop`.
- Ne jamais pousser directement sur `main`, sauf validation finale explicite.
- Faire un build Release x64 avant chaque commit important.
- Tester localement les fonctions modifiées avant de demander un merge.
- Les releases GitHub sont créées uniquement depuis `main`.
- Créer un tag uniquement après validation sur `main`.
- Publier les binaires uniquement dans GitHub Releases, jamais dans le dépôt Git.

## Documentation obligatoire

À chaque nouvelle version, Codex doit maintenir :

- `README.md` si l'usage ou les fonctionnalités visibles changent ;
- `CHANGELOG.md` systématiquement ;
- `RELEASE.md` si la procédure de build ou de release change ;
- la cohérence de version entre `AppInfo.cs`, le `.csproj` et la documentation.

## Build avant commit

Depuis la racine du dépôt :

```powershell
dotnet build .\Carrezance.Support.sln -c Release -p:Platform=x64
```

Le build doit rester à :

```text
0 Avertissement(s)
0 Erreur(s)
```

## Workflow recommandé

Basculer sur `develop` :

```powershell
git checkout develop
```

Créer `develop` si nécessaire :

```powershell
git checkout -b develop
git push -u origin develop
```

Commiter sur `develop` :

```powershell
git add .
git commit -m "Description courte"
git push origin develop
```

Après validation manuelle :

```powershell
git checkout main
git pull origin main
git merge develop
git push origin main
```

Tag de release depuis `main` :

```powershell
git tag vX.X.X
git push origin vX.X.X
```

## Fichiers à ne pas committer

Ne pas committer :

- exécutables générés ;
- ZIP de release ;
- contenu de `artifacts/` ;
- rapports locaux ;
- logs locaux ;
- dossiers `bin/`, `obj/`, `publish/`.

Les binaires doivent être ajoutés uniquement comme assets dans une GitHub Release.
