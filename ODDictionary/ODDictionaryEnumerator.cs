using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ODDictionary
{
    /// <summary>
    /// Basic enumerator, but I make use of the Observer design pattern to remove the enumerator after it is disposed of.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ODDictionaryEnumerator<TKey, TValue> : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private List<KeyValuePair<TKey, TValue>> EnumerationList { get; set; }
        private ICallbackOnDispose<TKey, TValue> CallbackOnDispose { get; set; }
        private int positionInList;

        public ODDictionaryEnumerator(List<KeyValuePair<TKey, TValue>> enumerationList, ICallbackOnDispose<TKey, TValue> toCallback)
        {
            positionInList = 0;
            EnumerationList = enumerationList;
            CallbackOnDispose = toCallback;
        }

        public bool MoveNext()
        {
            if (positionInList == -1)
                throw new InvalidOperationException();

            positionInList++;
            return (positionInList < EnumerationList.Count);
        }
        /// <summary>
        /// from MSDN: http://msdn.microsoft.com/en-us/library/system.collections.ienumerator.reset.aspx
        /// Notes to Implementers
        ///
        ///All calls to Reset must result in the same state for the enumerator. The preferred implementation 
        ///is to move the enumerator to the beginning of the collection, before the first element. 
        ///
        /// </summary>
        public void Reset()
        {
            positionInList = -1;
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                try
                {
                    return EnumerationList[positionInList];
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        // Wow is this an irritating language feature
        object IEnumerator.Current
        {
            get
            {
                try
                {
                    return EnumerationList[positionInList];
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        void IDisposable.Dispose() 
        {
            EnumerationList = null;
            if (CallbackOnDispose != null)
                CallbackOnDispose.DisposeOf(this);
        }

    }
}
