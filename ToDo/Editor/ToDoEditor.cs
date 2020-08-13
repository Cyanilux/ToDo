using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;

namespace Cyan.ToDo {

    [CustomEditor(typeof(ToDoSO))]
    public class ToDoSOEditor : ToDoEditor {

        protected override void Awake() {
            base.Awake();
            todoList = (target as ToDoSO).list;
        }

    }

    [CustomEditor(typeof(ToDo))]
    public class ToDoMBEditor : ToDoEditor {

        protected override void Awake() {
            base.Awake();

            ToDo todo = target as ToDo;
            todoList = todo.list;
            scenePath = todo.gameObject.scene.path;
            
            PrefabUtility.prefabInstanceUpdated += PrefabUpdated;
        }

        private void OnDisable() {
            PrefabUtility.prefabInstanceUpdated -= PrefabUpdated;
        }

        private void PrefabUpdated(GameObject prefab) {
            // Prefab created / updated

            // This would usually break all scene GameObject/Component references
            // But we can convert + serialise an ID, in order to retain references (see SceneReferencesHandler)
            
            // Note : This isn't called unless the object is SELECTED when the prefab is created since it's in a custom inspector!!

            if (scenePath == null || scenePath == "") return; // Ignore updates to the Prefab asset itself, we only care about scene overrides

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
            if (path == null || path == "") return;

            ToDo todo = target as ToDo;
            for (int i = 0; i < todo.list.tasks.Count; i++) {
                ToDoElement element = todo.list.tasks[i];
                if (element.objectReference != null) {
                    // Has reference
                    if (element.objectReferenceID == null || element.objectReferenceID == "") {
                        // Has no objectReferenceID, so is a scene-reference or Asset (not cross-scene already)
                        if (!IsReferenceAllowed(element.objectReference, null, out GameObject gameObject)) {
                            // Is in-scene object reference!
                            if (gameObject == prefab) {
                                // It's okay, it's the prefab itself, so we can ignore it!
                            } else {
                                // Is in-scene object reference, need to swap to cross-scene reference
                                LinkCrossSceneReference(element, element.objectReference, gameObject);
                            }
                        }
                    }
                }
            }

            // Apply Prefab Changes
            PrefabUtility.prefabInstanceUpdated -= PrefabUpdated;
            PrefabUtility.ApplyObjectOverride(todo, path, InteractionMode.AutomatedAction);
            PrefabUtility.prefabInstanceUpdated += PrefabUpdated;
            // Note, in 2018.3 seems to cause unity to crash/hold due to calling PrefabUpdated
            // so need to unregister + reregister to prevent infinite loop.
            // This didn't seem to happen in 2020.1, but should probably do it still just to be safe
        }
    }

    /// <summary>
    /// Editor / Custom Inspector for the To Do lists.
    /// Awake method should be overriden, and todoList value should be set.
    /// </summary>
    public class ToDoEditor : Editor {

        protected ToDoList todoList;
        protected string scenePath;
        // If MonoBehaviour, this is the scene the object is a part of. Used to prevent cross-scene GameObject/Component references.
        // If ScriptableObject, is null and used to warn about GameObject/Component scene references
        
        private ReorderableList reorderableList;
        
        private static Color textColor;

        private static Color focusColor = new Color(0, 170 / 255f, 187 / 255f, 0.5f);
        private static Color activeColor = new Color(0, 170 / 255f, 187 / 255f, 0.3f);
        
        private static Color completedColor = new Color(0.2f, 0.9f, 0.2f);
        private static Color completedTextColor = Color.black;

        private GUIStyle style_greyLabel;
        private GUIStyle style_textArea;
        private GUIStyle style_label;

        private bool actions;

