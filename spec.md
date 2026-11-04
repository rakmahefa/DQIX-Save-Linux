# Spécification — portage Linux natif de DQ9 Save Editor

- **Statut :** P0 à P7 implémentés (noyau portable, tests, services portables, shell Avalonia, 10 onglets métier, recherche de grottes, validation croisée, packaging Debian/AppImage, CI Windows+Linux). Différences résiduelles documentées dans docs/validation-report.md §6
- **Branche :** master
- **Commit de référence :** 79c36e37d3d2d1028f1deb2c8376128dcda0d38f
- **Date :** 2 novembre 2026
- **Documents associés :** [todo.md](todo.md) · [files-audit.md](files-audit.md)

## 1. Objectif

Créer une application Linux native pour éditer les sauvegardes de Dragon Quest IX, sans Wine, sans binaire PE Windows et sans dépendance à Windows Forms.

Le portage doit préserver le format binaire, les offsets, les encodages, le CRC, les fonctions métier et les données de jeu existantes. La stratégie consiste à isoler le moteur métier, le rendre portable et testable, puis à reconstruire l'interface avec Avalonia UI.

Le critère essentiel est de produire et exécuter un binaire ELF Linux x64. Un workflow exécuté sur Ubuntu qui publie un EXE Windows ne constitue pas un portage Linux.

## 2. État initial vérifié

Le dépôt comprend 210 fichiers suivis : 165 fichiers C#, 39 ressources .resx, un projet SDK .NET, une solution, deux binaires historiques et un workflow CI.

Le projet [DQCheat.Patched.0.7.csproj](DQCheat.Patched.0.7.csproj) cible actuellement net8.0-windows, avec OutputType WinExe, UseWindowsForms et ImportWindowsDesktopTargets. [Program.cs](DQ9_Cheat/Program.cs) démarre Windows Forms et [MainWindow.cs](DQ9_Cheat/MainWindow.cs) hérite de Form.

Le workflow [linux-build.yml](.github/workflows/linux-build.yml) s'exécute sur Ubuntu, mais cible win-x64 et active EnableWindowsTargeting. Il vérifie ensuite que DQ9_Cheat.exe est un exécutable PE32+ Windows. Le run [GitHub Actions #3](https://github.com/rakmahefa/Save-Editor-Decomp/actions/runs/41442605530) est vert : il prouve le cross-build Windows sur un runner Linux, pas l'exécution native Linux.

Le résumé du run #3 affiche 9 074 avertissements et zéro erreur. Les avertissements CA1416 sont très nombreux. Ils devront être réévalués après séparation des projets ; ne pas les masquer globalement sans analyse.

## 3. Décisions d'architecture

### 3.1 Interface : Avalonia UI + MVVM

Utiliser Avalonia pour la nouvelle interface multiplateforme, avec Views, ViewModels et Services. Ne pas chercher à exécuter WinForms sous Linux ni à faire croire qu'un simple changement de TargetFramework rendra l'interface portable.

Avalonia prend en charge Linux desktop. X11 est le chemin par défaut ; un backend Wayland natif est disponible en option depuis Avalonia 12.1.0. La version stable compatible avec .NET 10 doit être vérifiée et verrouillée pendant la phase d'amorçage. La première version doit fonctionner dans un environnement Linux desktop pris en charge par la version choisie.

Références :
- https://docs.avaloniaui.net/docs/supported-platforms
- https://docs.avaloniaui.net/docs/platform-specific-guides/linux

### 3.2 Runtime : .NET 10 LTS

Les nouveaux projets doivent cibler net10.0. Le projet Windows Forms historique, s'il reste utilisé pendant la transition, doit être retargeté vers net10.0-windows après validation du build.

Au 2 novembre 2026, .NET 8 arrive en fin de support le 10 novembre 2026 ; .NET 10 LTS est pris en charge jusqu'au 14 novembre 2028. Ne pas bâtir la nouvelle architecture sur .NET 8.

