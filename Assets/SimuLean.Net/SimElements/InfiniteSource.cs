namespace SimuLean
{
    /// <summary>
    /// Models a source that releases an new item whenever output turns available.
    /// </summary>
    public class InfiniteSource : Element, Eventcs
    {
        Item lastItem;

        int numberIterms;

        IList<int> itemSequence;

        int sequenceIndex;

        public InfiniteSource(string name, SimClock state, IList<int> sequence = null) : base(name, state)
        {
            this.itemSequence = sequence;
        }

        public void SetSequence(IList<int> sequence)
        {
            this.itemSequence = sequence;
            sequenceIndex = 0;
        }

        public override void Start()
        {
            numberIterms = 0;
            sequenceIndex = 0;

            simClock.ScheduleEvent(this, 0.0);
        }

        public override bool Unblock()
        {
            if (lastItem == null)
            {
                lastItem = CreateItem();
                if (lastItem == null)
                    return false;
            }

            if (this.GetOutput().SendItem(lastItem, this))
            {
                lastItem = null;
                return true;
            }

            return false;
        }

        public int GetNumberItems()
        {
            return numberIterms;
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
            while (true)
            {
                if (lastItem == null)
                {
                    lastItem = CreateItem();
                    if (lastItem == null)
                        break;
                }

                if (!this.GetOutput().SendItem(lastItem, this))
                    break;

                lastItem = null;
            }
        }

        /// <summary>
        /// Creates new item following the predefined sequence if provided.
        /// </summary>
        /// <returns>The item created or <c>null</c> if sequence is finished.</returns>
        Item CreateItem()
        {
            if (itemSequence != null && sequenceIndex >= itemSequence.Count)
                return null;

            int typeId = 1;
            if (itemSequence != null)
            {
                typeId = itemSequence[sequenceIndex];
                sequenceIndex++;
            }

            Item nItem = new Item(simClock.GetSimulationTime());
            nItem.SetId("type", typeId, typeId);
            nItem.vItem = vElement.GenerateItem(nItem.GetId());
            numberIterms++;

            return nItem;
        }

        public override bool CheckAvaliability(Item theItem)
        {
            return false;
        }
    }
}
