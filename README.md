# Carrezance Support

Version actuelle : **v1.3.4**

Carrezance Support est une application Windows portable d'assistance technique pour utilisateurs non techniques.

Elle permet de réaliser un diagnostic simple du poste, d'appliquer quelques réparations courantes et de générer des rapports support exploitables par un technicien.

## Fonctionnalités V1

- Accueil avec informations rapides du poste.
- Diagnostic système : Windows, réseau, disque, mémoire, dernier démarrage et uptime.
- Internet & Réseau : tests ping, test DNS, vidage du cache DNS.
- Imprimantes : liste des imprimantes, ouverture des paramètres Windows, redémarrage du spouleur avec confirmation et vérification administrateur.
- Outlook & Office : fermeture d'Outlook après confirmation, lancement en mode sans échec, ouverture Outlook Web et Microsoft 365.
- Nettoyage : analyse disque C: et nettoyage simple du dossier TEMP utilisateur.
- Assistance : copie et export des informations support.
- Mode technicien : raccourcis vers les consoles Windows courantes.
- Journalisation locale dans `CarrezanceSupport.log`.

## Nouveautés V1.1

- Affichage visible de la version de l'application.
- Détection Windows corrigée pour différencier Windows 10, Windows 11 et Windows Server selon le build réel.
- Section `À propos` dans l'onglet Assistance.
- Boutons pour ouvrir le dossier du dernier rapport, copier son chemin et ouvrir le dossier des logs.
- Statuts du tableau de bord améliorés pour le disque C: et la connexion Internet.
- Rapport TXT enrichi avec la version de l'application.

## Nouveautés V1.2

- Export d'un rapport HTML professionnel en plus du rapport TXT.
- Boutons pour exporter, ouvrir et copier le chemin du dernier rapport HTML.
- Rapport HTML compatible ouverture locale dans Edge ou Chrome.
- Ajout au diagnostic et aux rapports :
  - antivirus détecté via SecurityCenter2 si disponible ;
  - état BitLocker du disque C: ;
  - présence OneDrive ;
  - résumé Domaine AD / Workgroup ;
  - état Azure AD Join via `dsregcmd /status` si disponible ;
  - logiciels importants détectés : Microsoft Office / Microsoft 365, Outlook classique, Nouveau Outlook, Courrier / Calendrier Windows, Microsoft Teams, AnyDesk, TeamViewer, AutoCAD, Sage, Ciel, Google Chrome, Microsoft Edge, Mozilla Firefox, Opera.
- Gestion d'erreur renforcée : les informations indisponibles sont affichées comme `Non disponible`.

## Correctifs V1.2.1

- Démarrage accéléré : l'application ne lance plus le diagnostic complet au chargement.
- Diagnostic rapide séparé du diagnostic complet.
- Les informations avancées affichent `Non analysé` tant qu'un diagnostic complet n'a pas été lancé.
- Boutons de diagnostic rendus asynchrones pour éviter le gel de l'interface.
- Statut visible pendant l'analyse : lecture système, analyse réseau, sécurité, identité, logiciels, synthèse.
- Timeouts ajoutés :
  - antivirus / SecurityCenter2 : 2 secondes ;
  - BitLocker : 2 secondes ;
  - Azure AD Join : 3 secondes ;
  - ping et DNS : 1 seconde par test ;
  - logiciels importants : 2 secondes.
- Journalisation de la durée des blocs diagnostic : système, réseau, sécurité, identité et logiciels.

## Correctifs V1.2.2

- Correction du crash possible lors du clic sur `Lancer un diagnostic rapide`.
- Capture des exceptions dans les commandes asynchrones.
- Gestion globale des erreurs non capturées :
  - `DispatcherUnhandledException` ;
  - `AppDomain.CurrentDomain.UnhandledException` ;
  - `TaskScheduler.UnobservedTaskException`.
- Ajout d'une barre de progression dans l'Accueil et la page Diagnostic.
- Ajout d'un message d'étape courante pendant les diagnostics.
- Ajout d'un message d'erreur visible si une étape échoue.
- Le diagnostic rapide reste limité aux informations rapides et au test Internet simple.
- Les erreurs de diagnostic sont journalisées avec message et stack trace dans `CarrezanceSupport.log`.

