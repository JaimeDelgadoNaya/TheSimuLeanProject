using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static UnitySimuLean.UnityCombiner;
using UnitySimuLean;
using Debug = UnityEngine.Debug;



namespace SimuLean
{
    // Se asume que MultiServer y IArrivalListener están definidos en el proyecto
    public class Combiner : MultiServer, ArrivalListener, WorkStation
    {
        // Campos privados
        Queue<ServerProcess> idleProccesses;
        private new List<ServerProcess> workInProgress;
        Queue<ServerProcess> completed;

        ServerProcess theProcess;

        int[] requirements;
        DoubleRandomProcess delayStrategy;
        string name;

        CombinerInput[] inputs;
        bool batchMode;
        InputStrategy pullMode;
        bool updateRequirementsEnabled;
        List<string> updateLabels;


        // Constructor
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
            : base(new DoubleRandomProcess[] { delayStrategy }, name, simClock)
        {
            this.requirements = requirements;
            this.delayStrategy = delayStrategy;
            this.name = name;
            this.batchMode = batchMode;
            this.pullMode = pullMode ?? new DefaultStrategy();
            this.updateRequirementsEnabled = updateRequirements;
            this.updateLabels = updateLabels;
            theProcess = new ServerProcess(this, delayStrategy, 1);
            theProcess.SetState(State.IDLE);


            idleProccesses = new Queue<ServerProcess>(capacity);
            workInProgress = new List<ServerProcess>(capacity);
            completed = new Queue<ServerProcess>(capacity);

            // Creación de entradas (CombinerInput)
            inputs = new CombinerInput[requirements.Length];
            for (int i = 0; i < requirements.Length; i++)
            {
                inputs[i] = new CombinerInput(requirements[i], this, i, $"{name}.Input{i}", simClock, this.pullMode);
            }
        }



        // Inicia el combiner: crea el proceso principal y arranca cada entrada.
        public override void Start()
        {

            idleProccesses.Clear();
            workInProgress.Clear();
            completed.Clear();

            theProcess = new ServerProcess(this, delayStrategy, 1);
            theProcess.SetState(State.IDLE);


            foreach (var input in inputs)
            {
                input.Start();
            }
        }

        // Indica si la entrada especificada puede enviar un ítem al combiner.
        // La entrada principal (0) puede hacerlo cuando el proceso está IDLE o
        // ya se encuentra recibiendo. El resto de entradas solo cuando el
        // proceso está en estado RECEIVING.

