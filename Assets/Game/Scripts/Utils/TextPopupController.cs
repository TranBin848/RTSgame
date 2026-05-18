using UnityEngine;

public class TextPopupController : MonoBehaviour
{
    [SerializeField] private TextPopup m_TextPopupPrefab;
    public void Spam(string text, Color color, Vector3 position)
    {
        var textPopup = Instantiate(m_TextPopupPrefab);
        textPopup.transform.position = position;
        textPopup.SetText(text, color);
    }
}