Référence : https://dotnet.microsoft.com/en-us/platform/support/policy

### 3.3 Architecture cible

Arborescence proposée :

    src/
      DQ9.Core/
        SaveFormat/
        DataManager/
        GameData/
        Encoding/
        MapSearch/
        Services/
      DQ9.Avalonia/
        Views/
        ViewModels/
        Controls/
        Services/
        Assets/
      DQ9.WinForms/        (temporaire pendant la transition)
    tests/
      DQ9.Core.Tests/

- **DQ9.Core** : format de sauvegarde, modèles, données de jeu, codecs, calculs et recherche sans dépendance UI ou OS.
- **DQ9.Avalonia** : interface, navigation, ViewModels, commandes et intégration desktop.
- **DQ9.WinForms** : application historique temporaire, conservée comme référence pendant la migration.
- **DQ9.Core.Tests** : tests binaires et métier exécutables sur Linux et Windows.

Une interface de service doit isoler les dépendances de plateforme : sélecteur de fichiers, chemins, préférences et notifications.

Les projets peuvent être introduits progressivement. Ne pas déplacer tout le code dans un seul gros commit. Chaque étape doit rester compilable et réversible.

## 4. Périmètre fonctionnel à préserver

La migration doit conserver :
1. ouverture et validation d'une sauvegarde ;
2. affichage et édition des données générales du joueur ;
3. édition des personnages, équipements, objets, compétences et inventaires ;
4. gestion des quêtes, flags, titres et données de première victoire ;
5. gestion de l'auberge, visiteurs et Wi-Fi Shopping ;
6. gestion des cartes au trésor et des données associées ;
7. les deux blocs de sauvegarde et les données d'intermission lorsqu'elles existent ;
8. undo / redo et copie de données entre blocs ;
9. sauvegarde, sauvegarde sous un autre nom et mise à jour du CRC ;
10. recherche de cartes, génération / chargement du cache et progression ;
11. les règles d'encodage propres à Dragon Quest IX.

La disposition graphique peut évoluer, mais aucune fonction ne doit être retirée silencieusement. Toute différence de comportement doit être documentée.

## 5. Travaux de portabilité identifiés

### 5.1 Moteur de sauvegarde

Les dossiers DQ9_Cheat/DataManager, DQ9_Cheat/GameData et DQ9_Cheat/DLL sont les premiers candidats à l'extraction vers DQ9.Core.

SaveData.cs déclare une taille de données de 32 768 octets et un header de 16 octets. Les tailles, offsets, ordre des champs, règles d'encodage et CRC sont des invariants du format. Les modifier exige des tests binaires et une justification spécifique.

Ne pas remplacer le format binaire par JSON ou un nouveau format.

### 5.2 Recherche de cartes : WMI et chemins

Dans DQ9_Cheat/MapSearch/SearchDataFile.cs, les éléments suivants ont été observés :
- System.Management et ManagementClass("Win32_Processor") pour obtenir NumberOfCores ;
- System.Windows.Forms et Application.StartupPath pour localiser MapSearch.dat ;
- JS_Framework.Threading.ThreadControl pour exécuter les workers.

Remplacements requis :
- Environment.ProcessorCount comme valeur initiale portable, puis mesure et éventuel plafonnement du parallélisme ;
- injection du répertoire de cache utilisateur ;
- API de génération indépendante de l'interface ;
- progression et annulation explicites.

Le nombre de processeurs logiques peut différer du nombre de cœurs physiques observé via WMI : mesurer la charge et le temps de génération, ne pas supposer que les deux valeurs sont équivalentes.

Le cache MapSearch.dat est dérivé et régénérable. Le placer dans le cache utilisateur, contrôler sa taille / version avant lecture et écrire via fichier temporaire puis remplacement atomique.

### 5.3 Threading historique

JS_Framework/Threading/ThreadControl.cs crée directement des Thread, appelle Thread.SetApartmentState(ApartmentState.STA) pendant Initialize() et expose Thread.Abort().

