using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.WebPages;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ScreenshotHelperWindow : EditorWindow
{
    private enum CaptureMode
    {
        IncludeUI,
        ExcludeUI,
    }

    private enum FileFormat
    {
        PNG,
        JPG,
    }

    private enum PrefixSuffixFormat
    {
        None,
        AmericanDate,
        EnglishDate,
        AsianDate,
        Time12H,
        Time24H,
        // Index,
        // ProjectName,
        // SceneName,
    }

    // capture settings
    private CaptureMode m_currentCaptureMode;
    private int m_selectedCaptureMode = 0;
    private FileFormat m_currentFileFormat;
    private int m_selectedPrefixFormat;
    private PrefixSuffixFormat m_currentPrefixFormat;
    private int m_selectedSuffixFormat;
    private PrefixSuffixFormat m_currentSuffixFormat;

    // path
    private string m_path;
    private bool m_createSubfolder;
    private string m_subfolderName;

    // file name
    private string m_fileName;

    private bool m_usePrefix;
    private string m_prefix;

    private bool m_useSuffix;
    private string m_suffix;

    // window settings
    private bool m_isAdvancedMode;
    private bool m_showFormatGuide;
    private Vector2 m_scroll;


    [MenuItem("Window/JB Studio/Screenshot")]
    private static void Init()
    {
        ScreenshotHelperWindow window = (ScreenshotHelperWindow) GetWindow(typeof(ScreenshotHelperWindow));
        window.titleContent.text = "Screenshot Helper";
        window.minSize = new Vector2(340f, 150f);
    }

    private void OnGUI()
    {
        m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
        // Perform Capture
        GUILayout.Space(10f);

        if (GUILayout.Button("Capture", GUILayout.Height(50)))
        {
            Capture();
        }

        // Set Path
        GUILayout.Space(10f);
        GUILayout.Label($"Save Path", EditorStyles.boldLabel);

        EditorStyles.wordWrappedLabel.richText = true;
        EditorStyles.boldLabel.richText = true;
        EditorStyles.label.richText = true;

        GUILayout.Label(m_createSubfolder ? $"Current Path: {m_path}<color=yellow>{Path.DirectorySeparatorChar}{m_subfolderName}</color>" : $"Current Path: {m_path}",
            EditorStyles.wordWrappedLabel);

        if (!IsPathValid())
        {
            GUILayout.Label($"<color=red><b>Set valid path to save screenshot!</b></color>", EditorStyles.wordWrappedLabel);
        }

        GUILayout.Label($"Save screenshots to ...", EditorStyles.wordWrappedLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Project Directory (Default)"))
        {
            m_path = Path.GetDirectoryName(Application.dataPath);
        }

        if (GUILayout.Button("Desktop"))
        {
            m_path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        if (GUILayout.Button("My Pictures"))
        {
            m_path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        if (GUILayout.Button("Custom Path"))
        {
            SetCustomPath();
        }

        GUILayout.EndHorizontal();
        if (GUILayout.Button("Open Current Path in Explorer"))
        {
            if (!Directory.Exists($"{m_path}\\{m_subfolderName}"))
            {
                Directory.CreateDirectory($"{m_path}\\{m_subfolderName}");
            }

            System.Diagnostics.Process.Start($"{m_path}\\{m_subfolderName}");
        }

        m_createSubfolder = EditorGUILayout.Toggle($"Create Subfolder", m_createSubfolder);
        m_subfolderName = m_createSubfolder ? EditorGUILayout.TextField("Subfolder Name", m_subfolderName) : "";

        // Set file name with prefix & suffix
        GUILayout.Space(10f);
        GUILayout.Label($"File Name", EditorStyles.boldLabel);
        GUILayout.Label($"Final file name: <color=yellow>{m_prefix}</color>{m_fileName}<color=yellow>{m_suffix}</color>.{m_currentFileFormat.ToString().ToLower()}",
            EditorStyles.wordWrappedLabel);
        m_isAdvancedMode = EditorGUILayout.Toggle($"Advanced Mode", m_isAdvancedMode);
        if (m_isAdvancedMode)
        {
            m_showFormatGuide = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showFormatGuide, "Advanced Format Guide", true);
            if (m_showFormatGuide)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                GUILayout.Label(
                    "<b>{Date}:</b> Date\n" +
                    "<b>{Time}:</b> Current Time\n" +
                    "<b>{Idx}:</b> Index Number(Starts with 0, editable)\n" +
                    "For example, <b>\"{Date}_{Time}_Screenshot\"</b> will replace with " + $"<b>\"{DateTime.Now:yyyy.MM.dd}_{DateTime.Now:hh_mm_ss}_Screenshot\"</b>"
                    , EditorStyles.wordWrappedLabel);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            m_usePrefix = EditorGUILayout.Toggle($"Use Prefix", m_usePrefix);
            GUILayout.Space(20f);
            m_useSuffix = EditorGUILayout.Toggle($"Use Suffix", m_useSuffix);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            m_prefix = m_isAdvancedMode ? EditorGUILayout.TextField("Prefix", m_prefix) : "";
            if (m_usePrefix && m_useSuffix)
            {
                GUILayout.Space(20f);
            }

            m_suffix = m_isAdvancedMode ? EditorGUILayout.TextField("Suffix", m_suffix) : "";
            GUILayout.EndHorizontal();
        }
        else
        {
            string[] displayedPrefixSuffixFormat =
            {
                "None",
                "Date (MM.dd.yyyy)",
                "Date (dd.MM.yyyy)",
                "Date (yyyy.MM.dd)",
                "Time (hh_mm_ss, 12H)",
                "Time (HH_mm_ss, 24H)",
            };
            m_selectedPrefixFormat = EditorGUILayout.Popup("Prefix", m_selectedPrefixFormat, displayedPrefixSuffixFormat);
            m_currentPrefixFormat = (PrefixSuffixFormat) m_selectedPrefixFormat;
            m_selectedSuffixFormat = EditorGUILayout.Popup("Suffix", m_selectedSuffixFormat, displayedPrefixSuffixFormat);
            m_currentSuffixFormat = (PrefixSuffixFormat) m_selectedSuffixFormat;
        }

        m_fileName = EditorGUILayout.TextField("FileName", m_fileName);

        // Set capture type
        GUILayout.Space(10f);
        GUILayout.Label("Miscellaneous", EditorStyles.boldLabel);
        m_currentFileFormat = (FileFormat) EditorGUILayout.EnumPopup("Format", m_currentFileFormat);
        string[] displayedCaptureOptions =
        {
            "Include UI",
            "Don't Include UI",
        };
        m_selectedCaptureMode = EditorGUILayout.Popup("Capture Mode", m_selectedCaptureMode, displayedCaptureOptions);
        m_currentCaptureMode = (CaptureMode) m_selectedCaptureMode;

        switch (m_currentCaptureMode)
        {
            case CaptureMode.IncludeUI:
                break;

            case CaptureMode.ExcludeUI:
                break;

            default:
                Debug.Assert(false, m_currentCaptureMode);
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private string ConvertPrefixSuffixFormat(string src)
    {
        StringBuilder stringBuilder = new StringBuilder(src);
        stringBuilder
            .Replace("{Date}", $"")
            .Replace("{Time}",$"")
            .Replace("{Idx}", $"")
            .Replace("{Project}", $"")
            .Replace("{Scene}", $"");
        
        return stringBuilder.ToString();
    }

    private void SetCustomPath()
    {
        string temp = EditorUtility.OpenFolderPanel("Set path for screenshots", Path.GetDirectoryName(Application.dataPath), "");
        if (!String.IsNullOrEmpty(temp))
        {
            m_path = temp.Replace('/', '\\');
            Debug.Log($"New path assigned: {m_path}");
        }
    }

    private bool IsPathValid()
    {
        if (String.IsNullOrEmpty(m_path) || !Directory.Exists(m_path)) return false;
        return true;
    }

    private void Capture()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("The Unity Editor must be in Play Mode.");
            return;
        }

        if (!IsPathValid())
        {
            Debug.LogError("The path is invalid.");
            return;
        }

        ScreenshotHelperObject helper = Camera.main.gameObject.GetOrAddComponent<ScreenshotHelperObject>();

        helper.path = $"{(m_createSubfolder ? m_path + "\\" + m_subfolderName : m_path)}";
        helper.fileName = $"{m_prefix}{m_fileName}{m_suffix}";
        helper.format = m_currentFileFormat.ToString();

        switch (m_currentCaptureMode)
        {
            case CaptureMode.IncludeUI:
                helper.CaptureWithUI();
                break;
            case CaptureMode.ExcludeUI:
                helper.CaptureWithoutUI();
                break;
        }
    }
}