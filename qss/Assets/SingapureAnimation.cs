using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingapureAnimation : MonoBehaviour
{
    [SerializeField] Vector3 StartPosition = new Vector3(1.785f, 165.388f, 0);
    [SerializeField] AnimationCurve anim;
    CameraControllerInSpace cam;
      void Start()
    {
        cam = FindAnyObjectByType<CameraControllerInSpace>();
        Time.timeScale = 0;
        transform.SetParent(FindObjectOfType<UnitEarth>().transform);
    }

    // Update is called once per frame
    void Update()
    {
        cam.Pivot.rotation = Quaternion.Euler(StartPosition);
        cam.Pivot.localScale = Vector3.one * (0.4f+anim.Evaluate(Time.realtimeSinceStartup));
        if (cam.Pivot.localScale.x > 0.4f) Time.timeScale = 1;
        if (cam.Pivot.localScale.x > 1.04f) 
        {
            cam.TargetObjectRotation = cam.Pivot.rotation.eulerAngles;
            cam.animation = 1;
            DestroyImmediate(this);
            
        }

    }
}
