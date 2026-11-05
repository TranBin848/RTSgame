using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameFlowMenu : MonoBehaviour
{
    const string VolumeKey = "MasterVolume";

    [SerializeField] GameObject m_MainPanel;
    [SerializeField] GameObject m_PausePanel;
    [SerializeField] GameObject m_OptionsPanel;
    [SerializeField] Slider m_VolumeSlider;

    void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        if (m_VolumeSlider != null) m_VolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        if (m_OptionsPanel != null) m_OptionsPanel.SetActive(false);
        if (m_PausePanel != null) m_PausePanel.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (m_OptionsPanel != null && m_OptionsPanel.activeSelf) Back();
        else if (m_PausePanel != null) { if (m_PausePanel.activeSelf) Resume(); else Pause(); }
    }

    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("ProceduralGame");
    }

    public void Pause()
    {
        if (m_PausePanel == null) return;
        Time.timeScale = 0f;
        m_PausePanel.SetActive(true);
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        if (m_PausePanel != null) m_PausePanel.SetActive(false);
        if (m_OptionsPanel != null) m_OptionsPanel.SetActive(false);
    }

    public void ShowOptions()
    {
        if (m_MainPanel != null) m_MainPanel.SetActive(false);
        if (m_PausePanel != null) m_PausePanel.SetActive(false);
        if (m_OptionsPanel != null) m_OptionsPanel.SetActive(true);
    }

    public void Back()
    {
        if (m_OptionsPanel != null) m_OptionsPanel.SetActive(false);
        if (m_PausePanel != null) m_PausePanel.SetActive(true);
        else if (m_MainPanel != null) m_MainPanel.SetActive(true);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
    }

    public void Exit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