## Améliorations V1.2.3

- Mise en avant du rapport HTML comme format principal.
- Ajout dans `Diagnostic` des boutons :
  - `Exporter rapport HTML` ;
  - `Ouvrir le dernier rapport HTML` ;
  - `Copier le chemin du dernier rapport HTML`.
- Le rapport TXT reste disponible comme format technique secondaire.
- Rapport HTML renommé : `Rapport de diagnostic - Carrezance Support`.
- Ajout dans le rapport HTML de la mention `Rapport généré localement depuis Carrezance Support.`
- Ajout d'un badge indiquant le type de diagnostic : rapide ou complet.
- Affichage plus neutre des logiciels importants : `Détecté`, `Non détecté`, `Non analysé`, `Non disponible`.

## Correctifs V1.2.4

- Correction de la cohérence `Domaine / Workgroup`.
- La valeur `Domaine / Workgroup` utilise maintenant `Win32_ComputerSystem.PartOfDomain`, `Domain` et `Workgroup`.
- Le nom du poste n'est plus utilisé comme domaine ou workgroup de secours.
- Rapport TXT et HTML alignés sur la même logique d'identité machine.
- Détection Microsoft Office / Microsoft 365 améliorée :
  - liste des applications installées ;
  - clés Click-to-Run ;
  - dossiers `Program Files` et `Program Files (x86)`.
- Détection Outlook améliorée :
  - App Paths ;
  - clés 32/64 bits ;
  - chemins Office Click-to-Run courants.

## Améliorations V1.2.5

- Suppression de RustDesk de la liste des logiciels importants affichés dans l'interface et les rapports.
- Ajout de la détection des logiciels de production :
  - AutoCAD / Autodesk AutoCAD / AutoCAD LT ;
  - Sage, Sage 50, Sage 100, Sage Comptabilité, Sage Gestion Commerciale, Sage Paie et Sage Batigest ;
  - Ciel, Ciel Compta, Ciel Gestion Commerciale, Ciel Paye et Sage Ciel.
- Liste logiciels alignée entre Diagnostic, rapport TXT et rapport HTML.
- Affichage neutre des logiciels absents : `Non détecté` ne déclenche pas de statut d'alerte.
- Timeout global de détection logiciels conservé pour préserver la fluidité du diagnostic complet.

## Correctifs V1.2.6

- Fiabilisation de la détection des logiciels importants avec un modèle dédié :
  - statut : `Détecté`, `Non détecté`, `Non analysé`, `Non disponible` ;
  - source de détection ;
  - chemin ou identifiant détecté ;
  - niveau de confiance.
- Ajout de la détection du nouveau Outlook Microsoft Store / Appx, en plus de l'Outlook classique Microsoft Office.
- Correction des faux positifs Ciel : un simple dossier Sage ou ProgramData ne suffit plus.
- Correction des faux positifs Sage : un dossier générique ProgramData ne suffit plus.
- Ajout des navigateurs Mozilla Firefox et Opera à la liste des logiciels importants.
- Rapport HTML et rapport TXT enrichis avec la source de détection quand un logiciel est trouvé.
- Journalisation de chaque logiciel détecté avec son statut, sa source, son chemin/ID et sa confiance.

## Correctifs V1.2.7

- Correction du faux positif Ciel provoqué par des noms contenant le mot `Logiciel`, comme `NVIDIA Logiciel système PhysX`.
- La détection Ciel utilise maintenant un matching strict du token `Ciel` et ne détecte plus ce texte à l'intérieur d'un autre mot.
- Les correspondances partielles invalides sont journalisées avec le nom ignoré.
- Fiabilisation du matching logiciel sans impact sur AutoCAD, Chrome, Edge, Opera, AnyDesk ou TeamViewer.

## Améliorations V1.2.8

- Clarification de la détection Outlook en trois lignes distinctes :
  - `Outlook classique` pour Microsoft Office / OUTLOOK.EXE ;
  - `Nouveau Outlook` pour Microsoft.OutlookForWindows ;
  - `Courrier / Calendrier Windows` pour Microsoft.WindowsCommunicationsApps.
