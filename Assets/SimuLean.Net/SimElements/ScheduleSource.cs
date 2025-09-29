using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text; // Asegúrate de tener esta línea
using ExcelDataReader;  // Librería para leer Excel
using UnityEngine;



namespace SimuLean
{
    /// <summary>
    /// Models a source that creates items based on schedule read from file.
    /// </summary>
    class ScheduleSource : Element, Eventcs
    {
        Item lastItem;
        int numberIterms;
        String fileName;
        TextReader dataFile;
        Queue<Item> itemsInQueue;

        // Global map to associate a reference number with its priority. This
        // allows multiple ScheduleSources to share priority information so that
        // reinforcements can inherit the priority from their corresponding
        // sheet metal entry.
        private static Dictionary<string, int> priorityByReferencia = new Dictionary<string, int>();

        //Nuevas Variables para los parametros opcionales Mod
        private string fileType;
        private Dictionary<string, List<string>> dataDict;
        private Item modelItem;
        private string sheetName;

        // Variables de Estado adicionales (mod 3)
        private double currentArrivalTime; // Tiempo de llegada para la fila actual mod
        private string currentItemName;    // Nombre o tipo del ítem actual mod
        private int currentPendingQ;       // Número de ítems pendientes en la fila mod 
        private Dictionary<string, string> currentRow; // Fila actual de datos (encabezados y valores) mod 
        private IEnumerator<Dictionary<string, string>> rowIterator; // Añadir esta línea para definir rowIterator
        private List<Dictionary<string, string>> preprocessedRows; //Lista para reordenar filas de datos
        // Contador para asignar IDs únicos a cada ítem creado
        private int itemCounter = 0;
        // Constructor modificado con parámetros opcionales. Con autoSort=false se
        // respetará el orden provisto (útil para órdenes generados por el GA).
        /// <param name="autoSort">Cuando es false, se respeta el orden provisto (útil para órdenes generadas por el GA).</param>
        public ScheduleSource(String name, SimClock state, String fileName = null, Dictionary<string, List<string>> dataDict = null, Item modelItem = null, string sheetName = null, bool autoSort = true) : base(name, state)
        {
            this.fileName = fileName;
            this.dataDict = dataDict;
            this.modelItem = modelItem;
            this.sheetName = sheetName;

            itemsInQueue = new Queue<Item>();

            //determinar el tipo de archivo si se proporciono fileName Mod

            if (!string.IsNullOrEmpty(fileName))
            {
                //Estrae la extension y la convierte a minusculas gpt Mod
                this.fileType = Path.GetExtension(fileName).TrimStart('.').ToLower();
                // Por ejemplo, "xlsx", "csv", "data", etc.

                //si se utiliza archivo, inicializa el lector
                dataFile = File.OpenText(fileName);
            }
            else
            {
                this.fileType = null;
            }
            // Preprocess rows. When autoSort is true (default), rows are sorted
            // first by Time and then by Priority to ensure a deterministic
            // processing order. With autoSort=false the provided order is
            // respected, which is useful when la secuencia es generada por un GA.

            preprocessedRows = GetRowIterator().ToList();
            if (autoSort)
            {
                preprocessedRows.Sort((row1, row2) =>
                {
                    double time1 = row1.ContainsKey("Time") && double.TryParse(row1["Time"], out double t1)
                        ? t1 : simClock.GetSimulationTime();
                    double time2 = row2.ContainsKey("Time") && double.TryParse(row2["Time"], out double t2)
                        ? t2 : simClock.GetSimulationTime();
                    int cmp = time1.CompareTo(time2);
                    if (cmp != 0)
                    {
                        return cmp;
                    }

                    int priority1 = GetRowPriority(row1);
                    int priority2 = GetRowPriority(row2);
                    return priority1.CompareTo(priority2);
                });
            }

            // Populate the shared priority map if this file provides both a
            // reference and a priority column. This ensures that other sources
            // (e.g. the reinforcements) can inherit the same ordering.
            foreach (var row in preprocessedRows)
            {
                if (row.TryGetValue("Referencia", out string referencia))
                {
                    int prio = GetRowPriority(row);
                    if (prio != int.MaxValue)
                    {
                        priorityByReferencia[referencia] = prio;
                    }
                }
            }

        }

