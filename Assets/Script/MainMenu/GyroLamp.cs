using UnityEngine;

public class GyroLampUI : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] float maxAngle = 20f;
    [SerializeField] float smooth = 6f;
    [SerializeField] float gyroPower = 25f;

    [Header("Idle Swing")]
    [SerializeField] float idleAngle = 4f;
    [SerializeField] float idleSpeed = 1.2f;

    RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        float gyroX = Input.acceleration.x;

        // ===== idle ayunan =====
        float idle = Mathf.Sin(Time.time * idleSpeed) * idleAngle;

        // ===== gyro ayunan =====
        float gyroTarget = gyroX * gyroPower;

        // kalau HP digerakkan, idle dikurangi
        float gyroStrength = Mathf.Clamp01(Mathf.Abs(gyroX) * 3f);
        float targetZ = Mathf.Lerp(idle, gyroTarget, gyroStrength);

        targetZ = Mathf.Clamp(targetZ, -maxAngle, maxAngle);

        // smooth rotasi
        float currentZ = rt.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360;

        float z = Mathf.Lerp(currentZ, targetZ, Time.deltaTime * smooth);
        rt.localRotation = Quaternion.Euler(0, 0, z);
    }
}
