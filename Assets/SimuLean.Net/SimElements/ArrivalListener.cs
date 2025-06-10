namespace SimuLean
{
    /// <summary>
    /// Interface to model internal queues of elements.
    /// </summary>
    public interface ArrivalListener
    {
        /// <summary>
        /// Notifies main element a new item has been received.
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="source"></param>
        void ItemReceived(Item theItem, int source);
        
        VElement GetVElement();

        /// <summary>
        /// Informa si la entrada indicada puede entregar su ítem al elemento
        /// principal.
        /// </summary>
        /// <param name="inputId">Identificador de la entrada.</param>
        /// <returns>True si la entrada puede enviar el ítem.</returns>
        bool IsMainReceiving(int inputId);
    }
}