        public bool IsMainReceiving(int inputId)
        {
            if (theProcess == null)
                return false;

            var state = theProcess.GetState();

            return state == State.RECEIVING;
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
                }
            }
        }

        // Si el proceso está bloqueado, intenta enviar el ítem de salida y lo reinicia.
        public override bool Unblock()
        {
            if (theProcess.GetState() == State.BLOCKED)
            {
                if (GetOutput().SendItem(theProcess.GetItem(), this))
                {
                    if (completed.Count > 0)
                        completed.Dequeue();

                    idleProccesses.Enqueue(theProcess);

                    vElement.ReportState("Exit");
                    theProcess.SetState(State.IDLE);
                    GetInput().NotifyAvaliable(this);
                    return true;
                }
                else
                {
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
                //Debug.LogError("theProcess es null en Combiner.Receive");
                return false;
            }
            if (theProcess.GetState() == State.IDLE)
            {
                theProcess.SetState(State.RECEIVING);
                theProcess.SetItem(theItem);
                pullMode.UpdateStrategy(theItem);
                UpdateRequirements(theItem);
                ApplyItemLabels(theItem);

                for (int i = 0; i < GetInputsCount(); i++)
                {
                    GetComponentInput(i).Unblock();
                }
                return true;
            }
            return false;
        }

        // Notificado cuando una componente (entrada) recibe un ítem.
        public bool ComponentReceived(Item theItem, int source)
        {
            if (theProcess.GetState() == State.RECEIVING)
            {
                return CheckRequirements();
            }
            return false;
        }


        // Verifica si todas las entradas cumplen los requerimientos para continuar.
        private bool CheckRequirements()
        {
            if (theProcess.GetState() != State.RECEIVING)
                return false;

            bool ready = true;
            for (int i = 0; i < inputs.Length; i++)
            {

                if (inputs[i].GetQueueLength() < requirements[i])
                {
                    ready = false;
                    break;
                }
            }

            if (ready)
            {
                // Libera ítems de cada entrada
                for (int i = 0; i < inputs.Length; i++)
                {
                    var items = inputs[i].Release(requirements[i]);
                    foreach (var item in items)
                    {
                        if (batchMode)
                        {
                            theProcess.GetItem().AddItem(item);
                        }
                        // En modo no-batch se pueden realizar otras acciones, como simplemente descargar el ítem.
                    }
                }
                theProcess.SetState(State.BUSY);
                double processingTime = GetProcessTime(theProcess.GetItem());
                theProcess.loadTime = simClock.GetSimulationTime();
                theProcess.lastDelay = processingTime;
                simClock.ScheduleEvent(theProcess, processingTime);
                return true;
            }
            return false;
        }

        // Calcula el tiempo de proceso para el ítem principal usando sus labels.
        private double GetProcessTime(Item mainItem)
        {
            if (mainItem == null)
                return delayStrategy.NextValue();

            double soldadura = mainItem.GetLabelValue("tSoldadura") ?? delayStrategy.NextValue();
            double inspeccionOn = mainItem.GetLabelValue("inspeccionOn") ?? 0;
            double inspeccion = 0;
            if (inspeccionOn >= 1)
            {
                inspeccion = mainItem.GetLabelValue("tInspeccion") ?? 0;
            }
            double totalDelay = soldadura + inspeccion;

            //Debug.Log($"{GetName()}: tSoldadura={soldadura}, tInspeccion={inspeccion}, totalDelay={totalDelay}");

            return totalDelay;
        }

        // Aplica labels al recibir el ítem principal (por ejemplo, actualizar requerimientos).
        private void ApplyItemLabels(Item mainItem)
        {
            if (mainItem == null)
                return;

            double? nRefs = mainItem.GetLabelValue("nRefuerzos");
            if (nRefs != null && inputs.Length > 1)
            {
                int newReq = (int)nRefs.Value;
                requirements[1] = newReq;
                inputs[1].SetCapacity(newReq);
                //Debug.Log($"{GetName()}: nRefuerzos={newReq}");
            }
        }

        // Crea un nuevo ítem usando el tiempo actual del reloj de simulación.
        public Item CreateNewItem(Item sourceItem = null)
        {
            Item newItem = new Item(simClock.GetSimulationTime());
            newItem.SetId("type", 1, 1);

            newItem.vItem = vElement.GenerateItem(0);

            // Copy labels from the original item if provided
            if (sourceItem != null)
            {
                newItem.CopyLabelsFrom(sourceItem);
            }

            return newItem;

        }

        // Completa el proceso del servidor: intenta enviar el ítem resultante y actualiza el estado.
        void WorkStation.CompleteServerProcess(ServerProcess process)
        {
            UnityCombiner uc = vElement as UnityCombiner;
            if (uc != null && uc.visualMode == VisualMode.NewItem)
            {
                // 1. Crear el nuevo ítem combinado usando CreateNewItem().
                Item newItem = CreateNewItem();
                GameObject newCombinedItem = newItem.vItem as GameObject;
                if (newCombinedItem != null)
                {
                    newCombinedItem.transform.position = uc.itemPosition.position;
                }

                // 2. Enviar el nuevo ítem combinado al siguiente elemento.
                if (GetOutput().SendItem(newItem, this))
                {
                    //Debug.Log($"{GetName()}: Proceso completado en modo NewItem, ítem combinado enviado.");

                    // 3. Destruir el ítem principal (del next element) si existe.
                    if (process.theItem != null && process.theItem.vItem is GameObject mainGo)
                    {
                        UnityEngine.Object.Destroy(mainGo);
                    }
                    process.SetItem(null);

                    // 4. Limpiar la cola de ítems (inputs) para evitar acumulaciones.
                    Queue<Item> itemsQueue = GetItems();
                    foreach (Item it in new List<Item>(itemsQueue))
                    {
                        if (it.vItem is GameObject go)
                        {
                            UnityEngine.Object.Destroy(go);
                        }
                        it.vItem = null;
                    }
                    itemsQueue.Clear();

                    // 5. Reinicializar el proceso creando una nueva instancia para evitar mantener el ítem principal anterior.
                    ServerProcess newProcess = new ServerProcess(this, delayStrategy, 1);
                    newProcess.SetState(State.IDLE);
                    theProcess = newProcess;
                    idleProccesses.Enqueue(newProcess);

                    vElement.ReportState("Exit");
                    GetInput().NotifyAvaliable(this);
                }
                else
                {
                    theProcess.SetState(State.BLOCKED);
                    completed.Enqueue(process);
                    //Debug.LogWarning($"{GetName()}: No se pudo enviar el ítem combinado en modo NewItem.");
                }
            }
            else
            {
                // Comportamiento original para otros modos.
                Item theItem = process.theItem;
                workInProgress.Remove(process);
                if (GetOutput().SendItem(theItem, this))
                {
                    theProcess.SetState(State.IDLE);
                    idleProccesses.Enqueue(process);
                    vElement.ReportState("Exit");
                    GetInput().NotifyAvaliable(this);
                    //Debug.Log($"{GetName()}: Proceso completado, estado reiniciado a IDLE.");
                }
                else
                {
                    theProcess.SetState(State.BLOCKED);
                    completed.Enqueue(process);
                    //Debug.LogWarning($"{GetName()}: No se pudo enviar el ítem, proceso bloqueado.");
                }
            }
        }


        // Verifica la disponibilidad del combiner (disponible si el proceso principal está en estado IDLE).
        public override bool CheckAvaliability(Item theItem)
        {
            return theProcess.GetState() == State.IDLE;
        }

        // Métodos de ArrivalListener (se pueden ajustar según la interfaz definida)
        public bool ItemReceived(Item theItem, int source)
        {
            return ComponentReceived(theItem, source);
        }
    }

}
