using SimuLean.Headless;
using System;
using System.Collections;

namespace SimuLean
{
    /// <summary>
    /// Base class for model element.
    /// </summary>
    public abstract class Element
    {
        private Link input, output;
        protected SimClock simClock;
        public VElement vElement;
        readonly string name;
        private int type;
        static ArrayList elements;

        /// <summary>
        /// Constructor con VElement opcional para modo headless.
        /// </summary>
        /// <param name="name">Nombre del elemento</param>
        /// <param name="simClock">Reloj de simulación</param>
        /// <param name="vElement">Implementación de VElement (null para headless por defecto)</param>
        public Element(string name, SimClock simClock, VElement vElement = null)
        {
            this.name = name;
            this.simClock = simClock;
            
            // Si no se proporciona VElement, usar headless
            this.vElement = vElement ?? new HeadlessVElement(enableLogging: false);
            
            if (elements == null)
                elements = new ArrayList();
            
            elements.Add(this);
        }

        /// <summary>
        /// Returns list of all elements in the model.
        /// </summary>
        /// <returns></returns>
        public static ArrayList GetElements()
        {
            return elements;
        }

        /// <summary>
        /// Return number of elements in queue.
        /// </summary>
        /// <returns></returns>
        public static int GetInventory()
        {
            int count = 0;
            foreach (Element e in elements)
            {
                count += e.GetQueueLength();
            }
            return count;
        }

        /// <summary>
        /// Returns input Link of connections.
        /// </summary>
        /// <returns></returns>
        public Link GetInput()
        {
            return input;
        }

        /// <summary>
        /// Sets <paramref name="input"/> link for connections.
        /// </summary>
        /// <param name="input"></param>
        public void SetInput(Link input)
        {
            this.input = input;
        }

        /// <summary>
        /// Returns output Link of connections.
        /// </summary>
        /// <returns></returns>
        public Link GetOutput()
        {
            return output;
        }

        /// <summary>
        /// Sets <paramref name="output"/> link for connections.
        /// </summary>
        /// <param name="output"></param>
        public void SetOutput(Link output)
        {
            this.output = output;
        }

        /// <summary>
        /// Returns integer type of Element.
        /// </summary>
        /// <returns></returns>
        public int GetType()
        {
            return type;
        }

        /// <summary>
        /// Sets integer <paramref name="type"/> of Element.
        /// </summary>
        /// <param name="type"></param>
        public void SetType(int type)
        {
            this.type = type;
        }

        /// <summary>
        /// Returns Element's name.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return name;
        }

        /// <summary>
        /// Base method to start element's operation.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Gets the current items in the element
        /// </summary>
        /// <returns></returns>
        public abstract int GetQueueLength();

        /// <summary>
        /// Gets the free slots for receiving items, -1 if infinite
        /// </summary>
        /// <returns></returns>
        public abstract int GetFreeCapacity();

        // Input connector methods

        /// <summary>
        /// Checks Element's availability.
        /// </summary>
        /// <param name="theItem"></param>
        /// <returns>True if current items < element's capacity</returns>
        public abstract bool CheckAvaliability(Item theItem);

        /// <summary>
        /// Base method to receive a new <paramref name="theItem"/> from input element.
        /// </summary>
        /// <param name="theItem"></param>
        /// <returns>True if reception is performed.</returns>
        public abstract bool Receive(Item theItem);


        // Output connector methods

        /// <summary>
        /// Unblocks blocked transfer if any.
        /// </summary>
        /// <returns></returns>
        public abstract bool Unblock();
    }
}
