using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimuLean;  // Asegúrate de usar el namespace correcto donde están Item y VElement

/// <summary>
/// Implementación nula de la interfaz VElement para modo headless.
/// </summary>
public class NullVElement : VElement
{
    // No crea ningún objeto visual; simplemente retorna null.
    public object GenerateItem(int type)
    {
        // En modo headless no se generan representaciones visuales.
        return null;
    }

    // No realiza ninguna acción al cargar un ítem (visualización omitida).
    public void LoadItem(Item vItem)
    {
        // Intencionalmente vacío.
    }

    // No realiza ninguna acción al descargar un ítem (visualización omitida).
    public void UnloadItem(Item vItem)
    {
        // Intencionalmente vacío.
    }

    // Reporta el estado en la consola en lugar de la interfaz gráfica.
    public void ReportState(string msg)
    {
        // Opcional: Imprimir mensaje de estado en la consola para seguimiento.
        //Debug.Log($"[Headless] Estado: {msg}");
    }
}
