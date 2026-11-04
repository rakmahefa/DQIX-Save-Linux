# Audit des fichiers — préparation du portage Linux natif

- **Dépôt :** [rakmahefa/Save-Editor-Decomp](https://github.com/rakmahefa/Save-Editor-Decomp)
- **Branche :** master
- **Commit audité :** 79c36e37d3d2d1028f1deb2c8376128dcda0d38f
- **Date :** 2 novembre 2026
- **Objectif :** identifier le code réutilisable dans un Core portable et les dépendances à remplacer pour une application Linux native.

## 1. Synthèse

Le dépôt contient **210 fichiers suivis**, représentant environ **2,50 Mo** de contenu.

| Type | Nombre |
|---|---:|
| C# (.cs) | 165 |
| Ressources (.resx) | 39 |
| Projet (.csproj) | 1 |
| Solution (.sln) | 1 |
| Workflow (.yml) | 1 |
| .gitignore | 1 |
| EXE | 1 |
| DLL | 1 |
| **Total** | **210** |

Le code se divise en un moteur métier (données, sauvegardes, codecs, tables et recherche), une interface WinForms volumineuse, un petit framework graphique / threading et deux binaires historiques.

**Conclusion :** le moteur de sauvegarde est un bon candidat à l'extraction ; le coût principal du portage est la reconstruction de l'interface et la suppression des dépendances Windows de la recherche.

## 2. Inventaire par répertoire

| Répertoire / groupe | Fichiers | Contenu | Évaluation |
|---|---:|---|---|
| Racine | 4 | .gitignore, AssemblyInfo.cs, projet et solution | Configuration à faire évoluer |
| .github/workflows | 1 | CI actuelle | Build Windows sur Ubuntu |
| DQ9_Cheat/Controls | 84 | 49 C# et 35 .resx | Principal chantier UI |
| DQ9_Cheat/DataManager | 40 | Modèles, mutation, sauvegarde, undo / redo | Extraction Core prioritaire |
| DQ9_Cheat/GameData | 36 | Tables et modèles Dragon Quest IX | Extraction Core prioritaire |
| DQ9_Cheat/DLL | 4 | Encodages et correctifs de données | À préserver dans Core |
| DQ9_Cheat/MapSearch | 14 | Modèles, calculs et recherche de cartes | Mixte : métier portable et Windows |
| DQ9_Cheat/Properties | 3 | Ressources et paramètres générés | À auditer / migrer |
| DQ9_Cheat (fenêtres / entrée) | 5 | AboutBox, MainWindow, Program et ressources associées | À remplacer par Avalonia |
| JS_Framework/Controls | 9 | Contrôles WinForms personnalisés et ressource | À remplacer |
| JS_Framework (progression) | 3 | Types d'événement et d'état de progression | Contrats neutres à extraire |
| JS_Framework/Threading | 5 | Wrapper historique de threads | À remplacer |
| lib | 2 | EXE et DLL historiques | Référence binaire, dépendances à vérifier |

### Groupes métier

Les 40 fichiers de DQ9_Cheat/DataManager comprennent notamment :
- SaveData.cs et SaveDataManager.cs ;
- BasisData.cs, CharacterData.cs et CharacterManager.cs ;
- ItemData.cs, ItemCollectionData.cs et SmartItemData.cs ;
- AlchemyData.cs, FirstClearData.cs et ProcessFlag.cs ;
- QuestDataManager.cs, TitleData.cs et TreasureMapData.cs ;
- VisitorData.cs, VisitorManager.cs, RikkaData.cs et WifiShopping.cs ;
- les classes UndoRedo*.

Les 36 fichiers de DQ9_Cheat/GameData représentent les modèles et tables du jeu : objets, compétences, monstres, quêtes, métiers, lieux de sauvegarde, titres, cartes et coffres. Les conserver autant que possible pendant l'extraction.

Les 4 fichiers de DQ9_Cheat/DLL sont :
- DQ9Encoding.cs
- TemplateEncoding.cs
- StringFixer.cs
- SavePlaceListFixer.cs

Ils sont à préserver après vérification de leurs dépendances réelles.

## 3. Dépendances Windows identifiées

### 3.1 Projet

Fichier : [DQCheat.Patched.0.7.csproj](DQCheat.Patched.0.7.csproj)

Paramètres observés :
- TargetFramework : net8.0-windows ;
- OutputType : WinExe ;
- UseWindowsForms : true ;
- ImportWindowsDesktopTargets : true ;
- AllowUnsafeBlocks : true ;
- PackageReference System.Management version 8.0.0.

**Impact :** le projet actuel ne devient pas Linux natif par simple changement de RID. Créer un Core portable et une application Avalonia séparée.

.NET 8 arrive en fin de support le 10 novembre 2026. Prévoir le retargeting du client Windows vers net10.0-windows après validation du CI ; les nouveaux projets ciblent net10.0.

### 3.2 Entrée et fenêtre principale

- [Program.cs](DQ9_Cheat/Program.cs) initialise Windows Forms et lance Application.Run.
- [MainWindow.cs](DQ9_Cheat/MainWindow.cs) hérite de Form et instancie menus, onglets et dialogues WinForms.
- [AboutBox.cs](DQ9_Cheat/AboutBox.cs) est une autre fenêtre WinForms.

**Impact :** créer une nouvelle entrée et des fenêtres Avalonia ; ne pas référencer ces classes dans le projet Linux.

### 3.3 Contrôles graphiques

DQ9_Cheat/Controls contient 49 fichiers C# et 35 ressources .resx. Les sources utilisent notamment :
- System.Windows.Forms : Form, Panel, GroupBox, TextBox, ComboBox, NumericUpDown, RichTextBox, ListView, menus et dialogues ;
- System.Drawing : Graphics, Font, Color et dessin personnalisé ;
- cycle de vie, événements et propriétés WinForms.

Fichiers particulièrement importants :
- Controls/DataControlBase.cs : base de contrôles personnalisés, Graphics et OnPaint ;
- Controls/CharacterDataControl.cs : grand écran de personnage ;
- Controls/StayingListGroupBox.cs : écran complexe des visiteurs ;
- Controls/TreasureMapDataControl.cs : édition et visualisation des cartes ;
- Controls/MapSearch/MapSearchWindow.cs : fenêtre de recherche, filtres et résultats.

**Impact :** reconstruire les écrans en Avalonia avec XAML, ViewModels et composants réutilisables. Une conversion mécanique fichier par fichier n'est pas recommandée.

### 3.4 WMI et chemins dans la recherche

Fichier : [SearchDataFile.cs](DQ9_Cheat/MapSearch/SearchDataFile.cs)

Usages observés :
- import de System.Management ;
- ManagementClass("Win32_Processor") et lecture de NumberOfCores ;
- import de System.Windows.Forms ;
- Application.StartupPath pour placer MapSearch.dat ;
- utilisation de JS_Framework.Threading.ThreadControl.

**Impact :** remplacer WMI par Environment.ProcessorCount comme valeur de départ portable, injecter le chemin du cache utilisateur et extraire la génération vers un service indépendant de l'UI.

Le nombre de processeurs logiques peut différer du nombre de cœurs physiques obtenu auparavant via WMI. Mesurer le temps de génération et la charge au lieu de supposer une équivalence.

### 3.5 Wrapper de threading

Fichier : [ThreadControl.cs](JS_Framework/Threading/ThreadControl.cs)

Usages observés :
- création directe de Thread ;
- Thread.SetApartmentState(ApartmentState.STA) dans Initialize() ;
- Thread.Abort() exposé dans Abort() ;
- état d'arrêt personnalisé et callback de fin.

**Impact :** remplacer cette abstraction pour la recherche par Task / Parallel, CancellationToken, IProgress<T> et des primitives de synchronisation appropriées. L'appel STA peut provoquer une exception sur une plateforme non Windows ; Thread.Abort n'est pas une stratégie d'annulation portable.

Auditer tous les consommateurs avant de supprimer les types du framework historique.

### 3.6 Paramètres, ressources et métadonnées

- [Settings.cs](DQ9_Cheat/Properties/Settings.cs) hérite de ApplicationSettingsBase.
- Resources.cs / Resources.resx et les autres .resx contiennent les ressources de l'application historique.
- [AssemblyInfo.cs](AssemblyInfo.cs) contient des attributs d'assemblage et une déclaration PermissionSet historique.

**Impact :**
- rechercher les usages de Settings.Default avant migration ;
- migrer uniquement les paramètres effectivement utilisés ;
- inspecter les .resx, extraire les images / icônes nécessaires et vérifier les polices ;
- vérifier les attributs de sécurité historiques sur le runtime ciblé.

### 3.7 Code métier

Les fichiers inspectés dans DataManager — notamment SaveData.cs, SaveDataManager.cs, BasisData.cs, CharacterData.cs et ItemData.cs — utilisent principalement System et System.IO / les types de données du jeu. Aucune dépendance WinForms n'a été observée dans ces fichiers inspectés.

GameData est composé de tables et modèles. Les classes du dossier DLL sont également de bons candidats à l'extraction.

**Impact :** préserver ce code avec des changements minimaux. Toute modification d'offset, taille, CRC ou encodage exige des tests binaires.

## 4. Binaires hérités

| Fichier | Taille observée | Décision |
|---|---:|---|
| lib/DQCheat.Patched.0.7.exe | 659 456 octets | EXE Windows historique, non exécutable nativement sous Linux |
| lib/DQCheat.dll | 43 520 octets | Bibliothèque historique, références à auditer |

Ces fichiers ne figurent pas comme PackageReference ou ProjectReference explicite du csproj actuel. Vérifier les références statiques, ressources et dépendances avant retrait. Conserver leur provenance et leurs hashes pendant l'audit.

## 5. CI existant

Fichier : [linux-build.yml](.github/workflows/linux-build.yml)

Ce workflow :
1. s'exécute sur ubuntu-latest ;
2. installe le SDK .NET 10 ;
3. restaure et construit le projet net8.0-windows pour win-x64 ;
4. active EnableWindowsTargeting=true ;
5. publie DQ9_Cheat.exe ;
6. vérifie un fichier PE32+ executable GUI x86-64 pour MS Windows ;
7. téléverse l'artefact DQ9_Cheat-win-x64.

Le [run #3](https://github.com/rakmahefa/Save-Editor-Decomp/actions/runs/41442605530) est vert. Le build a zéro erreur et le résumé indique 9 074 avertissements. L'artefact de publication comprend six fichiers, dont DQ9_Cheat.exe, DQ9_Cheat.dll, les fichiers JSON runtime / dépendances, un PDB et System.Management.dll.

**Décision :** garder cette validation comme cross-build Windows sur runner Linux. Ajouter un job séparé pour construire DQ9.Avalonia avec RID linux-x64 et vérifier qu'il produit un ELF.

## 6. Priorité de migration

| Priorité | Groupe | Action |
|---|---|---|
| P0 | Tests actuellement absents | Créer tests et fixtures binaires |
| P1 | DQ9_Cheat/DataManager | Extraire vers DQ9.Core |
| P1 | DQ9_Cheat/GameData | Extraire vers DQ9.Core |
| P1 | DQ9_Cheat/DLL | Extraire vers DQ9.Core |
| P2 | DQ9_Cheat/MapSearch | Séparer algorithme, cache et UI |
| P2 | JS_Framework/Threading | Remplacer le wrapper de threads |
| P2 | JS_Framework/Progress*.cs | Migrer vers des contrats neutres |
| P2 | DQ9_Cheat/Properties/Settings.cs | Auditer et migrer les paramètres utilisés |
| P3–P5 | DQ9_Cheat/Controls | Recréer les écrans Avalonia |
| P3–P5 | MainWindow.cs, Program.cs, AboutBox.cs | Recréer l'entrée et les fenêtres |
| P3–P5 | Fichiers .resx | Extraire / convertir les assets utilisés |
| P7 | Workflow CI | Ajouter le job Linux natif en conservant la validation Windows |

## 7. Points à vérifier pendant l'implémentation

- [ ] Cartographier tous les usages de Settings.Default.
- [ ] Cartographier les appels à SearchDataFile et les hypothèses de thread UI.
- [ ] Vérifier la signature, la taille et la version éventuelle du cache MapSearch.dat.
- [ ] Inventorier les images et polices utilisées dans les .resx.
- [ ] Vérifier les références et la provenance des binaires dans lib/.
- [ ] Rechercher d'autres usages de WMI, Win32, registre, chemins Windows ou API système.
- [ ] Réexaminer les avertissements après retargeting réel vers .NET 10.
- [ ] Définir les écrans obligatoires pour une première version Linux utilisable.

## 8. Conclusion

La migration est faisable en extrayant le moteur métier puis en reconstruisant l'interface. DataManager, GameData et les codecs sont les candidats de réutilisation principaux. WinForms, le dessin personnalisé, WMI, les chemins d'installation et le wrapper STA / Abort doivent sortir du chemin d'exécution Linux.

Aucun fichier métier n'a été modifié pendant cet audit. Ce commit ajoute uniquement les documents de planification.
