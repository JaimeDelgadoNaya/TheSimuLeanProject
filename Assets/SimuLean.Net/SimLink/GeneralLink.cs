using System.Collections.Generic;
using UnityEngine;

namespace SimuLean
{
    public class GeneralLink : Link
    {
        List<Element> origins = new List<Element>();
        List<Element> outputs = new List<Element>();

        int currentIndex = 0;

        // Estrategia (puedes usar FirstAvailableStrategy u otra que implementes)
        IOutputStrategy strategy = new FirstAvailableStrategy();

        // Añadir origen (para NotifyAvaliable y control de peticiones)
        public void AddOrigin(Element origin)
        {
            if (!origins.Contains(origin))
                origins.Add(origin);
        }

        // Añadir destino
        public void AddOutput(Element output)
        {
            if (!outputs.Contains(output))
                outputs.Add(output);
        }

        // Enviar ítem desde un origen a uno de los destinos
        public bool SendItem(Item theItem, Element source)
        {
            if (outputs.Count == 0)
                return false;

            int index = strategy.SelectOutput(outputs, theItem);

            if (index >= 0 && index < outputs.Count && outputs[index].Receive(theItem))
            {
                currentIndex = (index + 1) % outputs.Count;
                Debug.Log($"[GeneralLink] Enviado desde {source.GetName()} a {outputs[index].GetName()}");
                return true;
            }

            //Debug.LogWarning($"[GeneralLink] Falló el envío desde {source.GetName()}");
            return false;
        }

        // Notificación de que hay un hueco en algún destino
        public bool NotifyAvaliable(Element source)
        {
            Debug.Log($"[GeneralLink] Notificación recibida de {source.GetName()}");
            foreach (Element origin in origins)
            {
                if (origin.Unblock())
                {
                    return true;
                }
            }
            return false;
        }

        // Crea un link de un solo origen a múltiples destinos
        public static GeneralLink CreateLink(Element origin, List<Element> destinations)
        {
            GeneralLink theLink = new GeneralLink();
            theLink.AddOrigin(origin);
            origin.SetOutput(theLink);

            foreach (Element dest in destinations)
            {
                theLink.AddOutput(dest);
                dest.SetInput(theLink);
            }

            return theLink;
        }

        // Crea un link de múltiples orígenes a múltiples destinos
        public static GeneralLink CreateLink(List<Element> origins, List<Element> destinations)
        {
            GeneralLink theLink = new GeneralLink();

            foreach (Element origin in origins)
            {
                theLink.AddOrigin(origin);
                origin.SetOutput(theLink);
            }

            foreach (Element dest in destinations)
            {
                theLink.AddOutput(dest);
                dest.SetInput(theLink);
            }

            return theLink;
        }
    }

    // Interfaces y estrategias de salida (puedes moverlas a otro archivo si prefieres)
    public interface IOutputStrategy
    {
        int SelectOutput(List<Element> outputs, Item item);
    }

    public class FirstAvailableStrategy : IOutputStrategy
    {
        public int SelectOutput(List<Element> outputs, Item item)
        {
            for (int i = 0; i < outputs.Count; i++)
            {
                if (outputs[i].CheckAvaliability(item))
                    return i;
            }
            return -1;
        }
    }
}

