using SimuLean;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnitySimuLean
{
    /// <summary>
    /// Unity Component for MultiServer (infinite servers) Element.
    /// </summary>
    public class UnityMultiServer : SElement, VElement
    {
        PoissonProcess[] cycleTime;
        MultiServer theWorkstation;

        // Transforms para colocación visual
        public Transform itemPosition;
        public Transform outItemPosition;
        Vector3 odVector;
        public float separation = 1f;

        // Parámetros de simulación
        public string elementName = "WS";
        public double cTime = 2.0;
        public int capacity = 1;

        // Animator para simular trabajo
        public Animator serverAnimator;

        void Start()
        {
            // Siempre nos registramos para construir la parte lógica.
            UnitySimClock.Instance.Elements.Add(this);
        }

        override public void InitializeSim()
        {
            // ----- Configuración de la lógica -----
            cycleTime = new PoissonProcess[capacity];
            for (int i = 0; i < capacity; i++)
                cycleTime[i] = new PoissonProcess(cTime);

            theWorkstation = new MultiServer(cycleTime, elementName, UnitySimClock.Instance.clock);

            // ----- Modo headless: anulamos la vista -----
            if (Experimenter.HeadlessActive)
            {
                theWorkstation.vElement = new NullVElement();
                return;
            }

            // ----- Modo gráfico: apuntamos a este componente -----
            theWorkstation.vElement = this;

            // precálculo del vector de salida para interpolaciones
            if (itemPosition == null)
                itemPosition = transform;

            if (outItemPosition != null)
                odVector = outItemPosition.position - itemPosition.position;
        }

        override public void StartSim()
        {
            // En headless no hacemos nada visual aquí
            if (Experimenter.HeadlessActive)
            {
                theWorkstation.Start();
                return;
            }

            // Aseguramos posición de inicio y arrancamos
            if (itemPosition == null)
                itemPosition = transform;

            theWorkstation.Start();
        }

        void FixedUpdate()
        {
            // 1) Si estamos en headless o la simulación no ha arrancado, nada que hacer
            if (Experimenter.HeadlessActive || theWorkstation == null)
                return;

            // 2) Asegurar que itemPosition nunca sea null
            if (itemPosition == null)
            {
                Debug.LogWarning($"{name}: itemPosition no asignado, usando transform por defecto.");
                itemPosition = transform;
            }

            // 3) Si no hay posición de salida, no hay animación que hacer
            if (outItemPosition == null)
                return;

            // 4) Recorrer solo si la lista existe
            var wipList = theWorkstation.workInProgress;
            if (wipList == null || wipList.Count == 0)
            {
                // Actualizar animador incluso si no hay procesos en curso
                if (serverAnimator != null)
                    serverAnimator.SetBool("WorkInProgress", false);
                return;
            }

            // 5) Interpolación segura
            foreach (ServerProcess sProcess in wipList)
            {
                if (sProcess == null || sProcess.theItem == null)
                    continue;

                var go = sProcess.theItem.vItem as GameObject;
                if (go == null)
                    continue;

                // Cálculo de avance
                float p = ((float)Time.time - (float)sProcess.loadTime) / (float)sProcess.lastDelay;
                if (p <= 1f)
                {
                    go.transform.position =
                        itemPosition.position +
                        odVector * p +
                        new Vector3(0f, separation, 0f);
                }
            }

            // 6) Actualizar animador
            if (serverAnimator != null)
                serverAnimator.SetBool("WorkInProgress", theWorkstation.GetItems().Count > 0);
        }

        override public Element GetElement()
        {
            return theWorkstation;
        }

        // ------------------------------------------------------------
        // Implementación explícita de VElement: TODO todo lo visual va protegido
        // ------------------------------------------------------------

        void VElement.ReportState(string msg)
        {
            if (Experimenter.HeadlessActive)
                return;

            // Reposiciona la cola de ítems
            var items = theWorkstation.GetItems();
            int i = 0;
            foreach (Item it in items)
            {
                var gItem = (GameObject)it.vItem;
                if (gItem != null)
                    gItem.transform.position = itemPosition.position + new Vector3(0f, separation * i, 0f);
                i++;
            }
        }

        object VElement.GenerateItem(int type)
        {
            // En este MultiServer no generamos prefabs nuevos;
            // devolvemos null siempre (y nunca instanciamos).
            return null;
        }

        void VElement.LoadItem(Item vItem)
        {
            if (Experimenter.HeadlessActive)
                return;

            // Ajusta la posición de un ítem cuando entra a la cola
            var gItem = (GameObject)vItem.vItem;
            if (gItem != null)
                gItem.transform.position = itemPosition.position +
                                           new Vector3(0f, separation * (theWorkstation.GetQueueLength() - 1), 0f);
        }

        void VElement.UnloadItem(Item vItem)
        {
            // No hay nada que hacer cuando sale un ítem
        }

        public override void RestartSim()
        {
            // Solo destruimos objetos en modo gráfico
            if (!Experimenter.HeadlessActive)
            {
                var items = theWorkstation.GetItems();
                foreach (Item it in items)
                    Destroy((GameObject)it.vItem);
            }
            StartSim();
        }
    }
}