- Microsoft.WindowsCommunicationsApps n'est plus considéré comme une preuve directe qu'Outlook est installé.
- Ajout d'une section `Synthèse` en haut du rapport HTML.
- La synthèse affiche le type de diagnostic, l'état global, le nombre de points OK, d'attention et critiques.
- Règles de synthèse :
  - critique : disque C: sous 10 Go, réseau/DNS absent, test Internet échoué, antivirus non disponible après diagnostic complet ;
  - attention : disque C: entre 10 et 20 Go, BitLocker désactivé/non disponible, poste en Workgroup, Azure AD Join non actif, Office absent, aucun outil de prise en main détecté ;
  - OK : disque OK, Internet OK, antivirus détecté, navigateur détecté, rapport généré.
- Les logiciels métier optionnels AutoCAD, Sage et Ciel ne déclenchent pas d'attention s'ils sont absents.

## Améliorations V1.2.9

- Nommage plus clair des rapports HTML :
  `CarrezanceSupport_Rapport_<NomPC>_<Utilisateur>_<yyyyMMdd_HHmmss>.html`.
- Ajout d'un ID de rapport au format `CRZ-YYYYMMDD-HHMMSS-NOMPC`.
- L'ID de rapport est affiché dans le rapport HTML, le rapport TXT et les informations support copiées.
- En-tête du rapport HTML enrichi : version, ID, type de diagnostic, date, poste et utilisateur.
- Synthèse HTML rendue plus lisible avec badge d'état global et message dédié si aucun point d'attention majeur n'est détecté.
- Section `Conclusion technicien` ajoutée en bas du rapport HTML.
- Tableau `Logiciels importants` amélioré avec colonnes : logiciel, statut, source, chemin/ID et confiance.
- Ajout d'un style `@media print` pour une impression ou un export PDF plus propre depuis le navigateur.

## Nouveautés V1.3.0

- Ajout des actions de réparation guidées avec confirmation, retour visuel, logs et historique.
- Réparations rapides disponibles depuis l'Accueil :
  - réparer l'accès aux sites web ;
  - réparer l'impression ;
  - fermer Outlook bloqué ;
  - nettoyage simple.
- Internet & Réseau :
  - vidage du cache DNS Windows uniquement ;
  - aucun reset Winsock ou reset IP en V1.3.
- Imprimantes :
  - redémarrage du spouleur d'impression ;
  - vidage sécurisé de la file d'attente dans `C:\Windows\System32\spool\PRINTERS`.
- Outlook & Office :
  - fermeture guidée d'Outlook bloqué ;
  - lancement du mode sans échec uniquement si Outlook classique est détecté.
- Nettoyage :
  - nettoyage limité au dossier TEMP de l'utilisateur courant ;
  - aucun document personnel, navigateur, profil complet ou dossier système sensible n'est nettoyé.
- Ajout d'un historique des actions dans l'interface, le rapport HTML et le rapport TXT.
- Actions nécessitant les droits administrateur :
  - réparer l'impression ;
  - vider la file d'attente impression.
- Les actions annulées, réussies ou échouées sont journalisées.

## Correctifs V1.3.1

- Correction de l'encodage des sorties console Windows pour les commandes comme `ipconfig /flushdns`.
- Fiabilisation des logs d'actions : chaque action écrit un début, une fin, un statut, une durée et un message.
- `Fermer Outlook bloqué` ajoute maintenant une fin de log et une entrée d'historique même si aucun processus Outlook n'est lancé.
- Les actions nécessitant les droits administrateur non accordés sont classées `Non exécuté` dans l'historique.
- Correction du résumé de nettoyage TEMP :
  - fichiers supprimés ;
  - dossiers supprimés ;
  - éléments ignorés ;
  - erreurs ignorées ;
  - espace réellement libéré après suppression réussie.
- Gestion propre des exceptions natives `DllNotFoundException` observées pendant la fermeture normale de l'application.

## Améliorations V1.3.2

- Ajout d'une relance guidée en administrateur pour les actions qui le nécessitent.
- Actions concernées :
  - `Réparer l'impression` ;
  - `Vider la file d'attente impression`.
