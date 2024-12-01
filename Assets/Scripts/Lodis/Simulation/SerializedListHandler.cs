using FixedPoints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Lodis.Simulation
{
    /// <summary>
    /// A manager for items that need to be serialized that are a part of some list that changes frequently.
    /// A list changing frequently between state saves can lead to deserialization errors due to the size and contents of the list changing.
    /// This keeps a constant reference of the serialized list objects to ensure the amount of items is constant between saving and loading.
    /// </summary>
    internal class SerializedListHandler<T> where T : ISerializedListObject
    {
        private List<T> _list;
        private List<ISerializedListObject> _serializedObjects = new List<ISerializedListObject>();
        private int serializedCount;

        /// <summary>
        /// Optional name for the list for debugging purposes.
        /// </summary>
        public string Name;

        /// <param name="list">The list of objects that needs to be potentially populated when the game state is deserialized.</param>
        public SerializedListHandler(List<T> list)
        {
            _list = list;

        }

        public void Serialize(BinaryWriter bw)
        {

            bw.Write(_list.Count);

            if (_list.Count == 0)
                return;

            foreach (ISerializedListObject obj in _list)
            {

                if (!_serializedObjects.Contains(obj))
                {
                    _serializedObjects.Add(obj);
                    bw.Write(_serializedObjects.Count - 1);
                }
                else
                {
                    bw.Write(_serializedObjects.IndexOf(obj));
                }
                obj.OnSerialize(bw);
                obj.FrameSerialized = GridGameManager.FrameNumber;
            }

            //Debug.Log($"Last serialized position {bw.BaseStream.Position}");
            //Debug.Log($"Serializing frame {GridGameManager.FrameNumber}: List count = {_list.Count}");
            serializedCount = _serializedObjects.Count;
        }

        public void Deserialize(BinaryReader br)
        {
            _list.Clear();
            //Debug.Log($"Last reading position for {Name} was {br.BaseStream.Position}");
            int count = br.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                int index = br.ReadInt32();

                if (index < 0 || index >= _serializedObjects.Count)
                {
                    //Debug.LogError($"Deserialized index {index} was not in range of the serialized object list with a count of {_serializedObjects.Count}. List was {Name}");
                    return;
                }

                ISerializedListObject obj = _serializedObjects[index];

                obj.OnDeserialize(br);

                if (!obj.CheckIfCanBeAddedToList() || _list.Contains((T)obj))
                {
                    continue;
                }

                if (obj.OnAddedToList != null)
                {
                    obj.OnAddedToList.Invoke();
                }
                else
                {
                    _list.Add((T)obj);
                }
            }

            //_serializedObjects.Clear();
            //return;
            for (int i = 0; i < _serializedObjects.Count; i++)
            {
                if (_serializedObjects[i].FrameSerialized > GridGameManager.FrameNumber)
                {
                    _serializedObjects.RemoveAt(i);
                }
            }
        }

    }
}
