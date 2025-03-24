using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.Port;
using Debug = UnityEngine.Debug;

namespace SimuLean
{
    // Se asume que MultiServer y IArrivalListener están definidos en el proyecto
    public class Combiner : Element, ArrivalListener, WorkStation
    {
        // Campos privados
        Queue<ServerProcess> idleProccesses;
        List<ServerProcess> workInProgress;
        Queue<ServerProcess> completed;

        ServerProcess theProcess;
        private int combinedItemCounter = 0;
        int[] requirements;
        DoubleRandomProcess delayStrategy;
        string name;
        int capacity;
        int currentItems;
        CombinerInput[] inputs;
        bool batchMode;
        InputStrategy pullMode;
        bool updateRequirementsEnabled;
        List<string> updateLabels;

        // Campos para debug y combinación
        private int completedItems = 0; // DEBUG: Contador de combinaciones realizadas
        private bool receivingItems = false; // DEBUG: Indica si se está en proceso de combinación

        public Combiner(
            int[] requirements,
            DoubleRandomProcess delayStrategy,
            string name,
            SimClock simClock,
            bool batchMode = false,
            InputStrategy pullMode = null,
            bool updateRequirements = false,
            List<string> updateLabels = null,
            int capacity = 1)
            : base( name, simClock)
        {
            this.requirements = requirements;
            theProcess = new ServerProcess(this, delayStrategy, 1);
            theProcess.SetState(State.IDLE);
            this.delayStrategy = delayStrategy;
            this.name = name;
            this.batchMode = batchMode;
            this.pullMode = pullMode ?? new DefaultStrategy();
            this.updateRequirementsEnabled = updateRequirements;
            this.updateLabels = updateLabels;
            this.capacity = capacity;
            currentItems = 0;
            // Inicializamos theProcess
            theProcess = new ServerProcess(this, new DoubleRandomProcess[] { delayStrategy }[0], 1);
            theProcess.SetState(State.IDLE);
            Debug.Log($"[Combiner] Constructor: theProcess inicializado, estado = {theProcess.GetState()}.");

            // Estructuras para manejar uno o más procesos (si capacity > 1)
            idleProccesses = new Queue<ServerProcess>(capacity);
            workInProgress = new List<ServerProcess>(capacity);
            completed = new Queue<ServerProcess>(capacity);

            // Creación de entradas (CombinerInput)
            inputs = new CombinerInput[requirements.Length];
            for (int i = 0; i < requirements.Length; i++)
            {
                inputs[i] = new CombinerInput(requirements[i], this, i, $"{name}.Input{i}", simClock, this.pullMode);
                Debug.Log($"[Combiner] Constructor: Input {i} creado con capacidad {requirements[i]}.");
            }
        }


        public override void Start()
        {
            Debug.Log("[Combiner] Start(): Iniciando el Combiner.");
            idleProccesses.Clear();
            workInProgress.Clear();
            completed.Clear();

            // Reconfiguramos el proceso principal y lo encolamos
            theProcess = new ServerProcess(this, delayStrategy, 1);
            theProcess.SetState(State.IDLE);
            idleProccesses.Enqueue(theProcess);

            currentItems = 0;
            Debug.Log($"[Combiner] Start(): theProcess reconfigurado, estado = {theProcess.GetState()}.");

            foreach (var input in inputs)
            {
                input.Start();
            }
            Debug.Log("[Combiner] Start(): Todos los inputs iniciados.");
        }

        // Verifica si el proceso principal está en estado RECEIVING o IDLE (disponible para recibir).
        public bool IsMainReceiving(int inputId)
        {
            if (theProcess == null)
                return false;

            bool result = (theProcess.GetState() == State.RECEIVING || theProcess.GetState() == State.IDLE);
            Debug.Log($"[Combiner] IsMainReceiving: Para entrada {inputId}, theProcess estado = {theProcess.GetState()}, retorna {result}.");
            return result;
        }

        // Retorna la entrada (CombinerInput) correspondiente al índice dado.
        public CombinerInput GetComponentInput(int i)
        {
            if (i < 0 || i >= inputs.Length)
                throw new ArgumentOutOfRangeException(nameof(i), "Índice fuera de rango");
            return inputs[i];
        }

        // Retorna el número de entradas.
        public int GetInputsCount()
        {
            return inputs.Length;
        }

        // Actualiza los requerimientos basados en las etiquetas del ítem principal.
        private void UpdateRequirements(Item theItem)
        {
            if (!updateRequirementsEnabled || updateLabels == null)
                return;

            for (int i = 0; i < updateLabels.Count; i++)
            {
                string label = updateLabels[i];
                var labelValue = theItem.GetLabelValue(label);
                if (labelValue != null && i < inputs.Length)
                {
                    int newReq = Convert.ToInt32(labelValue);
                    requirements[i] = newReq;
                    inputs[i].SetCapacity(newReq);
                    Debug.Log($"[Combiner] UpdateRequirements: Para entrada {i}, nuevo requerimiento = {newReq}.");
                }
            }
        }

