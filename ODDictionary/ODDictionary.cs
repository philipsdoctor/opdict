using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;

namespace ODDictionary
{
    public class ODDictionary<TKey, TValue> : IODDictionary<TKey, TValue>, ICallbackOnDispose<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        internal ODDictionaryNode<TKey, TValue>[] InnerArray {get;set;}
        internal IEnumerator PrimeNumberEnumerator; // prime numbers for hashtable size
        internal int CurrentSize {get;set;}

        // You may ask yourself, why not IEnumerator<KeyValuePair<TKey,TValue>> ? the answer is even though C# enumerators should have Reset(), the 
        // IEnumerator interface does not expose it.
        internal List<ODDictionaryEnumerator<TKey, TValue>> EnumeratorsToInvalidate; // a list of all enumerators to invalidate.  

        /// <summary>
        /// I opted for Optimistic over Pessimistic concurrency.  In this case it means for single
        /// threaded we don't pay the price of locks, and in multi-threaded it is up to the consumer to 
        /// figure out priorities/queues/locking.
        /// http://publib.boulder.ibm.com/infocenter/soliddb/v6r3/index.jsp?topic=/com.ibm.swg.im.soliddb.sql.doc/doc/pessimistic.vs.optimistic.concurrency.control.html
        /// </summary>
        private bool OptimisticConcurrencyControl; // simple control to detect concurrency exceptions rather than locking resources

        public ODDictionary()
        {
            PrimeNumberEnumerator = SetPrimeNumbers();
            PrimeNumberEnumerator.MoveNext();
            CurrentSize = 0;
            InnerArray = new ODDictionaryNode<TKey, TValue>[(int)PrimeNumberEnumerator.Current];
            EnumeratorsToInvalidate = new List<ODDictionaryEnumerator<TKey, TValue>>();
            OptimisticConcurrencyControl = false;
        }

        /// <summary>
        /// Allows the user to set the initial value of the inner array
        /// </summary>
        /// <param name="initialArraySize">Int32 for the size of the inner array</param>
        public ODDictionary(int initialArraySize)
        {
            PrimeNumberEnumerator = SetPrimeNumbers();

            while (PrimeNumberEnumerator.MoveNext() != false)
            { 
                // next grow will be at least initialArraySize *2
                if ((int)PrimeNumberEnumerator.Current > (initialArraySize * 2))
                    break;
            }

            CurrentSize = 0;
            InnerArray = new ODDictionaryNode<TKey, TValue>[initialArraySize];
            EnumeratorsToInvalidate = new List<ODDictionaryEnumerator<TKey, TValue>>();
            OptimisticConcurrencyControl = false;
        }

        /// <summary>
        /// Hash tables will often perform better with prime numbers for the table size, I found a nice
        /// list here and I'm using it to set initial size and grow values
        /// http://planetmath.org/encyclopedia/GoodHashTablePrimes.html
        /// </summary>
        protected IEnumerator SetPrimeNumbers()
        {
            var PrimeNumbers = new int[] { 12289, 24593, 49157, 98317, 196613, 393241, 786433, 1572869, 3145739, 6291469, 12582917, 25165843, 50331653, 100663319, 201326611, 402653189, 805306457, 1610612741 };
            return PrimeNumbers.GetEnumerator();
        }

        /// <summary>
        /// This operation will grow the inner array and sweep up deleted nodes
        /// This is an expensive operation that we should minimize the usage of if possible
        /// </summary>
        /// <returns></returns>
        protected void GrowInnerArray()
        {
            var oldNodes = InnerArray;
           
            int newArraySize;
            if (PrimeNumberEnumerator.MoveNext() != false)
            {
                newArraySize = (int)PrimeNumberEnumerator.Current;
            }
            else
            {
                throw new ArgumentOutOfRangeException("The ODDictionary has reached its maximum capacity.");
            }
            
            InnerArray = new ODDictionaryNode<TKey, TValue>[newArraySize];

            foreach (var node in oldNodes)
            {
                if (node != null && node.IsDeleted == false)
                {
                    // get the initial probe value
                    int keyLocation = GetIndexForKey(node.Key);

                    // confirm that key is not present and get first available insert location
                    bool keyAlreadyPresent;
                    keyLocation = FirstAvailable(keyLocation, node.Key, out keyAlreadyPresent);

                    //we don't need to worry about keyAlreadyPresent because we're repopulating a new array
                    InnerArray[keyLocation] = node;
                }
            }
        }

        /// <summary>
        /// This helper method will accept a key and return the integer array index that the key should be placed at.
        /// This method ignores collisions.
        /// </summary>
        /// <param name="Key">The key to get the index for, not null</param>
        /// <returns>The integer index for this key</returns>
        protected int GetIndexForKey(TKey key)
        {
            // validation
            if (key == null)
                throw new ArgumentNullException("Key may be null.");

            // Get hashcode for the key, mod by array length, hashcode can be negative at times
            int index = Math.Abs(key.GetHashCode() % InnerArray.Length);

            return index;
        }

