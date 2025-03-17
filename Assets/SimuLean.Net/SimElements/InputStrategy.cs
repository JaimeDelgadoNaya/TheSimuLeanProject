using System;
using System.Collections.Generic;

namespace SimuLean
{
    /// <summary>
    /// Estrategia base para la validación de ítems.
    /// </summary>
    public abstract class InputStrategy
    {
        /// <summary>
        /// Determina si el ítem cumple la estrategia.
        /// </summary>
        /// <param name="item">El ítem a evaluar.</param>
        /// <returns>True si cumple, false en caso contrario.</returns>
        public abstract bool IsValid(Item item);

        /// <summary>
        /// Actualiza la estrategia a partir de la información del ítem.
        /// La implementación por defecto no realiza cambios.
        /// </summary>
        /// <param name="item">El ítem que llega.</param>
        public virtual void UpdateStrategy(Item item)
        {
            // Implementación por defecto: no hace nada.
        }
    }

    /// <summary>
    /// Estrategia por defecto: siempre retorna true.
    /// </summary>
    public class DefaultStrategy : InputStrategy
    {
        public DefaultStrategy()
        {
        }

        public override bool IsValid(Item item)
        {
            return true;
        }
    }

    /// <summary>
    /// Estrategia que valida un ítem en función de un valor requerido de una etiqueta.
    /// </summary>
    public class SingleLabelStrategy : InputStrategy
    {
        private string requiredLabelName;
        private double? requiredLabelValue;

        /// <summary>
        /// Crea una estrategia para validar un ítem basado en el valor de una etiqueta.
        /// </summary>
        /// <param name="requiredLabelName">Nombre de la etiqueta requerida.</param>
        /// <param name="requiredLabelValue">
        /// Valor requerido para la etiqueta. Si no se especifica, se actualizará en UpdateStrategy.
        /// </param>
        public SingleLabelStrategy(string requiredLabelName, double? requiredLabelValue = null)
        {
            this.requiredLabelName = requiredLabelName;
            this.requiredLabelValue = requiredLabelValue;
        }

        /// <summary>
        /// Actualiza la estrategia con el valor de la etiqueta obtenida del ítem.
        /// </summary> 
        /// <param name="item">El ítem del que obtener el valor.</param>
        public override void UpdateStrategy(Item item)
        {
            // Actualiza el valor requerido con el obtenido del ítem.
            requiredLabelValue = item.GetLabelValue(requiredLabelName);
        }

        public override bool IsValid(Item item)
        {
            // Se valida si el ítem contiene la etiqueta y si su valor coincide.
            if (item.Labels.ContainsKey(requiredLabelName))
            {
                double? value = item.GetLabelValue(requiredLabelName);
                return value.HasValue && requiredLabelValue.HasValue && value.Value == requiredLabelValue.Value;
            }
            return false;
        }
    }

    /// <summary>
    /// Estrategia que valida el ítem si alguno de los pares (etiqueta, valor aceptado) coincide.
    /// </summary>
    public class MultiLabelStrategy : InputStrategy
    {
        // Se asume que acceptedLabels mapea el nombre de la etiqueta a un valor aceptado.
        private Dictionary<string, object> acceptedLabels;

        /// <summary>
        /// Crea una estrategia que valida el ítem basado en múltiples etiquetas.
        /// </summary>
        /// <param name="acceptedLabels">
        /// Diccionario con las etiquetas y sus valores aceptados.
        /// Por ejemplo: { "Color": "Rojo", "Tamaño": 10 }
        /// </param>
        public MultiLabelStrategy(Dictionary<string, object> acceptedLabels)
        {
            // Se realiza una copia para evitar modificaciones externas.
            this.acceptedLabels = new Dictionary<string, object>(acceptedLabels);
        }

        public override bool IsValid(Item item)
        {
            Dictionary<string, object> labels = item.GetAllLabels();
            foreach (var kvp in acceptedLabels)
            {
                string labelName = kvp.Key;
                object acceptedValue = kvp.Value;
                if (labels.ContainsKey(labelName))
                {
                    // Se compara el valor del ítem con el valor aceptado.
                    if (labels[labelName].Equals(acceptedValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void UpdateStrategy(Item item)
        {
            Dictionary<string, object> labels = item.GetAllLabels();
            // Actualiza los valores aceptados si el ítem posee la etiqueta.
            foreach (var key in acceptedLabels.Keys)
            {
                if (labels.ContainsKey(key))
                {
                    acceptedLabels[key] = labels[key];
                }
            }
        }
    }
}