        [MenuItem("GameObject/Create Other/To Do List (MonoBehaviour)")]
        static void Create() {
            GameObject obj = new GameObject("To Do");
            obj.AddComponent<ToDo>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Cyan.ToDo/Remove Scene References Handler")]
        static void DeleteSceneReferences() {
            if (EditorUtility.DisplayDialog("Remove Scene Reference Handler",
                "Hey! This will remove the hidden GameObject containing the SceneReferences component in the current active scene. It handles : \n\n"+
                "\u2022 Cross-scene object references for ToDo (MonoBehaviour)\n" +
                "\u2022 Scene object references for ToDo (ScriptableObject)\n\n" +
                "This will break any references that tasks have to objects in this scene!", "Delete it!", "Abort!")) {
                Scene scene = SceneManager.GetActiveScene();
                SceneReferencesHandler sceneReferencesHandler = SceneReferencesHandler.GetSceneReferencesHandler(scene);
                if (sceneReferencesHandler != null) {
                    Debug.Log($"Cyan.ToDo : Scene References Handler has been removed from scene \"{scene.name}\"");
                    DestroyImmediate(sceneReferencesHandler.gameObject);
                }
            }
        }

        [MenuItem("GameObject/Cyan.ToDo/Select Scene References Handler")]
        static void SelectSceneReferences() {
            Scene scene = SceneManager.GetActiveScene();
            SceneReferencesHandler sceneReferencesHandler = SceneReferencesHandler.GetSceneReferencesHandler(scene);
            if (sceneReferencesHandler != null) {
                Selection.activeGameObject = sceneReferencesHandler.gameObject;
            }
        }

        [MenuItem("GameObject/Cyan.ToDo/Remove Scene References Handler", true)]
        [MenuItem("GameObject/Cyan.ToDo/Select Scene References Handler", true)]
        static bool ValidateSceneReferences() {
            Scene scene = SceneManager.GetActiveScene();
            return (SceneReferencesHandler.GetSceneReferencesHandler(scene) != null);
        }

        protected virtual void Awake() { }
        
        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();
            
            CreateTextStyles();
            ResetTextColor();

            if (reorderableList == null) {
                // Create Reorderable List
                reorderableList = new ReorderableList(todoList.tasks, typeof(ToDoElement));
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
                        Undo.RecordObject(target, "Remove Completed Tasks");
                        todoList.RemoveCompleted();
                        RecordPrefabInstancePropertyModifications(target);
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
                    Undo.RecordObject(target, "Import Tasks");
                    string path = EditorUtility.OpenFilePanel("Import", "Assets", "txt");
                    if (path.Length != 0) {
                        Import(path);
                    }
                    RecordPrefabInstancePropertyModifications(target);
                }
            }

#if UNITY_2019_1_OR_NEWER
            EditorGUILayout.EndFoldoutHeaderGroup();
#endif
        }
        
        private void OnAdd(ReorderableList list) {
            Undo.RecordObject(target, "Task Added");
            todoList.tasks.Add(new ToDoElement());
            RecordPrefabInstancePropertyModifications(target);
        }

        private void OnRemove(ReorderableList list) {
            Undo.RecordObject(target, "Task Removed");
            todoList.tasks.RemoveAt(list.index);
            RecordPrefabInstancePropertyModifications(target);
        }

