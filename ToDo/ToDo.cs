using System.Collections.Generic;
using UnityEngine;

namespace Cyan.ToDo {

    public class ToDo : MonoBehaviour {

        public string listName = "To Do";
        public List<ToDoElement> list = new List<ToDoElement>();
        
        public void OnDrawGizmos() { }

    }
    
    [System.Serializable]
    public class ToDoElement {
        public bool completed = false;
        public string text = "";
        public Object objectReference;

        public bool editing = false;

        public ToDoElement() { }
    }
}
