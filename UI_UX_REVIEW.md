# Revue UI/UX spécialiste — DQIX-Save-Linux (shell Avalonia)

- **Date :** 4 novembre 2026
- **Réviseur :** rakmahefa-bot (revue de spécialiste UI/UX)
- **Périmètre :** `src/DQ9.Avalonia` (shell, 12 écrans métier, dialogues, ViewModels) au commit `f61b1a5`, branche `ui-ux-review`. Le client WinForms historique (`DQ9_Cheat/`) n'est audité que comme référence de parité.
- **Méthode :** revue statique complète (AXAML + ViewModels + code-behind), vérification croisée avec `spec.md`, `docs/validation-report.md` et le backlog UX-01→UX-23 existant, calcul des contrastes WCAG 2.1 (formule officielle, cf. annexe A). Limitation : pas d'exécution runtime dans l'environnement de revue ; les captures existantes du harnais `DQ9_SMOKE_SHOTS` (cf. validation-report §6) sont prises comme référence, et l'annexe C recommande de les automatiser davantage.
- **Documents associés :** [spec.md](spec.md) · [docs/validation-report.md](docs/validation-report.md) · [docs/screen-parity.md](docs/screen-parity.md)

---

## 1. Synthèse exécutive

Le port Avalonia est **remarquablement bien outillé** pour un projet de cette taille : tokens de design centralisés, backlog UX numéroté déjà traité (UX-01 à UX-23), états d'interface systématiques (overlay premier lancement, empty states, garde de fermeture), validation `INotifyDataErrorInfo` propre. La base est saine : **aucune restructuration n'est nécessaire**.

La revue identifie néanmoins **5 constats majeurs (S1)** dont trois touchent directement la confiance de l'utilisateur dans un outil qui manipule des sauvegardes de jeu irremplaçables : raccourcis clavier annoncés mais non fonctionnels, écriture de la sauvegarde sans filet de sécurité (pas de backup ni d'écriture atomique), action destructrice sans confirmation, recherche de cartes qui gèle l'interface, et couleurs d'état illisibles en thème sombre.

| Dimension | Note /10 | Lecture |
|---|---:|---|
| Architecture informationnelle & navigation | 8.0 | 12 onglets clairs, cartes regroupées, block switch explicite |
| Système visuel, tokens & cohérence | 8.5 | Échelles sémantiques (micro/small/medium/wide), styles partagés |
| États, feedback & gestion d'erreurs | 8.0 | Overlay, empty states, status bar à sévérités, dirty indicator |
| Clavier & efficacité expert | 5.0 | Gestes affichés mais **morts** (S1-1), pas de drag & drop ni récents |
| Accessibilité | 5.5 | Contraste FAIL en sombre (S1-5), labels non associés (S2-1) |
| Sécurité des données (réelle & perçue) | 6.0 | Écriture sans backup (S1-2), action destructive sans confirm (S1-3) |
| Robustesse runtime | 6.5 | Recherche synchrone → freeze (S1-4), pas de handler global (S3-3) |
| **Global** | **7.0** | Très bonne base ; les 5 S1 sont tous corrigeables en ≤ 1 jour |

**Top 5 priorités immédiates** : S1-1 (15 min) → S1-5 (30 min) → S1-3 (30 min) → S1-2 (1–2 h + test) → S1-4 (1–2 h).

---

## 2. Points forts à préserver

