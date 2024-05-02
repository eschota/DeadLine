using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityPos : MonoBehaviour
{
    [SerializeField] public City city;
    RectTransform rect;
    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(city.transform.position);
        rect.position = screenPoint;

    }
}