        // Si el proceso está bloqueado, intenta enviar el ítem de salida y lo reinicia.
        public override bool Unblock()
        {
            if (theProcess != null && theProcess.GetState() == State.BLOCKED)
            {
                Debug.Log("[Combiner] Unblock(): Proceso en estado BLOCKED, intentando enviar ítem.");

                if (GetOutput().SendItem(theProcess.GetItem(), this))
                {
                    // Si logra enviar el ítem, sacamos el proceso de completed
                    if (completed.Count > 0) completed.Dequeue();

                    idleProccesses.Enqueue(theProcess);
                    theProcess.SetState(State.IDLE);

                    vElement.ReportState("Exit");

                    // Notificamos que este Combiner está disponible
                    GetInput().NotifyAvaliable(this);

                    Debug.Log("[Combiner] Unblock(): Proceso enviado, estado reiniciado a IDLE.");
                    return true;
                }
                else
                {
                    Debug.LogWarning("[Combiner] Unblock(): No se pudo enviar el ítem, se mantiene BLOCKED.");
                    return false;
                }
            }
            return false;
        }

        VElement ArrivalListener.GetVElement()
        {
            return vElement;
        }

        // Recibe un ítem y actualiza el proceso, la estrategia y los requerimientos.
        public override bool Receive(Item theItem)
        {
            if (theProcess == null)
            {
                Debug.LogError("[Combiner] Receive(): theProcess es null.");
                return false;
            }
            if (theProcess.GetState() == State.IDLE && currentItems < capacity)
            {
                Debug.Log($"[Combiner] Receive(): Recibiendo ítem {theItem.GetId()}.");
                theProcess.SetState(State.RECEIVING);
                theProcess.SetItem(theItem);
                pullMode.UpdateStrategy(theItem);
                UpdateRequirements(theItem);

                for (int i = 0; i < GetInputsCount(); i++)
                {
                    GetComponentInput(i).Unblock();
                }
                return true;
            }
            Debug.Log($"[Combiner] Receive(): No se pudo recibir el ítem {theItem.GetId()} porque el proceso no está en estado IDLE.");
            return false;
        }

        // Notificado cuando una componente (entrada) recibe un ítem.
        public bool ComponentReceived(Item theItem, int source)
        {
            Debug.Log($"[Combiner] ComponentReceived(): Recibido ítem {theItem.GetId()} desde entrada {source}.");
            // Forzamos la comprobación de requisitos sin importar el estado actual.
            bool result = CheckRequirements();
            Debug.Log($"[Combiner] ComponentReceived(): CheckRequirements retornó {result}.");
            return result;
        }

        // Verifica si todas las entradas cumplen los requerimientos para continuar y, en ese caso, combina los ítems.
        private bool CheckRequirements()
        {
            Debug.Log("[Combiner] CheckRequirements: Invocado.");

            // (Opcional) Podríamos requerir que theProcess esté en RECEIVING, 
            // pero a veces se suelta otro ítem después de una combinación. 

            bool ready = true;
            for (int i = 0; i < inputs.Length; i++)
            {
                int qLength = inputs[i].GetQueueLength();
                Debug.Log($"[Combiner] CheckRequirements: Entrada {i} tiene {qLength} ítems; requerimiento = {requirements[i]}.");
                if (qLength < requirements[i])
                {
                    Debug.Log($"[Combiner] CheckRequirements: Entrada {i} NO cumple el requerimiento.");
                    ready = false;
                    break;
                }
                else
                {
                    Debug.Log($"[Combiner] CheckRequirements: Entrada {i} cumple el requerimiento.");
                }
            }

            if (ready)
            {
                Debug.Log("[Combiner] CheckRequirements: Todos los requerimientos se cumplen. Procediendo a combinar.");
                completedItems++;
                receivingItems = true;
                currentItems++;
                // Creamos el ítem final (combinado)
                Item newItem = CreateNewItem();
                Debug.Log($"[Combiner] CheckRequirements: Nuevo ítem combinado creado con ID {newItem.GetId()}.");

                // Por cada entrada, liberamos 'requirements[i]' ítems
                for (int i = 0; i < inputs.Length; i++)
                {
                    var items = inputs[i].Release(requirements[i]);
                    Debug.Log($"[Combiner] CheckRequirements: Liberados {items.Count} ítems de la entrada {i}.");

                    foreach (var item in items)
                    {
                        // Si no quieres verlos más en escena, haz:
                        vElement.UnloadItem(item);

                        // Si es batchMode, se “agregan” físicamente al newItem
                        // (depende de tu definición; si no usas batch, simplemente se descargan y ya)
                        if (batchMode)
                        {
                            newItem.AddItem(item);
                            Debug.Log($"[Combiner] CheckRequirements: Agregado ítem {item.GetId()} al newItem en modo batch.");
                        }
                        else
                        {
                            // En modo normal, solo “desaparecen” (Unload) y no se guarda relación
                            Debug.Log($"[Combiner] CheckRequirements: Ítem {item.GetId()} descargado (no batch).");
                        }
                    }
                }

                receivingItems = false;

                // El proceso pasa a BUSY y le asignamos el newItem
                theProcess.SetItem(newItem);
                theProcess.SetState(State.BUSY);

                // Obtenemos el retardo y programamos el evento
                double delayTime = theProcess.GetDelay();
                Debug.Log($"[Combiner] CheckRequirements: Programando evento con retardo {delayTime}.");
                simClock.ScheduleEvent(theProcess, delayTime);

                return true;
            }
            else
            {
                Debug.Log("[Combiner] CheckRequirements: No se cumplen los requerimientos.");
            }
            return false;
        }

