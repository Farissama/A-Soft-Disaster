using UnityEngine;

public class RotateRays : MonoBehaviour
{
    public float speed = 20f;

    void Update()
    {
        transform.Rotate(0, 0, speed * Time.deltaTime);
    }
}