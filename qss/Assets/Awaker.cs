using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Awaker : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("delayedAnimation");
        Application.ExternalEval("console.log('Unity Awake function called');");
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
