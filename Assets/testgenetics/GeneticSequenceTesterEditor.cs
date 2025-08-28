using UnityEditor;
using UnityEngine;

namespace UnitySimuLean
{
    [CustomEditor(typeof(GeneticSequenceTester))]
    public class GeneticSequenceTesterEditor : Editor
    {
        SerializedProperty enableOptimizationProp;
        SerializedProperty settingsProp;
        SerializedProperty simulationRunnerProp;

        void OnEnable()
        {
            enableOptimizationProp = serializedObject.FindProperty("enableOptimization");
            settingsProp = serializedObject.FindProperty("settings");
            simulationRunnerProp = serializedObject.FindProperty("simulationRunner");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(enableOptimizationProp);
            EditorGUILayout.PropertyField(simulationRunnerProp);

            if (enableOptimizationProp.boolValue)
            {
                EditorGUILayout.PropertyField(settingsProp, true);

                if (Application.isPlaying)
                {
                    if (GUILayout.Button("Run Optimization"))
                    {
                        ((GeneticSequenceTester)target).RunOptimization();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
