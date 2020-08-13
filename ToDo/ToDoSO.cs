using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cyan.ToDo {

    /// <summary>
    /// ScriptableObject version of the To Do list
    /// </summary>
    [CreateAssetMenu(fileName = "To Do", menuName = "To Do List (ScriptableObject)", order = 1)]
    public class ToDoSO : ScriptableObject {

        public ToDoList list = new ToDoList();

    }

}