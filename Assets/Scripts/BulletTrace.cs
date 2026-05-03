using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTrace : MonoBehaviour
{
    [SerializeField] private float fadeSpeed = 10f; // trace fade speed
    
    private LineRenderer lr;
    private Color startColor;
    private Color endColor;
    private float alpha = 1f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        startColor = lr.startColor;
        endColor = lr.endColor;
    }
    
    public void SetTrace(Vector3 startPoint, Vector3 endPoint)
    {
        lr.SetPosition(0, startPoint);
        lr.SetPosition(1, endPoint);
    }

    // --- [เพิ่มฟังก์ชันนี้เข้ามา เพื่อให้ Shotgun สั่งเปลี่ยนสีได้] ---
    public void SetColor(Color newStartColor, Color newEndColor)
    {
        startColor = newStartColor;
        endColor = newEndColor;
        
        if (lr != null)
        {
            lr.startColor = startColor;
            lr.endColor = endColor;
        }
    }

    void Update()
    {   
        //slowly decrease alpha value
        alpha -= Time.deltaTime * fadeSpeed;
        
        Color currentColorStart = new Color(startColor.r, startColor.g, startColor.b, alpha);
        Color currentColorEnd = new Color(endColor.r, endColor.g, endColor.b, alpha);
        
        lr.startColor = currentColorStart;
        lr.endColor = currentColorEnd;

        // If cant see anything left > destroy
        if (alpha <= 0f)
        {
            Destroy(gameObject);
        }
    }
}