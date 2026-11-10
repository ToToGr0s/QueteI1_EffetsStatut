using System.Linq;
using QueteI1.StatusEffects.Content;
using QueteI1.StatusEffects.Presentation;
using UnityEditor;
using UnityEngine;

namespace QueteI1.StatusEffects.EditorTools
{
    /// <summary>
    /// Crée les effets d'exemple (assets) s'ils n'existent pas encore, puis remplit la bibliothèque.
    /// Un asset déjà présent n'est jamais écrasé : les réglages faits à la main sont conservés.
    /// Pour repartir des valeurs du code, supprimer l'asset puis relancer la génération.
    /// </summary>
    public static class EffectAssetGenerator
    {
        public const string EffectsFolder = "Assets/_Project/Data/Effects";
        public const string LibraryPath = "Assets/_Project/Data/EffectLibrary.asset";

        [MenuItem("Quête I1/Générer les effets d'exemple", priority = 1)]
        static void GenerateFromMenu() => EnsureAssets();

        public static EffectLibrary EnsureAssets()
        {
            EditorFolders.Ensure(EffectsFolder);

            Define("01_Soin", "Soin", "Rend 30 points de vie d'un coup.",
                new Color(0.35f, 0.85f, 0.4f), EffectPolarity.Buff,
                DurationType.Instant, 1f, StackingRule.Refresh, 1,
                new InstantHealthComponent(HealthChangeKind.Heal, 30f));

            Define("02_Frappe", "Frappe", "Un gros coup : 25 dégâts. La défense en bloque une partie.",
                new Color(0.95f, 0.3f, 0.25f), EffectPolarity.Debuff,
                DurationType.Instant, 1f, StackingRule.Refresh, 1,
                new InstantHealthComponent(HealthChangeKind.Damage, 25f));

            Define("03_Poison", "Poison",
                "Fait perdre 3 PV chaque seconde pendant 6 s. Relance-le : il s'empile jusqu'à 5 fois et pique plus fort !",
                new Color(0.55f, 0.85f, 0.15f), EffectPolarity.Debuff,
                DurationType.Timed, 6f, StackingRule.Stack, 5,
                new PeriodicHealthComponent(HealthChangeKind.Damage, 3f, 1f, scaleWithStacks: true, ignoreDefense: true));

            Define("04_Regeneration", "Régénération",
                "Rend 6 PV chaque seconde pendant 5 s. Le relancer remet juste le chrono à zéro.",
                new Color(0.3f, 0.95f, 0.75f), EffectPolarity.Buff,
                DurationType.Timed, 5f, StackingRule.Refresh, 1,
                new PeriodicHealthComponent(HealthChangeKind.Heal, 6f, 1f, scaleWithStacks: false));

            Define("05_Ralentissement", "Ralentissement",
                "Rend plus lent : −25 % de vitesse. S'empile jusqu'à 3 fois (−75 %).",
                new Color(0.35f, 0.55f, 1f), EffectPolarity.Debuff,
                DurationType.Timed, 5f, StackingRule.Stack, 3,
                new StatModifierComponent(new ModifierEntry(StatType.Speed, ModifierOperation.Multiplicative, -25f)));

            Define("06_Hate", "Hâte",
                "+2 de vitesse pendant 6 s. Ce bonus est ajouté AVANT les pourcentages (comme le ralentissement).",
                new Color(1f, 0.6f, 0.2f), EffectPolarity.Buff,
                DurationType.Timed, 6f, StackingRule.Refresh, 1,
                new StatModifierComponent(new ModifierEntry(StatType.Speed, ModifierOperation.Additive, 2f)));

            Define("07_Rage", "Rage",
                "+25 % de dégâts à chaque fois, mais −10 de défense. S'empile jusqu'à 3 fois.",
                new Color(0.9f, 0.2f, 0.5f), EffectPolarity.Buff,
                DurationType.Timed, 8f, StackingRule.Stack, 3,
                new StatModifierComponent(
                    new ModifierEntry(StatType.Damage, ModifierOperation.Multiplicative, 25f),
                    new ModifierEntry(StatType.Defense, ModifierOperation.Additive, -10f)));

            Define("08_Armure", "Armure",
                "+50 de défense pendant 8 s : les coups font moins mal (mais pas le poison).",
                new Color(0.7f, 0.72f, 0.8f), EffectPolarity.Buff,
                DurationType.Timed, 8f, StackingRule.Refresh, 1,
                new StatModifierComponent(new ModifierEntry(StatType.Defense, ModifierOperation.Additive, 50f)));

            Define("09_Etourdissement", "Étourdissement",
                "Pendant 2,5 s : impossible de bouger ou d'attaquer. On ne peut pas le relancer tant qu'il est actif.",
                new Color(1f, 0.9f, 0.3f), EffectPolarity.Debuff,
                DurationType.Timed, 2.5f, StackingRule.Reject, 1,
                new BlockActionsComponent(ActionType.Move | ActionType.Attack));

            Define("10_Benediction", "Bénédiction",
                "Un bonus pour toujours : +25 PV max et +3 dégâts. Une seule bénédiction à la fois.",
                new Color(1f, 0.95f, 0.7f), EffectPolarity.Buff,
                DurationType.Permanent, 1f, StackingRule.Reject, 1,
                new StatModifierComponent(
                    new ModifierEntry(StatType.MaxHealth, ModifierOperation.Additive, 25f),
                    new ModifierEntry(StatType.Damage, ModifierOperation.Additive, 3f)));

            Define("11_Purification", "Purification",
                "Enlève d'un coup tous les effets négatifs (les malus).",
                new Color(0.75f, 0.9f, 1f), EffectPolarity.Buff,
                DurationType.Instant, 1f, StackingRule.Refresh, 1,
                new DispelComponent(EffectPolarity.Debuff));

            AssetDatabase.SaveAssets();
            return SyncLibrary();
        }

        /// <summary>Remplit la bibliothèque avec tous les effets du projet, triés par nom de fichier.</summary>
        [MenuItem("Quête I1/Synchroniser la bibliothèque d'effets", priority = 2)]
        public static EffectLibrary SyncLibrary()
        {
            var definitions = AssetDatabase.FindAssets("t:" + nameof(EffectDefinition))
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .Select(AssetDatabase.LoadAssetAtPath<EffectDefinition>)
                .Where(definition => definition != null)
                .ToList();

            var library = AssetDatabase.LoadAssetAtPath<EffectLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<EffectLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.SetEffects(definitions);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"Bibliothèque d'effets : {definitions.Count} effets.");
            return library;
        }

        public static EffectDefinition Load(string fileName) =>
            AssetDatabase.LoadAssetAtPath<EffectDefinition>($"{EffectsFolder}/{fileName}.asset");

        static void Define(string fileName, string displayName, string description, Color color,
            EffectPolarity polarity, DurationType durationType, float duration, StackingRule rule, int maxStacks,
            params EffectComponent[] components)
        {
            string path = $"{EffectsFolder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<EffectDefinition>(path) != null)
                return;

            var definition = ScriptableObject.CreateInstance<EffectDefinition>();
            definition.Configure(displayName, durationType, duration, rule, maxStacks, components, polarity, color, description);
            AssetDatabase.CreateAsset(definition, path);
        }
    }

    static class EditorFolders
    {
        /// <summary>Crée récursivement un dossier « Assets/... » s'il n'existe pas.</summary>
        public static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