        /// <summary>
        /// Finds the first available non-key location in the index to perform an insert. Or the key location if it is already present.
        /// Instead of an out parameter, throw could be used, but in this case, finding a key is to be expected, and try/catch should not be used
        /// for control flow.
        /// </summary>
        /// <param name="keyLocation">Starting index to probe</param>
        /// <param name="key">The to be inserted</param>
        /// <param name="keyAlreadyPresent">Outpool boolean, flag is true is the key is already present</param>
        /// <returns>Location to perform insert or the location of the key if already present</returns>
        protected int FirstAvailable(int keyLocation, TKey key, out bool keyAlreadyPresent)
        {
            ODDictionaryNode<TKey, TValue> currentNode = InnerArray[keyLocation];
            int firstGoodLocation = -1;
            keyAlreadyPresent = false;
            // probe the values, if it's null we're good
            while (currentNode != null)
            {
                // We found an indentical key, insert is not logically possible, stop searching
                if (currentNode.Key.Equals(key) && !currentNode.IsDeleted)
                {
                    keyAlreadyPresent = true;
                    break;
                }

                // we found a deleted location, remember it for later to use it
                if (currentNode.IsDeleted && firstGoodLocation == -1)
                    firstGoodLocation = keyLocation;

                keyLocation = (keyLocation + 1) % InnerArray.Length;
                currentNode = InnerArray[keyLocation];                
            }

            // if we didn't find the key, but we found a first location that's prior to our first null, then return that.
            if (!keyAlreadyPresent && firstGoodLocation != -1)
                keyLocation = firstGoodLocation;

            return keyLocation;
        }

