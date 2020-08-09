using System.Collections.Generic;
using UnityEngine;

namespace Cyan.ToDo {

    /// <summary>
    /// MonoBehaviour version of the To Do list
    /// </summary>
    [AddComponentMenu("Cyan/To Do", 1)]
    public class ToDo : MonoBehaviour {

        public ToDoList list = new ToDoList();
        
        public void OnDrawGizmos() { }

    }
    
    [System.Serializable]
    public class ToDoList {

        public string listName = "To Do";
        public List<ToDoElement> tasks = new List<ToDoElement>();

        public void RemoveCompleted() {
            for (int i = tasks.Count - 1; i >= 0; i--) {
                ToDoElement element = tasks[i];
                if (element.completed) {
                    tasks.RemoveAt(i);
                }
            }
        }
    }
    
    [System.Serializable]
    public class ToDoElement {
        public bool completed = false;
        public string text = "";
        
        public Object objectReference; // (if cross-scene reference, this is the SceneAsset instead, but that only works in-editor)
        public string objectReferenceID; // ID for serializing cross-scene reference (see SceneReferences)
        [System.NonSerialized] public Object tempObjectReference; // actual object for cross-scene reference

        [System.NonSerialized] public bool editing = false;

        public ToDoElement() { }
    }

}
