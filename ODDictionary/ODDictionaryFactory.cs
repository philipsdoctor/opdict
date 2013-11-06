using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ODDictionary
{
    public class ODDictionaryFactory
    {
        public static IODDictionary<TKey, TValue> CreateDictionary<TKey, TValue>() 
            where TKey: class
            where TValue: class
        {
            return new ODDictionary<TKey, TValue>();
        }
    }
}
