using System;
using NUnit.Framework;
using ODDictionary;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace ODDictionaryTests
{
    class ODDictionaryEnumeratorTests
    {
        List<KeyValuePair<String, String>> innerList;
        ODDictionaryEnumerator<String, String> enumerator;
        [SetUp]
        public void Setup()
        {
            innerList = new List<KeyValuePair<String, String>>();
            innerList.Add(new KeyValuePair<string,string>("Key","Value"));
            innerList.Add(new KeyValuePair<string,string>("Key2","Value2"));
            enumerator = new ODDictionaryEnumerator<string,string>(innerList, null);
        }

        [Test]
        public void Enumerator_ImplementsIDisposible()
        {
            using (var enumeratorDisposible = new ODDictionaryEnumerator<string, string>(innerList, null)) // using block demonstrates IDisposible interface
            {
                Assert.Pass("Didn't throw an error");
            }
        }

        [Test]
        public void Enumerator_ReturnsCurrent()
        {
            var item = enumerator.Current;
            Assert.AreEqual(item.Key, "Key");
            Assert.AreEqual(item.Value, "Value");
        }

        [Test]
        public void Enumerator_MoveNextReturnsFalseAtEnd()
        {
            var item = enumerator.Current;
            enumerator.MoveNext();
            var item2 = enumerator.Current;
            Assert.IsFalse(enumerator.MoveNext());            
        }

        [Test]
        public void Enumerator_MoveNextReturnsTrueIfNext()
        {
            var item = enumerator.Current;
            Assert.IsTrue(enumerator.MoveNext());
         }

        [Test]
        public void Enumerator_MoveNextAdvancesPosition()
        {
            var item = enumerator.Current;
            enumerator.MoveNext();
            var item2 = enumerator.Current;
            Assert.AreNotEqual(item, item2);
        }

        [Test]
        [ExpectedException("System.InvalidOperationException")]
        public void Enumerator_ResetInvalidatesNextCall()
        {
            var item = enumerator.Current;
            enumerator.Reset();
            var item2 = enumerator.Current;
        }

    }
}
