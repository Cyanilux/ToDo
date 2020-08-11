using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cyan.ToDo {
    /// <summary>
    /// Stores references to objects in the scene, assigning a GUID.
    /// That GUID can then be used to obtain the object, allowing for cross-scene references
    /// (as well as references to scene objects for prefabs + ScriptableObject)
    /// </summary>
    [AddComponentMenu("Cyan/Scene References Handler", 2)]
    public class SceneReferencesHandler : MonoBehaviour {
        
        public static bool disallowSceneReferences = false;
        /*
         * Set to true if you want to disable this functionality.
         * Storing scene references dirties the scene, so you might want to disable it if working with collab.
         * Any existing references won't be lost, but GetSceneReferencesHandler(scene) will return null.
         * 
         * If you want to remove/break existing references for a scene, use GameObject -> Cyan.ToDo -> Remove Scene References Handler
        */

#if UNITY_2020_1_OR_NEWER
        [SerializeField] private SerializableDictionary<string, Object> objects = new SerializableDictionary<string, Object>();
#else
        [SerializeField] private SerializableDictionary_StringObject objects = new SerializableDictionary_StringObject();
#endif

        private void Reset() {
            if (gameObject.hideFlags != HideFlags.HideInHierarchy) {
                Debug.LogWarning("Cyan.ToDo : Adding a SceneReferences component to a scene manually is not advised. (see console for more info)\n" +
                    "The ToDo system automatically adds this component when required to handle scene object references " +
                    "for the ScriptableObject version of the To Do list, or cross-scene references for the MonoBehaviour version. " +
                    "Regardless of adding the component manually, SceneReferences.GetSceneReferencesHandler will always use the hidden one, " +
                    "or create a hidden one if it doesn't exist.");
            }
        }

        /// <summary>
        /// Obtains the SceneReferences component on a hidden GameObject inside the given Scene.
        /// If createIfNull is true, and it doesn't exist, it will create and add it to the scene.
        /// Otherwise returns null.
        /// </summary>
        public static SceneReferencesHandler GetSceneReferencesHandler(Scene scene, bool createIfNull = false) {
            if (disallowSceneReferences) return null;
            GameObject[] roots = scene.GetRootGameObjects();

            SceneReferencesHandler sceneReferencesHandler = null;
            for (int i = 0; i < roots.Length; i++) {
                GameObject root = roots[i];
                if (root.TryGetComponent(out sceneReferencesHandler)) {
                    return sceneReferencesHandler;
                }
            }

            // Create Scene Reference Handler in scene
            if (createIfNull) {
                GameObject obj = new GameObject();
                obj.hideFlags = HideFlags.HideInHierarchy;
                SceneManager.MoveGameObjectToScene(obj, scene);
                obj.transform.SetSiblingIndex(0);
                sceneReferencesHandler = obj.AddComponent<SceneReferencesHandler>();
            }
            return sceneReferencesHandler;
        }

        public string RegisterObject(Object obj) {
            string key = GetKeyFromObject(obj);
            if (key != null) return key;
            System.Guid guid = System.Guid.NewGuid();
            string id = guid.ToString();
            objects.Add(id, obj);
            return id;
        }

        public Object GetObjectFromID(string id) {
            if (objects.TryGetValue(id, out Object obj)) {
                return obj;
            }
            return null;
        }

        public string GetKeyFromObject(Object obj) {
            foreach (KeyValuePair<string, Object> entry in objects) {
                if (entry.Value == obj) {
                    return entry.Key;
                }
            }
            return null;
        }

    }

}