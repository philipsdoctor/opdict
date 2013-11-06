using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ODDictionary
{
    public interface ICallbackOnDispose<TKey, TValue>
    {
        void DisposeOf(ODDictionaryEnumerator<TKey, TValue> enumerator);
    }
}
