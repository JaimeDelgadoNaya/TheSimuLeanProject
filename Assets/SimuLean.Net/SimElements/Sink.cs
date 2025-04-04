using System;
using System.Linq;
using UnityEngine;

namespace SimuLean
{
    /// <summary>
    /// Models a basic sink that destroys arriving items.
    /// </summary>
    public class Sink : Element
    {
        int numberIterms;

        public Sink(String name, SimClock state) : base(name, state)
        {
        }

        public int GetNumberIterms()
        {
            return numberIterms;
        }

        public override void Start()
        {
            numberIterms = 0;
            Debug.Log($"[Sink] Start(): Inicializando Sink. Contador reiniciado a {numberIterms}.");
        }

        public override bool Unblock()
        {
            throw new System.InvalidOperationException("The Sink cannot receive notifications."); //To change body of generated methods, choose Tools | Templates.
        }

        override public int GetQueueLength()
        {
            return 0;
        }
        override public int GetFreeCapacity()
        {
            return -1;
        }

        public override bool Receive(Item theItem)
        {
            Debug.Log($"[Sink] Receive(): Se ha recibido el ítem {theItem.GetId()}. Eliminando el elemento.");
            vElement.LoadItem(theItem);
            numberIterms++;

            Debug.Log($"[Sink] Receive(): Ítems procesados hasta el momento: {numberIterms}.");
            return true;
        }

        public override bool CheckAvaliability(Item theItem)
        {
            return true;
        }
    }
}
