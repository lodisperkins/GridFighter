using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Assets.Scripts.Lodis.Simulation;
using NUnit.Framework;

namespace Lodis.Tests
{
    public class SerializedListHandlerTests
    {
        [Test]
        public void SerializeThenDeserialize_ReorderedNumberObjectsRestoreTheirSavedListOrder()
        {
            var numbers = new SerializedListHandler<SerializedNumber>("Number List");
            var first = new SerializedNumber("First", 10);
            var second = new SerializedNumber("Second", 20);
            var third = new SerializedNumber("Third", 30);

            try
            {
                // This first list assigns retained slots: first=0, second=1, third=2.
                numbers.SetList(new List<SerializedNumber> { first, second, third });
                AssertListOrder(numbers, first, second, third);

                // The active-list order changes. The saved snapshot must restore both
                // the original object references and this exact active-list order.
                numbers.SetList(new List<SerializedNumber> { second, third, first });
                AssertListOrder(numbers, second, third, first);
                byte[] savedState = Serialize(numbers);
                AssertSerializedIndices(savedState, 1, 2, 0);

                first.SetValue(-1);
                second.SetValue(-1);
                third.SetValue(-1);
                numbers.Clear();

                Deserialize(numbers, savedState);

                AssertListOrder(numbers, second, third, first);
                Assert.That(first.Value, Is.EqualTo(10));
                Assert.That(second.Value, Is.EqualTo(20));
                Assert.That(third.Value, Is.EqualTo(30));
            }
            finally
            {
                numbers.Destroy();
            }
        }

        [Test]
        public void AddRemoveAndReAdd_RestoresTheOriginalRetainedSlot()
        {
            var numbers = new SerializedListHandler<SerializedNumber>("Number List");
            var first = new SerializedNumber("First", 10);
            var second = new SerializedNumber("Second", 20);
            int addedCount = 0;
            int removedCount = 0;
            first.OnAddedToList += () => addedCount++;
            first.OnRemovedFromList += () => removedCount++;
            bool originalOnlineState = SetOnlineGameStarted(true);

            try
            {
                numbers.Add(first);
                numbers.Add(second);
                numbers.Add(first);

                AssertListOrder(numbers, first, second);
                Assert.That(addedCount, Is.EqualTo(1));
                AssertSerializedIndices(Serialize(numbers), 0, 1);

                byte[] savedState = Serialize(numbers);

                numbers.Remove(first);
                AssertListOrder(numbers, second);
                Assert.That(removedCount, Is.EqualTo(1));
                AssertSerializedIndices(Serialize(numbers), 1);

                numbers.Add(first);
                AssertListOrder(numbers, second, first);
                AssertSerializedIndices(Serialize(numbers), 1, 0);

                first.SetValue(-1);
                second.SetValue(-1);
                numbers.Clear();
                Deserialize(numbers, savedState);

                AssertListOrder(numbers, first, second);
                Assert.That(first.Value, Is.EqualTo(10));
                Assert.That(second.Value, Is.EqualTo(20));
            }
            finally
            {
                SetOnlineGameStarted(originalOnlineState);
                numbers.Destroy();
            }
        }

        [Test]
        public void AddRemoveAddAnotherThenReAdd_RestoresEachNumberToItsOriginalSlot()
        {
            var numbers = new SerializedListHandler<SerializedNumber>("Number List");
            var first = new SerializedNumber("First", 10);
            var second = new SerializedNumber("Second", 20);
            var third = new SerializedNumber("Third", 30);
            bool originalOnlineState = SetOnlineGameStarted(true);

            try
            {
                numbers.Add(first);
                numbers.Add(second);
                numbers.Add(third);
                AssertListOrder(numbers, first, second, third);
                AssertSerializedIndices(Serialize(numbers), 0, 1, 2);

                numbers.Remove(first);
                AssertListOrder(numbers, second, third);
                AssertSerializedIndices(Serialize(numbers), 1, 2);

                numbers.Add(first);
                AssertListOrder(numbers, second, third, first);
                byte[] savedState = Serialize(numbers);
                AssertSerializedIndices(Serialize(numbers), 1, 2, 0);

                first.SetValue(-1);
                second.SetValue(-1);
                third.SetValue(-1);
                numbers.Clear();
                Deserialize(numbers, savedState);

                AssertListOrder(numbers, second, third, first);
                Assert.That(first.Value, Is.EqualTo(10));
                Assert.That(second.Value, Is.EqualTo(20));
                Assert.That(third.Value, Is.EqualTo(30));
            }
            finally
            {
                SetOnlineGameStarted(originalOnlineState);
                numbers.Destroy();
            }
        }

