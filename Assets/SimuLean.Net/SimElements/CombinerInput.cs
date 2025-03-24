using System.Collections.Generic;
using UnityEngine;

namespace SimuLean
{
    /// <summary>
    /// CombinerInput: Representa la entrada del Combiner, similar a la versión en Python.
    /// </summary>
    public class CombinerInput : Element
    {
        int capacity;
        int currentItems;
        int inputId;
        Queue<Item> itemsQueue;
        ArrivalListener arrivalListener;
        InputStrategy inputStrategy;
        private int myConstrainedInput;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">Capacidad máxima de la entrada.</param>
        /// <param name="arrivalListener">Objeto ArrivalListener (por ejemplo, el Combiner) que recibirá notificaciones.</param>
        /// <param name="inputId">Identificador de la entrada.</param>
        /// <param name="name">Nombre de la entrada.</param>
        /// <param name="simClock">Reloj de simulación.</param>
        /// <param name="inputStrategy">Estrategia para validar los ítems; si es nula se usa DefaultStrategy.</param>
        public CombinerInput(int capacity, ArrivalListener arrivalListener, int inputId, string name, SimClock simClock, InputStrategy inputStrategy = null)
            : base(name, simClock)
        {
            this.capacity = capacity;
            this.currentItems = 0;
            this.inputId = inputId;
            // Se inicializa la cola con la capacidad indicada.
            itemsQueue = new Queue<Item>(capacity);
            this.arrivalListener = arrivalListener;
            // Si no se proporciona una estrategia, se utiliza la estrategia por defecto.
            this.inputStrategy = inputStrategy ?? new DefaultStrategy();
        }

        /// <summary>
        /// Inicializa la entrada: limpia la cola y reinicia el contador de ítems.
        /// </summary>
        public override void Start()
        {
            itemsQueue.Clear();
            currentItems = 0;
            Debug.Log($"[CombinerInput] Start(): Entrada {inputId} iniciada, cola limpia y currentItems = {currentItems}.");
        }

        /// <summary>
        /// Libera hasta 'quantity' ítems de la cola.
        /// </summary>
        /// <param name="quantity">Cantidad de ítems a liberar.</param>
        /// <returns>Cola con los ítems liberados.</returns>
        public Queue<Item> Release(int quantity)
        {
            Queue<Item> releasedItems = new Queue<Item>();

            for (int i = 0; i < quantity; i++)
            {
                if (itemsQueue.Count > 0)
                {
                    Item theItem = itemsQueue.Dequeue();
                    releasedItems.Enqueue(theItem);
                    currentItems--;
                    GetInput().NotifyAvaliable(this);
                    Debug.Log($"[CombinerInput] Release(): Ítem liberado. currentItems ahora es {currentItems}.");
                }
                else
                {
                    break;
                }
            }
            return releasedItems;
        }

        /// <summary>
        /// Devuelve el número de ítems actualmente en la cola.
        /// </summary>
        public override int GetQueueLength()
        {
            Debug.Log($"[CombinerInput] GetQueueLength(): Entrada {inputId} tiene {currentItems} ítems.");
            return currentItems;
        }

        /// <summary>
        /// Devuelve la capacidad libre.
        /// </summary>
        public override int GetFreeCapacity()
        {
            return capacity - currentItems;
        }

        /// <summary>
        /// Notifica la disponibilidad llamando al método NotifyAvaliable del objeto Link obtenido vía GetInput().
        /// </summary>
        public override bool Unblock()
        {
            Debug.Log($"[CombinerInput] Unblock(): Notificando disponibilidad desde entrada {inputId}.");
            // Se asume que GetInput() retorna un objeto que implemente la interfaz Link.
            if(this.CheckAvaliability(null))
            {
                this.Release(itemsQueue.Count);
                return true;
            }

            this.GetInput().NotifyAvaliable(this);
            return true;
        }

        /// <summary>
        /// Recibe un ítem. Si cumple la disponibilidad, lo encola y notifica al ArrivalListener.
        /// </summary>
        public override bool Receive(Item theItem)
        {
            if (CheckAvaliability(theItem))
            {
                var visual = arrivalListener.GetVElement();
                if (visual != null)
                {
                    visual.LoadItem(theItem);
                }
                else
                {
                    Debug.LogWarning("[CombinerInput] Receive(): No se encontró el elemento visual (vElement).");
                }

                currentItems++;
                theItem.SetConstrainedInput(this.inputId);
                itemsQueue.Enqueue(theItem);

                arrivalListener.ItemReceived(theItem, inputId);
                // Notificamos a la conexión que la entrada está disponible
                this.GetInput().NotifyAvaliable(this);
                return true;
            }
            return false;
        }





        /// <summary>
        /// Verifica si es posible recibir el ítem.
        /// </summary>
        public override bool CheckAvaliability(Item theItem)
        {
            bool capacityOk = (currentItems < capacity) || (capacity < 0);
            bool valid = inputStrategy.IsValid(theItem);
            Debug.Log($"[CombinerInput] CheckAvaliability (entrada {inputId}): capacityOk={capacityOk}, valid={valid}.");
            return capacityOk && valid;
        }

        /// <summary>
        /// Devuelve la capacidad máxima de la entrada.
        /// </summary>
        public int GetCapacity()
        {
            return capacity;
        }

        /// <summary>
        /// Actualiza la capacidad máxima de la entrada.
        /// </summary>
        public void SetCapacity(int newCapacity)
        {
            capacity = newCapacity;
            Debug.Log($"[CombinerInput] SetCapacity(): Capacidad de entrada {inputId} actualizada a {capacity}.");
        }

        /// <summary>
        /// Devuelve la cola de ítems de la entrada.
        /// </summary>
        public Queue<Item> GetItems()
        {
            return itemsQueue;
        }
    }
}
