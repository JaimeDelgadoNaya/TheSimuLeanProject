using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimuLean
{
    /// <summary>
    /// Combiner: procesa la ensamblación de ítems, esperando que cada entrada cumpla su requerimiento.
    /// Hereda de Element e implementa ArrivalListener y WorkStation.
    /// Adaptado para manejar múltiples procesos mediante colas y utilizar el nuevo CombinerInput.
    /// </summary>
    public class Combiner : Element, ArrivalListener, WorkStation
    {
        // Estructuras para manejo de múltiples procesos
        private Queue<ServerProcess> idleProcesses;
        private List<ServerProcess> workInProgress;
        private Queue<ServerProcess> completed;
        private int capacity;

        private List<int> requirements;
        private DoubleRandomProcess delayStrategy;
        private bool batchMode;
        private InputStrategy pullMode;
        private bool updateRequirementsEnabled;
        private List<string> updateLabels;
        private List<CombinerInput> inputs;

        bool receivingItems = false;
        int completedItems;

        /// <summary>
        /// Constructor actualizado.
        /// Orden de parámetros:
        /// requirements, delayStrategy, name, sClock, batchMode, pullMode, updateRequirements, updateLabels, capacity
        /// </summary>
        public Combiner(
            List<int> requirements,
            DoubleRandomProcess delayStrategy,
            string name,
            SimClock sClock,
            bool batchMode = false,
            InputStrategy pullMode = null,
            bool updateRequirements = false,
            List<string> updateLabels = null,
            int capacity = 1)
            : base(name, sClock)
        {
            this.requirements = new List<int>(requirements);
            this.delayStrategy = delayStrategy;
            this.batchMode = batchMode;
            this.pullMode = pullMode ?? new DefaultStrategy();
            this.updateRequirementsEnabled = updateRequirements;
            this.updateLabels = updateLabels;
            this.capacity = capacity;

            idleProcesses = new Queue<ServerProcess>(capacity);
            workInProgress = new List<ServerProcess>(capacity);
            completed = new Queue<ServerProcess>(capacity);

            // Crear una entrada (CombinerInput) por cada requerimiento utilizando el nuevo constructor
            inputs = new List<CombinerInput>();
            for (int i = 0; i < requirements.Count; i++)
            {
                inputs.Add(new CombinerInput(requirements[i], this, i, $"{name}.Input{i}", sClock, this.pullMode));
            }
            Debug.Log($"{GetName()}: Constructor de Combiner completado. Número de entradas: {inputs.Count}");
        }

        /// <summary>
        /// Inicia el proceso del Combiner.
        /// Crea procesos y llama a Start() en cada entrada.
        /// </summary>
        public override void Start()
        {
            Debug.Log($"{GetName()}: Start() de Combiner invocado.");
            idleProcesses.Clear();
            workInProgress.Clear();
            completed.Clear();

            // Crear procesos según la capacidad
            for (int i = 0; i < capacity; i++)
            {
                ServerProcess process = new ServerProcess(this, delayStrategy, 1);
                process.SetState(State.IDLE);
                idleProcesses.Enqueue(process);
            }

            // Iniciar cada entrada
            foreach (var input in inputs)
            {
                input.Start();
                Debug.Log($"{GetName()}: Entrada {inputs.IndexOf(input)} inicializada (Start() de CombinerInput).");
            }

            completedItems = 0;
        }

        /// <summary>
        /// Recibe un ítem, actualiza la estrategia y verifica requerimientos.
        /// </summary>
        public override bool Receive(Item theItem)
        {
            Debug.Log($"{GetName()}: Receive() llamado para el item {theItem.GetId()}.");
            pullMode.UpdateStrategy(theItem);
            UpdateRequirements(theItem);
            CheckRequirements();
            return true;
        }

        /// <summary>
        /// Notifica que una entrada ha recibido un ítem, para desencadenar la verificación.
        /// </summary>
        public bool ComponentReceived(Item theItem, int source)
        {
            Debug.Log($"{GetName()}: ComponentReceived() llamado para item {theItem.GetId()} en entrada {source}.");
            CheckRequirements();
            return true;
        }

        /// <summary>
        /// Implementación de ItemReceived de ArrivalListener.
        /// </summary>
        public void ItemReceived(Item theItem, int source)
        {
            Debug.Log($"{GetName()}: ItemReceived() llamado para item {theItem.GetId()} en entrada {source}.");
            ComponentReceived(theItem, source);
        }

        /// <summary>
        /// Implementación de GetVElement de ArrivalListener.
        /// </summary>
        public VElement GetVElement() => vElement;

        /// <summary>
        /// Actualiza los requerimientos basados en las etiquetas del ítem principal.
        /// </summary>
        private void UpdateRequirements(Item theItem)
        {
            if (!updateRequirementsEnabled || updateLabels == null)
            {
                Debug.Log($"{GetName()}: UpdateRequirements() no se ejecuta (updateRequirementsEnabled: {updateRequirementsEnabled}, updateLabels: {(updateLabels == null ? "null" : "asignado")}).");
                return;
            }

            Debug.Log($"{GetName()}: Ejecutando UpdateRequirements() para el item {theItem.GetId()}.");
            for (int i = 0; i < updateLabels.Count; i++)
            {
                string label = updateLabels[i];
                var labelValue = theItem.GetLabelValue(label);
                if (labelValue != null && i < inputs.Count)
                {
                    int newReq = Convert.ToInt32(labelValue);
                    requirements[i] = newReq;
                    inputs[i].SetCapacity(newReq);
                    Debug.Log($"{GetName()}: Requerimiento actualizado para entrada {i}: {newReq}");
                }
            }
        }

        /// <summary>
        /// Verifica si se cumplen los requerimientos de todas las entradas.
        /// Si se cumplen, libera ítems y programa un proceso.
        /// </summary>
        private void CheckRequirements()
        {
            if (idleProcesses.Count == 0)
            {
                Debug.Log($"{GetName()}: No hay procesos inactivos disponibles.");
                return;
            }

            bool ready = true;
            for (int i = 0; i < inputs.Count; i++)
            {
                int qLength = inputs[i].GetQueueLength();
                if (qLength < requirements[i])
                {
                    Debug.Log($"{GetName()}: Entrada {i} no cumple requerimiento (cola: {qLength}, requerido: {requirements[i]}).");
                    ready = false;
                    break;
                }
                else
                {
                    Debug.Log($"{GetName()}: Entrada {i} cumple requerimiento (cola: {qLength}, requerido: {requirements[i]}).");
                }
            }

            if (ready)
            {
                Debug.Log($"{GetName()}: Todos los requerimientos cumplidos. Liberando ítems.");
                Item newItem = CreateNewItem();
                ServerProcess process = idleProcesses.Dequeue();

                for (int i = 0; i < inputs.Count; i++)
                {
                    // Release ahora devuelve Queue<Item> según el nuevo CombinerInput
                    var items = inputs[i].Release(requirements[i]);
                    foreach (var item in items)
                    {
                        if (batchMode)
                        {
                            newItem.AddItem(item);
                            Debug.Log($"{GetName()}: Ítem {item.GetId()} agregado al item principal (batchMode).");
                        }
                        else
                        {
                            Debug.Log($"{GetName()}: Ítem {item.GetId()} liberado de la entrada {i}.");
                        }
                    }
                }

                process.SetItem(newItem);
                process.SetState(State.BUSY);
                workInProgress.Add(process);
                float delayTime = (float)process.GetDelay();
                Debug.Log($"{GetName()}: Requerimientos completos. Programando evento con retardo {delayTime}.");
                simClock.ScheduleEvent(process, delayTime);

                // Verificar nuevamente si se pueden ensamblar más ítems
                CheckRequirements();
            }
            else
            {
                Debug.Log($"{GetName()}: Requerimientos no cumplidos, esperando más ítems.");
            }
        }

        /// <summary>
        /// Intenta desbloquear el Combiner enviando ítems completados.
        /// </summary>
        public override bool Unblock()
        {
            Debug.Log($"{GetName()}: Unblock() invocado.");
            if (completed.Count > 0)
            {
                ServerProcess process = completed.Peek();
                Item theItem = process.GetItem();
                if (GetOutput().SendItem(theItem, this))
                {
                    completed.Dequeue();
                    idleProcesses.Enqueue(process);
                    vElement.ReportState("Exit");
                    CheckRequirements();
                    Debug.Log($"{GetName()}: Unblock() exitoso, proceso reiniciado a IDLE.");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"{GetName()}: Unblock() falló al enviar el item.");
                    return false;
                }
            }
            return false;
        }

        // Implementación explícita de ArrivalListener
        void ArrivalListener.ItemReceived(Item theItem, int source)
        {
            if (!receivingItems)
            {
                CheckRequirements();
            }
        }

        VElement ArrivalListener.GetVElement()
        {
            return vElement;
        }

        /// <summary>
        /// Completa el proceso del servidor enviando el ítem resultante.
        /// </summary>
        void WorkStation.CompleteServerProcess(ServerProcess process)
        {
            Item theItem = process.theItem;
            Debug.Log($"{GetName()}: CompleteServerProcess() llamado para el ítem {theItem.GetId()}.");
            workInProgress.Remove(process);

            if (GetOutput().SendItem(theItem, this))
            {
                process.SetState(State.IDLE);
                idleProcesses.Enqueue(process);
                vElement.ReportState("Exit");
                CheckRequirements();
                Debug.Log($"{GetName()}: Proceso completado, estado reiniciado a IDLE.");
            }
            else
            {
                process.SetState(State.BLOCKED);
                completed.Enqueue(process);
                Debug.LogWarning($"{GetName()}: No se pudo enviar el ítem, proceso bloqueado.");
            }
        }

        /// <summary>
        /// Crea un nuevo ítem usando el tiempo actual de la simulación.
        /// </summary>
        public Item CreateNewItem()
        {
            Item newItem = new Item(simClock.GetSimulationTime());
            Debug.Log($"{GetName()}: Nuevo ítem creado con tiempo {simClock.GetSimulationTime()}.");
            return newItem;
        }

        /// <summary>
        /// Retorna una entrada en la posición indicada.
        /// </summary>
        public CombinerInput GetComponentInput(int i)
        {
            return inputs[i];
        }

        /// <summary>
        /// Retorna el número de entradas.
        /// </summary>
        public int GetInputsCount()
        {
            return inputs.Count;
        }

        public override bool CheckAvaliability(Item theItem)
        {
            return true;
        }

        public override int GetQueueLength()
        {
            int q = 0;
            foreach (var input in inputs)
            {
                q += input.GetQueueLength();
            }
            q += workInProgress.Count;
            q += completed.Count;
            return q;
        }

        public override int GetFreeCapacity()
        {
            return capacity;
        }

        /// <summary>
        /// Notifica disponibilidad invocando la verificación de requerimientos.
        /// </summary>
        public void NotifyAvaliable()
        {
            Debug.Log($"{GetName()}: Notificando disponibilidad.");
            CheckRequirements();
        }

        /// <summary>
        /// Retorna true si algún proceso en curso se encuentra en estado RECEIVING.
        /// </summary>
        public bool IsMainReceiving()
        {
            if (idleProcesses.Count > 0)
            {
                Debug.Log($"{GetName()}: IsMainReceiving() retorna true (procesos inactivos disponibles).");
                return true;
            }
            foreach (var process in workInProgress)
            {
                if (process.GetState() == State.RECEIVING)
                {
                    Debug.Log($"{GetName()}: IsMainReceiving() retorna true (proceso en RECEIVING).");
                    return true;
                }
            }
            Debug.Log($"{GetName()}: IsMainReceiving() retorna false.");
            return false;
        }


        public Queue<Item> GetItems()
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

        // Implementación explícita de la interfaz WorkStation
        string WorkStation.GetName()
        {
            return GetName();
        }
    }

    public enum State
    {
        IDLE = 1,
        RECEIVING = 2,
        BUSY = 3,
        BLOCKED = 4
    }
}
