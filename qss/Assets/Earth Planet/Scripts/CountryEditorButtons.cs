using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
[CustomEditor(typeof(Country))]
public class CountryEditorButtons : Editor
{

    public override void OnInspectorGUI()
    {


        Country myScript = (Country)target;
        if (GUILayout.Button("Bake Mesh Colors In Editor"))
        {
            myScript.BakeColorWealthToMesh();
        }


        DrawDefaultInspector();
    }
}
#endif