using System.Collections.Generic;
using UnityEngine;

namespace Cyan.ToDo {

#if !UNITY_2020_1_OR_NEWER
    // Unity can't serialise generic fields like "SerializableDictionary<string, Object> fieldName" (prior to 2020.1)
    // But it can serialise a class that inherits a generic class, so we need this for previous versions :
    [System.Serializable]
    public class SerializableDictionary_StringObject : SerializableDictionary<string, Object> {

    }
#endif

    /// <summary>
    /// A version of Dictionary which is serialisable by converting it to and from Lists
    /// </summary>
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
        // Note : TKey and TVaue must be Serializable

        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();
        
        public void OnBeforeSerialize() {
            // Convert Dictionary to Lists, so Unity can Serialize it
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this) {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
        
        public void OnAfterDeserialize() {
            // Convert Lists to Dictionary
            Clear();
            for (int i = 0; i < keys.Count; i++) {
                Add(keys[i], values[i]);
            }
        }
    }
}