Cette abstraction ne doit pas être réutilisée dans le Core Linux. Remplacer la recherche par Task / Parallel, CancellationToken, IProgress<T> et des mécanismes synchronisés tels que Interlocked ou les collections concurrentes.

Ne pas simplement enlever l'appel STA : rechercher tous les consommateurs et vérifier leurs hypothèses. Le Core ne doit jamais piloter directement un thread UI ou un contrôle graphique.

### 5.4 Chemins et paramètres

Ne jamais écrire dans le répertoire de l'exécutable ou dans un AppImage monté en lecture seule.

Créer un service qui respecte les variables XDG lorsqu'elles sont définies, avec repli documenté :
- configuration : XDG_CONFIG_HOME ou ~/.config/dq9-save-editor/ ;
- données durables : XDG_DATA_HOME ou ~/.local/share/dq9-save-editor/ ;
- cache régénérable : XDG_CACHE_HOME ou ~/.cache/dq9-save-editor/.

Utiliser AppContext.BaseDirectory uniquement pour les ressources livrées en lecture seule. Les fichiers utilisateur passent par le service de chemins.

Auditer DQ9_Cheat/Properties/Settings.cs et les usages de Settings.Default. Migrer uniquement les paramètres effectivement utilisés, puis retirer l'ancien wrapper.

### 5.5 Interface et ressources

Les 49 fichiers C# de DQ9_Cheat/Controls, les contrôles de JS_Framework/Controls, MainWindow.cs, AboutBox.cs et les 39 ressources .resx constituent le chantier UI principal.

Les contrôles utilisent WinForms, System.Drawing.Graphics, OnPaint, Font, Color, NumericUpDown, RichTextBox, menus, dialogues et composants personnalisés. Les reconstruire dans Avalonia avec XAML, ViewModels et composants réutilisables ; ne pas convertir mécaniquement chaque fichier.

Inspecter les .resx, extraire les images / icônes réellement utilisées et vérifier les polices. Ne pas supposer que les ressources de WinForms sont directement consommables par Avalonia.

## 6. Tests et invariants binaires

Il n'existe pas encore de projet de tests dédié. Installer une base de non-régression avant l'extraction du code métier.

### Tests obligatoires du Core

- ouverture de fixtures valides et validation des sections ;
- ouverture puis sauvegarde sans modification : conservation byte-à-byte lorsque le format le permet ;
- modification d'un champ, sauvegarde et réouverture ;
- vérification du CRC avant et après sauvegarde ;
- fichiers tronqués, taille invalide, sections absentes ou corrompues ;
- limites des inventaires, personnages, quêtes et cartes ;
- tests des codecs DQ9 ;
- tests undo / redo et copie entre blocs ;
- tests déterministes de génération et de recherche de cartes.

Les fixtures doivent être synthétiques ou explicitement autorisées. Ne pas committer de sauvegardes personnelles.

### Tests de concurrence / recherche

- comparer les sorties avec une baseline connue ;
- vérifier la reproductibilité avec plusieurs degrés de parallélisme ;
- tester annulation, progression finale, erreurs worker et relance ;
- rejeter et régénérer un cache incomplet / inconnu ;
- vérifier les zones d'écriture des workers et les compteurs de progression.

### Tests UI et système

- tests des ViewModels et services sans fenêtre native ;
- Avalonia headless si utile et compatible avec la version retenue ;
- smoke test sous Ubuntu 24.04 via X11 / XWayland ;
- test manuel du backend Wayland natif seulement s'il est activé ;
- ouverture, modification et sauvegarde sous un nouveau nom.

## 7. CI et livrables

Conserver les validations Windows pendant la migration. Ajouter un job Linux distinct qui construit réellement DQ9.Avalonia pour linux-x64.

