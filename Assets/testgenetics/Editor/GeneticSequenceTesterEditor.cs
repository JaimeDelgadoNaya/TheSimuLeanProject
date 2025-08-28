#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnitySimuLean
{
    [CustomEditor(typeof(GeneticSequenceTester))]
    public class GeneticSequenceTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var enableOpt = serializedObject.FindProperty("enableOptimization");
            EditorGUILayout.PropertyField(enableOpt);

            if (enableOpt.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("numberOfParts"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("generations"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("populationSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationRunner"));

                if (GUILayout.Button("Run Optimization"))
                {
                    ((GeneticSequenceTester)target).RunOptimization();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
