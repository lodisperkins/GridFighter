using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Lodis.Simulation
{
    public delegate void ListEvent();
    /// <summary>
    /// An object that will be serialized in the game state and is a part of some frequently changing list.
    /// </summary>
    internal interface ISerializedListObject
    {
        public ListEvent OnAddedToList { get; set; }
        public ListEvent OnRemovedFromList { get; set; }

        public int FrameSerialized { get; set; }

        /// <summary>
        /// Used to determined whether this should be added to its managing list when the game state is deserialized.
        /// </summary>
        public bool CheckIfCanBeAddedToList();

        /// <summary>
        /// Called when the serialized list handler is serialized.
        /// </summary>
        public void OnSerialize(BinaryWriter bw);
        /// <summary>
        /// Called when the serialized list handler is deserialized.
        /// </summary>
        public void OnDeserialize(BinaryReader br);
    }
}
