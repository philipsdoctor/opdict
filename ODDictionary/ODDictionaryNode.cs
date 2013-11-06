using System;

namespace ODDictionary
{
    public class ODDictionaryNode<TKey, TValue>
    {

        /// <summary>
        /// Basic data storage class, properties use private member variables because otherwise there's some support issues with generic properties
        /// </summary>
        private TKey _key;
        private TValue _value;
        private bool _isDeleted;

        public ODDictionaryNode(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            IsDeleted = false;
        }
        
        public TKey Key
        {
            get { return _key; }
            
            set {

                if (value == null)
                    throw new ArgumentNullException("Key cannot be null");
                _key = value; 
            }
        }

        public TValue Value
        {
            get { return _value; }

            set
            {

                if (value == null)
                    throw new ArgumentNullException("Value cannot be null");
                _value = value;
            }
        }

        public bool IsDeleted
        {
            get { return _isDeleted; }
            set { _isDeleted = value; }
        }
    }
}