- Si l'application est lancée en utilisateur standard, un message propose clairement `Relancer en administrateur` ou `Annuler`.
- La relance utilise l'UAC Windows avec le verbe `runas` et ne contourne jamais les protections système.
- L'instance actuelle se ferme uniquement après lancement réussi de la nouvelle instance administrateur.
- Les refus ou annulations UAC sont journalisés et ajoutés à l'historique comme `Non exécuté`.
- Ajout de l'indicateur `Mode actuel : Utilisateur standard / Administrateur` dans l'interface.
- Ajout du `Mode d'exécution` dans le rapport HTML et le rapport TXT.

## Correctifs V1.3.3

- Correction du crash WPF lors de la demande de relance administrateur.
- Remplacement du dialogue personnalisé par une `MessageBox` standard Oui/Non plus robuste.
- Suppression de l'usage de `RegisterName`, `NameScope` et des noms dynamiques dans le dialogue d'élévation.
- Protection ajoutée autour de l'ouverture du dialogue administrateur : en cas d'échec, l'action est journalisée et l'application continue.
- Amélioration du libellé nettoyage TEMP pour afficher `0 octet` au lieu de `0 o`.

## Correctifs V1.3.4

- Amélioration de la lisibilité des logs.
- Réduction des logs redondants `Bloc système terminé` lors du chargement des vues.
- Le diagnostic rapide écrit maintenant une fin claire : `Diagnostic rapide terminé en X ms`.
- Le diagnostic complet conserve les logs par bloc utiles : système, sécurité, identité et logiciels.
- Harmonisation de plusieurs catégories de logs : `[Diagnostic]`, `[Action]`, `[Admin]`, `[Détection logiciel]`, `[Rapport]`.
- Correction du libellé de nettoyage TEMP avec une formulation stable : `espace libéré : 0 octet`.

## Prérequis développeur

- Windows 10 ou Windows 11 x64.
- SDK .NET 8 installé, avec workload Windows Desktop/WPF.
- Visual Studio 2022 ou la CLI `dotnet`.

Vérification :

```powershell
dotnet --list-sdks
```

## Commande de build

Depuis le dossier `Carrezance.Support` :

```powershell
dotnet build .\Carrezance.Support.sln -c Release -p:Platform=x64
```

## Commande de publication

```powershell
dotnet publish .\Carrezance.Support.App\Carrezance.Support.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false
```

## Emplacement de l'exécutable final

Après publication :

```text
Carrezance.Support\Carrezance.Support.App\bin\Release\net8.0-windows\win-x64\publish\Carrezance Support.exe
```

Le fichier `Carrezance Support.exe` est prévu pour être transmis directement au client, sans installateur.

## Logs et rapports

- Log local : `CarrezanceSupport.log`, dans le dossier de l'exécutable.
- Rapports TXT : dossier Documents de l'utilisateur.
- Rapports HTML : dossier Documents de l'utilisateur.
- Les derniers rapports générés peuvent être ouverts ou copiés depuis l'onglet Assistance.

Format du log :

```text
[Date Heure] [Utilisateur] [Action] [Résultat]
```

## Sécurité

- Aucune donnée d'identification n'est stockée.
- Aucun script téléchargé depuis Internet n'est exécuté.
- Les actions sensibles demandent confirmation.
- Les actions nécessitant les droits administrateur vérifient le niveau de droits avant exécution.
- Les erreurs sont capturées et affichées avec un message simple.

## Procédure de test V1.2

