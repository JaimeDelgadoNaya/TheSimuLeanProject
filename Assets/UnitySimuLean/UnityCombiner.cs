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
        public int[] requirements = { 2};
        public int[] initialBatchQuantity = { 2 };      // Lista de requerimientos (capacidad) de cada entrada.
        
        public bool batchMode = false;                      // Modo batch: si true, agrega componentes al ítem padre; de lo contrario, crea un nuevo ítem compuesto.
        public bool updateRequirements = false;             // Si se deben actualizar dinámicamente los requerimientos.
        public string[] updateLabels;                       // Etiquetas para actualizar requerimientos (opcional).

        // Prefab y posiciones para el ítem combinado:
        public GameObject combinedItemPrefab;
        public Transform itemPosition;
        public Transform outItemPosition;
        public float separation = 0f;

        // Parámetros adicionales:
        public double meanDelay = 5.0;                      // Retardo medio (para la estrategia de retardo constante).
        public int capacity;                  // Capacidad (para crear la lista de procesos).
        public string elementName = "Combiner";     // Nombre del elemento.
        Vector3 odVector;
        private Combiner theCombiner;         // Instancia interna del Combiner.

        // Animation
        public Animator serverAnimator;
        public VisualMode visualMode = VisualMode.MainOnly;

        public enum VisualMode
        {
            //Stacked,   // Los items se apilan uno encima del otro. (No utilizar no funciona)
            MainOnly,  // Se muestra solo el item principal y se destruyen los demás.
            NewItem    // Se crea un item nuevo y se eliminan los items originales.
        }


        void Start()
        {
            // Se añade este componente al reloj de simulación.
            UnitySimClock.Instance.Elements.Add(this);
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (serverAnimator != null && theCombiner != null)
            {
                if (theCombiner.GetItems().Count > 0)
                    serverAnimator.SetBool("WorkInProgress", true);
                else
                    serverAnimator.SetBool("WorkInProgress", false);
            }
        }

        public override void ConnectSim()
        {
            if (myInputs.Length != requirements.Length)
            {
                Debug.LogError("Distintos requerimientos y entradas: myInputs.Length=" + myInputs.Length + ", requirements.Length=" + requirements.Length);
                return; // Se evita continuar si las longitudes no coinciden.
            }

            for (int i = 0; i < myInputs.Length; i++)
            {
                GeneralLink.CreateLink(myInputs[i].GetElement(), new List<Element> { theCombiner.GetComponentInput(i) });
                //SimpleLink.CreateLink(myInputs[i].GetElement(), theCombiner.GetComponentInput(i));
            }

            base.ConnectSim();
        }

        public override void InitializeSim()
        {
            //Debug.Log($"{this.name}: Inicializando InitializeSim() en UnityCombiner.");

            // Crear estrategia de retardo constante usando meanDelay.
            DoubleRandomProcess delayProcess = new ConstantRandomProcess((float)meanDelay);
            // Asignar el nombre del elemento a partir del GameObject.
            this.elementName = gameObject.name;
            //Debug.Log($"{this.name}: elementName asignado: {elementName}.");

            // Convertir initialBatchQuantity en un arreglo de requerimientos.
            int[] reqList = requirements;
            // Debug.Log($"{this.name}: Lista de requerimientos creada con valor: {string.Join(", ", reqList)}");

            // Verificar que capacity tenga un valor mayor a 0.
            if (capacity <= 0)
            {
                capacity = 1;
                //Debug.LogWarning($"{this.name}: capacity no estaba definido o es 0. Se asigna capacity = 1 por defecto.");
            }
            List<string> labels = updateLabels != null ? new List<string>(updateLabels) : new List<string>();

            // Crear la instancia del Combiner con la firma actualizada:
            // Parámetros: (requerimientos, estrategia de retardo, nombre, reloj,
            //             modo batch, estrategia de pull, updateRequirements, etiquetas, capacity)
            theCombiner = new Combiner(reqList, delayProcess, elementName, UnitySimClock.Instance.clock,
                                        batchMode, null, updateRequirements, labels, capacity);
            //Debug.Log($"{this.name}: Combiner creado.");

            if (Experimenter.HeadlessActive)
            {
                //Asignar un VElement nulo que no genera graficos
                theCombiner.vElement = new NullVElement();
            }
            else
            {
                //Asignación normal para modo grafico
                theCombiner.vElement = this;
            }
            //Debug.Log($"{this.name}: vElement asignado ({(Experimenter.HeadlessActive ? "NullVElement" : "UnityVElement")}).");



            // Calcular vector de desplazamiento si outItemPosition está asignado.
            if (outItemPosition != null)
            {
                odVector = outItemPosition.position - itemPosition.position;
                //Debug.Log($"{this.name}: odVector calculado: {odVector}.");
            }
            else
            {
                //Debug.LogWarning($"{this.name}: outItemPosition es null, no se calculará odVector.");
            }

            // Debug.Log($"[InitializeSim] {this.name}: Finalizado InitializeSim() con elementName {elementName}.");
        }

        public override void StartSim()
        {
            if (itemPosition == null)
                itemPosition = transform;
            // Debug.Log($"{this.name}: Iniciando StartSim().");
            theCombiner.Start();
        }

        void VElement.ReportState(string msg)
        {
            // Obtenemos la cola de ítems y la convertimos a lista.
            Queue<Item> itemsQueue = theCombiner.GetItems();
            List<Item> itemsList = new List<Item>(itemsQueue);

            if (visualMode == VisualMode.NewItem || visualMode == VisualMode.MainOnly)
            {
                // En modo NewItem la creación y envío se gestiona en CompleteServerProcess.
                // Se limpia la cola para evitar reprocesar el ítem principal.
                itemsQueue.Clear();
            }
            /*
            else if (visualMode == VisualMode.Stacked)   //No utilizar no funciona
            {
                // Lista para acumular todos los GameObjects a apilar.
                List<GameObject> stackItems = new List<GameObject>();

                if (itemsList.Count > 0)
                {
                    // 1. El primer ítem es el principal.
                    if (itemsList[0].vItem is GameObject mainGo)
                    {
                        stackItems.Add(mainGo);
                    }
                    // 2. Si el ítem principal tiene subítems (por ejemplo, items de inputs en batchMode),
                    // se añaden al listado.
                    if (itemsList[0].GetSubItems() != null)
                    {
                        foreach (Item sub in itemsList[0].GetSubItems())
                        {
                            if (sub.vItem is GameObject subGo)
                            {
                                stackItems.Add(subGo);
                            }
                        }
                    }
                    // 3. Si por alguna razón hay más ítems en la cola (además del principal), se agregan.
                    for (int i = 1; i < itemsList.Count; i++)
                    {
                        if (itemsList[i].vItem is GameObject go)
                        {
                            stackItems.Add(go);
                        }
                    }
                }

                // Ahora, posicionamos todos los GameObjects recogidos en stackItems.
                for (int i = 0; i < stackItems.Count; i++)
                {
                    GameObject go = stackItems[i];
                    if (go != null)
                    {
                        go.SetActive(true);
                        // Posición base para el principal y offset vertical para cada siguiente.
                        go.transform.position = itemPosition.position + new Vector3(0f, separation * i, 0f);
                    }
                }
            
            }
            */
        }

        public override Element GetElement()
        {
            return theCombiner;
        }

        // Implementación de VElement:

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

        /// <summary>
        /// Carga el ítem principal en la posición base. Si batchMode = true, 
        /// también procesa sus sub-ítems para mostrarlos visualmente.
        /// </summary>
        void VElement.LoadItem(Item vItem)
        {
            if (visualMode == VisualMode.NewItem || visualMode == VisualMode.MainOnly)
            {
                // En modo NewItem, no se carga el ítem principal, se destruye su representación.
                if (vItem.vItem is GameObject tempGItem)
                {
                    Destroy(tempGItem);
                }
                vItem.vItem = null;
                return;
            }

            // Lógica original para otros modos:
            GameObject gItem = vItem.vItem as GameObject;
            if (gItem != null)
            {
                gItem.transform.position = itemPosition.position + new Vector3(0f, separation * (theCombiner.GetQueueLength() - 1), 0f);
                if (batchMode && vItem.GetSubItems() != null)
                {
                    foreach (Item sub in vItem.GetSubItems())
                    {
                        if (sub.vItem == null)
                        {
                            sub.vItem = (this as VElement).GenerateItem(sub.GetId());
                        }
                        if (sub.vItem is GameObject subGItem)
                        {
                            subGItem.transform.SetParent(gItem.transform, false);
                            subGItem.transform.localPosition = new Vector3(
                                Random.Range(-0.2f, 0.2f),
                                Random.Range(-0.2f, 0.2f),
                                0f
                            );
                        }
                    }
                }
            }
        }





        /// <summary>
        /// Descarga el ítem principal y, en modo batch, también destruye los sub-ítems.
        /// </summary>
        void VElement.UnloadItem(Item vItem)
        {
            if (vItem.vItem is GameObject gItem)
            {
                if (batchMode && vItem.GetSubItems() != null)
                {
                    foreach (Item sub in vItem.GetSubItems())
                    {
                        if (sub.vItem is GameObject subGItem)
                            Destroy(subGItem);
                    }
                }
                Destroy(gItem);
            }
        }

        public override void RestartSim()
        {
            Queue<Item> items = theCombiner.GetItems();
            foreach (Item it in items)
            {
                if (it.vItem is GameObject go)
                    Destroy(go);
                if (batchMode && it.GetSubItems() != null)
                {
                    foreach (Item sub in it.GetSubItems())
                    {
                        if (sub.vItem is GameObject subGo)
                            Destroy(subGo);
                    }
                }
            }
            //Debug.Log($"{this.name}: RestartSim() invocado, reiniciando StartSim().");
            StartSim();
        }
    }
}