        /// <summary>
        /// Returns an Enumerator of type <TKey, TValue>, Requirements reqest lack of a yeild operator in code
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var returnList =
                InnerArray
                .Where(filter => (filter != null && filter.IsDeleted == false))
                .Select(output => 
                    new KeyValuePair<TKey, TValue>(
                        CloneObject(output.Key), 
                        CloneObject(output.Value)
                    )).ToList<KeyValuePair<TKey, TValue>>();
            var enumerator = new ODDictionaryEnumerator<TKey, TValue>(returnList, this);
            EnumeratorsToInvalidate.Add(enumerator);
            return enumerator;
        }


        /// <summary>
        /// See comments in RecursiveFieldCopy()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal T CloneObject<T>(T obj) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException("Object cannot be null");
            return (T)RecursiveFieldCopy(obj);
        }
        /// <summary>
        /// I had several false starts on this method. 
        /// My first attempt did this:
        /// System.Reflection.MethodInfo inst = obj.GetType().GetMethod("MemberwiseClone", 
        ///     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
        /// To dodge the IClonable requirement, and then call it recursively, but I kept tripping up on Arrays.
        /// I then tried a more classic serializable approach, but I really didn't want to constrain users to 
        /// use only serializable objects.
        /// I found this article http://www.codeproject.com/Articles/38270/Deep-copy-of-objects-in-C
        /// Which got me most of the way there, but I had to tweak it a bit.  
        /// 
        /// First off it relied on Activator.CreateInstance() which failed my tests with non-default constructors, so I swapped to Invoke().
        /// 
        /// Second off, the Array section crashed if your class wasn't in the Dictionary assembly.  As I can imagine a dictionary user
        /// might want to use their own custom types, I swapped that to detecting the assembly with the type, and getting that type.
        /// 
        /// Those two changes made it much more robust, and I'm now happy with it.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal object RecursiveFieldCopy(object obj)
        {
            if (obj == null) return null;

            Type type = obj.GetType();
            if (type.IsValueType) // value types can be safely returned
            {
                return obj;
            }
            else if (type == typeof(string)) // strings are immutable and can be safely returned
            {
                return obj;
            }
            else if (type.IsArray) // if the type is an array...
            {
                var array = obj as Array;
                 
                var typeName = type.FullName.Replace("[]", string.Empty); // get the name of the type that we have an array of                
                var assembly = Assembly.GetAssembly(obj.GetType()); // get the assembly with that type
                var elementType = assembly.GetTypes().FirstOrDefault(t => t.FullName == typeName); // find the type by name           
                if (elementType == null)
                    throw new ArgumentException("Missing type definition for object to clone.");
                Array copied = Array.CreateInstance(elementType, array.Length); // create a new array
                for (int i = 0; i < array.Length; i++) // populate it recursively
                {
                    copied.SetValue(RecursiveFieldCopy(array.GetValue(i)), i);
                }
                return Convert.ChangeType(copied, obj.GetType());
            }
            else if (type.IsClass)
            {
                object cloneWithoutConstructor = new object();
                System.Reflection.MethodInfo inst = obj.GetType().GetMethod("MemberwiseClone",
                   System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (inst != null)
                    cloneWithoutConstructor = inst.Invoke(obj, null);

                FieldInfo[] fields = type.GetFields(BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object fieldValue = field.GetValue(obj);
                    if (fieldValue == null)
                        continue;
                    field.SetValue(cloneWithoutConstructor, RecursiveFieldCopy(fieldValue));
                }
                return cloneWithoutConstructor;
            }
            else
                throw new ArgumentException("Type specified is unknown.");

        }

        /// <summary>
        /// Add a Key/Value pair to the collection.  Throws exception if key already present.
        /// </summary>
        /// <param name="key">Key to be used for retrieval, may not be null</param>
        /// <param name="value">Value to be returned on retrieval</param>
        public void Add(TKey key, TValue value)
        {
            // check concurrency control
            if (OptimisticConcurrencyControl == true)
                throw new Exception("Optimistic concurrency control violated");
            // set control
            OptimisticConcurrencyControl = true;

            // validation
            if (key == null || value == null)
            {
                OptimisticConcurrencyControl = false;
                throw new ArgumentNullException("Neither key nor value may be null.");
            }

            // To minimize collisions, we will grow the collection when it is half full
            if (CurrentSize > InnerArray.Length / 2)
            {
                GrowInnerArray();
            }

            // get the initial probe value
            int keyLocation = GetIndexForKey(key);          

            // confirm that key is not present and get first available insert location
            bool keyAlreadyPresent;
            keyLocation = FirstAvailable(keyLocation, key, out keyAlreadyPresent);

            if (keyAlreadyPresent)
            {
                OptimisticConcurrencyControl = false;
                throw new ArgumentException("An item with the same key has already been added.");
            }

            var odDictionaryNode = new ODDictionaryNode<TKey,TValue>(key, value);
            InnerArray[keyLocation] = odDictionaryNode;
            OptimisticConcurrencyControl = false; // release control
            InvalidateEnumerators();
            CurrentSize++;
            
        }

        /// <summary>
        /// Remove method, will search for they key and make it unavailable for retrieval
        /// </summary>
        /// <param name="key">Key to serach for</param>
        /// <returns>True if item is found and removed, false if not found.</returns>
        public bool Remove(TKey key)
        {
            // check concurrency control
            if (OptimisticConcurrencyControl == true)
                throw new Exception("Optimistic concurrency control violated");
            // set control
            OptimisticConcurrencyControl = true;

            // validation
            if (key == null)
            {
                OptimisticConcurrencyControl = false;
                throw new ArgumentNullException("Key may not be null.");                
            }
            
            int keyLocation = GetIndexForKey(key);

            // confirm that key is not present and get first available insert location
            bool keyAlreadyPresent;
            keyLocation = FirstAvailable(keyLocation, key, out keyAlreadyPresent);

            // validate that key actually there
            if (!keyAlreadyPresent)
            {
                OptimisticConcurrencyControl = false;
                return false;
            }

            InnerArray[keyLocation].IsDeleted = true;
            CurrentSize--; // TODO: think about this
            OptimisticConcurrencyControl = false; // release control
            InvalidateEnumerators();
            return true;
        }

        /// <summary>
        /// The find & return method.  Specify a key and get the return value requested or set value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                // check concurrency control
                if (OptimisticConcurrencyControl == true)
                    throw new Exception("Optimistic concurrency control violated");
                OptimisticConcurrencyControl = true;

                // validation
                if (key == null)
                {
                    OptimisticConcurrencyControl = false;
                    throw new ArgumentNullException("Key may not be null.");
                }

                int keyLocation = GetIndexForKey(key);

                // Find the key
                bool keyAlreadyPresent;
                keyLocation = FirstAvailable(keyLocation, key, out keyAlreadyPresent);

                if (!keyAlreadyPresent)
                {
                    OptimisticConcurrencyControl = false;
                    throw new KeyNotFoundException("The given key was not present in the dictionary.");
                }

                var value = InnerArray[keyLocation].Value;
                OptimisticConcurrencyControl = false;
                return value;
            }
            set
            {
                // check concurrency control
                if (OptimisticConcurrencyControl == true)
                    throw new Exception("Optimistic concurrency control violated");
                OptimisticConcurrencyControl = true;

                int keyLocation = GetIndexForKey(key);

                // Find the key
                bool keyAlreadyPresent;
                keyLocation = FirstAvailable(keyLocation, key, out keyAlreadyPresent);

                // if it's not yet in the dictionary, add it
                if (!keyAlreadyPresent)
                {
                    var odDictionaryNode = new ODDictionaryNode<TKey, TValue>(key, value);
                    InnerArray[keyLocation] = odDictionaryNode;
                    CurrentSize++;
                    OptimisticConcurrencyControl = false;
                }
                else
                {
                    // else just update the value
                    InnerArray[keyLocation].Value = value;
                    OptimisticConcurrencyControl = false;
                }
                InvalidateEnumerators();
            }
        }

        /// <summary>
        /// When an enumerator is disposed of, the dictionary gets a callback and it is removed from the list of enumerators to invalidate
        /// </summary>
        /// <param name="enumerator"></param>
        public void DisposeOf(ODDictionaryEnumerator<TKey, TValue> enumerator)
        {
            EnumeratorsToInvalidate.Remove(enumerator);
        }

        /// <summary>
        /// Something has happened to invalidate all enumerators, iterate over the list invalidating them all
        /// </summary>
        protected void InvalidateEnumerators()
        {            
            foreach (var enumerator in EnumeratorsToInvalidate)
            {
                enumerator.Reset();
            }
        }
    }
}