        /// <summary>
        /// Retorna un iterador sobre las filas de datos, ya sea a partir de un diccionario o de un archivo.
        /// Cada fila se representa como un Dictionary&lt;string, string&gt; con pares "encabezado-valor".
        /// </summary>

        private IEnumerable<Dictionary<string, string>> GetRowIterator()
        {
            // Caso 1: Usar diccionario de datos (dataDict)
            if (dataDict != null)
            {
                List<string> headersList = new List<string>(dataDict.Keys);
                int rowCount = dataDict.Values.First().Count;
                for (int i = 0; i < rowCount; i++)
                {
                    Dictionary<string, string> rowDict = new Dictionary<string, string>();
                    foreach (var header in headersList)
                    {
                        rowDict[header] = dataDict[header][i];
                    }
                    yield return rowDict;
                }
            }
            // Caso 2: Usar archivo (fileName)
            else if (!string.IsNullOrEmpty(fileName))
            {
                // Para archivos CSV: lectura manual (delimitador de coma)
                if (fileType == "csv")
                {
                    using (var reader = new StreamReader(fileName))
                    {
                        string headerLine = reader.ReadLine();
                        if (headerLine == null) yield break;
                        string[] headers = headerLine.Split(',');
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;
                            string[] values = line.Split(',');
                            Dictionary<string, string> rowDict = new Dictionary<string, string>();
                            for (int i = 0; i < headers.Length && i < values.Length; i++)
                            {
                                rowDict[headers[i]] = values[i];
                            }
                            yield return rowDict;
                        }
                    }
                }

                else if (fileType == "xlsx")
                {
                    // Registrar el proveedor de codificaciones
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            DataSet result = reader.AsDataSet();
                            DataTable table = null;
                            if (string.IsNullOrEmpty(sheetName))
                            {
                                table = result.Tables[0];
                            }
                            else
                            {
                                table = result.Tables[sheetName] ?? result.Tables[0];
                            }
                            if (table == null || table.Rows.Count == 0) yield break;
                            int colCount = table.Columns.Count;
                            string[] headers = new string[colCount];
                            // Se asume que la primera fila contiene los encabezados
                            for (int i = 0; i < colCount; i++)
                            {
                                headers[i] = table.Rows[0][i]?.ToString() ?? "";
                            }
                            // Iterar a partir de la segunda fila
                            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
                            {
                                Dictionary<string, string> rowDict = new Dictionary<string, string>();
                                for (int colIndex = 0; colIndex < colCount; colIndex++)
                                {
                                    rowDict[headers[colIndex]] = table.Rows[rowIndex][colIndex]?.ToString() ?? "";
                                }
                                yield return rowDict;
                            }
                        }
                    }
                }

