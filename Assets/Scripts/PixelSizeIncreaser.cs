using UnityEngine;
using System.Collections;

public class PixelSizeIncreaser : MonoBehaviour
{
    public PixelationPost pixelCam;
    public float startValue;
    public float EndValue;
    public float speed;
    private float currentSize;

    public void Start()
    {
        currentSize = startValue;
        pixelCam._cellSize = startValue;
    }

    public void SetPixelSize(float percentage)
    {

        float range = startValue - EndValue;
        float subtractAmount = (range * percentage);
        float size = startValue - subtractAmount;

        pixelCam._cellSize = Mathf.Lerp(currentSize, size, (Mathf.Sin(Time.fixedTime * speed) + 1) / 2);
        currentSize = size;
    }
}
