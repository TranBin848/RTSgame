using UnityEngine;

public class PointToClick : MonoBehaviour
{
    [SerializeField] private float m_Lifetime = 0.5f;
    private float m_Timer;
    void Update()
    {
        m_Timer += Time.deltaTime;
        if (m_Timer >= m_Lifetime)
        {
            Destroy(gameObject);
        }
    }
}