- Lancer `Carrezance Support.exe` sans installation.
- Vérifier que la barre latérale affiche `Carrezance Support v1.3.4`.
- Vérifier l'Accueil : nom du poste, utilisateur, Windows, disque C: et statut Internet.
- Vérifier que l'application s'ouvre rapidement sans attendre l'antivirus, BitLocker, Azure AD ou les logiciels.
- Cliquer sur `Lancer un diagnostic rapide` et vérifier que le statut passe immédiatement à `Diagnostic en cours...`.
- Vérifier que la barre de progression passe par les étapes : initialisation, lecture système, lecture réseau, test Internet, mise à jour du rapport, terminé.
- Vérifier que l'interface reste utilisable pendant l'analyse.
- Vérifier que Windows 11 est bien affiché comme Windows 11 sur un poste Windows 11.
- Aller dans `Diagnostic` et vérifier que les informations avancées sont à `Non analysé` avant diagnostic complet.
- Cliquer sur `Lancer le diagnostic` et vérifier qu'il reste rapide.
- Cliquer sur `Lancer un diagnostic complet` et vérifier les nouvelles informations : antivirus, BitLocker, OneDrive, Domaine AD, Azure AD Join et logiciels importants.
- Vérifier que le diagnostic complet affiche aussi une progression claire.
- Exporter un rapport TXT et vérifier qu'il reste lisible.
- Exporter un rapport HTML depuis `Diagnostic` ou `Assistance`.
- Depuis `Diagnostic`, tester `Ouvrir le dernier rapport HTML` avant export : un message clair doit s'afficher.
- Depuis `Diagnostic`, tester `Copier le chemin du dernier rapport HTML` avant export : un message clair doit s'afficher.
- Ouvrir le rapport HTML localement dans Edge ou Chrome.
- Vérifier le titre du rapport HTML : `Rapport de diagnostic - Carrezance Support`.
- Vérifier la mention : `Rapport généré localement depuis Carrezance Support.`
- Vérifier le badge du type de diagnostic.
- Vérifier que le rapport HTML contient les sections système, réseau, disque, mémoire, sécurité, identité, logiciels importants, tests Internet et actions récentes.
- Vérifier les badges visuels OK / Attention / Critique dans le rapport HTML.
- Vérifier que les logiciels absents affichent `Non détecté` sans alerte inutile.

## Checklist de validation terrain V1.2.4

- Sur un poste hors domaine, vérifier que `Domaine / Workgroup` affiche `Workgroup : WORKGROUP` ou le nom réel du workgroup.
- Sur un poste joint à un domaine AD, vérifier que `Domaine / Workgroup` affiche `Domaine AD : <nom du domaine>`.
- Vérifier que `Domaine / Workgroup` n'affiche pas le nom du poste en session locale.
- Vérifier que `Domaine AD` reste séparé et lisible dans le rapport HTML.
- Lancer un diagnostic rapide et exporter un rapport HTML.
- Lancer un diagnostic complet et exporter un rapport HTML.
- Vérifier dans les deux rapports que les champs non analysés affichent `Non analysé`.
- Vérifier sur un poste Microsoft 365 Click-to-Run que Office et Outlook sont détectés si présents.
- Vérifier que l'application reste fluide pendant le diagnostic complet.
- Vérifier que les boutons HTML de Diagnostic et Assistance fonctionnent.
- Dans `Assistance`, tester `Ouvrir le dernier rapport HTML` avant export : un message clair doit s'afficher.
- Après export HTML, tester l'ouverture du dernier rapport HTML et la copie de son chemin.
- Tester `Ouvrir le dossier des logs`.
- Vérifier que les informations indisponibles apparaissent comme `Non disponible`.
- Vérifier que les informations avancées non lancées apparaissent comme `Non analysé`.
- Vérifier dans le log les lignes de durée des blocs diagnostic.
- Vérifier que l'interface reste cohérente : bleu nuit, blanc, gris clair.

## Procédure de test rapide V1.2.5

- Lancer `Carrezance Support.exe` et vérifier que la version affichée est `v1.2.5`.
- Lancer un diagnostic rapide et vérifier que les logiciels avancés restent à `Non analysé`.
- Lancer un diagnostic complet et vérifier la section `Logiciels importants`.
- Vérifier que RustDesk n'apparaît plus dans Diagnostic, le rapport TXT ou le rapport HTML.
- Vérifier que la liste contient : Microsoft Office / Microsoft 365, Outlook, Microsoft Teams, AnyDesk, TeamViewer, AutoCAD, Sage, Ciel, Google Chrome et Microsoft Edge.
- Vérifier qu'un logiciel absent affiche `Non détecté` sans badge ou statut d'alerte.
- Sur un poste équipé, vérifier qu'AutoCAD, Sage ou Ciel passent bien à `Détecté`.
- Exporter un rapport HTML et vérifier que l'ordre des logiciels est identique à celui de l'interface.
- Exporter un rapport TXT et vérifier que la section logiciels utilise la même liste.
- Vérifier que le diagnostic complet reste fluide malgré la détection logiciels.

