using FixedPoints;
using Lodis.Utility;
using SharedGame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;
using UnityEngine;
using UnityGGPO;

namespace Assets.Scripts.Lodis.Simulation
{
    /// <summary>
    /// A manager for items that need to be serialized that are a part of some list that changes frequently.
    /// A list changing frequently between state saves can lead to deserialization errors due to the size and contents of the list changing.
    /// This keeps a constant reference of the serialized list objects to ensure the amount of items is constant between saving and loading.
    /// </summary>
    public class SerializedListHandler<T> : IEnumerable<T> where T : ISerializedListObject
    {
        private List<T> _list = new List<T>();
        private ISerializedListObject[] _serializedObjects;
        private int _serializedCount;
        private long _listMask;
        /// <summary>
        /// Optional name for the list for debugging purposes.
        /// </summary>
        public string Name;
        /// <summary>
        ///If true, will call serialize/deserialize for each item in the list. 
        ///Otherwise will only keep serialize/deserialize the order of items in the list.
        /// </summary>
        public bool ShouldSerializeItemsIndividually = true;

        public int Count => _list.Count;
        public T this[int index] => _list[index];
        public bool IsSerializedArrayEmpty => Array.TrueForAll(_serializedObjects, obj => obj == null);

        /// <param name="list">The list of objects that needs to be potentially populated when the game state is deserialized.</param>
        public SerializedListHandler(string name = "")
        {
            Name = name;
            _serializedObjects = new ISerializedListObject[64];
            GridGame.OnResimulationStarted += CleanSerializedArray;
        }

        /// <summary>
        /// Produces a lightweight deterministic fingerprint of serialized list data
        /// for sync-test diagnostics.
        /// </summary>
        private static int CalcFletcher32(byte[] data)
        {
            uint sum1 = 0;
            uint sum2 = 0;

            for (int i = 0; i < data.Length; ++i)
            {
                sum1 = (sum1 + data[i]) % 0xffff;
                sum2 = (sum2 + sum1) % 0xffff;
            }

            return unchecked((int)((sum2 << 16) | sum1));
        }

        /// <summary>
        /// Finds an item in the active list first, then falls back to the retained
        /// serialized object list. If the retained version is found, it is added back
        /// to the active list so callers can reuse it immediately.
        /// </summary>
        public bool TryGetItem(Func<T, bool> predicate, out T item)
        {
            item = _list.FirstOrDefault(predicate);

            if (item != null)
            {
                return true;
            }

            foreach (ISerializedListObject serializedObject in _serializedObjects)
            {
                if (serializedObject is not T candidate)
                    continue;

                if (!predicate(candidate))
                    continue;

                if (!_list.Contains(candidate))
                {
                    _list.Add(candidate);
                }

                item = candidate;
                return true;
            }

            return false;
        }

        private void AddToArray(T item)
        {
            //Check and see if we can have an empty slot to put this item in.
            int emptyIndex = Array.FindIndex(_serializedObjects, obj => obj == null);

            //If we found an empty slot, add the item to it and return the index.
            if (emptyIndex != -1)
            {
                _serializedObjects[emptyIndex] = item;

                //Update the mask so we can save the state of the list for this frame.
                _listMask |= 1L << emptyIndex;
                return;
            }
        }

        public void Add(T item)
        {
            if (item == null)
                return;

            if (!_list.Contains(item))
            {
                _list.Add(item);
            }
            else
            {
                return;
            }

#if SYNC_TEST
            AddToArray(item);
            item.FrameAddedToSerializedList = GridGameManager.FrameNumber;
#else

            if (GridGameManager.OnlineGameStarted)
            {
                AddToArray(item);
                item.FrameAddedToSerializedList = GridGameManager.FrameNumber;
            }
#endif
            item.OnAddedToList?.Invoke();
        }

        public void Destroy(bool reuseArray = false)
        {
            _listMask = 0;
            _list.Clear();
            _serializedObjects = reuseArray ? new ISerializedListObject[64] : null;
            GridGame.OnResimulationStarted -= CleanSerializedArray;
        }