        private void DrawHeader(Rect rect) {
            if (style_greyLabel == null) {
                style_greyLabel = new GUIStyle(GUI.skin.label);
                style_greyLabel.alignment = TextAnchor.MiddleRight;
                style_greyLabel.normal.textColor = Color.grey;
            }
            EditorGUI.LabelField(rect, "@Cyanilux", style_greyLabel);

            CreateTextStyles();
            ResetTextColor();

            EditorGUI.BeginChangeCheck();
            string listName = EditorGUI.TextField(rect, todoList.listName, style_textArea);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(target, "List Name Change");
                todoList.listName = listName;
                RecordPrefabInstancePropertyModifications(target);
            }
        }

        private float GetWidth() {
            return Screen.width - 87;
        }

        private void CreateTextStyles() {
            if (style_textArea == null) {
                style_textArea = new GUIStyle(GUI.skin.label);
                style_textArea.alignment = TextAnchor.UpperLeft;
                style_textArea.wordWrap = true;
                style_textArea.richText = true;
                textColor = style_textArea.normal.textColor;
            }

            if (style_label == null) {
                style_label = new GUIStyle(GUI.skin.label);
                style_label.alignment = TextAnchor.UpperLeft;
                style_label.wordWrap = false;
                style_label.richText = false;
            }
        }

        private void ResetTextColor() {
            SetTextColor(textColor);
        }

        private void SetTextColor(Color textColor) {
            style_textArea.normal.textColor = textColor;
            style_textArea.focused.textColor = textColor;
            style_textArea.hover.textColor = textColor;

            style_label.normal.textColor = textColor;
            style_label.focused.textColor = textColor;
            style_label.hover.textColor = textColor;

            GUI.skin.settings.cursorColor = textColor;
        }

        private float ElementHeight(int index) {
            ToDoElement element = todoList.tasks[index];
            
            float width = GetWidth();
            CreateTextStyles();
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
            ToDoElement element = todoList.tasks[index];

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
            ToDoElement element = todoList.tasks[index];

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
                Undo.RecordObject(target, "Toggled Task Completion");
                element.completed = completed;
                RecordPrefabInstancePropertyModifications(target);
            }
            
            // Text Colours
            if (completed) {
                SetTextColor(completedTextColor);
            } else {
                ResetTextColor();
            }

            // This prevents text area from highlighting all text on focus
            bool preventSelection = (UnityEngine.Event.current.type == EventType.MouseDown);
            //Color cursorColor = GUI.skin.settings.cursorColor;
            if (preventSelection) {
                GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);
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
                Undo.RecordObject(target, "Edited Task Text");
                element.text = text;
                RecordPrefabInstancePropertyModifications(target);
            }
            
            // Object Reference
            if (element.objectReference) { // either in-scene reference, or is SceneAsset
                Object objectReference = null;
                bool isNormalReference = false;
                bool valid = true;
                string scenePath = null;
                if (element.objectReferenceID != null && element.objectReference.GetType() == typeof(SceneAsset)) {
                    // Cross-scene object reference (or scene object refence for ScriptableObject)
                    if (element.tempObjectReference != null) {
                        objectReference = element.tempObjectReference;
                    } else {
                        SceneAsset sceneAsset = element.objectReference as SceneAsset;
                        scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                        Scene scene = SceneManager.GetSceneByPath(scenePath);
                        valid = scene.IsValid();
                        if (valid) {
                            SceneReferencesHandler sceneReferencesHandler = SceneReferencesHandler.GetSceneReferencesHandler(scene);
                            if (sceneReferencesHandler != null) {
                                objectReference = sceneReferencesHandler.GetObjectFromID(element.objectReferenceID);
                                element.tempObjectReference = objectReference;
                            }
                        }
                    }
                } else {
                    // Normal in-same-scene object reference
                    objectReference = element.objectReference;
                    isNormalReference = true;
                }
                
                // Object Field
                if (valid) {
                    if (objectReference != null) {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.LabelField(
                            new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - 27, h),
                            "Linked Object : " + (isNormalReference ? "" : "*"),
                            style_label);
                        x += 100;
                        Object obj = EditorGUI.ObjectField(
                            new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - x, h),
                            objectReference,
                            typeof(Object), true);
                        if (EditorGUI.EndChangeCheck()) {
                            SetTaskObject(element, obj);
                        }
                    } else {
                        EditorGUI.LabelField(
                            new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - 27, h),
                            "Linked Object : * (Cross-scene reference lost?)",
                            style_label);
                    }
                } else {
                    EditorGUI.LabelField(
                            new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - 27, h),
                            $"Linked Object : * (Object in scene '{scenePath}')",
                            style_label);
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
                        SetTaskObject(element, obj);
                    }
                    currentEvent.Use();
                }
            }
        }

        private void RecordPrefabInstancePropertyModifications(Object target) {
#if UNITY_2018_3_OR_NEWER
            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(target);
#else
            bool isPrefabInstance = (PrefabUtility.GetPrefabType(target) == PrefabType.PrefabInstance);
#endif
            if (isPrefabInstance) {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        protected void LinkCrossSceneReference(ToDoElement element, Object obj, GameObject gameObject) {
            Scene scene = gameObject.scene;
            SceneAsset sceneAsset = GetSceneAsset(scene);
            SceneReferencesHandler sceneReferencesHandler = SceneReferencesHandler.GetSceneReferencesHandler(scene, true);

            Undo.RecordObjects(new Object[] { target, sceneReferencesHandler }, "Changed Task Object");
            
            element.objectReference = sceneAsset;
            element.objectReferenceID = sceneReferencesHandler.RegisterObject(obj);
            element.tempObjectReference = null;

            //Debug.Log("SceneObjectReference : " + element.objectReferenceID);
            RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        protected void LinkObjectReference(ToDoElement element, Object obj) {
            Undo.RecordObject(target, "Changed Task Object");

            element.objectReference = obj;
            element.objectReferenceID = null;
            element.tempObjectReference = null;

            //Debug.Log("ObjectReference");
            RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        private void SetTaskObject(ToDoElement element, Object obj) {
            if (scenePath == null) {
                // ToDo is Prefab / ScriptableObject Asset
                if (IsReferenceAllowed(obj, null, out GameObject gameObject)) {
                    // Asset
                    LinkObjectReference(element, obj);
                } else {
                    // Scene Reference, Store SceneAsset in objectReference
                    LinkCrossSceneReference(element, obj, gameObject);
                }
            } else if (!IsReferenceAllowed(obj, scenePath, out GameObject gameObject)) {
                // ToDo is MonoBehaviour in Scene, but obj is in different scene. Cross-scene reference
                LinkCrossSceneReference(element, obj, gameObject);
            } else {
                // ToDo is MonoBehaviour, obj is GameObject/Component in same scene or is an Asset
                LinkObjectReference(element, obj);
            }
        }

        private SceneAsset GetSceneAsset(Scene scene) {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
        }

        /* Unused, but might allow for object references that doesn't dirty the scene.
         * Rather than registering the object with SceneReferencesHandler
         * It could just save the object's path in the Hierarchy.
         * However, renames/changes to that path will break the reference.
         * If you want to implement this, you'd likely need to use it in the LinkCrossSceneReference method
        private string GetObjectPath(GameObject gameObject, Object obj) {
            if (gameObject == null) return null;
            List<string> path = new List<string>();

            // If component, append component type to path
            Component component = obj as Component;
            if (component != null) {
                path.Add(":" + component.GetType());
            }

            // Get Hierarchy / Transforms from GameObject to Root
            Transform t = gameObject.transform;
            path.Add(t.name);
            while (t.parent != null) {
                t = t.parent;
                path.Add(t.name + "/");
            }
            
            // Turn into string (e.g. "Root/Child/GameObject:ComponentType")
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string s in Enumerable.Reverse(path)) {
                stringBuilder.Append(s);
            }
            return stringBuilder.ToString();
        }*/

        protected bool IsReferenceAllowed(Object obj, string scenePath, out GameObject gameObject) {
            // Get GameObject
            gameObject = obj as GameObject;
            if (gameObject == null) {
                Component component = obj as Component;
                if (component != null) {
                    gameObject = component.gameObject;
                }
            }
            if (gameObject == null) return true; // Not a GameObject, would be an Asset, so always allowed.
            return (gameObject.scene.path == scenePath); // Same scene = allow, Cross-scene = not allowed
        }

        /// <summary>
        /// Exports the To Do tasks list to a txt file at the specifed path.
        /// Note, doesn't include object references
        /// </summary>
        /// <param name="path"></param>
        private void Export(string path) {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < todoList.tasks.Count; i++) {
                ToDoElement element = todoList.tasks[i];
                string line = "";
                if (element.completed) line += "[Complete]";
                line += element.text.Replace("\n", @"\n");
                stringBuilder.AppendLine(line);
            }

            File.WriteAllText(path, stringBuilder.ToString());
        }

        /// <summary>
        /// Imports tasks from a txt file at the specified path.
        /// </summary>
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
                todoList.tasks.Add(element);
            }
        }
    }
}