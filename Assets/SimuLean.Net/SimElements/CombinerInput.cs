using System.Collections.Generic;
using UnityEngine;

namespace SimuLean
{
    /// <summary>
    /// CombinerInput: Representa la entrada del Combiner, similar a la versi�n en Python.
    /// </summary>
    public class CombinerInput : Element
    {
        int capacity;
        int currentItems;
        int inputId;
        Queue<Item> itemsQueue;
        ArrivalListener arrivalListener;
        InputStrategy inputStrategy;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">Capacidad m�xima de la entrada.</param>
        /// <param name="arrivalListener">Objeto ArrivalListener (por ejemplo, el Combiner) que recibir� notificaciones.</param>
        /// <param name="inputId">Identificador de la entrada.</param>
        /// <param name="name">Nombre de la entrada.</param>
        /// <param name="simClock">Reloj de simulaci�n.</param>
        /// <param name="inputStrategy">Estrategia para validar los �tems; si es nula se usa DefaultStrategy.</param>
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
        /// Inicializa la entrada: limpia la cola y reinicia el contador de �tems.
        /// </summary>
        public override void Start()
        {
            itemsQueue.Clear();
            currentItems = 0;
            //Debug.Log($"[CombinerInput] Start(): Entrada {inputId} iniciada, cola limpia y currentItems = {currentItems}.");
        }

        /// <summary>
        /// Libera hasta 'quantity' �tems de la cola.
        /// </summary>
        /// <param name="quantity">Cantidad de �tems a liberar.</param>
        /// <returns>Cola con los �tems liberados.</returns>
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
                    // GetInput().NotifyAvaliable(this); Esto no est�
                    //Debug.Log($"[CombinerInput] Release(): �tem liberado. currentItems ahora es {currentItems}.");
                }
                else
                {
                    break;
                }
            }
            return releasedItems;
        }

        /// <summary>
        /// Devuelve el n�mero de �tems actualmente en la cola.
        /// </summary>
        public override int GetQueueLength()
        {
            //Debug.Log($"[CombinerInput] GetQueueLength(): Entrada {inputId} tiene {currentItems} �tems.");
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
        /// Notifica la disponibilidad llamando al m�todo NotifyAvaliable del objeto Link obtenido v�a GetInput().
        /// </summary>
        public override bool Unblock()
        {
            //Debug.Log($"[CombinerInput] Unblock(): Notificando disponibilidad desde entrada {inputId}.");

            this.GetInput().NotifyAvaliable(this);
            return true;
        }

        /// <summary>
        /// Recibe un �tem. Si cumple la disponibilidad, lo encola y notifica al ArrivalListener.
        /// </summary>
        public override bool Receive(Item theItem)
        {
            if (CheckAvaliability(theItem))
            {
                //Debug.Log($"[CombinerInput] Receive(): Aceptando �tem {theItem.GetId()} en entrada {inputId}.");
                currentItems++;
                theItem.SetConstrainedInput(this.inputId);
                itemsQueue.Enqueue(theItem);
                arrivalListener.GetVElement().LoadItem(theItem);
                //Estaba mal programado (Javi)
                if (!this.arrivalListener.ItemReceived(theItem, inputId))
                {
                    this.GetInput().NotifyAvaliable(this);
                }


                // Se asume que arrivalListener es un Combiner:
                //Esto sobra (Javi)
                //Combiner combiner = arrivalListener as Combiner;
                //if (combiner != null)
                //{
                //    combiner.ItemReceived(theItem, inputId);
                //}

                return true;
            }
            return false;
        }

        /// <summary>
        /// Verifica si es posible recibir el �tem.
        /// </summary>
        public override bool CheckAvaliability(Item theItem)
        {
            bool capacityOk = this.currentItems < this.capacity;
            bool valid = inputStrategy?.IsValid(theItem) ?? true;

            // Consultamos al ArrivalListener si esta entrada puede enviar un �tem.
            bool mainReceiving = arrivalListener.IsMainReceiving(this.inputId);
            return capacityOk && valid && mainReceiving;
        }

        /// <summary>
        /// Devuelve la capacidad m�xima de la entrada.
        /// </summary>
        public int GetCapacity()
        {
            return capacity;
        }

        /// <summary>
        /// Actualiza la capacidad m�xima de la entrada.
        /// </summary>
        public void SetCapacity(int newCapacity)
        {
            capacity = newCapacity;
            //Debug.Log($"[CombinerInput] SetCapacity(): Capacidad de entrada {inputId} actualizada a {capacity}.");
        }

        /// <summary>
        /// Devuelve la cola de �tems de la entrada.
        /// </summary>
        public Queue<Item> GetItems()
        {
            return itemsQueue;
        }
    }
}
