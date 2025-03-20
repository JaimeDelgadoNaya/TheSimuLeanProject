using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
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

        // Verifica si el proceso principal está en estado RECEIVING.

        public bool IsMainReceiving(int inputId)
        {
            return theProcess != null && (theProcess.GetState() == State.RECEIVING || theProcess.GetState() == State.IDLE);
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
                Debug.LogError("theProcess es null en Combiner.Receive");
                return false;
            }
            if (theProcess.GetState() == State.IDLE)
            {
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
                double delayTime = theProcess.GetDelay();
                simClock.ScheduleEvent(theProcess, delayTime);
                return true;
            }
            return false;
        }

        // Crea un nuevo ítem usando el tiempo actual del reloj de simulación.
        public Item CreateNewItem()
        {
            Item newItem = new Item(simClock.GetSimulationTime());
            newItem.SetId("type", 1, 1);

            newItem.vItem = vElement.GenerateItem(0);

            return newItem;

        }

        // Completa el proceso del servidor: intenta enviar el ítem resultante y actualiza el estado.
        void WorkStation.CompleteServerProcess(ServerProcess process)
        {
            Item theItem = process.theItem;
            workInProgress.Remove(process);
            if (GetOutput().SendItem(theItem, this))
            {
                theProcess.SetState(State.IDLE);
                idleProccesses.Enqueue(process);
                vElement.ReportState("Exit");
                GetInput().NotifyAvaliable(this);
                CheckRequirements();
                Debug.Log($"{GetName()}: Proceso completado, estado reiniciado a IDLE.");
            }
            else
            {
                theProcess.SetState(State.BLOCKED);
                completed.Enqueue(process);
                Debug.LogWarning($"{GetName()}: No se pudo enviar el ítem, proceso bloqueado.");
            }
        }

        // Verifica la disponibilidad del combiner (disponible si el proceso principal está en estado IDLE).
        public override bool CheckAvaliability(Item theItem)
        {
            return theProcess.GetState() == State.IDLE;
        }

        // Métodos de ArrivalListener (se pueden ajustar según la interfaz definida)
        public void ItemReceived(Item theItem, int source)
        {
            ComponentReceived(theItem, source);
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
