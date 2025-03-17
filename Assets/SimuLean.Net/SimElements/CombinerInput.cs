using System.Collections.Generic;
using UnityEngine;

namespace SimuLean
{
    /// <summary>
    /// CombinerInput: Representa la entrada del Combiner.
    /// Adaptado para heredar de Element y asemejarse en estructura a ConstrainedInput.
    /// Opción B: Se mantiene la verificación de arrivalListener.IsMainReceiving() en Receive() y CheckAvaliability().
    /// Se requiere que Combiner.IsMainReceiving() retorne true cuando haya procesos inactivos (IDLE) o en RECEIVING.
    /// </summary>
    public class CombinerInput : Element
    {
        int capacity;
        int currentItems;
        int inputId;
        Queue<Item> itemsQ;
        Combiner arrivalListener;
        InputStrategy inputStrategy;


        public CombinerInput(int capacity, Combiner arrivalListener, int inputId, string inputName, SimClock clock, InputStrategy inputStrategy)
            : base(inputName, clock)
        {
            this.capacity = capacity;
            this.inputId = inputId;
            this.arrivalListener = arrivalListener;
            this.inputStrategy = inputStrategy;
            itemsQ = new Queue<Item>(capacity);
            currentItems = 0;
            Debug.Log($"[CombinerInput] Constructor: Entrada {inputId} creada con capacidad {capacity}.");
        }

        public override void Start()
        {
            itemsQ.Clear();
            currentItems = 0;
            Debug.Log($"[CombinerInput] Start(): Entrada {inputId} iniciada, cola limpia y currentItems = {currentItems}.");
        }

        /// <summary>
        /// Libera hasta 'quantity' ítems de la cola y actualiza currentItems.
        /// Se llama NotifyAvaliable() una sola vez si se libera al menos un ítem.
        /// </summary>
        public Queue<Item> Release(int quantity)
        {
            Queue<Item> released = new Queue<Item>();
            Debug.Log($"[CombinerInput] Release(): Entrada {inputId} intentando liberar {quantity} ítems.");
            int releasedCount = 0;
            // Usamos while para liberar mientras haya ítems y no se supere la cantidad requerida.
            while (releasedCount < quantity && itemsQ.Count > 0)
            {
                released.Enqueue(itemsQ.Dequeue());
                currentItems--;
                releasedCount++;
                Debug.Log($"[CombinerInput] Release(): Ítem liberado. currentItems ahora es {currentItems}.");
            }
            // Notificar disponibilidad si se liberó al menos un ítem.
            if (releasedCount > 0)
            {
                arrivalListener.NotifyAvaliable();
            }
            return released;
        }

        public override int GetQueueLength()
        {
            Debug.Log($"[CombinerInput] GetQueueLength(): Entrada {inputId} tiene {currentItems} ítems.");
            return currentItems;
        }

        public override int GetFreeCapacity()
        {
            return capacity - currentItems;
        }

        public override bool Unblock()
        {
            Debug.Log($"[CombinerInput] Unblock(): Notificando disponibilidad desde entrada {inputId}.");
            arrivalListener.NotifyAvaliable();
            return true;
        }

        public override bool Receive(Item theItem)
        {
            // Se conserva la verificación de arrivalListener.IsMainReceiving()
            if ((currentItems < capacity || capacity < 0)
                && inputStrategy.IsValid(theItem)
                && arrivalListener.IsMainReceiving())
            {
                currentItems++;
                theItem.SetConstrainedInput(this.inputId);
                itemsQ.Enqueue(theItem);
                arrivalListener.GetVElement().LoadItem(theItem);
                arrivalListener.ItemReceived(theItem, inputId);
                Debug.Log($"[CombinerInput] Receive(): Ítem {theItem.GetId()} encolado en entrada {inputId}. Nuevo currentItems = {currentItems}.");

                bool compRec = arrivalListener.ComponentReceived(theItem, inputId);
                if (!compRec)
                {
                    Debug.Log($"[CombinerInput] Receive(): ComponentReceived() devolvió false, notificando disponibilidad.");
                    arrivalListener.NotifyAvaliable();
                }
                return true;
            }
            else
            {
                Debug.Log($"[CombinerInput] Receive(): No se pudo recibir el ítem {theItem.GetId()} en entrada {inputId}.");
                return false;
            }
        }

        public override bool CheckAvaliability(Item theItem)
        {
            // Se conserva la verificación de arrivalListener.IsMainReceiving()
            bool available = ((currentItems < capacity || capacity < 0)
                              && inputStrategy.IsValid(theItem)
                              && arrivalListener.IsMainReceiving());
            Debug.Log($"[CombinerInput] CheckAvaliability(): Ítem {theItem.GetId()} en entrada {inputId}: available = {available}.");
            return available;
        }

        public int GetCapacity()
        {
            return capacity;
        }

        public void SetCapacity(int newCapacity)
        {
            capacity = newCapacity;
            Debug.Log($"[CombinerInput] SetCapacity(): Capacidad de entrada {inputId} actualizada a {capacity}.");
        }

        public Queue<Item> GetItems()
        {
            return itemsQ;
        }
    }
}
