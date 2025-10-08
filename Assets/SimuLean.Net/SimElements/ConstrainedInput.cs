using System;
using System.Collections.Generic;

namespace SimuLean
{
    /// <summary>
    /// Models MultiAssemble input queue to manage receptions.
    /// </summary>
    public class ConstrainedInput : Element
    {
        int capacity;
        int currentItems;
        int inputId;
        Queue<Item> itemsQ;
        ArrivalListener aListener;

        /// <summary>
        /// Constructor con soporte para modo headless.
        /// </summary>
        /// <param name="capacity">Capacidad de la cola</param>
        /// <param name="aListener">Listener de llegadas</param>
        /// <param name="inputId">ID de entrada</param>
        /// <param name="myName">Nombre del elemento</param>
        /// <param name="sClock">Reloj de simulación</param>
        /// <param name="vElement">Implementación de VElement (null para headless por defecto)</param>
        public ConstrainedInput(int capacity, ArrivalListener aListener, int inputId, String myName, SimClock sClock, VElement vElement = null)
            : base(myName, sClock, vElement)
        {
            this.aListener = aListener;
            this.capacity = capacity;
            this.inputId = inputId;
            itemsQ = new Queue<Item>(capacity);
        }

        public override void Start()
        {
            itemsQ.Clear();
            currentItems = 0;
        }

        public Queue<Item> Release(int quantity)
        {
            Queue<Item> wholeItems = new Queue<Item>();

            for (int i = 0; i < quantity; i++)
            {
                wholeItems.Enqueue(itemsQ.Dequeue());
                currentItems--;
                GetInput().NotifyAvaliable(this);
            }

            return wholeItems;
        }

        override public int GetQueueLength()
        {
            return currentItems;
        }

        override public int GetFreeCapacity()
        {
            return capacity - currentItems;
        }

        public override bool Unblock()
        {
            return true;
        }

        public override bool Receive(Item theItem)
        {
            if (currentItems < capacity || capacity < 0)
            {
                currentItems++;
                theItem.SetConstrainedInput(this.inputId);
                itemsQ.Enqueue(theItem);
                aListener.GetVElement().LoadItem(theItem);
                aListener.ItemReceived(theItem, inputId);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool CheckAvaliability(Item theItem)
        {
            if (capacity < 0)
            {
                return true;
            }
            return currentItems < capacity;
        }

        public int GetCapacity()
        {
            return capacity;
        }

        public Queue<Item> GetItems()
        {
            return itemsQ;
        }
    }
}