Le CI Linux doit :
- exécuter DQ9.Core.Tests ;
- publier DQ9.Avalonia en Release pour linux-x64 ;
- vérifier le binaire avec file et readelf : ELF x86-64, jamais PE ;
- réaliser un smoke test ;
- téléverser un dossier publié ou une archive tar.gz.

Le CI Windows existant reste actif pendant la transition. Il sera adapté pour construire le Core et l'application Windows selon l'architecture retenue.

Livrables progressifs :
1. dossier linux-x64 publié ;
2. archive tar.gz ;
3. AppImage après validation du comportement hors du répertoire de build ;
4. paquet Debian si une distribution système est souhaitée.

La première livraison n'est pas bloquée par AppImage ou .deb : valider d'abord le binaire Linux et ses dépendances runtime.

## 8. Phases et critères de sortie

| Phase | Contenu | Estimation indicative | Critère de sortie |
|---|---|---:|---|
| P0 | Référence binaire, fixtures, tests de base, CI Windows | 2–4 j | Référence reproductible |
| P1 | Extraction de DQ9.Core | 3–5 j | Core compilable sans dépendance Windows |
| P2 | WMI, threads, cache, chemins XDG, paramètres | 2–4 j | Services utilisables sous Linux |
| P3 | Shell Avalonia, navigation, dialogues, services | 3–5 j | Application qui démarre et ouvre une sauvegarde |
| P4 | Migration des écrans métier | 8–12 j | Édition des fonctions principales |
| P5 | Cartes, recherche, progression et outils | 5–8 j | Fonctions avancées utilisables |
| P6 | Tests et comparaison Windows / Linux | 3–5 j | Aucune divergence binaire inexpliquée |
| P7 | CI, archive, AppImage et .deb optionnel | 2–4 j | Publication reproductible |

Estimation indicative totale : 28 à 47 jours de développement. Réestimer après P1 et P3, lorsque la taille réelle de l'interface et les comportements implicites seront mieux connus.

## 9. Ordre de migration des écrans

1. shell principal, ouverture / sauvegarde et sélection du bloc ;
2. données générales et informations du personnage ;
3. personnages, équipements et inventaires ;
4. quêtes, flags, titres et données annexes ;
5. auberge, visiteurs et Wi-Fi Shopping ;
6. cartes au trésor et recherche ;
7. fenêtres secondaires, préférences et finitions.

Toutes les opérations métier passent par des commandes ViewModel et le Core. Les appels Avalonia / WinForms ne doivent pas entrer dans le Core.

## 10. Risques et garde-fous

| Risque | Mesure |
|---|---|
| Corruption de sauvegarde | Fixtures, round-trip, CRC et comparaison byte-à-byte |
| Fonction oubliée | Matrice de parité par écran dans todo.md |
| Régression Windows | Conserver un build Windows vert |
| Dépendance Windows cachée | Recherche WMI, Win32, chemins et UI avant suppression |
| Cache écrit en zone read-only | Chemins XDG et test AppImage |
| Différences de polices / rendu | Assets explicites et validation visuelle |
| AOT trop tôt | Ne pas activer Native AOT avant validation du publish standard |
| Dépendances Avalonia / Linux | Verrouillage des versions et test Ubuntu |

## 11. Hors périmètre initial

- réécriture du format de sauvegarde ;
- ajout de fonctions de triche non présentes dans la référence ;
- refonte algorithmique des tables sans tests ;
- Native AOT comme prérequis ;
- support de toutes les distributions dès la première version ;
- retrait immédiat du client Windows de référence.

## 12. Définition de « Linux natif terminé »

- l'application Avalonia compile pour linux-x64 ;
- le binaire principal est ELF x86-64 ;
- Wine, Mono WinForms et PE ne sont pas nécessaires à l'exécution ;
- ouverture, modification, sauvegarde et CRC passent les tests ;
- la recherche fonctionne sans WMI ni WinForms ;
- les fonctions principales sont présentes dans la matrice de parité ;
- CI Linux et Windows verts ;
- l'archive peut être extraite et lancée sur une installation Linux documentée.