        // Crea un nuevo ítem usando el tiempo actual del reloj de simulación.
        public Item CreateNewItem()
        {
            Debug.Log("[Combiner] CreateNewItem: Invocado.");

            Item newItem = new Item(simClock.GetSimulationTime());
            combinedItemCounter++;
            // Asigna un ID, p.e. "type=combinedItemCounter;"
            newItem.SetId("type",1,1);

            if (vElement == null)
            {
                Debug.LogError("[Combiner] CreateNewItem: vElement es null; no se puede generar la parte visual.");
            }
            else
            {
                // Generar objeto visual
                newItem.vItem = vElement.GenerateItem(0);
                if (newItem.vItem == null)
                {
                    Debug.LogError("[Combiner] CreateNewItem: GenerateItem retornó null.");
                }
                else
                {
                    Debug.Log($"[Combiner] CreateNewItem: Nuevo ítem creado con ID {newItem.GetId()} y vItem asignado.");
                }
            }
            return newItem;
        }

        // Completa el proceso del servidor: intenta enviar el ítem resultante y actualiza el estado.
        void WorkStation.CompleteServerProcess(ServerProcess theprocess)
        {
            Item theItem = theprocess.theItem;
            Debug.Log($"[Combiner] CompleteServerProcess(): Proceso completado para ítem {theItem.GetId()}.");

            bool sendOk = GetOutput().SendItem(theItem, this);
            if (sendOk)
            {
                Debug.Log($"[Combiner] CompleteServerProcess(): Ítem {theItem.GetId()} enviado a la salida.");
                // Decrementamos el contador de combinaciones activas
                currentItems--;
                theprocess.SetState(State.IDLE);
                idleProccesses.Enqueue(theprocess);

                // Reemplazamos el proceso activo con uno inactivo disponible
                if (idleProccesses.Count > 0)
                {
                    theProcess = idleProccesses.Dequeue();
                }

                vElement.ReportState("Exit");

                // Se verifica si hay más ítems para combinar
                CheckRequirements();
                GetInput().NotifyAvaliable(this);
            }
            else
            {
                Debug.LogWarning($"[Combiner] CompleteServerProcess(): No se pudo enviar el ítem {theItem.GetId()}, proceso bloqueado.");
                theprocess.SetState(State.BLOCKED);
                completed.Enqueue(theprocess);
                // Reintento después de 1 unidad de tiempo
                simClock.ScheduleEvent(theprocess, 1.0);
            }
        }

        // Verifica la disponibilidad del combiner (disponible si el proceso principal está en estado IDLE).
        public override bool CheckAvaliability(Item theItem)
        {
            // Ahora se verifica que el proceso activo esté en IDLE y que no se supere la capacidad de combinaciones.
            bool available = (theProcess != null && theProcess.GetState() == State.IDLE && currentItems < capacity);
            Debug.Log($"[Combiner] CheckAvaliability: Retorna {available} (currentItems: {currentItems}, capacity: {capacity}).");
            return available;
        }

        //Getting all the current items to display them
        public  Queue<Item> GetItems()
        {
            Queue<Item> myItems = new Queue<Item>();
            foreach (ServerProcess sp in workInProgress)
            {
                myItems.Enqueue(sp.GetCurrentItem());
            }

            foreach (ServerProcess sp in completed)
            {
                myItems.Enqueue(sp.GetCurrentItem());
            }

            return myItems;
        }

        // Métodos de ArrivalListener (se pueden ajustar según la interfaz definida)
        public void ItemReceived(Item theItem, int source)
        {
            Debug.Log($"[Combiner] ItemReceived: Notificando recepción del ítem {theItem.GetId()} desde entrada {source}.");
            ComponentReceived(theItem, source);
        }
        override public int GetQueueLength()
        {
            int q = 0;

            foreach (CombinerInput ci in inputs)
            {
                q += ci.GetQueueLength();
            }

            return workInProgress.Count + completed.Count + q;
        }
        override public int GetFreeCapacity()
        {
            return capacity - currentItems;
        }
    }

    
}
