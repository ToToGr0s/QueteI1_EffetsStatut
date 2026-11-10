using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace QueteI1.StatusEffects.EditorTools
{
    /// <summary>
    /// Inspecteur d'un effet : n'affiche que les champs utiles selon le type de durée et la règle
    /// de cumul, et permet d'ajouter n'importe quel composant d'effet (trouvé automatiquement).
    /// </summary>
    [CustomEditor(typeof(EffectDefinition))]
    public sealed class EffectDefinitionEditor : Editor
    {
        SerializedProperty displayName, description, color, polarity;
        SerializedProperty durationType, duration, stackingRule, maxStacks, separatePerSource, components;

        void OnEnable()
        {
            displayName = serializedObject.FindProperty("displayName");
            description = serializedObject.FindProperty("description");
            color = serializedObject.FindProperty("color");
            polarity = serializedObject.FindProperty("polarity");
            durationType = serializedObject.FindProperty("durationType");
            duration = serializedObject.FindProperty("duration");
            stackingRule = serializedObject.FindProperty("stackingRule");
            maxStacks = serializedObject.FindProperty("maxStacks");
            separatePerSource = serializedObject.FindProperty("separatePerSource");
            components = serializedObject.FindProperty("components");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(displayName, new GUIContent("Nom affiché"));
            EditorGUILayout.PropertyField(description, new GUIContent("Description"));
            EditorGUILayout.PropertyField(color, new GUIContent("Couleur"));
            EditorGUILayout.PropertyField(polarity, new GUIContent("Polarité"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Durée", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(durationType, new GUIContent("Type"));
            var type = (DurationType)durationType.enumValueIndex;
            if (type == DurationType.Timed)
                EditorGUILayout.PropertyField(duration, new GUIContent("Durée (s)"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cumul", EditorStyles.boldLabel);
            if (type == DurationType.Instant)
            {
                EditorGUILayout.HelpBox("Effet instantané : appliqué une fois puis terminé, aucune règle de cumul.", MessageType.None);
            }
            else
            {
                EditorGUILayout.PropertyField(stackingRule, new GUIContent("Si déjà actif"));
                var rule = (StackingRule)stackingRule.enumValueIndex;
                if (rule == StackingRule.Stack)
                    EditorGUILayout.PropertyField(maxStacks, new GUIContent("Stacks max"));
                EditorGUILayout.PropertyField(separatePerSource, new GUIContent("Une instance par source"));
                EditorGUILayout.HelpBox(rule switch
                {
                    StackingRule.Stack => "Ajoute un stack (jusqu'au max) et remet la durée à zéro. Au max, seule la durée est remise à zéro.",
                    StackingRule.Refresh => "Remet la durée à zéro, sans stack supplémentaire.",
                    _ => "La nouvelle application est refusée tant que l'effet est actif."
                }, MessageType.None);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Composants (ce que fait l'effet)", EditorStyles.boldLabel);
            DrawComponents();

            if (GUILayout.Button("Ajouter un composant…"))
                ShowAddMenu();

            serializedObject.ApplyModifiedProperties();

            var definition = (EffectDefinition)target;
            var warnings = new List<string>();
            definition.CollectWarnings(warnings);
            foreach (var warning in warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        void DrawComponents()
        {
            for (int i = 0; i < components.arraySize; i++)
            {
                var element = components.GetArrayElementAtIndex(i);
                var component = element.managedReferenceValue as EffectComponent;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string title = component == null ? "Composant vide" : ComponentName(component.GetType());
                        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                        GUI.enabled = i > 0;
                        if (GUILayout.Button("▲", GUILayout.Width(24f)))
                        {
                            components.MoveArrayElement(i, i - 1);
                            break;
                        }
                        GUI.enabled = i < components.arraySize - 1;
                        if (GUILayout.Button("▼", GUILayout.Width(24f)))
                        {
                            components.MoveArrayElement(i, i + 1);
                            break;
                        }
                        GUI.enabled = true;
                        if (GUILayout.Button("✕", GUILayout.Width(24f)))
                        {
                            components.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }

                    if (component == null)
                        continue;

                    var iterator = element.Copy();
                    var end = element.GetEndProperty();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
                    {
                        enterChildren = false;
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    EditorGUILayout.LabelField(component.Describe(), EditorStyles.miniLabel);
                }
            }
            GUI.enabled = true;
        }

        void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom<EffectComponent>())
            {
                if (type.IsAbstract || type.IsGenericType || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                var componentType = type;
                menu.AddItem(new GUIContent(ComponentName(type)), false, () =>
                {
                    serializedObject.Update();
                    components.arraySize++;
                    components.GetArrayElementAtIndex(components.arraySize - 1).managedReferenceValue =
                        Activator.CreateInstance(componentType);
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        static string ComponentName(Type type)
        {
            var info = type.GetCustomAttribute<EffectComponentInfoAttribute>();
            return info != null ? info.Name : ObjectNames.NicifyVariableName(type.Name);
        }
    }
}
