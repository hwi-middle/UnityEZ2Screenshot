using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingCubeDemo : MonoBehaviour
{
    public float speed = 50f;

    void Start()
    {
        StartCoroutine(Scale());
    }

    void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * speed, Space.World);
    }

    IEnumerator Scale()
    {
        var original = transform.localScale;
        var scaled = transform.localScale * 3;

        while (true)
        {
            float time = 0f;
            float duration = 2f;
            
            while (time < duration)
            {
                transform.localScale = Vector3.Lerp(original, scaled, time / duration);
                yield return null;
                time += Time.deltaTime;
            }

            time = 0f;
            while (time < duration)
            {
                transform.localScale = Vector3.Lerp(scaled, original, time / duration);
                yield return null;
                time += Time.deltaTime;
            }
        }
    }
}