## Procédure de test rapide V1.2.6

- Lancer `Carrezance Support.exe` et vérifier que la version affichée est `v1.2.6`.
- Lancer un diagnostic rapide et vérifier que les logiciels restent à `Non analysé`.
- Lancer un diagnostic complet et vérifier que la section `Logiciels importants` contient Firefox et Opera.
- Sur un poste avec le nouveau Outlook, vérifier que `Outlook` est `Détecté` avec une source `Microsoft Store / Appx`.
- Sur un poste sans Ciel, vérifier que `Ciel` reste `Non détecté`, même si un dossier Sage générique existe.
- Vérifier que les logiciels détectés affichent une source et un chemin ou identifiant dans le rapport HTML.
- Exporter un rapport TXT et vérifier que la section logiciels affiche aussi source, chemin/ID et confiance pour les logiciels détectés.
- Vérifier que `Non détecté` reste neutre et ne déclenche pas d'alerte visuelle.
- Vérifier dans `CarrezanceSupport.log` que les détections logiciels sont journalisées.

## Procédure de test rapide V1.2.7

- Lancer `Carrezance Support.exe` et vérifier que la version affichée est `v1.2.7`.
- Lancer un diagnostic complet sur un poste avec `NVIDIA Logiciel système PhysX`.
- Vérifier que `Ciel` affiche `Non détecté` si Ciel n'est pas installé.
- Vérifier que le rapport HTML ne mentionne plus NVIDIA PhysX comme source de Ciel.
- Vérifier dans `CarrezanceSupport.log` la ligne `Ciel ignoré : correspondance partielle non valide`.
- Vérifier qu'un vrai nom comme `Ciel Compta`, `Sage Ciel` ou `Ciel Gestion Commerciale` reste détectable.

## Procédure de test rapide V1.2.8

- Lancer `Carrezance Support.exe` et vérifier que la version affichée est `v1.2.8`.
- Lancer un diagnostic complet et vérifier les lignes `Outlook classique`, `Nouveau Outlook` et `Courrier / Calendrier Windows`.
- Vérifier que Microsoft.WindowsCommunicationsApps apparaît uniquement comme `Courrier / Calendrier Windows`.
- Exporter un rapport HTML et vérifier la présence de la carte `Synthèse` en haut du rapport.
- Vérifier que la synthèse affiche l'état global et les compteurs OK / Attention / Critique.
- Vérifier qu'un diagnostic rapide indique que certaines vérifications avancées n'ont pas été lancées.
- Vérifier qu'AutoCAD, Sage et Ciel absents ne créent pas d'attention.
- Vérifier dans `CarrezanceSupport.log` la ligne de génération de synthèse avec l'état global calculé.

## Procédure de test rapide V1.2.9

- Lancer `Carrezance Support.exe` et vérifier que la version affichée est `v1.2.9`.
- Exporter un rapport HTML et vérifier le nom du fichier : `CarrezanceSupport_Rapport_<NomPC>_<Utilisateur>_<yyyyMMdd_HHmmss>.html`.
- Vérifier que l'en-tête HTML affiche la version, l'ID de rapport, le type de diagnostic, la date, le poste et l'utilisateur.
- Vérifier que le rapport TXT contient aussi l'ID de rapport.
- Utiliser `Copier les informations support` et vérifier la présence de la version, de l'ID et du chemin du dernier rapport HTML si disponible.
- Vérifier que la section `Logiciels importants` est affichée en colonnes.
- Vérifier la présence de la section `Conclusion technicien`.
- Depuis le navigateur, tester l'impression ou l'export PDF et vérifier que les cartes ne sont pas coupées de manière gênante.

## Procédure de test V1.3