        [Test]
        public void RemoveAtAndRemoveAll_RemoveMatchingItemsAndRaiseEvents()
        {
            var numbers = new SerializedListHandler<SerializedNumber>("Number List");
            var first = new SerializedNumber("First", 10);
            var second = new SerializedNumber("Second", 20);
            var third = new SerializedNumber("Third", 30);
            int removedCount = 0;
            first.OnRemovedFromList += () => removedCount++;
            second.OnRemovedFromList += () => removedCount++;
            third.OnRemovedFromList += () => removedCount++;
            bool originalOnlineState = SetOnlineGameStarted(true);

            try
            {
                numbers.Add(first);
                numbers.Add(second);
                numbers.Add(third);
                AssertListOrder(numbers, first, second, third);

                numbers.RemoveAt(1);
                AssertListOrder(numbers, first, third);
                Assert.That(numbers.Contains(second), Is.False);

                int removed = numbers.RemoveAll(number => number.Value >= 10);

                Assert.That(removed, Is.EqualTo(2));
                AssertListOrder(numbers);
                Assert.That(removedCount, Is.EqualTo(3));

                byte[] savedState = Serialize(numbers);
                AssertSerializedIndices(savedState);
                Deserialize(numbers, savedState);
                AssertListOrder(numbers);
            }
            finally
            {
                SetOnlineGameStarted(originalOnlineState);
                numbers.Destroy();
            }
        }

        [Test]
        public void Clear_KeepsRetainedObjectsAvailableForTryGetItem()
        {
            var numbers = new SerializedListHandler<SerializedNumber>("Number List");
            var number = new SerializedNumber("Number", 10);
            bool originalOnlineState = SetOnlineGameStarted(true);

            try
            {
                numbers.Add(number);
                numbers.Clear();

                AssertListOrder(numbers);
                Assert.That(numbers.TryGetSerializedItem(item => item == number, out SerializedNumber restored), Is.True);
                Assert.That(restored, Is.SameAs(number));
                AssertListOrder(numbers, number);

                byte[] savedState = Serialize(numbers);
                AssertSerializedIndices(savedState, 0);
                number.SetValue(-1);
                numbers.Clear();
                Deserialize(numbers, savedState);

                AssertListOrder(numbers, number);
                Assert.That(number.Value, Is.EqualTo(10));
            }
            finally
            {
                SetOnlineGameStarted(originalOnlineState);
                numbers.Destroy(reuseArray: true);
            }
        }

        [Test]
        public void Deserialize_CorruptedItemChecksum_IdentifiesTheListAndItem()
        {
            var numbers = new SerializedListHandler<SerializedNumber>("Number List");
            var number = new SerializedNumber("Checksum Target", 10);

            try
            {
                numbers.SetList(new List<SerializedNumber> { number });
                byte[] savedState = Serialize(numbers);

                // The final byte belongs to this item's stored Int32 checksum.
                savedState[savedState.Length - 1] ^= 0x01;

                Exception exception = Assert.Throws<Exception>(() => Deserialize(numbers, savedState));

                Assert.That(exception.Message, Does.Contain("Number List"));
                Assert.That(exception.Message, Does.Contain("Checksum Target"));
            }
            finally
            {
                numbers.Destroy();
            }
        }

        [Test]
        public void CleanSerializedArray_RemovesObjectsOutsideTheRollbackWindow()
        {
            var numbers = new SerializedListHandler<SerializedNumber>("Number List");
            var number = new SerializedNumber("Number", 10);

            try
            {
                numbers.SetList(new List<SerializedNumber> { number });
                AssertListOrder(numbers, number);
                numbers.Remove(number);
                AssertListOrder(numbers);
                int rollbackFrame = 100;
                number.FrameRemovedFromActiveList = rollbackFrame - GetRollbackWindow();
                byte[] savedState = Serialize(numbers);

                InvokeCleanup(numbers, rollbackFrame, rollbackFrame + 1);
                Deserialize(numbers, savedState);

                AssertListOrder(numbers);
                Assert.That(numbers.IsSerializedArrayEmpty, Is.True);
                Assert.That(numbers.TryGetSerializedItem(item => item == number, out _), Is.False);
            }
            finally
            {
                numbers.Destroy();
            }
        }