                // Archivos de tipo "data" (delimitados por espacios)
                else if (fileType == "data")
                {
                    using (var reader = new StreamReader(fileName))
                    {
                        string headerLine = reader.ReadLine();
                        if (headerLine == null) yield break;
                        string[] headers = headerLine.Split(' ');
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;
                            string[] values = line.Split(' ');
                            Dictionary<string, string> rowDict = new Dictionary<string, string>();
                            for (int i = 0; i < headers.Length && i < values.Length; i++)
                            {
                                rowDict[headers[i]] = values[i];
                            }
                            yield return rowDict;
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException($"Tipo de archivo '{fileType}' no soportado.");
                }
            }
            else
            {
                yield break; // No se proporcionó ni dataDict ni fileName
            }
        }

        /// <summary>
        /// Obtiene la prioridad almacenada en la fila, ignorando mayúsculas.
        /// Si no se encuentra o no es numérica, devuelve int.MaxValue.
        /// </summary>
        /// <summary>
        /// Obtiene la prioridad almacenada en la fila, ignorando mayúsculas.
        /// Si no se encuentra o no es numérica, devuelve int.MaxValue.
        /// </summary>
        private int GetRowPriority(Dictionary<string, string> row)
        {
            if (row == null)
                return int.MaxValue;

            // Some spreadsheets use "priorities" instead of "Priority" as the
            // column header. Look for either of them in a case-insensitive way.
            string key = row.Keys.FirstOrDefault(k =>
                k.Equals("Priority", StringComparison.OrdinalIgnoreCase) ||
                k.Equals("priorities", StringComparison.OrdinalIgnoreCase));

            if (key != null && int.TryParse(row[key], out int parsed))
                return parsed;

            // If no explicit priority is found, try to obtain it from the
            // shared map using the reference number.
            if (row.TryGetValue("Referencia", out string referencia) &&
                priorityByReferencia.TryGetValue(referencia, out int mapped))
            {
                return mapped;
            }

            return int.MaxValue;
        }

        /// <summary>
        /// Devuelve el valor de una etiqueta ignorando mayúsculas.
        /// </summary>
        private double? GetItemLabelValueIgnoreCase(Item item, string label)
        {
            if (item == null || string.IsNullOrEmpty(label))
                return null;

            var all = item.GetAllLabels();
            foreach (var kvp in all)
            {
                if (kvp.Key.Equals(label, StringComparison.OrdinalIgnoreCase) ||
                    (label.Equals("Priority", StringComparison.OrdinalIgnoreCase) &&
                     kvp.Key.Equals("priorities", StringComparison.OrdinalIgnoreCase)))
                {
                    if (kvp.Value is double d)
                        return d;
                }
            }
            return null;
        }

        public override void Start()
        {
            numberIterms = 0;
            itemCounter = 0;
            ScheduleNext();
        }


        /// <summary>
        /// Itera sobre la cola completa, intentando enviar cada ítem mientras sea posible.
        /// Si el primer ítem se envía correctamente, lo elimina y sigue intentando con el siguiente.
        /// El bucle se detiene cuando se encuentra un ítem que no puede enviarse,
        /// retornando true si al menos se envió alguno, o false si no se pudo enviar ninguno
        /// </summary>
        public override bool Unblock()
        {
            bool enviadoAlgunItem = false;

            while (itemsInQueue.Count > 0)
            {
                Item theItem = itemsInQueue.Peek();
                if (this.GetOutput().SendItem(theItem, this))
                {
                    itemsInQueue.Dequeue();
                    // Se elimina la notificación, ya que no queremos modificar Element.
                    enviadoAlgunItem = true;
                }
                else
                {
                    // Si el primer ítem no se puede enviar, detenemos el proceso.
                    break;
                }
            }

            return enviadoAlgunItem;
        }



        public override bool Receive(Item theItem)
        {
            throw new System.InvalidOperationException("The Source cannot receive Items."); //To change body of generated methods, choose Tools | Templates.
        }

        override public int GetQueueLength()
        {
            return 0;
        }
        override public int GetFreeCapacity()
        {
            return 0;
        }
        void Eventcs.Execute()
        {
            //Debug.Log("Execute() llamado.");
            // Genera y procesa ítems mientras CreateItem() retorne uno
            Item newItem = CreateItem();
            while (newItem != null)
            {
                //Debug.Log("Procesando ítem (ID: " + newItem.GetId() + ", Prioridad: " + newItem.priority + ").");
                // Intenta enviar el ítem; si no se puede, se añade a la cola de ítems bloqueados
                if (!this.GetOutput().SendItem(newItem, this))
                {
                    //Debug.Log("Fallo al enviar el ítem, se añade a la cola.");
                    itemsInQueue.Enqueue(newItem);
                }
                numberIterms++;
                newItem = CreateItem();
            }
            //Debug.Log("Todos los ítems de la fila actual procesados. Programando siguiente fila.");
            // Una vez procesados todos los ítems de la fila actual, programa el siguiente evento
            ScheduleNext();
        }



        Item CreateItem()
        {
            //Debug.Log("CreateItem() llamado. currentPendingQ = " + currentPendingQ);
            // Si no quedan ítems pendientes, retorna null
            if (currentPendingQ <= 0)
            {
                return null;
            }

            // Decrementa la cantidad pendiente
            currentPendingQ--;
            int priority = 0; // se obtendrá tras aplicar las etiquetas

            // Crea el ítem usando el tiempo actual de la simulación
            Item newItem = new Item(simClock.GetSimulationTime());

            
            // Asigna atributos adicionales primero
            // Itera sobre cada clave en currentRow, omitiendo las básicas ("Time", "Name", "Q")
            foreach (var kvp in currentRow)
            {
                if (kvp.Key == "Time" || kvp.Key == "Name" || kvp.Key == "Q")
                    continue;

                
                newItem.SetLabelValue(kvp.Key, kvp.Value);
                //Debug.Log("Atributo asignado: " + kvp.Key + " = " + kvp.Value);
            }


            // Obtiene la prioridad a partir de la etiqueta, ignorando mayúsculas
            double? prioVal = GetItemLabelValueIgnoreCase(newItem, "Priority");
            if (prioVal != null)
            {
                priority = (int)prioVal.Value;
            }
            else
            {
                // If the label was not found, fall back to the priority derived
                // from the current row (which may come from the shared map).
                priority = GetRowPriority(currentRow);
            }

            // Asigna tipo, id y prioridad
            itemCounter++;
            newItem.SetId(currentItemName, itemCounter, priority);
            //Debug.Log($"Ítem creado de tipo: {currentItemName} con ID: {itemCounter}");
            // La prioridad ya fue asignada mediante SetId
            //Debug.Log("Prioridad asignada: " + priority);
            newItem.vItem = vElement.GenerateItem(newItem.GetId());
            //Debug.Log("vItem asignado al ítem.");

            return newItem;
        }
        public override bool CheckAvaliability(Item theItem)
        {
            return false;
        }

        /// <summary>
        /// Programa el siguiente evento leyendo la siguiente fila de datos y actualizando las variables de estado.
        /// </summary>
        private void ScheduleNext()
        {
            // Debug.Log("ScheduleNext() llamado.");
            // Inicializa el iterador la primera vez.
            if (rowIterator == null)
            {
                // Use the preprocessed and already ordered rows
                rowIterator = preprocessedRows.GetEnumerator();
                //Debug.Log("Row iterator inicializado.");
            }

            // Avanza a la siguiente fila; si no hay más, cierra recursos y termina.
            if (!rowIterator.MoveNext())
            {
                //Debug.Log("No hay más filas. Cerrando archivo.");
                dataFile?.Close();
                return;
            }

            // Actualiza la fila actual.
            currentRow = rowIterator.Current;
            //Debug.Log("Fila actual leída: " + string.Join(", ", currentRow.Select(kvp => kvp.Key + "=" + kvp.Value)));
            // Actualiza currentArrivalTime usando el valor de la columna "Time".
            if (currentRow.ContainsKey("Time") && double.TryParse(currentRow["Time"], out double time))
            {
                currentArrivalTime = time;
            }
            else
            {
                currentArrivalTime = simClock.GetSimulationTime();
            }
            //Debug.Log("Tiempo de llegada actual: " + currentArrivalTime);
            // Actualiza currentItemName usando el valor de la columna "Name".
            if (currentRow.ContainsKey("Name"))
            {
                currentItemName = currentRow["Name"];
            }
            else
            {
                currentItemName = "Default";
            }
            //Debug.Log("Nombre del ítem actual: " + currentItemName);
            // Actualiza currentPendingQ usando el valor de la columna "Q".
            if (currentRow.ContainsKey("Q") && int.TryParse(currentRow["Q"], out int q))
            {
                currentPendingQ = q;
            }
            else
            {
                currentPendingQ = 1;
            }
            //Debug.Log("Cantidad pendiente (Q): " + currentPendingQ);
            // Calcula el retraso (delay) para el siguiente evento.
            double delay = Math.Max(0, currentArrivalTime - simClock.GetSimulationTime());
            //Debug.Log("Programando el siguiente evento con retraso: " + delay);
            // Programa el siguiente evento en el simulador.
            simClock.ScheduleEvent(this, delay);
        }

        public Queue<Item> GetItems()
        {
            return itemsInQueue;

        }
    }
}

