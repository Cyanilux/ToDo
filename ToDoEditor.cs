using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Text;

namespace Cyan.ToDo {

    [CustomEditor(typeof(ToDo))]
    public class ToDoEditor : Editor {

        private ToDo todoList;
        private ReorderableList reorderableList;
        
        private static Color textColor;// = new Color(0.8f, 0.8f, 0.8f);

        private static Color focusColor = new Color(0, 170 / 255f, 187 / 255f, 0.5f);
        private static Color activeColor = new Color(0, 170 / 255f, 187 / 255f, 0.3f);
        
        private static Color completedColor = new Color(0.2f, 0.9f, 0.2f);
        private static Color completedTextColor = Color.black;

        private GUIStyle style_greyLabel;
        private GUIStyle style_textArea;

        private bool actions;

        private void Awake() {
            todoList = target as ToDo;
        }

        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();

            if (style_textArea == null) {
                style_textArea = new GUIStyle(GUI.skin.label);
                style_textArea.alignment = TextAnchor.UpperLeft;
                style_textArea.wordWrap = true;
                style_textArea.richText = true;

                textColor = style_textArea.normal.textColor;
            }
            
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
#if UNITY_2019_1_OR_NEWER
            actions = EditorGUILayout.BeginFoldoutHeaderGroup(actions, "Actions");
#else
            actions = true;
#endif
            if (actions) {
                if (GUILayout.Button(new GUIContent("Remove Completed Tasks", "Remove all tasks marked as complete (green)"))) {
                    if (EditorUtility.DisplayDialog("Remove Completed Tasks",
                        $"Are you sure you want to remove all completed tasks for the To Do List \"{todoList.listName}\"?", "Yes!", "Nooo!")) {
                        // Remove Completed Tasks
                        Undo.RecordObject(todoList, "Remove Completed Tasks");
                        todoList.RemoveCompleted();
                    }
                }

                if (GUILayout.Button(new GUIContent("Export Tasks", "Export tasks as Text File (this won't include object references)"))) {
                    // Export Tasks
                    string path = EditorUtility.SaveFilePanel("Export", "Assets", todoList.listName + "_" + PlayerSettings.productName + ".txt", "txt");
                    if (path.Length != 0) {
                        Export(path);
                    }
                }

                if (GUILayout.Button(new GUIContent("Import Tasks", "Import tasks from Text File (won't affect existing tasks)"))) {
                    // Import Tasks
                    Undo.RecordObject(todoList, "Import Tasks");
                    string path = EditorUtility.OpenFilePanel("Import", "Assets", "txt");
                    if (path.Length != 0) {
                        Import(path);
                    }
                }
            }

#if UNITY_2019_1_OR_NEWER
            EditorGUILayout.EndFoldoutHeaderGroup();
#endif
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
            string listName = EditorGUI.TextField(rect, todoList.listName, style_textArea);
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

#if !UNITY_2020_1_OR_NEWER
            // rect.height is wrong for older versions (tested: 2018.3, 2019.3), so needs recalculating
            elementRect.height = ElementHeight(index);
#else
            // Handling the completed/green background for older versions causes issues while reordering
            if (element.completed) {
                EditorGUI.DrawRect(elementRect, completedColor);
            }
#endif
            if (focus) {
                EditorGUI.DrawRect(elementRect, focusColor);
            }else if (active) {
                EditorGUI.DrawRect(elementRect, activeColor);
            }
        }

        private void DrawElement(Rect rect, int index, bool active, bool focus) {
            ToDoElement element = todoList.list[index];

#if !UNITY_2020_1_OR_NEWER
            // rect.height is wrong for older versions (tested: 2018.3, 2019.3), so needs recalculating
            rect.height = ElementHeight(index);

            // Versions older than 2020 can't handle this in DrawElementBackground
            Rect elementRect = new Rect(rect.x, rect.y + 1, rect.width, rect.height - 1);
            if (element.completed) {
                EditorGUI.DrawRect(elementRect, completedColor);
            }
            if (focus) {
                EditorGUI.DrawRect(elementRect, focusColor);
            } else if (active) {
                EditorGUI.DrawRect(elementRect, activeColor);
            }
#endif
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
            
            // Text Colours
            if (completed) {
                style_textArea.normal.textColor = completedTextColor;
                style_textArea.focused.textColor = completedTextColor;
                style_textArea.hover.textColor = completedTextColor;
            } else {
                style_textArea.normal.textColor = textColor;
                style_textArea.focused.textColor = textColor;
                style_textArea.hover.textColor = textColor;
            }
            
            // If editing, turn off richText
            if (element.editing) {
                style_textArea.richText = false;
            }

            // Text Area
            float x = h + 5;
            float textHeight = rect.height;
            if (element.objectReference) {
                textHeight -= 25;
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
                EditorGUI.LabelField(
                    new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - 27, h),
                    "Linked Object : ",
                    style_textArea);
                x += EditorGUIUtility.labelWidth;
                Object obj = EditorGUI.ObjectField(
                    new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - x, h),
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

        private void Export(string path) {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < todoList.list.Count; i++) {
                ToDoElement element = todoList.list[i];
                string line = "";
                if (element.completed) line += "[Complete]";
                line += element.text.Replace("\n", @"\n");
                stringBuilder.AppendLine(line);
            }

            File.WriteAllText(path, stringBuilder.ToString());
        }

        private void Import(string path) {
            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];

                bool complete = false;
                string text = line;
                if (line.StartsWith("[Complete]")) {
                    text = line.Substring(10);
                    complete = true;
                }

                text = text.Replace(@"\n", "\n");

                ToDoElement element = new ToDoElement() {
                    text = text,
                    completed = complete
                };
                todoList.list.Add(element);
            }
        }
    }
}