1. **Design tokens centralisés** (`App.axaml` §4) : espacements, largeurs sémantiques (`FieldMicroWidth` 64 → `FieldWideWidth` 220 documentées par cas d'usage), tailles typographiques, métriques de contrôles. Chaque écran en dérive — c'est ce qui rend l'ensemble cohérent et le changement global possible en un fichier.
2. **États d'interface systématiques** : overlay « Open a DQ9 save to begin » avec bouton d'action réel (UX-08, `MainWindow.axaml` 115–128), empty state du panneau résultats (UX-18, `SearchScreen.axaml` 91–99), status bar à sévérités rouge/ambre (UX-05), indicateur « ● Unsaved changes » avec tooltip explicatif (UX-04).
3. **Garde de fermeture exemplaire** (`MainWindow.axaml.cs` 63–88) : dialogue Save/Discard/Cancel, défaut = **Save** (choix sûr), Discard stylé `danger`, Escape = Cancel. Le pattern TaskDialog (~60 lignes, zéro dépendance) est le bon compromis.
4. **Validation UX-01 bien architecturée** (`EditorScreenViewModel` 68–131) : `INotifyDataErrorInfo` → bordure rouge native Fluent, écho status bar, purge des erreurs après undo/refresh (`ClearAllFieldErrors`). Le parseur tolérant `ValidateParse<T>` évite la duplication.
5. **Détails de pickers soignés** (UX-09/UX-10/UX-23) : focus automatique dans la recherche à l'ouverture (`SearchPickerWindow.axaml.cs` 36–40), Enter/Escape mappés, préselection qui **saute la ligne « (Empty — clears slot) »** pour qu'un Enter parasite ne vide pas le slot (`SearchPickerViewModel.Rebuild`, ligne 94), regex invalide → repli en recherche littérale sans exception (106–120), pseudo-entrée moteur désambiguïsée « (Keep current vocation) » (165–167).
6. **Pédagogie des valeurs codées** (UX-20) : combobox « Male/Female » avec tooltip « 0 = male, 1 = female », tooltips métier sur les zones non évidentes (intermission, tone 0–7, exp « as edited by the original tool »).
7. **Typographie embarquée** : `WithInterFont()` (Program.cs 44) garantit un rendu identique quelle que soit la distribution — bonne décision pour Linux.

---

## 3. Constats S1 — majeurs

### S1-1 · Raccourcis clavier annoncés mais non fonctionnels

**Preuve.** `MainWindow.axaml` 60–68 affiche `InputGesture="Ctrl+O"`, `Ctrl+S`, `Ctrl+Z`, `Ctrl+Y` ; or dans Avalonia `InputGesture` est **purement décoratif**. Un grep `HotKey|KeyBinding|KeyGesture` sur `src/DQ9.Avalonia` retourne **zéro résultat**. Le commentaire de `MainWindow.axaml.cs` 61 (« closing (and Ctrl+Q) ») promet en plus un Ctrl+Q jamais bindé.

**Impact.** Le menu ment à l'utilisateur : Ctrl+S n'enregistre pas, Ctrl+Z n'annule pas. Pour un éditeur où l'annulation est un filet de sécurité, c'est le constat le plus grave de la revue côté interaction — et le plus simple à corriger.

**Correctif** (~15 min) — dans `MainWindow.axaml`, juste après `<DockPanel>` ouvert ou sur la Window :

```xml
<Window.KeyBindings>
  <KeyBinding Gesture="Ctrl+O" Command="{Binding OpenCommand}" />
  <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
  <KeyBinding Gesture="Ctrl+Y" Command="{Binding RedoCommand}" />
  <KeyBinding Gesture="Ctrl+Z" Command="{Binding UndoCommand}" />
  <KeyBinding Gesture="Ctrl+Q" Command="{Binding ExitCommand}" />
</Window.KeyBindings>
```

Les `InputGesture` du menu deviennent alors exacts et continuent d'afficher les gestes. Ajouter au passage Ctrl+Shift+S → `SaveAsCommand` et documenter les gestes dans le menu Save As.

### S1-2 · Écriture de la sauvegarde sans filet de sécurité

**Preuve.** `MainViewModel.SaveToFile` (`MainViewModel.cs` 240–252) : `FileMode.Create` **directement sur l'unique copie** de la sauvegarde. Ni écriture temporaire + rename atomique, ni copie `.bak` de l'ancienne version. Un crash, un disque plein ou une déconnexion USB au milieu de `WriteData` laisse un fichier tronqué. La spec traite le risque « Corruption de sauvegarde » (§10) uniquement côté format/tests ; le flux d'écriture du shell ne le couvre pas.

**Impact.** C'est le point de confiance n°1 d'un éditeur de sauvegardes : l'utilisateur doit pouvoir *savoir* que l'outil ne peut pas détruire son save d'origine. Aujourd'hui une seule écriture malheureuse suffit — et l'utilisateur moyen ne fait pas lui-même de copies.

**Correctif** (~1–2 h + test) :

```csharp
private bool SaveToFile(string path)
{
    var tmp = path + ".tmp";
    try
    {
        using (var output = new FileStream(tmp, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(output))
        {
            SaveDataManager.Instance.WriteData(writer);
        }

        var backup = Path.ChangeExtension(path, ".bak");
        if (File.Exists(path))
            File.Copy(path, backup, overwrite: true);

        File.Move(tmp, path, overwrite: true);   // remplacement atomique
    }
    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
    {
        ReportError($"Could not write '{path}': {ex.Message}");
        return false;
    }
    // ... inchangé
}
```

Compléments recommandés : mentionner le backup dans le statut (« Saved 'X' (previous version kept as X.bak) »), un test round-trip qui vérifie que `.bak` contient bien l'ancien octet modifié, et à terme un réglage dans un futur menu Settings. Le `XdgPaths` existant n'est pas concerné : le `.bak` vit à côté du fichier édité, là où l'utilisateur le cherchera.

### S1-3 · « Copy block 1 to block 2 » : action destructrice sans confirmation

**Preuve.** `MainViewModel.CopyBlock1ToBlock2` (271–280) s'exécute immédiatement et remplace les données d'intermission du bloc 2. L'infrastructure de confirmation existe pourtant déjà (`TaskDialog`, danger styling, UX-16) mais n'est pas branchée sur cette commande — incohérence avec la garde UX-04 qui, elle, est implémentée.

**Impact.** Un clic parasite écrase des données (DQVC/visiteurs) sans étape d'annulation visible. L'opération est undoable côté moteur (`UndoRedoSaveDataCopy`), mais rien ne l'indique dans le menu — l'utilisateur ne testera jamais Ctrl+Z pour la découvrir (cf. S1-1).

**Correctif** (~30 min) — même pattern que `_confirmDiscardEdits` : injecter `Func<Task<bool>> _confirmDestructive` dans `MainViewModel`, la fenêtre l'implémente avec

```csharp
private async Task<bool> ConfirmOverwriteBlock2Async()
    => "copy" == await TaskDialog.ShowAsync(this, "Overwrite block 2?",
        "Copying block 1 replaces the intermission data currently stored in block 2. Continue?",
        new TaskDialog.Choice("Cancel", "cancel", isDefault: true, isCancel: true),
        new TaskDialog.Choice("Copy — overwrite", "copy", danger: true));
```

Défaut = **Cancel** (pas de dégât par Escape/Enter parasites), danger sur l'action. Mettre à jour le tooltip du menu (« overwrite existing intermission data »).

### S1-4 · Recherche de cartes : gel de l'interface

**Preuve.** `SearchScreenViewModel.Search()` (376–419) : `service.LoadCache()` (ligne 394 — relecture + parsing **complet** du cache à chaque clic ; la baseline documentée fait 389 592 entrées, validation-report §2) puis `Where` + `OrderBy` sur l'ensemble, **le tout sur le thread UI**. La génération de cache, elle, est correctement asynchrone (`Task.Run` + `Progress<T>` + CTS, 325–367) — la recherche n'a pas bénéficié du même traitement.

**Impact.** À chaque Search, l'application se fige (chargement + filtre + tri d'une centaine de milliers de lignes) sans indicateur : l'app paraît plantée, le clic répété aggrave le cas.

