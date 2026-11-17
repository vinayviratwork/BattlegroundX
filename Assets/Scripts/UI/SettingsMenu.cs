using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button     closeButton;
    [SerializeField] private Button     applyButton;

    
    [Header("Sensitivity")]
    [SerializeField] private Slider   mouseSensSlider;
    [SerializeField] private TMP_Text mouseSensLabel;
    [SerializeField] private Slider   mobileSensSlider;
    [SerializeField] private TMP_Text mobileSensLabel;

    
    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle       vsyncToggle;
    [SerializeField] private TMP_Dropdown fpsCapDropdown;

    
    [Header("Audio")]
    [SerializeField] private Slider   masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeLabel;
    [SerializeField] private Slider   sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeLabel;

    
    private const float DefaultMouseSens  = 2f;
    private const float DefaultMobileSens = 1.5f;
    private const int   DefaultQuality    = 2;
    private const float DefaultMasterVol  = 1f;
    private const float DefaultSFXVol     = 0.8f;

    
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        
        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);
        if (applyButton != null) applyButton.onClick.AddListener(ApplyAndClose);

        
        if (mouseSensSlider  != null) mouseSensSlider.onValueChanged.AddListener(v => UpdateLabel(mouseSensLabel,  v, "Mouse Sens"));
        if (mobileSensSlider != null) mobileSensSlider.onValueChanged.AddListener(v => UpdateLabel(mobileSensLabel, v, "Touch Sens"));
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(v => UpdateLabel(masterVolumeLabel, v, "Master Vol"));
        if (sfxVolumeSlider  != null) sfxVolumeSlider.onValueChanged.AddListener(v => UpdateLabel(sfxVolumeLabel, v, "SFX Vol"));

        
        BuildQualityDropdown();
        BuildFPSDropdown();

        LoadSettings();
        ApplySettings();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleSettings();
    }

    
    public void ToggleSettings()
    {
        if (settingsPanel == null) return;
        bool nowOpen = !settingsPanel.activeSelf;
        settingsPanel.SetActive(nowOpen);

        if (nowOpen)
        {
            LoadSettings();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null && !settingsPanel.activeSelf)
            ToggleSettings();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
            ToggleSettings();
    }

    public void ApplyAndClose()
    {
        SaveSettings();
        ApplySettings();
        CloseSettings();
    }

    
    private void LoadSettings()
    {
        float mouseSens  = PlayerPrefs.GetFloat("BX_MouseSens",  DefaultMouseSens);
        float mobileSens = PlayerPrefs.GetFloat("BX_MobileSens", DefaultMobileSens);
        int   quality    = PlayerPrefs.GetInt("BX_Quality",      DefaultQuality);
        float masterVol  = PlayerPrefs.GetFloat("BX_MasterVol",  DefaultMasterVol);
        float sfxVol     = PlayerPrefs.GetFloat("BX_SFXVolume",  DefaultSFXVol);
        int   vsync      = PlayerPrefs.GetInt("BX_VSync",        1);
        int   fpsCap     = PlayerPrefs.GetInt("BX_FPSCap",       0);

        if (mouseSensSlider    != null) mouseSensSlider.value    = mouseSens;
        if (mobileSensSlider   != null) mobileSensSlider.value   = mobileSens;
        if (qualityDropdown    != null) qualityDropdown.value    = quality;
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
        if (sfxVolumeSlider    != null) sfxVolumeSlider.value    = sfxVol;
        if (vsyncToggle        != null) vsyncToggle.isOn         = vsync == 1;
        if (fpsCapDropdown     != null) fpsCapDropdown.value     = fpsCap;

        UpdateLabel(mouseSensLabel,     mouseSens,  "Mouse Sens");
        UpdateLabel(mobileSensLabel,    mobileSens, "Touch Sens");
        UpdateLabel(masterVolumeLabel,  masterVol,  "Master Vol");
        UpdateLabel(sfxVolumeLabel,     sfxVol,     "SFX Vol");
    }

    private void SaveSettings()
    {
        if (mouseSensSlider    != null) PlayerPrefs.SetFloat("BX_MouseSens",  mouseSensSlider.value);
        if (mobileSensSlider   != null) PlayerPrefs.SetFloat("BX_MobileSens", mobileSensSlider.value);
        if (qualityDropdown    != null) PlayerPrefs.SetInt("BX_Quality",      qualityDropdown.value);
        if (masterVolumeSlider != null) PlayerPrefs.SetFloat("BX_MasterVol",  masterVolumeSlider.value);
        if (sfxVolumeSlider    != null) PlayerPrefs.SetFloat("BX_SFXVolume",  sfxVolumeSlider.value);
        if (vsyncToggle        != null) PlayerPrefs.SetInt("BX_VSync",        vsyncToggle.isOn ? 1 : 0);
        if (fpsCapDropdown     != null) PlayerPrefs.SetInt("BX_FPSCap",       fpsCapDropdown.value);
        PlayerPrefs.Save();
    }

    
    private void ApplySettings()
    {
        
        int q = qualityDropdown != null ? qualityDropdown.value : DefaultQuality;
        QualitySettings.SetQualityLevel(q, true);

        
        bool vsync = vsyncToggle == null || vsyncToggle.isOn;
        QualitySettings.vSyncCount = vsync ? 1 : 0;

        
        int[] fpsCaps = { -1, 30, 60, 90, 120 };
        int fpsCap = fpsCapDropdown != null ? fpsCapDropdown.value : 0;
        Application.targetFrameRate = fpsCaps[Mathf.Clamp(fpsCap, 0, fpsCaps.Length - 1)];

        
        float mouseSens  = mouseSensSlider  != null ? mouseSensSlider.value  : DefaultMouseSens;
        float mobileSens = mobileSensSlider != null ? mobileSensSlider.value : DefaultMobileSens;
        ApplySensitivityToPlayers(mouseSens, mobileSens);

        
        float masterVol = masterVolumeSlider != null ? masterVolumeSlider.value : DefaultMasterVol;
        AudioListener.volume = masterVol;
    }

    private static void ApplySensitivityToPlayers(float mouseSens, float mobileSens)
    {
        foreach (var ctrl in FindObjectsOfType<NetworkPlayerController>())
        {
            if (!ctrl.IsOwner) continue;
            
            
            var field = typeof(NetworkPlayerController)
                .GetField("mouseSensitivity",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(ctrl, mouseSens);
        }
    }

    
    private void BuildQualityDropdown()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        var opts = new System.Collections.Generic.List<string>();
        foreach (string level in QualitySettings.names) opts.Add(level);
        if (opts.Count == 0) opts.AddRange(new[]{"Very Low","Low","Medium","High","Very High","Ultra"});
        qualityDropdown.AddOptions(opts);
    }

    private void BuildFPSDropdown()
    {
        if (fpsCapDropdown == null) return;
        fpsCapDropdown.ClearOptions();
        fpsCapDropdown.AddOptions(new System.Collections.Generic.List<string>
            { "Unlimited", "30", "60", "90", "120" });
    }

    private static void UpdateLabel(TMP_Text label, float value, string prefix)
    {
        if (label != null)
            label.text = $"{prefix}: {value:F1}";
    }

    
    public static float GetMouseSensitivity()  =>
        PlayerPrefs.GetFloat("BX_MouseSens",  DefaultMouseSens);
    public static float GetMobileSensitivity() =>
        PlayerPrefs.GetFloat("BX_MobileSens", DefaultMobileSens);
}