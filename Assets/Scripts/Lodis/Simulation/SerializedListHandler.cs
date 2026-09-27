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

        #region Simulation Functions

        

        #endregion

        private List<T> _list = new List<T>();
        private ISerializedListObject[] _serializedObjects;
        // Debug metadata captured during the normal serialization pass so logging
        // can show the bounded payload size without serializing the item again.
        private readonly Dictionary<ISerializedListObject, int> _serializedPayloadLengths = new();
        // Stores the retained-object slot for every active item in _list. Unlike a
        // bitmask, this preserves the order the items had when the state was saved.
        private readonly List<byte> _serializedObjectIndices = new List<byte>();
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
        public T this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (ReferenceEquals(_list[index], value))
                    return;

                if (_list.Contains(value))
                    throw new ArgumentException("The same object cannot occupy multiple positions in a serialized list.", nameof(value));

                if (!IsStatic)
                {
                    int slotIndex = GetOrCreateSerializedSlotIndex(value);

                    if (slotIndex < 0)
                    {
                        throw new Exception($"Serialized list {Name} has reached its 64-item retained-object limit while replacing list index {index}.");
                    }

                    _serializedObjectIndices[index] = (byte)slotIndex;
                    value.FrameAddedToSerializedList = GridGameManager.FrameNumber;
                }

                _list[index] = value;
            }
        }

        public bool IsSerializedArrayEmpty => Array.TrueForAll(_serializedObjects, obj => obj == null);
        public bool IsStatic { get; set; }

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
        public bool TryGetSerializedItem(Func<T, bool> predicate, out T item, bool addRetainedToActive = true)
        {
            //Check if the item is in the active list first.
            item = _list.FirstOrDefault(predicate);

            if (item != null)
            {
                return true;
            }

            //Otherwise check and see if its in the serialized list and if it is add it back to the active list and return it.
            foreach (ISerializedListObject serializedObject in _serializedObjects)
            {
                T candidate = (T)serializedObject;

                if (!predicate(candidate))
                    continue;

                if (!_list.Contains(candidate) && addRetainedToActive)
                {
                    Add(candidate);
                }

                item = candidate;
                return true;
            }

            return false;
        }

        private int GetOrCreateSerializedSlotIndex(T item)
        {
            if (IsStatic)
                return -1;

            int slotIndex = Array.FindIndex(_serializedObjects, obj => ReferenceEquals(obj, item));

            if (slotIndex < 0)
                slotIndex = Array.FindIndex(_serializedObjects, obj => obj == null);

            if (slotIndex >= 0)
                _serializedObjects[slotIndex] = item;

            return slotIndex;
        }

        public void Add(T item)
        {
            if (item == null)
                return;

            if (_list.Contains(item))
                return;

            if (!IsStatic)
            {
                int slotIndex = GetOrCreateSerializedSlotIndex(item);

                if (slotIndex < 0)
                {
                    throw new Exception($"Serialized list {Name} has reached its 64-item retained-object limit while adding {item.ListDisplayName}.");
                }

                _serializedObjectIndices.Add((byte)slotIndex);
                item.FrameAddedToSerializedList = GridGameManager.FrameNumber;
            }

            _list.Add(item);
            item.OnAddedToList?.Invoke();
        }

        public void Destroy(bool reuseArray = false)
        {
            _serializedObjectIndices.Clear();
            _list.Clear();
            _serializedObjects = reuseArray ? new ISerializedListObject[64] : null;
            GridGame.OnResimulationStarted -= CleanSerializedArray;
        }

        public void Clear()
        {
            _serializedObjectIndices.Clear();
            _list.Clear();
        }

        /// <summary>
        /// Replaces this handler's active list with the contents of another
        /// serialized list and rebuilds its ordered retained-slot indices to match.
        /// </summary>
        public void SetList(SerializedListHandler<T> other)
        {
            SetList(other?._list);
        }

        /// <summary>
        /// Replaces this handler's active list with the provided items and rebuilds
        /// the current retained-slot order so it reflects the exact active-list order.
        /// </summary>
        public void SetList(IList<T> items)
        {
            _list.Clear();
            _serializedObjectIndices.Clear();

            if (items == null)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];

                if (item == null || _list.Contains(item))
                    continue;

                if (!IsStatic)
                {
                    int slotIndex = GetOrCreateSerializedSlotIndex(item);

                    if (slotIndex < 0)
                    {
                        throw new Exception($"Serialized list {Name} could not find or create a retained slot for {item.ListDisplayName}.");
                    }

                    _serializedObjectIndices.Add((byte)slotIndex);
                }

                _list.Add(item);
            }
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
            int listIndex = _list.IndexOf(item);

            if (listIndex < 0)
                return;
             
            _list.RemoveAt(listIndex);

            if (!IsStatic)
                _serializedObjectIndices.RemoveAt(listIndex);

            item.OnRemovedFromList?.Invoke();
            item.FrameRemovedFromActiveList = GridGameManager.FrameNumber;
        }

        public void RemoveAt(int index)
        {
            Remove(_list[index]);
        }

        public int RemoveAll(Predicate<T> match)
        {
            List<T> itemsToRemove = _list.FindAll(match);

            foreach (T item in itemsToRemove)
            {
                Remove(item);
            }

            return itemsToRemove.Count;
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

            if (IsStatic)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    T obj = _list[i];

                    if (obj == null)
                    {
                        throw new Exception($"Serialized list {Name} found a null object at index {i} while serializing. Found on frame {GridGameManager.FrameNumber}.");
                    }

#if UNITY_EDITOR
                    if (CheckShouldIgnoreItem(obj))
                        continue;
#endif

                    if (ShouldSerializeItemsIndividually)
                        SerializeItem(bw, obj);
                }

                return;
            }

            if (_serializedObjectIndices.Count > byte.MaxValue)
            {
                throw new Exception($"Serialized list {Name} cannot serialize more than {byte.MaxValue} active items.");
            }

            bw.Write((byte)_serializedObjectIndices.Count);

            // Write every retained slot first so Deserialize can reconstruct the exact
            // active-list order before it reads any object payloads.
            for (int i = 0; i < _serializedObjectIndices.Count; i++)
                bw.Write(_serializedObjectIndices[i]);

            for (int i = 0; i < _serializedObjectIndices.Count; i++)
            {
                int slotIndex = _serializedObjectIndices[i];
                ISerializedListObject obj = _serializedObjects[slotIndex];

                if (obj == null)
                {
                    throw new Exception($"Serialized list {Name} found no retained object at slot {slotIndex} while serializing active list index {i}. Found on frame {GridGameManager.FrameNumber}.");
                }

#if UNITY_EDITOR
                if (CheckShouldIgnoreItem(obj))
                    continue;
#endif
                if (ShouldSerializeItemsIndividually)
                    SerializeItem(bw, obj);
            }
        }


        public void Deserialize(Deserializer br)
        {
#if UNITY_EDITOR
            if (ShouldIgnoreListForSerialization())
            {
                return;
            }
#endif
            if (IsStatic)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    T obj = _list[i];

                    if (obj == null)
                    {
                        throw new Exception($"Serialized list {Name} found a null object at index {i} while deserializing. Found on frame {GridGameManager.FrameNumber}.");
                    }

#if UNITY_EDITOR
                    if (CheckShouldIgnoreItem(obj))
                        continue;
#endif

                    if (ShouldSerializeItemsIndividually)
                        DeserializeItem(br, obj);
                }

                return;
            }

            _list.Clear();
            _serializedObjectIndices.Clear();

            int serializedCount = br.ReadByte();

            if (serializedCount > _serializedObjects.Length)
            {
                throw new Exception($"Serialized list {Name} deserialized invalid active item count {serializedCount}. The retained-object capacity is {_serializedObjects.Length}.");
            }

            for (int i = 0; i < serializedCount; i++)
            {
                byte slotIndex = br.ReadByte();

                if (slotIndex >= _serializedObjects.Length)
                {
                    throw new Exception($"Serialized list {Name} deserialized invalid retained slot {slotIndex} at active list index {i}.");
                }

                if (_serializedObjectIndices.Contains(slotIndex))
                {
                    throw new Exception($"Serialized list {Name} deserialized retained slot {slotIndex} more than once in the same active-list snapshot.");
                }

                T obj = (T)_serializedObjects[slotIndex];

                if (obj == null)
                {
                    throw new Exception($"Serialized list {Name} found no retained object at slot {slotIndex} while deserializing active list index {i}. Found on frame {GridGameManager.FrameNumber}.");
                }

                _serializedObjectIndices.Add(slotIndex);
                _list.Add(obj);
            }

            for (int i = 0; i < _list.Count; i++)
            {
                T obj = _list[i];
#if UNITY_EDITOR
                if (CheckShouldIgnoreItem(obj))
                {
                    Debug.Log("<color=yellow>" + $"Not deserializing item: {obj.ListDisplayName} in list: {Name}" + "</color>");
                    continue;
                }
#endif
                if (ShouldSerializeItemsIndividually)
                    DeserializeItem(br, obj);
            }
        }

        /// <summary>
        /// Writes an item's exact payload as a bounded block followed by its
        /// Fletcher-32 checksum. The temporary buffer prevents checksum generation
        /// from invoking the item's serializer a second time.
        /// </summary>
        private void SerializeItem(BinaryWriter writer, ISerializedListObject item)
        {
            using MemoryStream payloadStream = new MemoryStream();
            using BinaryWriter payloadWriter = new BinaryWriter(payloadStream);

            item.OnSerialize(payloadWriter);
            payloadWriter.Flush();
            byte[] payload = payloadStream.ToArray();
            int checksum = CalcFletcher32(payload);
            item.SerializedChecksum = checksum;
            _serializedPayloadLengths[item] = payload.Length;

            writer.Write(payload.Length);
            writer.Write(payload);
            writer.Write(checksum);

            // Record this item at the exact point its payload is complete, before
            // later serializers can alter the live state shown by the deep log.
            GridGame.AppendDeepSerializeItemLog(Name, item, payload.Length, checksum);
        }

        /// <summary>
        /// Reads and validates one bounded item payload before applying it. A bad
        /// checksum or an item that consumes the wrong number of bytes identifies
        /// the owning list and item before later list entries are corrupted.
        /// </summary>
        private void DeserializeItem(Deserializer reader, ISerializedListObject item)
        {
            // Read the byte count written before this item's serialized payload.
            int payloadLength = reader.ReadInt32();

            // Reject invalid lengths before they can make the stream read backwards or allocate incorrectly.
            if (payloadLength < 0)
            {
                throw new Exception($"Serialized list {Name} read a negative payload length {payloadLength} for {item.ListDisplayName}. Frame: {GridGameManager.FrameNumber}.");
            }

            // Check that the outer stream still contains the declared payload and its checksum.
            if (reader.BaseStream.CanSeek)
            {
                long remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;

                if (payloadLength > remainingBytes - sizeof(int))
                {
                    throw new EndOfStreamException($"Serialized list {Name} read payload length {payloadLength} for {item.ListDisplayName}, but only {remainingBytes} bytes remain for its payload and checksum. Frame: {GridGameManager.FrameNumber}.");
                }
            }

            // Read exactly this item's bounded payload so one bad item cannot shift later reads.
            byte[] payload = reader.ReadBytes(payloadLength);

            // Check that deserialization did not read fewer payload bytes than the saved size.
            if (payload.Length != payloadLength)
            {
                throw new EndOfStreamException($"Serialized list {Name} could not read the full {payloadLength}-byte payload for {item.ListDisplayName}. Only {payload.Length} bytes were available. Frame: {GridGameManager.FrameNumber}.");
            }

            // Read the saved checksum and calculate the checksum of the bytes we actually received.
            int serializedChecksum = reader.ReadInt32();
            int calculatedChecksum = CalcFletcher32(payload);
            item.SerializedChecksum = calculatedChecksum;

            // Stop immediately when the payload differs from the bytes that were originally serialized.
            if (calculatedChecksum != serializedChecksum)
            {
                throw new Exception($"Serialized list checksum mismatch. List: {Name}. Item: {item.ListDisplayName}. Expected: {serializedChecksum}. Actual: {calculatedChecksum}. Frame: {GridGameManager.FrameNumber}.");
            }

            // Limit the item to its own payload while it restores its fields.
            using MemoryStream payloadStream = new MemoryStream(payload, writable: false);
            using Deserializer payloadReader = reader.CreateChild(payloadStream, $"{Name} > {item.ListDisplayName}");
            item.OnDeserialize(payloadReader);

            // Verify that the item read every expected byte and did not leave fields unread.
            if (payloadStream.Position != payloadStream.Length)
            {
                throw new Exception($"Serialized list {Name} item {item.ListDisplayName} consumed {payloadStream.Position} of {payloadStream.Length} payload bytes while deserializing. Frame: {GridGameManager.FrameNumber}.");
            }
        }

        private void CleanSerializedArray(int rollbackFrame, int targetFrame)
        {
            for (int i = 0; i < _serializedObjects.Length; i++)
            {
                ISerializedListObject obj = _serializedObjects[i];

                //If the slot is already empty or if its still being used by the list we can skip it.
                if (obj == null || _serializedObjectIndices.Contains((byte)i))
                    continue;

                int timeDifference = Math.Abs(obj.FrameRemovedFromActiveList - rollbackFrame);

                if (timeDifference >= GetRollbackWindow())
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

            // Mirror the actual serialized shape: list header, ordered retained slots,
            // then each active entry's payload in that same order.
            sb.AppendLine($"{Name}");
            sb.AppendLine($"Serialized Count: {_list.Count}");

            if (IsStatic)
                sb.AppendLine($"Serialized List is Static, no retained indices are used.");
            else
                sb.AppendLine($"Serialized Indices: {string.Join(", ", _serializedObjectIndices)}");

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
                string retainedSlot = IsStatic ? "Static" : _serializedObjectIndices[i].ToString();
                sb.AppendLine($"Serialized Object {i} (Retained Slot {retainedSlot}): {_list[i].ListDisplayName}");
                if (ShouldSerializeItemsIndividually)
                {
                    int payloadLength = _serializedPayloadLengths.TryGetValue(_list[i], out int value) ? value : 0;
                    sb.AppendLine($"Serialized Payload Length: {payloadLength}");
                    sb.AppendLine($"Serialized Checksum: {_list[i].SerializedChecksum}");
                }
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
            //"New Entity Components"
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