- Lancer `Carrezance Support.exe` et vérifier que la version affichée est `v1.3.0`.
- Depuis l'Accueil, tester les boutons de réparations rapides et annuler chaque confirmation : l'historique doit indiquer `Annulé`.
- Exécuter `Réparer l'accès aux sites web` : vérifier le message de succès et l'entrée dans l'historique.
- Sans droits administrateur, tester `Réparer l'impression` : le message de droits administrateur doit s'afficher.
- En administrateur, tester `Réparer l'impression` et `Vider la file d'attente impression`.
- Tester `Fermer Outlook bloqué` avec Outlook fermé puis ouvert.
- Tester `Lancer Outlook en mode sans échec` sur un poste avec et sans Outlook classique.
- Tester `Nettoyage simple` et vérifier que seul le dossier TEMP utilisateur est ciblé.
- Exporter un rapport HTML et vérifier la section `Historique des actions`.
- Exporter un rapport TXT et vérifier que l'historique y est aussi présent.

## Procédure de test V1.3.1

- Lancer `Réparer l'accès aux sites web` et vérifier que le log affiche correctement `Cache de résolution DNS vidé`.
- Fermer Outlook bloqué avec Outlook fermé et vérifier l'historique `Non exécuté`.
- Lancer `Nettoyage simple` et vérifier la cohérence entre fichiers/dossiers supprimés et espace libéré.
- Tester `Réparer impression` sans administrateur et vérifier l'historique `Non exécuté`.
- Fermer l'application et vérifier qu'aucune fausse erreur critique native n'est loguée à la fermeture.

## Procédure de test V1.3.2

- Lancer `Carrezance Support.exe` en utilisateur standard.
- Vérifier l'indicateur `Mode actuel : Utilisateur standard` dans la barre latérale.
- Cliquer sur `Réparer l'impression` et vérifier le message proposant `Relancer en administrateur` ou `Annuler`.
- Cliquer sur `Annuler` et vérifier l'historique : `Non exécuté`, `Droits administrateur requis, relance annulée par l'utilisateur`.
- Recommencer puis cliquer sur `Relancer en administrateur`.
- Refuser l'UAC et vérifier l'historique : `Non exécuté`, `Relance administrateur refusée ou annulée`.
- Accepter l'UAC et vérifier que la nouvelle instance affiche `Mode actuel : Administrateur`.
- En administrateur, tester `Réparer l'impression` et `Vider la file d'attente impression`.
- Exporter un rapport HTML et vérifier la ligne `Mode d'exécution`.
- Vérifier dans `CarrezanceSupport.log` les traces de relance administrateur proposée, acceptée, refusée ou annulée.

## Procédure de test V1.3.3

- Lancer `Carrezance Support.exe` sans droits administrateur.
- Cliquer sur `Réparer l'impression` depuis l'Accueil : le dialogue administrateur doit s'afficher sans crash.
- Cliquer sur `Non` et vérifier l'historique `Non exécuté`.
- Cliquer sur `Réparer l'impression` depuis l'onglet Imprimantes et répéter le test.
- Cliquer sur `Vider la file d'attente impression` sans droits admin et vérifier le même comportement.
- Cliquer sur `Oui`, refuser l'UAC et vérifier que l'application reste ouverte avec l'historique `Relance administrateur refusée ou annulée`.
- Lancer `Nettoyage simple` sur un TEMP vide ou verrouillé et vérifier que `0 octet` est affiché correctement.

## Procédure de test V1.3.4

- Lancer l'application et vérifier que le chargement ne génère pas plusieurs lignes `Bloc système terminé`.
- Lancer un diagnostic rapide depuis l'Accueil et vérifier une seule ligne finale `Diagnostic rapide terminé en X ms`.
- Lancer un diagnostic complet depuis `Diagnostic` et vérifier les blocs utiles dans les logs.
- Lancer `Nettoyage simple` et vérifier que le message affiche `espace libéré : 0 octet` si rien n'a été supprimé.
- Exporter un rapport HTML et vérifier que l'historique reprend le même libellé de nettoyage.

## Prochaines améliorations

- Ajouter un logo et une icône applicative définitifs dans `Assets`.
- Ajouter un mode technicien protégé par code ou mot de passe local.
- Ajouter une configuration pour le site Carrezance, l'email support et l'outil de télémaintenance.
- Ajouter un export PDF optionnel.
- Ajouter des tests unitaires sur les services sans dépendance UI.
- Ajouter une signature de code pour rassurer Windows SmartScreen.
- Ajouter une page dédiée à l'historique complet des actions.
