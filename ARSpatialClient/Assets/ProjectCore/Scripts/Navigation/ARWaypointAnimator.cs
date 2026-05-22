using UnityEngine;

/// <summary>
/// Adds floating, spinning, and pulsing animations to AR path waypoints and destination markers.
/// </summary>
public class ARWaypointAnimator : MonoBehaviour
{
    public bool animatePosition = true;
    public float bobSpeed = 2f;
    public float bobHeight = 0.05f;

    public bool animateRotation = false;
    public float spinSpeed = 90f; // degrees per second

    public bool animateScale = false;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.2f;

    private Vector3 m_StartPosition;
    private Vector3 m_StartScale;
    private float m_TimeOffset;

    void Start()
    {
        m_StartPosition = transform.localPosition;
        m_StartScale = transform.localScale;
        
        // Random offset so all arrows don't bob exactly in sync
        m_TimeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        float t = Time.time + m_TimeOffset;

        if (animatePosition)
        {
            float yOffset = Mathf.Sin(t * bobSpeed) * bobHeight;
            transform.localPosition = m_StartPosition + new Vector3(0, yOffset, 0);
        }

        if (animateRotation)
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        if (animateScale)
        {
            float scaleMultiplier = 1.0f + (Mathf.Sin(t * pulseSpeed) * pulseAmount);
            transform.localScale = m_StartScale * scaleMultiplier;
        }
    }
}
