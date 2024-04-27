using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
[CustomEditor(typeof(BSPTree))]
public class BSPTreeInspectorButton : Editor
{ 
 
    public override void OnInspectorGUI()
    {
            DrawDefaultInspector();

            BSPTree myScript = (BSPTree)target;
            if (GUILayout.Button("Calculate BSP TREE"))
            {
                myScript.Generate();
            }
        if (GUILayout.Button("Clear BSP TREE"))
        {
            myScript.Clear();
            }
        }

}
#endif