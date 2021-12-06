using LookingGlass.Menu;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MenuController))]
public class MenuControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get a reference to the controller
        MenuController controller = (MenuController)target;

        // Show default inspector property editor
        if (DrawDefaultInspector())
        {
            // If anything changed, sync child controls
            controller.SyncControls();
        }
    }
}
