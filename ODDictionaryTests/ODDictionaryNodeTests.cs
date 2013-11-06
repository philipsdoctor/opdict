using System;
using NUnit.Framework;
using ODDictionary;


namespace ODDictionaryTests
{
    public class ODDictionaryNodeTests
    {
        ODDictionaryNode<String, String> odDictionaryNode;
        [SetUp]
        public void Setup()
        {
            odDictionaryNode = new ODDictionaryNode<String, String>("Key","Value");
        }

        [Test]
        public void Constructor()
        {
            var odDictionaryNodeConstructor = new ODDictionaryNode<String, String>("Key", "Value");
            Assert.NotNull(odDictionaryNodeConstructor);
        }

        [Test]
        public void GetKey()
        {
            Assert.AreEqual("Key", odDictionaryNode.Key);
        }

        [Test]
        public void SetKey()
        {
            odDictionaryNode.Key = "NewKey";
            Assert.AreEqual("NewKey", odDictionaryNode.Key);
        }

        [Test]
        [ExpectedException("System.ArgumentNullException")]
        public void SetKeyNotNull()
        {
            odDictionaryNode.Key = null;
        }

        [Test]
        public void GetValue()
        {
            Assert.AreEqual("Value", odDictionaryNode.Value);
        }

        [Test]
        public void SetValue()
        {
            odDictionaryNode.Value = "NewValue";
            Assert.AreEqual("NewValue", odDictionaryNode.Value);
        }

        [Test]
        [ExpectedException("System.ArgumentNullException")]
        public void SetValueNotNull()
        {
            odDictionaryNode.Value = null;
        }

        [Test]
        public void GetIsDeleted()
        {
            Assert.IsFalse(odDictionaryNode.IsDeleted);
        }

        [Test]
        public void SetIsDeleted()
        {
            odDictionaryNode.IsDeleted = true;
            Assert.IsTrue(odDictionaryNode.IsDeleted);
        }
    }
}
