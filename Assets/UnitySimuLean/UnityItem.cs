using System.Collections.Generic;
using UnityEngine;

namespace SimuLean
{
    public class UnityItem : MonoBehaviour
    {
        [Header("Modelo")]
        public Item item;  // Asigna el Item desde código o inspección (debug)

        [Header("Etiquetas (Labels)")]
        [SerializeField]
        private List<LabelEntry> labels = new List<LabelEntry>();

        [System.Serializable]
        public class LabelEntry
        {
            public string key;
            public string value;
        }

        void OnValidate()
        {
            if (item != null)
            {
                UpdateLabelsFromItem();
            }
        }

        [ContextMenu("Actualizar etiquetas")]
        public void UpdateLabelsFromItem()
        {
            labels.Clear();
            Dictionary<string, object> itemLabels = item.GetAllLabels();
            foreach (var kvp in itemLabels)
            {
                labels.Add(new LabelEntry
                {
                    key = kvp.Key,
                    value = kvp.Value.ToString()
                });
            }
        }
    }
}