        [Test]
        public void CleanSerializedArray_KeepsObjectsWithinTheRollbackWindow()
        {
            var numbers = new SerializedListHandler<SerializedNumber>("Number List");
            var number = new SerializedNumber("Number", 10);

            try
            {
                numbers.SetList(new List<SerializedNumber> { number });
                AssertListOrder(numbers, number);
                numbers.Remove(number);
                AssertListOrder(numbers);
                int rollbackFrame = 100;
                number.FrameRemovedFromActiveList = rollbackFrame - (GetRollbackWindow() - 1);
                byte[] savedState = Serialize(numbers);

                InvokeCleanup(numbers, rollbackFrame, rollbackFrame + 1);
                Deserialize(numbers, savedState);

                AssertListOrder(numbers);
                Assert.That(numbers.IsSerializedArrayEmpty, Is.False);
                Assert.That(numbers.TryGetSerializedItem(item => item == number, out SerializedNumber restored), Is.True);
                Assert.That(restored, Is.SameAs(number));
                AssertListOrder(numbers, number);
            }
            finally
            {
                numbers.Destroy();
            }
        }

        private static byte[] Serialize(SerializedListHandler<SerializedNumber> numbers)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            numbers.Serialize(writer);
            return stream.ToArray();
        }

        private static void Deserialize(SerializedListHandler<SerializedNumber> numbers, byte[] savedState)
        {
            using var stream = new MemoryStream(savedState);
            using var deserializer = new Deserializer(stream);
            numbers.Deserialize(deserializer);
        }

        private static void AssertSerializedIndices(byte[] savedState, params byte[] expectedIndices)
        {
            Assert.That(savedState[0], Is.EqualTo(expectedIndices.Length), "The first byte is the ordered retained-slot count.");
            Assert.That(savedState.Length, Is.GreaterThanOrEqualTo(expectedIndices.Length + 1));

            for (int i = 0; i < expectedIndices.Length; i++)
            {
                Assert.That(savedState[i + 1], Is.EqualTo(expectedIndices[i]), $"Unexpected retained slot at saved list index {i}.");
            }
        }

        private static void AssertListOrder(SerializedListHandler<SerializedNumber> numbers, params SerializedNumber[] expectedItems)
        {
            Assert.That(numbers.Count, Is.EqualTo(expectedItems.Length));

            for (int i = 0; i < expectedItems.Length; i++)
            {
                Assert.That(numbers[i], Is.SameAs(expectedItems[i]), $"Unexpected item at list index {i}.");
            }
        }

        private static void InvokeCleanup(SerializedListHandler<SerializedNumber> numbers, int rollbackFrame, int targetFrame)
        {
            MethodInfo cleanupMethod = typeof(SerializedListHandler<SerializedNumber>).GetMethod(
                "CleanSerializedArray",
                BindingFlags.Instance | BindingFlags.NonPublic);

            cleanupMethod.Invoke(numbers, new object[] { rollbackFrame, targetFrame });
        }

        private static int GetRollbackWindow()
        {
            MethodInfo rollbackWindowMethod = typeof(SerializedListHandler<SerializedNumber>).GetMethod(
                "GetRollbackWindow",
                BindingFlags.Static | BindingFlags.NonPublic);

            return (int)rollbackWindowMethod.Invoke(null, null);
        }

        private static bool SetOnlineGameStarted(bool onlineGameStarted)
        {
            PropertyInfo property = typeof(GridGameManager).GetProperty(
                nameof(GridGameManager.OnlineGameStarted),
                BindingFlags.Static | BindingFlags.Public);
            bool originalValue = (bool)property.GetValue(null);
            property.GetSetMethod(true).Invoke(null, new object[] { onlineGameStarted });
            return originalValue;
        }

        private sealed class SerializedNumber : ISerializedListObject
        {
            public SerializedNumber(string name, int value)
            {
                ListDisplayName = name;
                Value = value;
            }

            public int Value { get; private set; }
            public string ListDisplayName { get; }
            public ListEvent OnAddedToList { get; set; }
            public ListEvent OnRemovedFromList { get; set; }
            public int FrameAddedToSerializedList { get; set; }
            public int FrameRemovedFromActiveList { get; set; }
            public int SerializedChecksum { get; set; }

            public void OnSerialize(BinaryWriter writer)
            {
                writer.Write(Value);
            }

            public void SetValue(int value)
            {
                Value = value;
            }

            public void OnDeserialize(Deserializer reader)
            {
                Value = reader.ReadInt32();
            }

            public void OnLogGameState(StringBuilder stringBuilder)
            {
                stringBuilder.AppendLine($"{ListDisplayName}: {Value}");
            }
        }
    }
}