**Correctif** (~1–2 h) : réutiliser le pattern existant —
1. mettre `IsBusy = true` (la ProgressBar et la désactivation des boutons sont déjà câblées dessus) ;
2. `await Task.Run(...)` pour `LoadCache` (uniquement si le cache n'est pas déjà chargé pour ce chemin) + filtre + tri ;
3. réassigner `Results` sur le thread UI à la sortie ;
4. bouton « Cancel » commun génération/recherche ou au minimum désactiver Search pendant le traitement (`IsNotBusy` existe déjà).

Bonus cohérence : conserver le cache en mémoire tant que `CacheFilePath` et la taille du fichier n'ont pas changé, pour que le second Search soit instantané.

### S1-5 · Couleurs d'état illisibles en thème sombre

**Preuve.** `App.axaml` 44–46 définit `ErrorBrush #C42B1C`, `WarningBrush #9D5D00`, `SuccessBrush #0F7B0F` en valeurs uniques, avec `RequestedThemeVariant="Default"` (ligne 4) — l'app suit donc le thème système, thème sombre compris. Ratios WCAG 2.1 calculés (annexe A) :

| Brosse | Fond clair (~#FAFAFA) | Fond sombre (~#1F1F1F) | Verdict |
|---|---:|---:|---|
| Error #C42B1C | 5.42:1 ✅ | 2.91:1 ❌ | AA exige 4.5:1 (texte 12–14 px) |
| Warning #9D5D00 | 5.03:1 ✅ | 3.14:1 ❌ | idem |
| Success #0F7B0F | 5.21:1 ✅ | 3.03:1 ❌ | idem |

**Impact.** Le « ● Unsaved changes », les messages `.error`/`.warning` de la status bar et la bordure `TextBox.invalid` deviennent difficiles à lire dès que l'utilisateur est en dark — configuration très fréquente sur Linux desktop. UX-11 avait corrigé le cas « Opacity pour faire du gris » ; les trois brosses d'état restent le chaînon manquant.

**Correctif** (~30 min) — dictionnaires par variante de thème (les variantes proposées sont calculées ≥ 6.2:1 sur les fonds sombres, annexe A) :

```xml
<ResourceDictionary.ThemeDictionaries>
  <ResourceDictionary x:Key="Light">
    <SolidColorBrush x:Key="ErrorBrush" Color="#C42B1C" />
    <SolidColorBrush x:Key="WarningBrush" Color="#9D5D00" />
    <SolidColorBrush x:Key="SuccessBrush" Color="#0F7B0F" />
  </ResourceDictionary>
  <ResourceDictionary x:Key="Dark">
    <SolidColorBrush x:Key="ErrorBrush" Color="#FF8A7A" />
    <SolidColorBrush x:Key="WarningBrush" Color="#E9A23B" />
    <SolidColorBrush x:Key="SuccessBrush" Color="#6CCB5F" />
  </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

Ajouter un screenshot `DQ9_SMOKE_SHOTS` en variante Dark dans le smoke CI pour verrouiller la non-régression.

---

## 4. Constats S2 — importants

### S2-1 · Accessibilité : étiquettes non associées et métriques fixes

- Les `TextBlock.lbl` ne sont **pas associés** aux champs qu'ils décrivent (aucun `AutomationProperties.Name` dans le projet) : un lecteur d'écran annonce des TextBox anonymes, et le tooltip pédagogique n'est pas lu au focus clavier. Correctif minimal : `AutomationProperties.Name="Money"` etc. sur chaque champ (commencer par General et Characters, les deux écrans les plus utilisés) ; un `AutomationProperties.LabeledBy` vers le label est l'option élégante à valider selon la version Avalonia.
- **Largeurs fixes** (`FieldMicroWidth`…`FieldWideWidth`, `MinHeight 30`) : à 150 % de text scaling (accessibilité GNOME, très utilisée), le texte clippe à l'intérieur des champs alors que la police grossit. Recommandations : passer medium/wide en `MinWidth` (le WrapPanel absorbe la variation), garder des largeurs fixes uniquement pour les micro-champs à nombre de chiffres borné (le commentaire UX-12 le justifie : mois, jour, heure), et ajouter une passe de screenshots à 150 %.
- Les dialogues (`TaskDialog`, `SearchPickerWindow`) n'exposent pas de propriétés d'automation (rôle, nom) — mineur mais facile en même temps que S2-1.

### S2-2 · Ouverture de fichier : pas de filtres, pas de récents, pas de drag & drop

`MainWindow.axaml.cs` 27–36 : `FileTypeFilter = ["All files"]` uniquement. Pour un outil dédié : filtres « DeSmuME (.dsv) », « no$gba (.sav) », « All files » (toujours en dernier recours) — 15 min. Le commentaire de `MainViewModel.OpenFile` (ligne 182, « drag-drop later ») assume le TODO : `DragDrop.AllowDrop` sur la Window avec validation `CheckFile` + garde dirty existante ≈ 1 h, c'est le raccourci d'usage le plus naturel de ce type d'outil. Enfin : mémoriser le dernier dossier et proposer File ▸ Open Recent (le service Settings/XdgPaths existe déjà) ≈ 2 h.

### S2-3 · Status bar « dernier message gagne »

`SetFieldError` (EditorScreenViewModel 82–88) écho le message dans la status bar — bien — mais l'erreur suivante l'écrase ; en édition dense (Job levels & experience : 12 jobs × 2 champs), l'utilisateur qui tabule vite perd le message du champ qu'il vient de quitter ; il ne reste que la bordure rouge, sans texte. Correctifs possibles, par coût croissant : (a) le tooltip du champ en erreur porte le message ; (b) un `TextBlock.error` local sous le premier champ en erreur de la carte ; (c) un résumé « N fields need attention » cliquable qui focus le champ.

### S2-4 · Bloc 2 désactivé sans explication visible

`MainWindow.axaml` 80–83 : le radio « Block 2 (intermission) » est désactivé par `IsEnabled={Binding HasIntermission}`, mais quand il l'est, rien à l'écran n'explique pourquoi (le tooltip existe mais peu découvrable sur un contrôle désactivé). Correctif : `TextBlock.hint` « (no block 2 data in this file) » visible quand `HasOpenFile && !HasIntermission` — 20 min, un binding.

### S2-5 · Fermeture pendant la génération du cache

`MainWindow.OnClosing` (63–76) ne consulte pas `SearchScreenViewModel.IsGenerating` : fermer pendant la génération (~1 min) laisse la `Task.Run` orpheline et interrompt potentiellement l'écriture du cache. Correctif : câbler `CancelGeneration()` avant la fermeture (ou attendre l'annulation — le service écrit déjà via temp+replace côté moteur, à confirmer par un test). 30 min.

### S2-6 · Code mort : `MoneyInput` / `ApplyMoneyCommand`

Non bindés dans **aucun** .axaml (grep project-wide) ; uniquement consommés par le smoke (`SmokeRunner.cs` 70–97). Risque : un mainteneur croit que le shell expose encore l'édition d'argent, alors que l'écran General est canonique. Supprimer du VM de production (le smoke peut écrire via l'écran General) ou assumer un vrai bloc « Quick actions » dans le shell — mais alors le binder.

---

## 5. Constats S3 — mineurs / polish

| ID | Constat | Référence | Suggestion |
|---|---|---|---|
| S3-1 | Titre « (Avalonia) » = jargon technique visible ; chemin + nom d'app dupliqués | `MainViewModel.cs` 21, 300–306 | `« fichier * — DQ9 Save Editor »` |
| S3-2 | Pas de menu Help / About (AboutBox reste WinForms-only, cf. validation-report §6) | menu `MainWindow.axaml` 58–72 | Help ▸ About : version, licence, lien repo |
| S3-3 | Pas de handler global d'exceptions : crash silencieux (seul `LogToTrace`) | `Program.cs` 37–47 | try/catch autour de `StartWithClassicDesktopLifetime` + `AppDomain.UnhandledException` → TaskDialog |
| S3-4 | 12 onglets à MinWidth 900 : « General, Characters, Items, Quests, Titles, Monsters, Flags, Alchemy, Rikka's Inn, Wi-Fi DQVC, Treasure maps, Map search » ≈ 930 px estimés (padding 10,5 + ~7 px/car.) — débordement probable | `App.axaml` 126–130 | vérifier à 900 px ; sinon libellés courts (« Rikka Inn », « Wi-Fi ») ou wrap du TabStrip |
| S3-5 | `Topmost="True"` sur TaskDialog superflu (ShowDialog est déjà modal au propriétaire) et agressif vis-à-vis des autres applications | `TaskDialog.axaml` 9 | retirer |
| S3-6 | `Button.danger` : les pseudo-états hover/pressed retombent sur l'accent Fluent → le rouge disparaît au survol | `App.axaml` 107–110 | styles `:pointerover` / `:pressed` dédiés |
| S3-7 | Géométrie de fenêtre et dernier dossier non persistés | — | Settings service XdgPaths déjà prêt |
| S3-8 | Chaînes EN en dur partout ; impossible de localiser sans toucher aux vues | tous les .axaml | centraliser (ResourceDictionary ou .resx) quand l'UI est stabilisée |
| S3-9 | Cap silencieux des résultats à 5 000 (affiché « of N » — bien) mais pas d'export ni de lien vers « filtre plus » | `SearchScreenViewModel.cs` 414–416 | export CSV/TXT (parité WinForms manquante, doc §6) |
| S3-10 | Overlay premier lancement : pas de « Open recent » une fois des fichiers connus | `MainWindow.axaml` 115–128 | ajouter après S2-2 |

---

## 6. Checklists de conformité

### Accessibilité (WCAG 2.1 AA / EN 301 549)

| Critère | Statut | Détail |
|---|---|---|
| 1.4.3 Contraste (minimum) | ⚠️ Partiel | PASS en clair (4.8–5.4:1), **FAIL en sombre** (2.5–3.1:1) — S1-5 |
| 1.4.10 Reflow | ⚠️ Partiel | WrapPanel + ScrollViewer corrects ; largeurs fixes risquées au text scaling — S2-1 |
| 1.4.11 Contraste (non-texte) | ✅ PASS | Bordures de cartes et focus Fluent adéquats |
| 2.1.1 Clavier | ⚠️ Partiel | Navigation tabulaire OK ; raccourcis promis non fonctionnels — S1-1 |
| 2.4.7 Focus visible | ✅ PASS | Template Fluent par défaut |
| 3.3.1 Identification des erreurs | ✅ PASS | INotifyDataErrorInfo + bordure + écho status bar |
| 3.3.2 Étiquettes ou instructions | ❌ FAIL | Labels non associés aux champs — S2-1 |
| 3.3.3 Suggestion après erreur | ⚠️ Partiel | Messages d'action clairs (« must be 0–99 ») mais écrasés — S2-3 |
| 4.1.2 Nom, rôle, valeur | ⚠️ Partiel | Combos/checks nommés par contenu ; TextBox sans nom — S2-1 |

### Sécurité des données (spécifique éditeur de sauvegardes)

| Exigence | Statut | Détail |
|---|---|---|
| Écriture atomique (temp + rename) | ❌ FAIL | `FileMode.Create` direct — S1-2 |
| Backup de la version précédente | ❌ FAIL | Aucun `.bak` — S1-2 |
| Confirmation des actions destructrices | ⚠️ Partiel | Discard guard ✅ (UX-04) ; copy block 2 ❌ — S1-3 |
| Undo / redo global | ✅ PASS | `UndoRedoMgr` + tests P0 |
| Intégrité (CRC recomposé) | ✅ PASS | ValidationTests §1 |
| Garde de fermeture avec edits | ✅ PASS | OnClosing + TaskDialog |
| Champs inertes avant chargement | ✅ PASS | UX-08, disable + overlay |
| Cancellation des tâches longues | ⚠️ Partiel | Génération ✅ ; recherche non asynchrone (S1-4) ; fermeture non gardée (S2-5) |

---

## 7. Plan d'action priorisé

**Quick wins (≤ 1 h chacun)**
1. S1-1 KeyBindings (15 min) — corrige la promesse clavier.
2. S1-5 ThemeDictionaries dark pour les 3 brosses (30 min) + screenshot dark en CI.
3. S1-3 confirmation Copy block 2 (30 min, infra existante).
4. S2-4 hint « no block 2 data » (20 min).
5. S3-5 Topmost off ; S3-6 hover danger ; S3-1 titre ; filtres de picker (S2-2a).
6. S2-6 suppression du code mort Money.

**Court terme (≤ 1 jour)**
7. S1-2 écriture atomique + `.bak` + test round-trip du backup.
8. S1-4 recherche asynchrone + cache réutilisé + annulable.
9. S2-1 AutomationProperties sur General + Characters ; test screenshots à 150 %.
10. S3-3 handler global d'exceptions → TaskDialog.
11. S2-5 garde de fermeture pendant la génération.

**Moyen terme**
12. Drag & drop + Open Recent + persistance géométrie/dernier dossier (Settings service).
13. Inline errors ou résumé de validation (S2-3b/c) sur les écrans denses.
14. Export des résultats de recherche ; About box ; centralisation des chaînes (i18n).
15. Passe a11y complète : LabeledBy, dialogues, 12 écrans, à l'occasion de la stabilisation P7+.

---

## Annexe A — ratios de contraste calculés (WCAG 2.1)

Luminance relative `L = 0.2126·R + 0.7152·G + 0.0722·B` (composantes linéarisées), ratio `(L1+0.05)/(L2+0.05)`. Surfaces de référence Fluent : clair `#FAFAFA` (fenêtre) / `#F5F5F5` (carte AltHigh) ; sombre `#1F1F1F` / `#2B2B2B`.

| Première plan | #FAFAFA | #F5F5F5 | #1F1F1F | #2B2B2B |
|---|---:|---:|---:|---:|
| Error #C42B1C (actuel) | 5.42:1 | 5.19:1 | 2.91:1 ❌ | 2.50:1 ❌ |
| Warning #9D5D00 (actuel) | 5.03:1 | 4.82:1 | 3.14:1 ❌ | 2.70:1 ❌ |
| Success #0F7B0F (actuel) | 5.21:1 | 4.99:1 | 3.03:1 ❌ | 2.60:1 ❌ |
| Error #FF8A7A (proposé dark) | 2.19:1 | 2.10:1 | **7.20:1** ✅ | **6.18:1** ✅ |
| Warning #E9A23B (proposé dark) | 2.08:1 | 1.99:1 | **7.61:1** ✅ | **6.54:1** ✅ |
| Success #6CCB5F (proposé dark) | 1.94:1 | 1.86:1 | **8.12:1** ✅ | **6.98:1** ✅ |

Les variantes dark proposées sont inutilisables en thème clair — d'où le passage obligatoire par `ThemeDictionaries` (elles n'y apparaissent jamais).

## Annexe B — hypothèses et limites de la revue

- Revue statique : les jugements de comportement (gestes non bindés, freeze de Search) dérivent du code et de la documentation du projet ; le harnais `DQ9_SMOKE_SHOTS` du dépôt permet de les confirmer visuellement en CI (recommandé en §7.2 et §7.9).
- Estimation de largeur du TabStrip (S3-4) : calcul approximatif (~930 px) basé sur Inter 14 px + padding 10,5 ; à vérifier au rendu réel à MinWidth 900.
- Les surfaces Fluent citées (#FAFAFA/#F5F5F5/#1F1F1F/#2B2B2B) sont des approximations des tokens par défaut ; les ratios restent dans les ordres de grandeur indiqués sur les surfaces voisines.

## Annexe C — enrichir le harnais de captures

Le dépôt dispose déjà de `DQ9_SMOKE_SHOTS=<dir>` (rendu headless de chaque onglet + pickers, cf. validation-report §6). Pour verrouiller la non-régression visuelle :
1. ajouter une passe **Dark** (`RequestedThemeVariant="Dark"` sur l'AppBuilder en mode shots) ;
2. ajouter une passe à **text scaling 150 %** si le harnais permet de forcer `LayoutHelper`/scale — sinon screenshot au zoom fenêtre ;
3. ajouter une passe à **MinWidth 900** (S3-4) ;
4. comparer les PNG entre runs (hash ou diff visuel) dans le job CI Linux.
