# ToDo
A simple in-editor To-Do list for Unity.<br />
Tested in Unity 2020.1, 2019.3 and 2018.3. Unsure if it'll work in versions prior to that.<br />

![To Do](gif.gif)

## Features :
• Attached as a **Component** (on a GameObject in Scene or Prefab), or as a **ScriptableObject** Asset. Can have as many as you want.<br />
• Uses a **Custom Inspector** based on **UnityEditorInternal.ReorderableList**. Easily reorganise tasks by dragging the left side of each element.<br />
• Title of ReorderableList can be edited.<br />
• Each task includes : **Completion Tickbox**, **Text Area**, and the option to link a **GameObject**, **Component** or **Asset** by dragging it onto the task. Once an object is linked, it can be removed by using the **Object Field** that appears and selecting *None* at the top.<br />
• Completed tasks turn green. Use the *"Remove Completed Tasks"* button to remove all completed tasks.<br />
• Supports **Rich Text**, (but is disabled while editing the task, so you can see what you are writing)<br />
• Has **Undo / Redo** Support.<br />
• **Import** Tasks from a txt file. Each line in the file will be added as a task. Lines starting with *"[Complete]"* are marked as completed.<br />
• **Export** Tasks to a txt file (object references will be lost though).<br />
• **Cross-Scene Object References** (and scene object references for ScriptableObject) is supported. Linking an object in the same scene will still use the direct object reference, but linking an object from a *different* scene will automatically create and use a hidden GameObject & SceneReferenceHandler script, which assigns a unique ID for the object. This handler is saved in the scene to keep references to objects in that scene, while the SceneAsset and ID is saved in the ToDo list. Cross-scene linked objects will show an asterisk on the task, and the object can only be obtained if the scene is loaded.<br />
<br />
## Known Bugs :
• Regular object references may be lost if a To Do list component is moved between scenes. (Cross-scene object references are unaffected)<br />
• Creating a Prefab from a GameObject containing the To Do list will update regular object references to the cross-scene method so they are not lost. However this can only occur if the To Do list is seleted / visible in the inspector currently.<br />
<br />
@Cyanilux<br />
:)