        public void Clear()
        {
            _listMask = 0;
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public T Find(Predicate<T> match)
        {
            return _list.Find(match);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Remove(T item)
        {
            if (!_list.Contains(item))
                return;
             
            _list.Remove(item);

            _listMask &= ~(1L << Array.IndexOf(_serializedObjects, item));

            item.OnRemovedFromList?.Invoke();
            item.FrameRemoved = GridGameManager.FrameNumber;
        }

        public int RemoveAll(Predicate<T> match)
        {
            List<T> itemsToRemove = _list.FindAll(match);

            foreach (T item in itemsToRemove)
            {
                item.OnRemovedFromList();
                item.FrameRemoved = GridGameManager.FrameNumber;


                _listMask &= ~(1L << Array.IndexOf(_serializedObjects, item));
            }

            return _list.RemoveAll(match);
        }

        public void Serialize(BinaryWriter bw)
        {
#if UNITY_EDITOR
            if (ShouldIgnoreListForSerialization())
            {
                Debug.Log("<color=yellow>" + $"Not serializing list: {Name}" + "</color>");
                return;
            }
#endif

            bw.Write(_listMask);

            foreach (ISerializedListObject obj in _list)
            {
#if UNITY_EDITOR
                if (CheckShouldIgnoreItem(obj))
                    continue;
#endif

                //obj.FrameSerialized = GridGameManager.FrameNumber;

                if (ShouldSerializeItemsIndividually)
                    obj.OnSerialize(bw);
            }
        }

        public void Deserialize(BinaryReader br)
        {
#if UNITY_EDITOR
            if (ShouldIgnoreListForSerialization())
            {
                return;
            }
#endif
            _listMask = br.ReadInt64();

            _list.Clear();

            for (int i = 0; i < 64; i++)
            {
                //If the bit for this index is not set, skip it.
                if ((_listMask & (1L << i)) == 0)
                {
                    continue;
                }

                //Otherwise lets get the item from the serialized objects array and add it to the list.
                T obj = (T)_serializedObjects[i];

                _list.Add(obj);

#if UNITY_EDITOR
                if (CheckShouldIgnoreItem(obj))
                    continue;
#endif
                if (ShouldSerializeItemsIndividually)
                    obj.OnDeserialize(br);
            }
        }

        private void CleanSerializedArray(int rollbackFrame, int targetFrame)
        {
            for (int i = 0; i < _serializedObjects.Length; i++)
            {
                ISerializedListObject obj = _serializedObjects[i];

                if (obj == null)
                    continue;

                int timeDifference = Math.Abs(obj.FrameAddedToSerializedList - rollbackFrame);

                if (timeDifference >= GetRollbackWindow() || obj.FrameAddedToSerializedList > rollbackFrame)
                {
                    _serializedObjects[i] = null;
                }
            }
        }

        public void OnLogGameState(StringBuilder sb)
        {
#if UNITY_EDITOR
            if (ShouldIgnoreListForSerialization())
            {
                return;
            }
#endif

            // Mirror the actual serialized shape: list header, then each active entry
            // in order with the retained key that Serialize writes before payload data.
            sb.AppendLine($"{Name}");
            sb.AppendLine($"Serialized Count: {_list.Count}");
            sb.AppendLine($"Serialized Mask: {_listMask:X16}");

            // Then log each active entry in the exact order Serialize iterates them.
            for (int i = 0; i < _list.Count; i++)
            {
               
#if UNITY_EDITOR
                if (CheckShouldIgnoreItem(_list[i]))
                {
                    sb.AppendLine($"Serialized Object {i}: {_list[i].ListDisplayName} PayloadSkippedByFilter");
                    continue;
                }
#endif
                sb.AppendLine($"Serialized Object {i}: {_list[i].ListDisplayName}");
                _list[i].OnLogGameState(sb);
            }
        }

        /// <summary>
        /// Uses the normal GGPO prediction window during standard play and the
        /// sync-test-specific rollback window when a sync test is active.
        /// </summary>
        private static int GetRollbackWindow()
        {
#if SYNC_TEST
            if (GridGameManager.CurrentSyncTestType != GridGameManager.SyncTestType.None)
                return GGPORunner.SyncTestRollbackWindow;
#endif
            return GGPO.MAX_PREDICTION_FRAMES;
        }


        #region Debug

#if UNITY_EDITOR

        private string[] _debugListFilter = new string[]
        {
            //"Entity List",
            //"Fixed Point Timer",
            //"FixedLerp",
        };

        private bool _shouldIgnoreWhatsInFilter = true;


        public string[] DebugItemFilter;
        public bool ShouldIgnoreWhatsInItemFilter;

        private bool ShouldIgnoreListForSerialization()
        {
            if (Name == null)
                return true;

            //If the list name is in in the filter and we're using the filter to say ignore whats in the filter we'll return true to ignore it.
            if (_shouldIgnoreWhatsInFilter && _debugListFilter.Contains(Name))
            {
                return true;
            }
            // If the list name is NOT in the filter and we're using the filter to say to ignore everything except what's in the filter then return true to ignore it.
            else if (!_shouldIgnoreWhatsInFilter && !_debugListFilter.Contains(Name))
            {
                return true;
            }

            //Otherwise return false to not ignore it.
            return false;
        }

        private bool CheckShouldIgnoreItem(ISerializedListObject obj)
        {
            if (DebugItemFilter == null)
                return false;

            if (obj == null || obj.ListDisplayName == null)
                return true;

            //If the list name is in in the filter and we're using the filter to say ignore whats in the filter we'll return true to ignore it.
            if (ShouldIgnoreWhatsInItemFilter && DebugItemFilter.Contains(obj.ListDisplayName))
            {
                return true;
            }
            // If the list name is NOT in the filter and we're using the filter to say to ignore everything except what's in the filter then return true to ignore it.
            else if (!ShouldIgnoreWhatsInItemFilter && !DebugItemFilter.Contains(obj.ListDisplayName))
            {
                return true;
            }

            //Otherwise return false to not ignore it.
            return false;
        }

        private void PrintFilter()
        {
            string mode = _shouldIgnoreWhatsInFilter ? "Excluding" : "Including";
            string filterContents = string.Join(", ", _debugListFilter);
        }
#endif

        #endregion

    }
}
