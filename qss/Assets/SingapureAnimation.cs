using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SingapureAnimation : MonoBehaviour
{
    
    [SerializeField] Image QSrect;
    [SerializeField] Image Black;
    [SerializeField] CanvasGroup CG;
    [SerializeField] Vector2 QSEndPos;
    [SerializeField] float typeSpeed = .10f;
    [SerializeField] TextMeshProUGUI t;
    [SerializeField] Transform cityPoint;
    [SerializeField] Vector3 StartPosition = new Vector3(1.785f, 165.388f, 0);
    [SerializeField] AnimationCurve anim;
    float timer;
    public float tTimer = 4;
    public float scaleMult;
    public float fadeMult;
    string fullText;
    CameraControllerInSpace cam;
    [SerializeField] GlobalEffectsController GEC;
    private float gekTimer = 8;
      void Start()
    { 
        cam = FindAnyObjectByType<CameraControllerInSpace>();
        Time.timeScale = 0;
        transform.SetParent(FindObjectOfType<UnitEarth>().transform);
        fullText = t.text; // Сохранение полного текста
        t.text = fullText.Substring(0,32);
        fullText=fullText.Substring( 32); // Очистка текста перед началом анимации
        CG.alpha = 0;
        
    }
    void Update()
    {
        Black.color = Color.Lerp(Color.black, new Color(0,0,0,0), (timer - scaleMult)/4);
        CG.alpha = timer / 2;
        timer += Time.unscaledDeltaTime; 
        tTimer -= Time.unscaledDeltaTime; gekTimer -= Time.unscaledDeltaTime;

        if(gekTimer < 0) {GEC.enabled = true; gekTimer = 1000;}
        if (fullText.Length>0 && tTimer<0)
        {
            tTimer = typeSpeed;
            t.text += fullText[0]; fullText= fullText.Substring(1);
        }
        

      //  QSrect.color = Color.Lerp(Color.black,Color.white,(timer-fadeMult)/2);
        QSrect.rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.35f, Vector3.one, (timer - scaleMult)/2);
        if (Input.GetKeyDown(KeyCode.R)) { SceneManager.LoadScene(SceneManager.GetActiveScene().name); timer = 0; }
        cam.Pivot.rotation = Quaternion.Euler(StartPosition);
        cam.Pivot.localScale = Vector3.one * (0.4f+anim.Evaluate(timer));
        cityPoint.transform.Rotate(cityPoint.transform.forward* timer);
        if (cam.Pivot.localScale.x > 0.4f) Time.timeScale = 1;
        if (cam.Pivot.localScale.x > 1.04f) 
        {
            cam.TargetObjectRotation = cam.Pivot.rotation.eulerAngles;
            cam.animation = 1;
            DestroyImmediate(this);
            
        }

    }
}
