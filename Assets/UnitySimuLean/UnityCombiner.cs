using SimuLean;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySimuLean
{
    /// <summary>
    /// Unity Component for Combiner element.
    /// Este componente inicializa y controla un Combiner de SimuLean.
    /// </summary>
    public class UnityCombiner : SElement, VElement
    {
        // Configuración del Combiner:
        public SElement[] myInputs;
        public int[] requirements = { 1 };
        public int initialBatchQuantity = 1;      // Valor para la lista de requerimientos (capacidad) de cada entrada.
        public double meanDelay = 5.0;              // Retardo medio (para la estrategia de retardo constante).
        public bool batchMode = false;              // Modo batch: si true, agrega componentes al ítem padre; de lo contrario, crea un nuevo ítem compuesto.
        public bool updateRequirements = false;     // Si se debe actualizar dinámicamente los requerimientos.
        public string[] updateLabels;               // Etiquetas para actualizar requerimientos (opcional).

        // Prefab y posiciones para el ítem combinado:
        public GameObject combinedItemPrefab;
        public Transform itemPosition;
        public Transform outItemPosition;
        public float separation = 0f;

        // Parámetros adicionales:
        public double cTime = 2.0;                  // Para compatibilidad.
        public int capacity = 1;                    // Capacidad (se usa para crear la lista de procesos).
        public string elementName = "Combiner";     // Nombre del elemento.
        private SElement nextElement;
        Vector3 odVector;
        private Combiner theCombiner;               // Instancia interna del Combiner.

        

        void Start()
        {
            // Se añade este componente al reloj de simulación.
            UnitySimClock.Instance.Elements.Add(this);
        }

        override public void ConnectSim()
        {
            if (myInputs.Length != requirements.Length)
            {
                Debug.LogError("Distintos requerimientos y entradas: myInputs.Length=" + myInputs.Length + ", requirements.Length=" + requirements.Length);
                return; // Evitamos seguir si las longitudes no coinciden
            }

            for (int i = 0; i < myInputs.Length; i++)
            {
                SimpleLink.CreateLink(myInputs[i].GetElement(), theCombiner.GetComponentInput(i));
            }

            base.ConnectSim();
        }

        public override void InitializeSim()
        {
            Debug.Log($"{this.name}: Inicializando InitializeSim() en UnityCombiner.");
            // Crear estrategia de retardo constante usando meanDelay.
            DoubleRandomProcess delayProcess = new ConstantRandomProcess((float)meanDelay);
            // Asignar el nombre del elemento a partir del GameObject.
            this.elementName = gameObject.name;
            Debug.Log($"{this.name}: elementName asignado: {elementName}.");

            // Convertir initialBatchQuantity en una lista de requerimientos.
            List<int> reqList = new List<int> { initialBatchQuantity };
            Debug.Log($"{this.name}: Lista de requerimientos creada con valor: {initialBatchQuantity}.");

            // Crear la instancia del Combiner con la firma actualizada:
            // Parámetros: (lista de requerimientos, estrategia de retardo, nombre, reloj,
            //             modo batch, estrategia de pull, actualización de requerimientos, etiquetas, capacity)
            theCombiner = new Combiner(reqList, delayProcess, elementName, UnitySimClock.Instance.clock,
                                        batchMode, null, updateRequirements, new List<string>(updateLabels), capacity);
            Debug.Log($"{this.name}: Combiner creado.");

            // Asignar este componente como interfaz visual (vElement) para reportar estado y gestionar ítems.
            theCombiner.vElement = this;
            Debug.Log($"{this.name}: vElement asignado en el Combiner.");

            // Calcular vector de desplazamiento si outItemPosition está asignado.
            if (outItemPosition != null)
            {
                odVector = outItemPosition.position - itemPosition.position;
                Debug.Log($"{this.name}: odVector calculado: {odVector}.");
            }
            else
            {
                Debug.LogWarning($"{this.name}: outItemPosition es null, no se calculará odVector.");
            }

            Debug.Log($"[InitializeSim] {this.name}: Finalizado InitializeSim() con elementName {elementName}.");
        }

        public override void StartSim()
        {
            if (itemPosition == null)
            {
                itemPosition = transform;
            }
            Debug.Log($"{this.name}: Iniciando StartSim().");
            theCombiner.Start();
        }

        public override Element GetElement()
        {
            return theCombiner;
        }

        // Implementación de VElement:
        void VElement.ReportState(string msg)
        {
            Debug.Log($"Combiner state: {msg}");
        }

        object VElement.GenerateItem(int myId)
        {
            if (combinedItemPrefab == null)
                return null;

            GameObject newItem = Instantiate(combinedItemPrefab);
            newItem.SetActive(true);
            newItem.transform.position = transform.position;
            newItem.name = "CombinedItem" + myId;
            return newItem;
        }

        void VElement.LoadItem(Item vItem)
        {
            GameObject gItem = vItem.vItem as GameObject;
            if (gItem != null)
            {
                // Posicionar el ítem basado en itemPosition y separarlo según la cantidad total en la cola.
                gItem.transform.position = itemPosition.position + new Vector3(0f, separation * (theCombiner.GetQueueLength() - 1), 0f);
            }
        }

        void VElement.UnloadItem(Item vItem)
        {
            if (vItem.vItem is GameObject gItem)
            {
                Destroy(gItem);
            }
        }

        public override void RestartSim()
        {
            Queue<Item> items = theCombiner.GetItems();
            foreach (Item it in items)
            {
                Destroy(it.vItem as GameObject);
            }
            Debug.Log($"{this.name}: RestartSim() invocado, reiniciando StartSim().");
            StartSim();
        }
    }
}
