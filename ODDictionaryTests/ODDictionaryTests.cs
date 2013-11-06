using System;
using NUnit.Framework;
using ODDictionary;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ODDictionaryTests
{
    [TestFixture]
    public class ODDictionaryTests
    {
        ODDictionary<String, String> odDictionaryStringString;
        [SetUp]
        public void Setup()
        {
            odDictionaryStringString = new ODDictionary<String, String>(10);
            ExceptionThrown.Thrown = false;
        }

        [Test]
        // new requirement for classes only creates a problem for Int32 that I used here
        // unlike Java, C# doesn't have an Int ref type that I can find, hence you have to
        // box & unbox to an Object manually
        public void Constructor_ObjectKeyStringValue()
        {
            var odDictionary = new ODDictionary<Object, String>();
            Assert.NotNull(odDictionary);
        }

        [Test]
        public void Constructor_StringKeyStringValue()
        {
            var odDictionary = new ODDictionary<String, String>();
            Assert.NotNull(odDictionary);
        }

        [Test]
        public void Constructor_InitialSize10()
        {
            var odDictionary = new ODDictionary<String, String>(10);
            Assert.AreEqual(10, odDictionary.InnerArray.Length);
        }

        [Test]
        public void Add_StringKeyStringValue()
        {
            odDictionaryStringString.Add("Key", "Value");
            // throws exception if not found 
            var odDictionaryNode = odDictionaryStringString.InnerArray.Single(filter => (filter != null && filter.Key == "Key"));
            // checks value
            Assert.AreEqual("Value", odDictionaryNode.Value);
        }

        [Test]
        [ExpectedException("System.ArgumentException")]
        public void Add_DuplicateKey()
        {
            odDictionaryStringString.Add("Key", "Value");
            odDictionaryStringString.Add("Key", "Value");
        }

        [Test]
        [ExpectedException("System.ArgumentNullException")]
        public void GetIndexForKey_NullKey()
        {
            var odDictionaryChild = new ODDictionaryChild<String,String>();
            odDictionaryChild.GetIndexForKey(null);
        }

        /* GetHashCode is machine specific, this is just not a good test unfortunately.
        [Test]
        public void Add_CollisionGoesToNextValue()
        {
            var odDictionary = new ODDictionary<String, String>(10);
            String collisionString2 = "Key2";
            String collisionString6 = "Key6";
            
            int index2 = Math.Abs(collisionString2.GetHashCode() % 10); // value 9
            int index6 = Math.Abs(collisionString6.GetHashCode() % 10); // value 0
            
            odDictionary.Add("Key2", "Value");
            odDictionary.Add("Key6", "Value");
            Assert.AreEqual("Key2", odDictionary.InnerArray[9].Key);
            Assert.AreEqual("Key6", odDictionary.InnerArray[0].Key);         
        } */

        [Test]
        public void Remove_StringKey_ReturnsTrue()
        {
            odDictionaryStringString.Add("Key", "Value");
            bool removed = odDictionaryStringString.Remove("Key");
            Assert.IsTrue(removed);
        }

        [Test]
        public void Remove_StringKey_ReturnsFalseOnDoubleRemoval()
        {
            odDictionaryStringString.Add("Key", "Value");
            bool removed = odDictionaryStringString.Remove("Key");
            removed = odDictionaryStringString.Remove("Key");
            Assert.IsFalse(removed);
        }

        [Test]
        public void Remove_StringKey_ReturnsFalseOnKeyDoesNotExist()
        {
            odDictionaryStringString.Add("Key", "Value");
            bool removed = odDictionaryStringString.Remove("Key1");
            Assert.IsFalse(removed);
        }

        [Test]
        public void Remove_StringKey_MarkedDeleted()
        {
            odDictionaryStringString.Add("Key", "Value");
            bool removed = odDictionaryStringString.Remove("Key");
            var odDictionaryNode = odDictionaryStringString.InnerArray.Single(filter => (filter != null && filter.Key.Equals("Key")));
            Assert.IsTrue(odDictionaryNode.IsDeleted);
        }

        [Test]
        public void Remove_StringKey_MarkedDeletedWithCollision()
        {
            odDictionaryStringString.Add("Key2", "Value");
            odDictionaryStringString.Add("Key6", "Value");
            odDictionaryStringString.Remove("Key6");
            var odDictionaryNode = odDictionaryStringString.InnerArray.Single(filter => (filter != null && filter.Key.Equals("Key6")));
            Assert.IsTrue(odDictionaryNode.IsDeleted);
            odDictionaryStringString.Remove("Key2");
            var odDictionaryNode2 = odDictionaryStringString.InnerArray.Single(filter => (filter != null && filter.Key.Equals("Key2")));
            Assert.IsTrue(odDictionaryNode2.IsDeleted);
            
        }

        [Test]
        public void Remove_StringKey_MarkedDeletedWithCollisionChangeProbeOrder()
        {
            odDictionaryStringString.Add("Key2", "Value");
            odDictionaryStringString.Add("Key6", "Value");
            odDictionaryStringString.Remove("Key2");
            var odDictionaryNode = odDictionaryStringString.InnerArray.Single(filter => (filter != null && filter.Key.Equals("Key2")));
            Assert.IsTrue(odDictionaryNode.IsDeleted);
            odDictionaryStringString.Remove("Key6");
            var odDictionaryNode2 = odDictionaryStringString.InnerArray.Single(filter => (filter != null && filter.Key.Equals("Key6")));
            Assert.IsTrue(odDictionaryNode2.IsDeleted);
        }

        [Test]
        [ExpectedException("System.ArgumentException")]
        public void Remove_StringKey_RemoveThenAddWhereRemoved()
        {
            odDictionaryStringString.Add("Key2", "Value");
            odDictionaryStringString.Add("Key6", "Value");
            odDictionaryStringString.Remove("Key2");
            odDictionaryStringString.Add("Key6", "Value");
        }

        [Test]
        public void FirstAvailable_LocationUnoccupied()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            bool keyAlreadyPresent;
            int keyLocation = odDictionaryChild.FirstAvailable(5, "Key", out keyAlreadyPresent);
            Assert.IsFalse(keyAlreadyPresent);
            Assert.AreEqual(5, keyLocation);
        }


        [Test]
        public void FirstAvailable_LocationOccupied()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            bool keyAlreadyPresent;
            odDictionaryChild.InnerArray[5] = new ODDictionaryNode<string, string>("Key", "Value");
            int keyLocation = odDictionaryChild.FirstAvailable(5, "Key1", out keyAlreadyPresent);
            Assert.IsFalse(keyAlreadyPresent);
            Assert.AreEqual(6, keyLocation);
        }

        [Test]
        public void FirstAvailable_LocationOccupiedWithDeletedKey()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            bool keyAlreadyPresent;
            odDictionaryChild.InnerArray[5] = new ODDictionaryNode<string, string>("Key", "Value");
            odDictionaryChild.InnerArray[5].IsDeleted = true;
            int keyLocation = odDictionaryChild.FirstAvailable(5, "Key1", out keyAlreadyPresent);
            Assert.IsFalse(keyAlreadyPresent);
            Assert.AreEqual(5, keyLocation);
        }

        [Test]
        public void FirstAvailable_LocationOccupiedWithDeletedSelfKey()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            bool keyAlreadyPresent;
            odDictionaryChild.InnerArray[5] = new ODDictionaryNode<string, string>("Key", "Value");
            odDictionaryChild.InnerArray[5].IsDeleted = true;
            int keyLocation = odDictionaryChild.FirstAvailable(5, "Key", out keyAlreadyPresent);
            Assert.IsFalse(keyAlreadyPresent);
            Assert.AreEqual(5, keyLocation);
        }

        [Test]
        public void FirstAvailable_AlreadyPresent()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            bool keyAlreadyPresent;
            odDictionaryChild.InnerArray[5] = new ODDictionaryNode<string, string>("Key", "Value");
            int keyLocation = odDictionaryChild.FirstAvailable(5, "Key", out keyAlreadyPresent);
            Assert.IsTrue(keyAlreadyPresent);
            Assert.AreEqual(5, keyLocation);
        }

        [Test]
        public void FirstAvailable_AlreadyPresentInitialLocationOccupied()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            bool keyAlreadyPresent;
            odDictionaryChild.InnerArray[5] = new ODDictionaryNode<string, string>("Key", "Value");
            odDictionaryChild.InnerArray[6] = new ODDictionaryNode<string, string>("Key1", "Value");
            int keyLocation = odDictionaryChild.FirstAvailable(5, "Key1", out keyAlreadyPresent);
            Assert.IsTrue(keyAlreadyPresent);
            Assert.AreEqual(6, keyLocation);
        }
        
        [Test]
        public void Find_InDictionary()
        {
            odDictionaryStringString.Add("Key", "Value");
            Assert.AreEqual("Value", odDictionaryStringString["Key"]);
        }
        
        [Test]
        public void Find_InDictionaryWithCollision()
        {
            odDictionaryStringString.Add("Key2", "Value2");
            odDictionaryStringString.Add("Key6", "Value6");
            Assert.AreEqual("Value6", odDictionaryStringString["Key6"]);
        }

        [Test]
        [ExpectedException("System.Collections.Generic.KeyNotFoundException")]
        public void Find_NotInDictionary()
        {
            odDictionaryStringString.Add("Key", "Value");
            Assert.AreEqual("Value", odDictionaryStringString["Key1"]);
        }

        [Test]
        public void Update_InDictionary()
        {
            odDictionaryStringString.Add("Key", "Value");
            odDictionaryStringString["Key"] = "Value2";
            Assert.AreEqual("Value2", odDictionaryStringString["Key"]);
        }

        [Test]
        public void Update_InDictionaryWithCollision()
        {
            odDictionaryStringString.Add("Key2", "Value2");
            odDictionaryStringString.Add("Key6", "Value6");
            odDictionaryStringString["Key6"] = "Value6!";
            Assert.AreEqual("Value6!", odDictionaryStringString["Key6"]);
        }

        [Test]
        public void Update_NotInDictionary()
        {
            odDictionaryStringString["Key"] = "Value";
            Assert.AreEqual("Value", odDictionaryStringString["Key"]);
        }

        [Test]
        public void GrowInnerArray_SizeIncreased()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            odDictionaryChild.InnerArray[5] = new ODDictionaryNode<string, string>("Key", "Value");
            odDictionaryChild.InnerArray[6] = new ODDictionaryNode<string, string>("Key1", "Value");
            odDictionaryChild.InnerArray[7] = new ODDictionaryNode<string, string>("Key2", "Value2");
            odDictionaryChild.InnerArray[7].IsDeleted = true;
            int oldSize = odDictionaryChild.InnerArray.Length;
            odDictionaryChild.GrowInnerArray();
            Assert.IsTrue(odDictionaryChild.InnerArray.Length > oldSize);
        }

        [Test]
        public void GrowInnerArray_NotDeletedObjectsStillPresent()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            odDictionaryChild.InnerArray[5] = new ODDictionaryNode<string, string>("Key", "Value");
            odDictionaryChild.InnerArray[6] = new ODDictionaryNode<string, string>("Key1", "Value1");
            odDictionaryChild.InnerArray[7] = new ODDictionaryNode<string, string>("Key2", "Value2");
            odDictionaryChild.InnerArray[7].IsDeleted = true;
            odDictionaryChild.GrowInnerArray();
            Assert.AreEqual("Value", odDictionaryChild["Key"]);
            Assert.AreEqual("Value1", odDictionaryChild["Key1"]);
        }

        [Test]
        public void GrowInnerArray_DeletedObjectsCleaned()
        {
            var odDictionaryChild = new ODDictionaryChild<String, String>();
            odDictionaryChild.InnerArray[5] = new ODDictionaryNode<string, string>("Key", "Value");
            odDictionaryChild.InnerArray[6] = new ODDictionaryNode<string, string>("Key1", "Value1");
            odDictionaryChild.InnerArray[7] = new ODDictionaryNode<string, string>("Key2", "Value2");
            odDictionaryChild.InnerArray[7].IsDeleted = true;
            odDictionaryChild.GrowInnerArray();
            Assert.IsNull(odDictionaryChild.InnerArray.FirstOrDefault(filter => (filter != null && filter.Key.Equals("Key2"))));

        }
        
        [Test]
        public void CloneObject_IntsAreCopied()
        {
            var original = new TestCloneObjectPrimitive() { Age = 30 };
            var cloneOfOriginal = odDictionaryStringString.CloneObject<TestCloneObjectPrimitive>(original);
            Assert.IsFalse(original == cloneOfOriginal);
            Assert.IsTrue(cloneOfOriginal.Age == original.Age);
        }

        [Test]
        public void CloneObject_StringsAreImmutableAndUntouced()
        {
            var original = new TestCloneObjectStringProperty() { Name = "Philip S Doctor" };
            var cloneOfOriginal = odDictionaryStringString.CloneObject<TestCloneObjectStringProperty>(original);
            Assert.IsFalse(original == cloneOfOriginal);
            Assert.IsTrue(cloneOfOriginal.Name == original.Name);
        }

        [Test]
        public void CloneObject_CloneChildObject()
        {
            var originalChild = new TestCloneObjectStringProperty() { Name = "Philip S Doctor" };
            var originalParent = new TestCloneObjectWithChildObject() { NameParent = "Parent Object", ChildObject = originalChild };
            var cloneOfOriginal = odDictionaryStringString.CloneObject<TestCloneObjectWithChildObject>(originalParent);
            Assert.IsFalse(originalParent == cloneOfOriginal);
            Assert.IsFalse(originalParent.ChildObject == cloneOfOriginal.ChildObject);
            Assert.IsTrue(cloneOfOriginal.ChildObject.Name == originalParent.ChildObject.Name);
            Assert.IsTrue(cloneOfOriginal.NameParent == originalParent.NameParent);
        }

        [Test]
        public void CloneObject_CloneChildArray()
        {
            var originalParent = new TestCloneObjectWithChildArray() { NameParent = "Parent Object", ChildObject = new TestCloneObjectStringProperty[5] };
            originalParent.ChildObject[0] = new TestCloneObjectStringProperty() { Name = "Zero" };
            originalParent.ChildObject[1] = new TestCloneObjectStringProperty() { Name = "One" };
            originalParent.ChildObject[2] = new TestCloneObjectStringProperty() { Name = "Two" };
            originalParent.ChildObject[3] = new TestCloneObjectStringProperty() { Name = "Three" };
            originalParent.ChildObject[4] = null;
            var cloneOfOriginal = odDictionaryStringString.CloneObject<TestCloneObjectWithChildArray>(originalParent);
            Assert.IsFalse(originalParent == cloneOfOriginal);
            Assert.IsFalse(originalParent.ChildObject == cloneOfOriginal.ChildObject);
            Assert.IsTrue(cloneOfOriginal.ChildObject[0].Name == originalParent.ChildObject[0].Name);
            Assert.IsTrue(cloneOfOriginal.ChildObject[1].Name == originalParent.ChildObject[1].Name);
            Assert.IsTrue(cloneOfOriginal.ChildObject[2].Name == originalParent.ChildObject[2].Name);
            Assert.IsTrue(cloneOfOriginal.ChildObject[3].Name == originalParent.ChildObject[3].Name);
            Assert.IsTrue(cloneOfOriginal.ChildObject[4] == null);
            Assert.IsTrue(cloneOfOriginal.NameParent == originalParent.NameParent);
        }

        [Test]
        public void CloneObject_NoDefaultConstructor()
        {
            var originalParent = new TestCloneObjectNonDefaultConstructor(2) { NameParent = "Parent Object", ChildObject = new TestCloneObjectStringProperty[5] };
            originalParent.ChildObject[0] = new TestCloneObjectStringProperty() { Name = "Zero" };
            originalParent.ChildObject[1] = new TestCloneObjectStringProperty() { Name = "One" };
            originalParent.ChildObject[2] = new TestCloneObjectStringProperty() { Name = "Two" };
            originalParent.ChildObject[3] = new TestCloneObjectStringProperty() { Name = "Three" };
            originalParent.ChildObject[4] = null;
            var cloneOfOriginal = odDictionaryStringString.CloneObject<TestCloneObjectNonDefaultConstructor>(originalParent);
            Assert.IsFalse(originalParent == cloneOfOriginal);
            Assert.IsFalse(originalParent.ChildObject == cloneOfOriginal.ChildObject);
            Assert.IsTrue(cloneOfOriginal.ChildObject[0].Name == originalParent.ChildObject[0].Name);
            Assert.IsTrue(cloneOfOriginal.ChildObject[1].Name == originalParent.ChildObject[1].Name);
            Assert.IsTrue(cloneOfOriginal.ChildObject[2].Name == originalParent.ChildObject[2].Name);
            Assert.IsTrue(cloneOfOriginal.ChildObject[3].Name == originalParent.ChildObject[3].Name);
            Assert.IsTrue(cloneOfOriginal.ChildObject[4] == null);
            Assert.IsTrue(cloneOfOriginal.NameParent == originalParent.NameParent);
        }

        [Test]
        public void CloneObject_CloneGeneric()
        {
            var originalParent = new TestCloneObjectGeneric<String>() { Generic = "Philip S Doctor" };
            var cloneOfOriginal = odDictionaryStringString.CloneObject<TestCloneObjectGeneric<String>>(originalParent);
            Assert.IsFalse(originalParent == cloneOfOriginal);
            Assert.IsTrue(cloneOfOriginal.Generic == originalParent.Generic);
        }
        
        [Test]
        public void EnumeratorTest_ReturnsRightNumberOfValues()
        {
            odDictionaryStringString.Add("Key", "Value");

            using (var enumerator = odDictionaryStringString.GetEnumerator()) // using block demonstrates IDisposible interface
            {
                var item = enumerator.Current;
                Assert.AreEqual(item.Key, "Key");
                Assert.AreEqual(item.Value, "Value");
                Assert.IsFalse(enumerator.MoveNext());
            }
        }

        [Test]
        public void EnumeratorTest_ReturnsRightNumberOfValuesAfterRemoveCalled()
        {
            odDictionaryStringString.Add("Key", "Value");
            odDictionaryStringString.Add("Key2", "Value2");
            odDictionaryStringString.Remove("Key");

            using (var enumerator = odDictionaryStringString.GetEnumerator()) // using block demonstrates IDisposible interface
            {
                var item = enumerator.Current;
                Assert.AreEqual(item.Key, "Key2");
                Assert.AreEqual(item.Value, "Value2");
                Assert.IsFalse(enumerator.MoveNext());
            }
        }

        [Test]
        [ExpectedException("System.InvalidOperationException")]
        public void EnumeratorTest_ReturnsWhenNoValidObjects()
        {

            using (var enumerator = odDictionaryStringString.GetEnumerator()) // using block demonstrates IDisposible interface
            {
                Assert.IsNotNull(enumerator);
                var item = enumerator.Current;
            }
        }

        [Test]
        [ExpectedException("System.InvalidOperationException")]
        public void EnumeratorTest_InvalidatesEnumeratorsOnAdd()
        {
            odDictionaryStringString.Add("Key", "Value");


            using (var enumerator = odDictionaryStringString.GetEnumerator()) // using block demonstrates IDisposible interface
            {
                odDictionaryStringString.Add("Key2", "Value2");
                var item = enumerator.Current;
            }
        }

        [Test]
        [ExpectedException("System.InvalidOperationException")]
        public void EnumeratorTest_InvalidatesEnumeratorsOnRemove()
        {
            odDictionaryStringString.Add("Key", "Value");

            using (var enumerator = odDictionaryStringString.GetEnumerator()) // using block demonstrates IDisposible interface
            {
                odDictionaryStringString.Remove("Key");
                var item = enumerator.Current;
            }
        }

        [Test]
        [ExpectedException("System.InvalidOperationException")]
        public void EnumeratorTest_InvalidatesEnumeratorsOnUpdate()
        {
            odDictionaryStringString.Add("Key", "Value");


            using (var enumerator = odDictionaryStringString.GetEnumerator()) // using block demonstrates IDisposible interface
            {
                odDictionaryStringString["Key"] = "Value2";
                var item = enumerator.Current;
            }
        }

        [Test]
        public void Enumerator_NoExceptionAfterDispose()
        {
            odDictionaryStringString.Add("Key", "Value");

            using (var enumerator = odDictionaryStringString.GetEnumerator()) // using block demonstrates IDisposible interface
            {
                var item = enumerator.Current;
            }

            odDictionaryStringString.Remove("Key");
            Assert.Pass("Add/Remove can continue without hitting removed enumerators.");
        }

        [Test]
        public void MultiThreaded_ConcurrencyExceptionOnAdd()
        {

            
            ConcurrencyTester tester1 = new ConcurrencyTester(odDictionaryStringString, 0);
            ConcurrencyTester tester2 = new ConcurrencyTester(odDictionaryStringString, 20000);
            Thread thread1 = new Thread(new ThreadStart(tester1.AddItems));
            Thread thread2 = new Thread(new ThreadStart(tester2.AddItems));

            thread1.Start();
            thread2.Start();
            thread1.Join();
            thread2.Join();
            Assert.IsTrue(ExceptionThrown.Thrown, "Testing concurrency is dicey without modifying base code to add delays, if this test fails, try running it again.");
            
        }

        [Test]
        public void MultiThreaded_ConcurrencyExceptionOnRemove()
        {

            // some starting data
            for (int i = 0; i < 40000; i++)
            {
                odDictionaryStringString.Add("Key" + i, "Value" + i);
            }

            ConcurrencyTester tester1 = new ConcurrencyTester(odDictionaryStringString, 0);
            ConcurrencyTester tester2 = new ConcurrencyTester(odDictionaryStringString, 20000);
            Thread thread1 = new Thread(new ThreadStart(tester1.RemoveItems));
            Thread thread2 = new Thread(new ThreadStart(tester2.RemoveItems));

            thread1.Start();
            thread2.Start();
            thread1.Join();
            thread2.Join();
            Assert.IsTrue(ExceptionThrown.Thrown, "Testing concurrency is dicey without modifying base code to add delays, if this test fails, try running it again.");

        }

        [Test]
        public void MultiThreaded_ConcurrencyExceptionOnUpdate()
        {

            odDictionaryStringString.Add("Key" + 0, "Value" + 0);
            odDictionaryStringString.Add("Key" + 20000, "Value" + 20000);
            ConcurrencyTester tester1 = new ConcurrencyTester(odDictionaryStringString, 0);
            ConcurrencyTester tester2 = new ConcurrencyTester(odDictionaryStringString, 20000);
            Thread thread1 = new Thread(new ThreadStart(tester1.UpdateItems));
            Thread thread2 = new Thread(new ThreadStart(tester2.UpdateItems));

            thread1.Start();
            thread2.Start();
            thread1.Join();
            thread2.Join();
            Assert.IsTrue(ExceptionThrown.Thrown, "Testing concurrency is dicey without modifying base code to add delays, if this test fails, try running it again.");

        }
    }

    public class TestCloneObjectPrimitive
    {
        public Int32 Age { get; set; }
    }

    public class TestCloneObjectStringProperty
    {
        public String Name { get; set; }
    }

    public class TestCloneObjectWithChildObject
    {
        public String NameParent { get; set; }
        public TestCloneObjectStringProperty ChildObject { get; set; }
    }

    public class TestCloneObjectWithChildArray
    {
        public String NameParent { get; set; }
        public TestCloneObjectStringProperty[] ChildObject { get; set; }
    }

    public class TestCloneObjectNonDefaultConstructor
    {
        public TestCloneObjectNonDefaultConstructor(int nonDefaultConstructor)
        { }
        public String NameParent { get; set; }
        public TestCloneObjectStringProperty[] ChildObject { get; set; }
    }

    public class TestCloneObjectGeneric<T>
    {
        public T Generic { get; set; }
    }

    public class ConcurrencyTester
    {
        private ODDictionary<String,String> ODDictionary;
        private int counter;
        public ConcurrencyTester(ODDictionary<String, String> odDictionary, int counterStarter = 0)
        {
            ODDictionary = odDictionary;
            counter = counterStarter;
        }

        public void AddItems()
        {
            try
            {
                for (int i = counter; i < (10000 + counter); i++)
                {
                    ODDictionary.Add("Key" + i, "Value" + i);
                }
            }
            catch (Exception)
            {
                ExceptionThrown.Thrown = true;
            }
        }

        public void RemoveItems()
        {
            try
            {
                for (int i = counter; i < (10000 + counter); i++)
                {
                    ODDictionary.Remove("Key" + i);
                }
            }
            catch (Exception)
            {
                ExceptionThrown.Thrown = true;
            }
        }

        public void UpdateItems()
        {
            try
            {
                for (int i = counter; i < (1000 + counter); i++)
                {
                    ODDictionary["Key" + counter] = "Value";
                    
                }
            }
            catch (Exception)
            {
                ExceptionThrown.Thrown = true;
            }
        }
    }

    /// <summary>
    /// Throwing exceptions from threads is not only not supported by default, but isn't a great idea in 
    /// general. So we need to use this to help us detect a throw.
    /// </summary>
    public class ExceptionThrown
    {
        public static bool Thrown = false;
    }


    /// <summary>
    /// To test protected methods, we can just inherit and then modify the visibility in the child.  Mocks can be handled this 
    /// way too, although chosing a DI framework can be a superior choice. There's some outsanding writing on this subject in
    /// the book "The art of Unit Testing" http://artofunittesting.com/
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ODDictionaryChild<TKey, TValue> : ODDictionary.ODDictionary<TKey, TValue> 
        where TKey : class
        where TValue : class
    {
        public new int GetIndexForKey(TKey key)
        {
            return base.GetIndexForKey(key);
        }

        public new int FirstAvailable(int keyLocation, TKey key, out bool keyAlreadyPresent)
        {
            return base.FirstAvailable(keyLocation, key, out keyAlreadyPresent);
        }

        public new void GrowInnerArray()
        {
            base.GrowInnerArray();
        }
    }
}
