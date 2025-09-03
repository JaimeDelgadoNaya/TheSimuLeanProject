using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimuLean
{
    /// <summary>
    /// Models a sink that counts arriving assemblies, inspecciones y retrasos,
    /// y muestra el tiempo de simulación en cada recepción.
    /// </summary>
    public class Sink : Element
    {
        int numberIterms;
        int inspeccionesRealizadas;
        int retrasados;

        // Cuántos ítems de ensamblaje esperamos recibir (normalmente, número de chapas).
        public int expectedItems = 0;

        // Tiempo de simulación en el que inicia la simulación (normalmente 0).
        private double startTime;

        public Sink(string name, SimClock state) : base(name, state)
        {
        }

        public int GetNumberIterms()
        {
            return numberIterms;
        }

        public int GetInspecciones()
        {
            return inspeccionesRealizadas;
        }

        public int GetRetrasados()
        {
            return retrasados;
        }

        public override void Start()
        {
            numberIterms = 0;
            inspeccionesRealizadas = 0;
            retrasados = 0;
            // Guardamos el tiempo inicial (normalmente 0 al arrancar).
            startTime = simClock.GetSimulationTime();
            Debug.Log($"[Sink] Start(): Inicializando. startTime = {startTime:F2} s.");
        }

        public override bool Unblock()
        {
            throw new InvalidOperationException("The Sink cannot receive notifications.");
        }

        public override int GetQueueLength()
        {
            return 0;
        }

        public override int GetFreeCapacity()
        {
            return -1;
        }

        public override bool Receive(Item theItem)
        {
            double tiempoActual = simClock.GetSimulationTime();
            numberIterms++;

            // Contar inspección si la etiqueta inspeccionOn >= 1 (insensible a mayúsculas)
            var labelInspObj = theItem.GetLabelValueIgnoreCase("inspeccionOn");
            if (labelInspObj.HasValue && labelInspObj.Value >= 1)
            {
                inspeccionesRealizadas++;
            }

            // Contar retraso si el ítem tiene un Deadline/DueDate y llega tarde.
            double? labelDeadlineObj = theItem.GetLabelValueIgnoreCase("Deadline")
                                     ?? theItem.GetLabelValueIgnoreCase("DueDate");
            if (labelDeadlineObj.HasValue && tiempoActual > labelDeadlineObj.Value)
            {
                retrasados++;
            }

            vElement.LoadItem(theItem);

            // Mostrar contador con tiempo actual de simulación
            Debug.Log($"[Sink] Receive() t={tiempoActual:F2} s: Ítem {theItem.GetId()} procesado. " +
                      $"Total={numberIterms}, Inspecciones={inspeccionesRealizadas}, Retrasados={retrasados}.");

            // Si ya recibimos todos los ítems esperados, calculamos tiempo total
            if (expectedItems > 0 && numberIterms == expectedItems)
            {
                double totalTime = tiempoActual - startTime;
                Debug.Log($"[Sink] Todos los ítems esperados ({expectedItems}) han llegado.");
                Debug.Log($"[Sink] Tiempo total de simulación: {totalTime:F2} s (desde {startTime:F2} hasta {tiempoActual:F2}).");
            }

            return true;
        }

        public override bool CheckAvaliability(Item theItem)
        {
            return true;
        }
    }
}
