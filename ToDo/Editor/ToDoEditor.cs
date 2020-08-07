﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Cyan.ToDo {

    [CustomEditor(typeof(ToDo))]
    public class ToDoEditor : Editor {

        private ToDo todoList;
        private ReorderableList reorderableList;
        
        private static Color focusColor = new Color(0, 170 / 255f, 187 / 255f, 0.5f);
        private static Color activeColor = new Color(0, 170 / 255f, 187 / 255f, 0.3f);
        private static Color completedColor = new Color(0.2f, 0.9f, 0.2f);

        private GUIStyle style_greyLabel;
        private GUIStyle style_textArea;

        private void Awake() {
            todoList = target as ToDo;
        }

        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();
            
            if (reorderableList == null) {
                // Create Reorderable List
                reorderableList = new ReorderableList(todoList.list, typeof(ToDoElement));
                reorderableList.drawHeaderCallback = DrawHeader;
                reorderableList.elementHeightCallback = ElementHeight;
                reorderableList.drawElementCallback = DrawElement;
                reorderableList.drawElementBackgroundCallback = DrawElementBackground;
                reorderableList.onAddCallback = OnAdd;
                reorderableList.onRemoveCallback = OnRemove;
            }

            // Render Reorderable List
            reorderableList.DoLayoutList();

            // Buttons
            if (GUILayout.Button("Remove Completed Tasks")) {
                if (EditorUtility.DisplayDialog("Remove Completed Tasks",
                    $"Are you sure you want to remove all completed tasks for the To Do List \"{todoList.listName}\"?", "Yes!", "Nooo!")) {

                    Undo.RecordObject(todoList, "Remove Completed Tasks");

                    // Remove Completed Tasks
                    for (int i = todoList.list.Count - 1; i >= 0; i--) {
                        ToDoElement element = todoList.list[i];
                        if (element.completed) {
                            todoList.list.RemoveAt(i);
                        }
                    }

                }
            }
        }
        
        private void OnAdd(ReorderableList list) {
            Undo.RecordObject(todoList, "Task Added");
            todoList.list.Add(new ToDoElement());
        }

        private void OnRemove(ReorderableList list) {
            Undo.RecordObject(todoList, "Task Removed");
            todoList.list.RemoveAt(list.index);
        }

        private void DrawHeader(Rect rect) {
            if (style_greyLabel == null) {
                style_greyLabel = new GUIStyle(GUI.skin.label);
                style_greyLabel.alignment = TextAnchor.MiddleRight;
                style_greyLabel.normal.textColor = Color.grey;
            }
            EditorGUI.LabelField(rect, "@Cyanilux", style_greyLabel);
            
            EditorGUI.BeginChangeCheck();
            string listName = EditorGUI.TextField(rect, todoList.listName, GUI.skin.label);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(todoList, "List Name Change");
                todoList.listName = listName;
            }
        }

        private float GetWidth() {
            return Screen.width - 87;
        }

        private float ElementHeight(int index) {
            ToDoElement element = todoList.list[index];
            
            float width = GetWidth();
            if (style_textArea == null) {
                style_textArea = new GUIStyle(GUI.skin.label);
                style_textArea.alignment = TextAnchor.UpperLeft;
                style_textArea.wordWrap = true;
            }
            if (element.editing) {
                style_textArea.richText = false;
            }

            // Height
            float height = style_textArea.CalcHeight(new GUIContent(element.text), width) + 5;
            style_textArea.richText = true;

            if (element.objectReference != null) {
                height += EditorGUIUtility.singleLineHeight + 4;
            }
            height = Mathf.Max(EditorGUIUtility.singleLineHeight + 4, height);
            return height;
        }

        private void DrawElementBackground(Rect rect, int index, bool active, bool focus) {
            if (index < 0) return;
            ToDoElement element = todoList.list[index];
            
            Rect elementRect = new Rect(rect.x, rect.y + 1, rect.width, rect.height - 1);
            if (element.completed) {
                EditorGUI.DrawRect(elementRect, completedColor);
            }
            if (focus) {
                EditorGUI.DrawRect(elementRect, focusColor);
            }else if (active) {
                EditorGUI.DrawRect(elementRect, activeColor);
            }
        }

        private void DrawElement(Rect rect, int index, bool active, bool focus) {
            ToDoElement element = todoList.list[index];
            
            // Toggle
            float h = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginChangeCheck();
            bool completed = EditorGUI.Toggle(
                new Rect(rect.x + 2, rect.y + 2, h, h),
                element.completed);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(todoList, "Toggled Task Completion");
                element.completed = completed;
            }

            // This prevents text area from highlighting all text on focus
            bool preventSelection = (UnityEngine.Event.current.type == EventType.MouseDown);
            Color cursorColor = GUI.skin.settings.cursorColor;
            if (preventSelection) {
                GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);
            }

            // Text Area
            if (style_textArea == null) {
                style_textArea = new GUIStyle(GUI.skin.label);
                style_textArea.alignment = TextAnchor.UpperLeft;
                style_textArea.wordWrap = true;
                style_textArea.richText = true;
            }

            float x = h + 5;
            float textHeight = rect.height;
            if (element.objectReference) {
                textHeight -= 25;
            }

            // If editing, turn off richText
            if (element.editing) {
                style_textArea.richText = false;
            }

            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("TextArea");
            string text = EditorGUI.TextArea(
                new Rect(rect.x + x, rect.y + 2, rect.width - x, textHeight),
                element.text, style_textArea);

            element.editing = (GUI.GetNameOfFocusedControl() == "TextArea");
            style_textArea.richText = true;

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(todoList, "Edited Task Text");
                element.text = text;
            }

            // Reset Cursor Color
            if (preventSelection) {
                GUI.skin.settings.cursorColor = cursorColor;
            }

            // Object Field
            if (element.objectReference) {
                EditorGUI.BeginChangeCheck();
                Object obj = EditorGUI.ObjectField(
                    new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - 27, h),
                    "Linked Object : ",
                    element.objectReference,
                    typeof(Object), true);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(todoList, "Changed Task Object");
                    element.objectReference = obj;
                }
            }

            // Handle Drag & Drop Object onto Element
            UnityEngine.Event currentEvent = UnityEngine.Event.current;
            if (rect.Contains(currentEvent.mousePosition)) {
                if (currentEvent.type == EventType.DragUpdated) {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    currentEvent.Use();
                } else if (currentEvent.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();
                    if (DragAndDrop.objectReferences.Length > 0) {
                        Object obj = DragAndDrop.objectReferences[0];
                        Undo.RecordObject(todoList, "Changed Task Object");
                        element.objectReference = obj;
                    }
                    currentEvent.Use();
                }
            }

        }
    }
}