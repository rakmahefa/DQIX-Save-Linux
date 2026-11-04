# TODO — portage Linux natif

Référence : [spec.md](spec.md) · [files-audit.md](files-audit.md) · [docs/linux-port-notes.md](docs/linux-port-notes.md) · [docs/save-format.md](docs/save-format.md)  
Branche : master · Commit de base : 79c36e37d3d2d1028f1deb2c8376128dcda0d38f

Légende : [ ] à faire · [x] vérifié / terminé · [~] en cours ou partiel · [!] décision ou blocage.

## État initial

- [x] Inventaire de l'arborescence réalisé.
- [x] Cross-build Windows sur runner Ubuntu vérifié : [GitHub Actions #3](https://github.com/rakmahefa/Save-Editor-Decomp/actions/runs/41442605530).
- [x] Vérification que le run #3 produit un PE32+ Windows x64, pas un binaire Linux.
- [x] Dépendances Windows identifiées : WinForms, WMI, Application.StartupPath, SetApartmentState(STA), Thread.Abort.
- [x] P0 à P3 implémentés localement (commits « P0 », « P1 », « P2 », « P3 ») : noyau portable, tests de référence, services portables et shell Avalonia ; CI Linux ajoutée.

## P0 — baseline et tests de référence

- [x] Créer tests/DQ9.Core.Tests ciblant net10.0.
- [x] Constituer des fixtures synthétiques ou autorisées (SaveFileBuilder, remplissage xorshift seedé ; aucune sauvegarde personnelle).
- [x] Documenter tailles, offsets, sections et règles CRC (docs/save-format.md).
- [x] Tester ouverture puis sauvegarde sans modification (round-trip octet-à-octet, payloads aléatoires inclus).
- [x] Tester modification d'un champ puis réouverture.
- [x] Tester les deux blocs et le cas sans intermission.
- [x] Tester fichiers tronqués, tailles invalides et CRC incorrects (CRC non vérifié à l'ouverture, réparé à la sauvegarde — comportement d'origine documenté).
- [x] Capturer une baseline des résultats de recherche de cartes (24 entrées figées + déterminisme ; baseline complète 389 592 entrées opt-in via DQ9_SEARCH_FULL_BASELINE=1, sorties identiques avec 1 et 4 workers).
- [x] Garder le build Windows vert pendant la migration (retargeté net10.0-windows, publish win-x64 vérifié depuis Ubuntu).
- [~] Examiner les ~9 000 avertissements du build ; trier CA1416 et avertissements C# sans les masquer globalement (CA1416 disparu du Core ; WFO1000 traité proprement côté WinForms ; tri des avertissements restants reporté).

**Sortie P0 :** tests de référence reproductibles avant extraction. ✔ (56 tests verts sous Linux)

Gain non planifié des tests P0 : `SaveDataManager.ReadData` réinitialise désormais `_footerData` (un fichier sans footer n'hérite plus de celui du fichier précédemment ouvert — bug d'état du singleton trouvé par le round-trip).

## P1 — extraction de DQ9.Core

- [x] Créer src/DQ9.Core/DQ9.Core.csproj en net10.0.
- [x] Extraire DQ9_Cheat/GameData.
- [x] Extraire DQ9_Cheat/DataManager.
- [x] Extraire DQ9_Cheat/DLL.
- [x] Extraire les algorithmes et modèles portables de DQ9_Cheat/MapSearch.
- [x] Extraire les types de progression nécessaires vers des contrats neutres (JS_Framework/Progress* → src/DQ9.Core/Progress, espaces de noms conservés).
- [x] Séparer les services de sauvegarde de MainWindow (découplage UI ; config NumericUpDown transplantée dans DQ9_Cheat/UiRangeConfig.cs).
- [x] Supprimer WinForms et System.Drawing de DQ9.Core.
- [x] Supprimer System.Management de DQ9.Core.
- [x] Ajouter DQ9.Core à la solution.
- [x] Faire référencer DQ9.Core par l'application Windows historique.
- [x] Retargeter l'application Windows vers net10.0-windows après validation.
- [x] Vérifier le build Windows depuis Ubuntu avec EnableWindowsTargeting (publish PE32+ vérifié).
- [x] Exécuter les tests de non-régression du Core.

**Sortie P1 :** moteur partageable et compilable sans dépendance graphique Windows. ✔

## P2 — portabilité des services

### Recherche et threading

- [x] Remplacer WMI / ManagementClass("Win32_Processor") par Environment.ProcessorCount (injectable pour les tests).
- [x] Retirer System.Management après recherche de tous ses usages (aucun autre usage).
- [x] Remplacer Application.StartupPath par un chemin injecté.
- [x] Remplacer ThreadControl par Task / Parallel et CancellationToken (SearchDataFileService).
- [x] Supprimer l'hypothèse ApartmentState.STA (JS_Framework/Threading supprimé, zéro consommateur).
- [x] Supprimer Thread.Abort (annulation coopérative).
- [x] Synchroniser les compteurs et états partagés (Interlocked).
- [x] Découpler progression et annulation de l'interface (IProgress<SearchProgress> + CancellationToken).
- [x] Vérifier la même sortie avec 1, 2 et plusieurs workers (baseline complète 1 vs 4 workers, sorties identiques).
- [~] Tester annulation, relance et erreur worker (annulation testée ; propagation d'erreur implémentée — arrêt des workers frères — mais relance non testée automatiquement).

### Chemins et paramètres

- [x] Créer un service de chemins applicatifs portable (DQ9_Cheat.Services.XdgPaths).
- [x] Respecter XDG_CONFIG_HOME, XDG_DATA_HOME et XDG_CACHE_HOME (replis documentés ; variables relatives ignorées ; tests dédiés).
- [x] Déplacer MapSearch.dat dans le cache utilisateur (~/.cache/dq9-save-editor/MapSearch.dat).
- [x] Valider taille / version du cache avant lecture (taille exacte attendue, sinon InvalidDataException ; format brut conservé, régénérable).
- [x] Écrire le cache via fichier temporaire et remplacement atomique (File.Move overwrite, aucun résidu .tmp — testé).
- [x] Auditer les usages de Properties/Settings.cs et Settings.Default (aucun usage dans le code).
- [x] Migrer les paramètres réellement utilisés (rien à migrer).
- [x] Vérifier qu'aucune écriture ne cible le répertoire d'installation (chemin de cache injecté ; Core sans Application.StartupPath).

**Sortie P2 :** services portables, cache régénérable et chemins XDG. ✔

## P3 — shell Avalonia

- [x] Vérifier la version stable Avalonia compatible avec .NET 10 (12.1.5, backend Wayland natif disponible depuis 12.1.0).
- [x] Créer src/DQ9.Avalonia en net10.0.
- [x] Verrouiller les versions des packages (12.1.5).
- [x] Mettre en place la fenêtre, le thème (Fluent), les ressources et la navigation (navigation applicative complète en P4).
- [x] Définir les services de dialogues, fichiers, préférences et chemins (StorageProvider ; chemins XDG côté Core ; préférences persistantes reportées — rien à migrer).
- [x] Mettre en place MVVM et les commandes (ObservableObject / RelayCommand sans dépendance externe).
- [x] Implémenter ouvrir, sauvegarder, sauvegarder sous et fermer.
- [x] Implémenter sélection du bloc 1 / bloc 2 (bloc 2 conditionné à l'intermission).
- [x] Afficher les états modifié / non modifié et les erreurs (titre « * », barre de statut).
- [x] Tester le démarrage sous Ubuntu 24.04 via X11 / XWayland (smoke UI sous Xvfb : « SMOKE OK (ui) » ; smoke headless du pipeline ouvrir → modifier → enregistrer sous → rouvrir).

**Sortie P3 :** application Avalonia qui démarre et ouvre une sauvegarde. ✔ (publish linux-x64 ELF 64-bit vérifié par file + readelf)

## P4 — écrans métier

- [x] Créer une matrice de parité par écran. (docs/screen-parity.md)
- [x] Migrer les données générales et le profil. (Général : argent, banque, médailles, temps de jeu, compteurs, lieu, profil, gestes — 7 paires directionnelles + 15 tours appris)
- [x] Migrer les personnages et leurs attributs. (13 emplacements, identité, PV/PM, niveaux par classe, états, apparence)
- [x] Migrer équipements et inventaires. (8 emplacements d'équipement + 10 réserves + poches d'outils)
- [x] Migrer compétences et spécialités. (points de compétence, niveaux de compétence par classe 0-100, éditeur de bits d'effets/spécialités, Zoom / Encourager, grilles de révision par classe — "Vocation to Edit")
- [x] Migrer monstres et données associées. (307 entrées + édition groupée + compteur de complétion)
- [x] Migrer quêtes et flags de progression. (états, dates, compteurs ; drapeaux : règle, changements de classe, destinations)
- [x] Migrer titres et données de première victoire. (onglet « Titles & First clear »)
- [x] Migrer auberge et visiteurs. (grade, UID, 30 visiteurs, déplacement / suppression / création, taux de complétion monstre/objet/mode/forge 0-127)
- [x] Migrer Wi-Fi Shopping. (message, dates, 6 produits)
- [~] Migrer fenêtres secondaires et aide. (sélecteur d'objet et de profil migré ; À propos reporté — la version est dans le titre ; aucun contenu d'aide dans le client d'origine)
- [x] Extraire les images utiles des .resx. (aucune image fonctionnelle : icônes dessinées remplacées par les ressources Fluent)
- [x] Remplacer les contrôles dessinés par des composants Avalonia réutilisables. (ListBox + ComboBox + styles card/group/lbl ; VisionNumericUpDown → TextBox)
- [x] Tester lecture et écriture de chaque écran. (34 tests EditorScreensTests, 5 tests SearchPickerTests)
- [x] Tester undo / redo et copie de blocs depuis les commandes UI. (undo/redo par écran, édition groupée groupée, bloc 1 → bloc 2)

**Sortie P4 :** édition des principales données depuis Avalonia. ✔ (12 onglets, matrice de parité complète — tous les écrans ✔, différences résiduelles listées dans docs/validation-report.md §6)

## P5 — cartes au trésor et recherche

- [x] Migrer les filtres de cartes. (nom 1/2/3, boss, type, niveau, rang des monstres, étages, coffres)
- [x] Migrer la vue détaillée et les coffres. (détails de grotte + liste des coffres par rang ; onglet « Treasure maps »)
- [x] Migrer les conditions de recherche. (comparateur ==/!=/>=/<=, conditions optionnelles)
- [x] Migrer la fenêtre de résultats et ses tris. (onglet « Map search », tri niveau ↑/↓ et nom, détails par graine)
- [x] Ajouter progression et bouton Annuler. (génération en tâche de fond, ProgressBar, annulation coopérative)
- [x] Gérer cache absent, incomplet ou invalide. (statut, rejet de taille, rechargement)
- [x] Vérifier la sortie contre la baseline. (24 entrées figées P0 + déterminisme + équivalence 1/4 workers)
- [x] Tester avec un et plusieurs cœurs CPU. (SearchService_SameOutputWithOneAndFourWorkers + baseline complète opt-in)

**Sortie P5 :** recherche de cartes fonctionnelle sous Linux. ✔ (14 tests TreasureMapTests)

## P6 — validation

- [~] Exécuter les tests Core sous Linux et Windows. (Linux : 105 tests verts localement ; Windows : job CI windows-avalonia)
- [~] Comparer les sorties Windows / Linux sur les mêmes fixtures. (les mêmes tests s'exécutent sur les deux OS en CI ; comparaison locale limitée à Linux)
- [x] Vérifier CRC et octets hors des zones modifiées. (ValidationTests : CRC revalidés, localité argent/nom)
- [x] Vérifier les fichiers corrompus ou tronqués. (CheckFile rejette, cache de taille invalide rejeté)
- [x] Tester les chemins XDG définis et non définis. (XDG_CACHE_HOME enraciné honoré, relatif ignoré)
- [~] Vérifier les permissions et l'écriture depuis une application en zone read-only. (rien n'écrit dans le répertoire d'installation — audit P2 ; exécution réelle en lecture seule non automatisée)
- [x] Tester résolutions et facteurs DPI différents. (smoke UI Xvfb 1x et --global-scale-factor 2 : SMOKE OK)
- [x] Valider les parcours métier principaux. (FullScreenPipeline_RoundTripIsByteStable : 10 écrans, double enregistrement)
- [x] Documenter toute différence fonctionnelle restante. (docs/validation-report.md §6)

**Sortie P6 :** aucune divergence binaire inexpliquée. ✔ (docs/validation-report.md)

## P7 — CI et distribution

- [x] Ajouter job Ubuntu pour DQ9.Core.Tests.
- [x] Ajouter build / publish DQ9.Avalonia pour linux-x64.
- [x] Vérifier l'ELF avec file et readelf.
- [x] Ajouter un smoke test d'initialisation (headless + Xvfb).
- [x] Publier un artefact tar.gz (+ SHA-256).
- [x] Ajouter une matrice Windows x64 pour Avalonia. (win-x64 + win-arm64, vérification PE, tests sous Windows)
- [x] Garder temporairement le client WinForms de référence.
- [x] Ajouter le packaging AppImage. (pack/linux/build-appimage.sh, smoke --appimage-extract-and-run en CI)
- [x] Ajouter un paquet Debian si nécessaire. (pack/linux/build-deb.sh, /opt + lanceur + .desktop)
- [x] Générer SHA-256 des artefacts. (SHA256SUMS côté paquets ; .sha256 côté tar.gz et zip)
- [x] Documenter les bibliothèques système et distributions validées. (docs/linux-port-notes.md)
- [~] Tester l'archive sur une installation Ubuntu propre. (AppImage validée sur conteneur propre via --appimage-extract-and-run ; installation .deb manuelle restante)

**Sortie P7 :** artefacts Linux reproductibles et distribuables.

## Décisions à prendre pendant l'implémentation

- [x] Confirmer la version Avalonia stable compatible .NET 10 → 12.1.5 verrouillée.
- [~] Décider si Windows migre lui aussi vers Avalonia ou garde WinForms séparément. (matrice Avalonia Windows en CI ; WinForms reste le client de référence — décision finale en attente)
- [ ] Choisir le format de configuration.
- [x] Décider si le backend Wayland natif entre dans la première release → non (X11 par défaut ; Wayland 12.1.0 disponible en option).
- [ ] Définir les distributions Linux officiellement supportées.
- [ ] Renommer les espaces de noms historiques (DQ9_Cheat.*, FriedGinger.*, JS_Framework.*) vers DQ9.Core.* — reporté pour limiter le churn ; transitioin via InternalsVisibleTo.
