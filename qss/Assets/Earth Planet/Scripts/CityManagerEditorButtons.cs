using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
[CustomEditor(typeof(CityManager))]
public class CityManagerInspectorButton : Editor
{ 

    public override void OnInspectorGUI()
    {
      

        CityManager myScript = (CityManager)target;
        if (GUILayout.Button("Rebuild Cityes from JSON"))
        {
            myScript.SetCityes();
        }


        DrawDefaultInspector();
    }

}
#endif