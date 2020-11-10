using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor;

public class MenuController : MonoBehaviour
{
    [Header("Menu references")]
    public GameObject mainMenu;
    public GameObject settingsMenu;

    [Header("Main menu references")]
    public TextMeshProUGUI seedInputField;

    [Header("Settings menu references")]
    public Slider viewDistanceSlider;
    public TextMeshProUGUI viewDistanceText;
    [Space]
    public Slider mouseSensitivitySlider;
    public TextMeshProUGUI mouseSensitivityText;
    [Space]
    public Toggle threadingToggle;

    private Settings settings;

    private const string settingsFilePath = "/settings.cfg";

    public void Awake()
    {
        if (!File.Exists(Application.dataPath + settingsFilePath))
        {
            Debug.Log(GetType().Name + ": setting.cfg not found. Creating new file...");

            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + settingsFilePath, jsonExport);
            
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.Log(GetType().Name + ": settings.cfg found!. Loading settings...");

            string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }
    }

    public void StartGame()
    {
        //if (seedInputField.text == "")
        //{
        //    VoxelData.seed = Random.Range(int.MinValue, int.MaxValue).GetHashCode();
        //}

        VoxelData.seed = Mathf.Abs(seedInputField.text.GetHashCode() / 100);

        SceneManager.LoadScene("SampleScene");
    }

    public void EnterSettings()
    {
        viewDistanceSlider.value = settings.viewDistance;
        UpdateViewDistanceSlider();

        mouseSensitivitySlider.value = settings.mouseSensitivity;
        UpdateMouseSensitivitySlider();

        threadingToggle.isOn = settings.enableThreading;

        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void UpdateViewDistanceSlider()
    {
        viewDistanceText.text = "View Distance: " + viewDistanceSlider.value;
    }

    public void UpdateMouseSensitivitySlider()
    {
        mouseSensitivityText.text = "Mouse Sensitivity: " + mouseSensitivitySlider.value.ToString("F1");
    }

    public void LeaveSettings()
    {
        settings.viewDistance = (int)viewDistanceSlider.value;
        settings.mouseSensitivity = mouseSensitivitySlider.value;
        settings.enableThreading = threadingToggle.isOn;

        // write to the settings file
        string jsonExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + settingsFilePath, jsonExport);

        mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
