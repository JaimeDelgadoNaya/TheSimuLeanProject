using UnityEditor;
using UnityEngine;

namespace UnitySimuLean
{
    [CustomEditor(typeof(GeneticSequenceTester))]
    public class GeneticSequenceTesterEditor : Editor
    {
        private SerializedProperty _enableOptimization;
        private SerializedProperty _numberOfParts;
        private SerializedProperty _generations;
        private SerializedProperty _populationSize;
        private SerializedProperty _simulationRunner;

        private void OnEnable()
        {
            _enableOptimization = serializedObject.FindProperty("enableOptimization");
            _numberOfParts = serializedObject.FindProperty("numberOfParts");
            _generations = serializedObject.FindProperty("generations");
            _populationSize = serializedObject.FindProperty("populationSize");
            _simulationRunner = serializedObject.FindProperty("simulationRunner");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_enableOptimization, new GUIContent("Enable Optimization"));

            if (_enableOptimization.boolValue)
            {
                EditorGUILayout.PropertyField(_numberOfParts);
                EditorGUILayout.PropertyField(_generations);
                EditorGUILayout.PropertyField(_populationSize);
                EditorGUILayout.PropertyField(_simulationRunner);

                if (GUILayout.Button("Run Optimization"))
                {
                    foreach (var t in targets)
                    {
                        var tester = (GeneticSequenceTester)t;
                        tester.RunOptimization();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
