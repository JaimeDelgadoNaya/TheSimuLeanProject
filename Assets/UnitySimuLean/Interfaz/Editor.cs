using UnityEngine;
using UnityEditor;
using UnitySimuLean;

[CustomEditor(typeof(UnityCombiner))]
public class UnityCombinerEditor : Editor
{
    SerializedProperty batchModeProp;
    SerializedProperty initialBatchQuantityProp;

    void OnEnable()
    {
        // Se buscan las propiedades serializadas "batchMode" e "initialBatchQuantity" en UnityCombiner.
        batchModeProp = serializedObject.FindProperty("batchMode");
        initialBatchQuantityProp = serializedObject.FindProperty("initialBatchQuantity");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Dibujar el campo batchMode siempre.
        EditorGUILayout.PropertyField(batchModeProp);

        // Si batchMode está activado, se muestra el campo initialBatchQuantity.
        if (batchModeProp.boolValue)
        {
            EditorGUILayout.PropertyField(initialBatchQuantityProp, true);
        }

        // Dibujar el resto de las propiedades excepto las que ya se han mostrado.
        DrawPropertiesExcluding(serializedObject, "batchMode", "initialBatchQuantity");

        serializedObject.ApplyModifiedProperties();
    }
}
