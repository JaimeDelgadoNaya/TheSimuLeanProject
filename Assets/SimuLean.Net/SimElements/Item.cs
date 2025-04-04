using System.Collections.Generic;
using UnityEngine;

namespace SimuLean
{
    /// <summary>
    /// Base class for simulation items.
    /// </summary>
    public class Item
    {
        static int ITEM_NUMBER = 0;

        double creationTime;

        private string type;
        private int myId;
        private int myConstrainedInput;

        public int priority;

        public object vItem;

        public List<Item> subItems;

        protected Dictionary<string, double> attribDouble;


        public Item(double creationTime)
        {
            Item.ITEM_NUMBER++;

            this.creationTime = creationTime;
        }

        public void SetId(string type, int myId, int priority)
        {

            this.type = type;
            this.myId = myId;
            this.priority = priority;
        }

        public int GetId()
        {
            return myId;
        }

        public void SetcreationTime(double creationTime)
        {
            this.creationTime = creationTime;
        }

        public double GetCreationTime()
        {
            return creationTime;
        }

        public void SetConstrainedInput(int myConstrainedInput)
        {
            this.myConstrainedInput = myConstrainedInput;
        }

        public int GetConstrainedInput()
        {
            return myConstrainedInput;
        }

        public void AddItem(Item theItem)
        {
            if (subItems == null)
            {
                subItems = new List<Item>();
            }
            subItems.Add(theItem);
        }

        public List<Item> GetSubItems()
        {
            return subItems;
        }
        // ---------------------------
        // Métodos agregados para asignar tipo y atributos extra:
        public void SetType(string newType)
        {
            this.type = newType;
        }

        /// <summary>
        /// Asigna un atributo extra como un valor double.
        /// Si necesitas trabajar con otros tipos, puedes sobrecargar o convertir según sea necesario.
        /// </summary>
        public void SetLabelValue(string label, string value)
        {
            if (double.TryParse(value, out double dValue))
            {
                if (attribDouble == null)
                    attribDouble = new Dictionary<string, double>();
                attribDouble[label] = dValue;
            }
            else
            {
                // Si no es numérico, puedes decidir almacenarlo de otra forma,
                // o lanzar una excepción, o simplemente ignorarlo.
                // Aquí simplemente lo ignoramos.
            }
        }
        public int? GetBatchRequirement()
        {
            // Se asume que si el ítem tiene definido un atributo "BatchRequirement", se utilizará ese valor.
            // Puedes ajustar la clave ("BatchRequirement") según tu convención.
            if (attribDouble != null && attribDouble.ContainsKey("BatchRequirement"))
            {
                return (int)attribDouble["BatchRequirement"];
            }
            return null;
        }

        public double? GetLabelValue(string label)
        {
            if (attribDouble != null && attribDouble.ContainsKey(label))
            {
                return attribDouble[label];
            }
            return null;
        }

        /// <summary>
        /// Propiedad que retorna todas las etiquetas del ítem como un diccionario de string a object.
        /// Se utiliza para la validación en estrategias de entrada.
        /// </summary>
        public Dictionary<string, object> Labels
        {
            get
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                if (attribDouble != null)
                {
                    foreach (var kvp in attribDouble)
                    {
                        dict[kvp.Key] = kvp.Value;
                    }
                }
                return dict;
            }
        }

        /// <summary>
        /// Devuelve todas las etiquetas del ítem.
        /// </summary>
        public Dictionary<string, object> GetAllLabels()
        {
            return Labels;
        }

    }
}

