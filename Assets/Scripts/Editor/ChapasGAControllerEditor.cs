#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ChapasGA.Mono;

namespace ChapasGA.Editor
{
    [CustomEditor(typeof(ChapasGAController))]
    public class ChapasGAControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var controller = (ChapasGAController)target;
            if (GUILayout.Button("Cargar Excel"))
            {
                controller.LoadExcel();
            }
            if (GUILayout.Button("Ejecutar GA"))
            {
                controller.RunGA();
            }
            if (GUILayout.Button("Exportar CSV"))
            {
                controller.ExportCSV();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BestFitness", controller.BestFitness.ToString("F2"));
            EditorGUILayout.LabelField("Inspections", controller.TotalInspections.ToString());
            EditorGUILayout.LabelField("Delays", controller.TotalDelays.ToString());
        }
    }
}
#endif
