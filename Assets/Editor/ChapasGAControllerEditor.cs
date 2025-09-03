using ChapasGA.Mono;
using UnityEditor;
using UnityEngine;

namespace ChapasGA.Editor
{
    [CustomEditor(typeof(ChapasGAController))]
    public class ChapasGAControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var controller = (ChapasGAController)target;
            GUILayout.Space(10);
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
            EditorGUILayout.LabelField("BestFitness", controller.BestFitness.ToString());
            EditorGUILayout.LabelField("TotalInspections", controller.TotalInspections.ToString());
            EditorGUILayout.LabelField("TotalDelays", controller.TotalDelays.ToString());
            EditorGUILayout.LabelField("CSV Path", controller.CsvPath);
        }
    }
}
