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

    RTS.DOTS.GameplaySimulationSystem m_Match;
    ResearchTreePanel m_ResearchTree;
    CameraController m_LoadedCamera;
    public void PauseLoaded(CameraController camera)
    { m_LoadedCamera = camera; camera.LockCamera = true; Pause(); }
    public void InitializeResearchTree(ResearchTreePanel tree) => m_ResearchTree = tree;
    public void InitializeMatch(RTS.DOTS.GameplaySimulationSystem match)
    {
        if (match == null) throw new System.ArgumentNullException(nameof(match));
        if (m_Match != null && m_Match != match) throw new System.InvalidOperationException("Menu already belongs to another match.");
        m_Match = match;
    }

    void Awake()
    {
        Time.timeScale = MatchLoadBootstrap.IsLoading || ProceduralMapRuntime.HasFailed ? 0f : 1f;
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        if (m_VolumeSlider != null) m_VolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        if (m_OptionsPanel != null) m_OptionsPanel.SetActive(false);
        if (m_PausePanel != null) m_PausePanel.SetActive(false);
    }

    ProceduralMapAuthoring m_MapStatus;
    public void InitializeMapStatus(ProceduralMapAuthoring map) => m_MapStatus = map;
    bool m_FallbackNoticeClosed;

    void Update()
    {
        if (m_Match?.IsMatchOver == true) return;
        if (m_ResearchTree != null && m_ResearchTree.BlocksGameplayInput) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (m_OptionsPanel != null && m_OptionsPanel.activeSelf) Back();
        else if (m_PausePanel != null) { if (m_PausePanel.activeSelf) Resume(); else Pause(); }
    }

    void OnGUI()
    {
        if (ProceduralMapRuntime.HasFailed)
        {
            GUILayout.BeginArea(new Rect((Screen.width-460)/2f,(Screen.height-230)/2f,460,230), GUI.skin.box);
            GUILayout.Label("Không thể chuẩn bị bản đồ");
            GUILayout.Label("Trận đấu chưa bắt đầu. Bạn có thể thử lại hoặc trở về menu.");
            if(GUILayout.Button("Thử lại"))Play();
            if(GUILayout.Button("Về menu"))ReturnToMenu();
            if(GUILayout.Button("Thoát"))Exit();
            GUILayout.EndArea();
        }
        else if(!m_FallbackNoticeClosed && SceneManager.GetActiveScene().name=="ProceduralGame")
        {
            var generator=m_MapStatus;
            if(generator!=null && generator.UsedFallback)
            {
                GUILayout.BeginArea(new Rect((Screen.width-460)/2f,Screen.height-125,460,74),GUI.skin.box);
                GUILayout.Label("Đã dùng bản đồ dự phòng cùng bố cục và tài nguyên.");
                if(GUILayout.Button("Đã hiểu"))m_FallbackNoticeClosed=true;
                GUILayout.EndArea();
            }
        }
    }

    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("ProceduralGame");
    }

    public void ClosePausePanels()
    {
        if (m_PausePanel != null) m_PausePanel.SetActive(false);
        if (m_OptionsPanel != null) m_OptionsPanel.SetActive(false);
    }

    public void Pause()
    {
        if (m_Match?.IsMatchOver == true) return;
        if (m_PausePanel == null) return;
        Time.timeScale = 0f;
        m_PausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (m_Match?.IsMatchOver == true) return;
        if (ProceduralMapRuntime.IsCompositionBlocked || MatchLoadBootstrap.IsLoading) return;
        if (m_Match != null) m_Match.World.GetExistingSystemManaged<RTS.DOTS.SwarmSimulationSystem>().Paused = false;
        if (m_LoadedCamera != null) { m_LoadedCamera.LockCamera = false; m_LoadedCamera = null; }
        Time.timeScale = 1f;
        if (m_PausePanel != null) m_PausePanel.SetActive(false);
        if (m_OptionsPanel != null) m_OptionsPanel.SetActive(false);
    }

    public void ShowOptions()
    {
        if (m_Match?.IsMatchOver == true) return;
        if (m_MainPanel != null) m_MainPanel.SetActive(false);
        if (m_PausePanel != null) m_PausePanel.SetActive(false);
        if (m_OptionsPanel != null) m_OptionsPanel.SetActive(true);
    }

    public void Back()
    {
        if (m_Match?.IsMatchOver == true) return;
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
