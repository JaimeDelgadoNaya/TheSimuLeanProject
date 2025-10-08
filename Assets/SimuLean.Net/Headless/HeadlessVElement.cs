using System;

namespace SimuLean.Headless
{
    /// <summary>
    /// Implementación headless de VElement sin dependencias de Unity.
    /// Se usa para ejecutar simulaciones en segundo plano durante optimización.
    /// </summary>
    public class HeadlessVElement : VElement
    {
        private readonly bool enableLogging;

        /// <summary>
        /// Constructor con opción de habilitar logging para debugging.
        /// </summary>
        /// <param name="enableLogging">Si true, imprime mensajes en consola.</param>
        public HeadlessVElement(bool enableLogging = false)
        {
            this.enableLogging = enableLogging;
        }

        public void ReportState(string msg)
        {
            if (enableLogging)
            {
                Console.WriteLine($"[Headless] {msg}");
            }
            // No hacer nada en modo silencioso
        }

        public object GenerateItem(int type)
        {
            // No se generan objetos visuales en modo headless
            return null;
        }

        public void LoadItem(Item vItem)
        {
            // Asignar referencia nula al item (sin GameObject)
            vItem.vItem = null;
            
            if (enableLogging)
            {
                Console.WriteLine($"[Headless] Item {vItem.GetId()} cargado");
            }
        }

        public void UnloadItem(Item vItem)
        {
            if (enableLogging)
            {
                Console.WriteLine($"[Headless] Item {vItem.GetId()} descargado");
            }
            // No hacer nada
        }
    }
}