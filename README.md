# ToDo
For Unity, simple in-editor To-Do list.
• Attached as a Component on a GameObject. Can have as many as you want.
• Uses a Custom Inspector based on UnityEditorInternal.ReorderableList.
• Title of ReorderableList can be edited.
• Tasks can be reorganised by dragging the left side.
• Each task includes : Completion tickbox, text area, and the option to link a GameObject, Component or Asset by dragging it onto the task. Cross-scene references are not supported and will be cleared when reloading the scene.
• Completed tasks turn green. Use the "Remove Completed Tasks" button to remove all completed tasks.
• Supports Rich Text, (but disabled while editing that task, so you can see what you are writing)
• Has Undo / Redo Support.
