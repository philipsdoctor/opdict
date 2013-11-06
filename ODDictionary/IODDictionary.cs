using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ODDictionary
{
    public interface IODDictionary<TKey, TValue>
    {
        IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();
        void Add(TKey key, TValue value);
        bool Remove(TKey key);
        TValue this[TKey key] { get; set;} // this is a property that will allow us the pleasant syntax of myDictionary["key"] = value;
    }
}
