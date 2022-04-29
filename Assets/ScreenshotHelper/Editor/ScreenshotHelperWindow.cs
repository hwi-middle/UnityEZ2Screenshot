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
using UnityEngine.SceneManagement;

public class ScreenshotHelperWindow : EditorWindow
{
    // capture settings
    private enum ECaptureMode
    {
        IncludeUI,
        ExcludeUI,
    }

    private enum EFileFormat
    {
        PNG,
        JPG,
    }

    private enum EPrefixSuffixFormat
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

    private ECaptureMode m_currentECaptureMode;
    private EFileFormat m_currentEFileFormat;
    private EPrefixSuffixFormat m_currentPrefixFormat;
    private EPrefixSuffixFormat m_currentSuffixFormat;
    private int m_selectedSeparator;
    private string m_currentSeparator;
    private ScreenshotHelperObject m_helper;

    // path
    private string m_path;
    private bool m_createSubfolder;
    private string m_subfolderName;

    // file name
    private bool m_isAdvancedMode;
    private string m_fileName = "screenshot";
    private int m_screenshotIdx;
    private string m_prefix;
    private string m_suffix;
    private bool m_showPrefixSuffix;

    // window settings
    private Vector2 m_scroll;

    // advanced mode
    private enum ETimeType
    {
        Time12H,
        Time24H,
    }

    private enum EDateType
    {
        MonthDayYear,
        DayMonthYear,
        YearMonthDay
    }

    // time
    private ETimeType m_timeType;
    private string m_timeFormat;
    private string m_timeSeparator = "-";
    private bool m_useZerofillTime = true;
    private bool m_useSeconds = true;

    // date
    private EDateType m_dateType;
    private string m_dateFormat;
    private string m_dateSeparator = "-";
    private bool m_useZerofillDate = true;
    private bool m_useTwoDigitYear = true;

    // index
    private int m_incrementalValue = 1;
    private int m_minDigits = 4;

    // foldout
    private bool m_showFormatGuide;
    private bool m_showDateSettings;
    private bool m_showTimeSettings;
    private bool m_showIndexSettings;


    [MenuItem("Window/JB Studio/Screenshot Helper")]
    private static void Init()
    {
        ScreenshotHelperWindow window = (ScreenshotHelperWindow) GetWindow(typeof(ScreenshotHelperWindow));
        window.titleContent.text = "Screenshot Helper";
        // window.minSize = new Vector2(340f, 150f);
    }

    private void OnGUI()
    {
        // Set helper object
        if (Camera.main == null)
        {
            Debug.LogError("There is no main camera.");
            return;
        }

        // helper = Camera.main.gameObject.GetOrAddComponent<ScreenshotHelperObject>();

        // Use rich text
        EditorStyles.wordWrappedLabel.richText = true;
        EditorStyles.boldLabel.richText = true;
        EditorStyles.label.richText = true;
        EditorStyles.helpBox.richText = true;
        EditorStyles.helpBox.fontSize = 12;

        // Initialize scroll view
        m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

        // Error handling
        if (m_helper != null && m_helper.isFileConflictOccured)
        {
            HandleErrorOnGUI();
            return;
        }

        // Perform Capture
        GUILayout.Space(10f);

        if (GUILayout.Button("Take a Screenshot", GUILayout.Height(80)))
        {
            Capture();
        }

        // Set Path
        SetPathOnGUI();

        // Set file name
        SetFileNameOnGUI();

        // Miscellaneous settings
        SetMiscellaneousOnGUI();

        EditorGUILayout.EndScrollView();
    }

    private void HandleErrorOnGUI()
    {
        GUILayout.Space(10f);

        GUILayout.Label("Error", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox($"File already Exists. Do you want to replace it?", MessageType.Error);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel", GUILayout.Height(60)))
        {
            m_helper.RemoveComponentFromCamera();
        }

        if (GUILayout.Button("Retry", GUILayout.Height(60)))
        {
            // Error resolved
            if (!File.Exists($"{m_helper.conflictedFileFullPath}"))
            {
                m_helper.Save();
            }
        }

        if (GUILayout.Button("Replace", GUILayout.Height(60)))
        {
            m_helper.Save();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Existing File", GUILayout.Height(60)))
        {
            if (!Directory.Exists($"{m_path}\\{m_subfolderName}"))
            {
                Debug.Log("This file is not exist now.");
            }
            else
            {
                System.Diagnostics.Process.Start($"{m_path}\\{m_subfolderName}\\{m_fileName}.{m_currentEFileFormat}");
            }
        }

        if (GUILayout.Button("Open Directory", GUILayout.Height(60)))
        {
            if (!Directory.Exists($"{m_path}\\{m_subfolderName}"))
            {
                Debug.Log("This directory is not exist now.");
            }
            else
            {
                System.Diagnostics.Process.Start($"{m_path}\\{m_subfolderName}");
            }
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void SetPathOnGUI()
    {
        GUILayout.Space(10f);
        GUILayout.Label($"Save Path", EditorStyles.boldLabel);
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
            Debug.Log($"New path assigned: {m_path}");
        }

        if (GUILayout.Button("Desktop"))
        {
            m_path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Debug.Log($"New path assigned: {m_path}");
        }

        if (GUILayout.Button("My Pictures"))
        {
            m_path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            Debug.Log($"New path assigned: {m_path}");
        }

        if (GUILayout.Button("Custom Path"))
        {
            SetCustomPath();
            Debug.Log($"New path assigned: {m_path}");
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

        m_createSubfolder = EditorGUILayout.Toggle("Create Subfolder", m_createSubfolder);
        m_subfolderName = m_createSubfolder ? EditorGUILayout.TextField("Subfolder Name", m_subfolderName) : "";
    }

    private void SetFileNameOnGUI()
    {
        GUILayout.Space(10f);
        GUILayout.Label($"File Name", EditorStyles.boldLabel);

        m_isAdvancedMode = EditorGUILayout.Toggle($"Advanced Mode", m_isAdvancedMode);
        if (m_isAdvancedMode)
        {
            EditorGUILayout.HelpBox($"Final file name: <b>{ConvertPrefixSuffixFormat(m_fileName)}.{m_currentEFileFormat.ToString().ToLower()}</b>", MessageType.Info);
            m_fileName = EditorGUILayout.TextField("FileName", m_fileName);
            m_showFormatGuide = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showFormatGuide, "Advanced Format Guide", true);
            if (m_showFormatGuide)
            {
                GUILayout.Label(
                    "<b>{Date}:</b> Date\n" +
                    "<b>{Time}:</b> Current Time\n" +
                    "<b>{Idx}:</b> Index Number\n" +
                    "<b>{Product}:</b> Product Name " + $"({Application.productName})\n" +
                    "<b>{Scene}:</b>\n" +
                    "For example, <b>\"{Product}_Screenshot\"</b> will replace with " + $"<b>\"{Application.productName}_Screenshot\"</b>", EditorStyles.wordWrappedLabel);
            }

            // Date Settings
            m_showDateSettings = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showDateSettings, "Date Settings", true);
            if (m_showDateSettings)
            {
                EditorGUILayout.HelpBox($"Current Date Format: {m_dateFormat}", MessageType.None);

                string[] dateFormat =
                {
                    "Month-Day-Year",
                    "Day-Month-Year",
                    "Year-Month-Day",
                };
                m_dateType = (EDateType) EditorGUILayout.Popup("Date Format", (int) m_dateType, dateFormat);
                m_useTwoDigitYear = EditorGUILayout.Toggle("2-digit Year", m_useTwoDigitYear);
                m_dateSeparator = EditorGUILayout.TextField("Separator", m_dateSeparator);
                m_useZerofillDate = EditorGUILayout.Toggle("Leading Zero", m_useZerofillDate);
            }

            // Time Settings
            m_showTimeSettings = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showTimeSettings, "Time Settings", true);
            if (m_showTimeSettings)
            {
                EditorGUILayout.HelpBox($"Current Time Format: {m_timeFormat}", MessageType.None);

                string[] timeFormat =
                {
                    "12H",
                    "24H",
                };

                m_timeType = (ETimeType) EditorGUILayout.Popup("12H/24H", (int) m_timeType, timeFormat);
                m_useSeconds = EditorGUILayout.Toggle("Show seconds", m_useSeconds);
                m_timeSeparator = EditorGUILayout.TextField("Separator", m_timeSeparator);
                m_useZerofillTime = EditorGUILayout.Toggle("Leading Zero", m_useZerofillTime);
            }

            // Index Settings
            m_showIndexSettings = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showIndexSettings, "Index Settings", true);
            if (m_showIndexSettings)
            {
                EditorGUILayout.HelpBox($"Current Index: {m_screenshotIdx.ToString($"D{m_minDigits}")}\n" +
                                        $"Next Index: {(m_screenshotIdx + m_incrementalValue).ToString($"D{m_minDigits}")}", MessageType.None);

                m_screenshotIdx = EditorGUILayout.IntField("Current Index", m_screenshotIdx);
                m_incrementalValue = EditorGUILayout.IntField("Incremental Value", m_incrementalValue);
                m_minDigits = EditorGUILayout.IntSlider("Minimum Digits", m_minDigits, 1, 10);
            }

            // Set Values
            // Set Date
            string year = m_useTwoDigitYear ? "yy" : "yyyy";
            string month = m_useZerofillDate ? "MM" : "M";
            string day = m_useZerofillDate ? "dd" : "d";

            switch (m_dateType)
            {
                case EDateType.MonthDayYear:
                    m_dateFormat = $"{month}{m_dateSeparator}{day}{m_dateSeparator}{year}";
                    break;
                case EDateType.DayMonthYear:
                    m_dateFormat = $"{day}{m_dateSeparator}{month}{m_dateSeparator}{year}";
                    break;
                case EDateType.YearMonthDay:
                    m_dateFormat = $"{year}{m_dateSeparator}{month}{m_dateSeparator}{day}";
                    break;
            }

            // Set Time
            string hour = m_useZerofillTime ? "hh" : "h";
            string min = m_useZerofillTime ? "mm" : "m";
            string sec = m_useZerofillTime ? "ss" : "s";

            switch (m_timeType)
            {
                case ETimeType.Time12H:
                    m_timeFormat = $"{hour}{m_timeSeparator}{min}";
                    break;
                case ETimeType.Time24H:
                    m_timeFormat = $"{hour.ToUpper()}{m_timeSeparator}{min}";
                    break;
            }

            if (m_useSeconds)
            {
                m_timeFormat += $"{m_timeSeparator}{sec}";
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Final file name: <color=yellow>{m_prefix}</color>{m_fileName}<color=yellow>{m_suffix}{m_screenshotIdx:D4}</color>.{m_currentEFileFormat.ToString().ToLower()}",
                MessageType.Info);

            m_fileName = EditorGUILayout.TextField("FileName", m_fileName);
            if (GUILayout.Button("Reset Index"))
            {
                m_screenshotIdx = 0;
            }
            m_showPrefixSuffix = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showPrefixSuffix, "Prefix & Suffix", true);
            if (m_showPrefixSuffix)
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
                m_currentPrefixFormat = (EPrefixSuffixFormat) EditorGUILayout.Popup("Prefix", (int) m_currentPrefixFormat, displayedPrefixSuffixFormat);
                m_currentSuffixFormat = (EPrefixSuffixFormat) EditorGUILayout.Popup("Suffix", (int) m_currentSuffixFormat, displayedPrefixSuffixFormat);
            }

            string[] prefixSuffixSeparator =
            {
                "_",
                "-",
                ".",
                @":",
                "(Space)",
                "None",
            };
            m_selectedSeparator = EditorGUILayout.Popup("Separator", m_selectedSeparator, prefixSuffixSeparator);
            switch (prefixSuffixSeparator[m_selectedSeparator])
            {
                case "(Space)":
                    m_currentSeparator = " ";
                    break;
                case "None":
                    m_currentSeparator = "";
                    break;
                default:
                    m_currentSeparator = prefixSuffixSeparator[m_selectedSeparator];
                    break;
            }

            m_prefix = ConvertPrefixSuffixEnum(m_currentPrefixFormat);
            m_prefix = m_currentPrefixFormat == EPrefixSuffixFormat.None ? m_prefix : $"{m_prefix}{m_currentSeparator}";
            m_suffix = ConvertPrefixSuffixEnum(m_currentSuffixFormat);
            m_suffix = m_currentSuffixFormat == EPrefixSuffixFormat.None ? $"{m_suffix}{m_currentSeparator}" : $"{m_currentSeparator}{m_suffix}{m_currentSeparator}";
        }
    }

    private void SetMiscellaneousOnGUI()
    {
        GUILayout.Space(10f);
        GUILayout.Label("Miscellaneous", EditorStyles.boldLabel);
        m_currentEFileFormat = (EFileFormat) EditorGUILayout.EnumPopup("Format", m_currentEFileFormat);
        string[] displayedCaptureOptions =
        {
            "Include UI",
            "Don't Include UI",
        };
        m_currentECaptureMode = (ECaptureMode) EditorGUILayout.Popup("Capture Mode", (int) m_currentECaptureMode, displayedCaptureOptions);

        switch (m_currentECaptureMode)
        {
            case ECaptureMode.IncludeUI:
                break;

            case ECaptureMode.ExcludeUI:
                break;

            default:
                Debug.Assert(false, m_currentECaptureMode);
                break;
        }
    }

    private string ConvertPrefixSuffixEnum(EPrefixSuffixFormat format)
    {
        switch (format)
        {
            case EPrefixSuffixFormat.None:
                return "";

            case EPrefixSuffixFormat.AmericanDate:
                return $"{DateTime.Now:MM.dd.yyyy}";

            case EPrefixSuffixFormat.EnglishDate:
                return $"{DateTime.Now:dd.MM.yyyy}";

            case EPrefixSuffixFormat.AsianDate:
                return $"{DateTime.Now:yyyy.MM.dd}";

            case EPrefixSuffixFormat.Time12H:
                return $"{DateTime.Now:hh_mm_ss}";

            case EPrefixSuffixFormat.Time24H:
                return $"{DateTime.Now:HH_mm_ss}";

            default:
                Debug.Assert(false);
                return null;
        }
    }

    private string ConvertPrefixSuffixFormat(string src)
    {
        StringBuilder stringBuilder = new StringBuilder(src);
        stringBuilder
            .Replace("{Date}", $"{DateTime.Now.ToString($"{m_dateFormat}")}")
            .Replace("{Time}", $"{DateTime.Now.ToString($"{m_timeFormat}")}")
            .Replace("{Idx}", $"{m_screenshotIdx.ToString($"D{m_minDigits}")}")
            .Replace("{Product}", $"{Application.productName}")
            .Replace("{Scene}", $"{SceneManager.GetActiveScene().name}");

        return stringBuilder.ToString();
    }

    private void SetCustomPath()
    {
        string temp = EditorUtility.OpenFolderPanel("Set path for screenshots", Path.GetDirectoryName(Application.dataPath), "");
        if (!String.IsNullOrEmpty(temp))
        {
            m_path = temp.Replace('/', '\\');
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
        
        if (m_createSubfolder && String.IsNullOrEmpty(m_subfolderName))
        {
            Debug.LogError("Input subfolder name.");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("Main Camera not found.");
            return;
        }
        
        m_helper = Camera.main.gameObject.GetOrAddComponent<ScreenshotHelperObject>();
        m_helper.path = $"{(m_createSubfolder ? m_path + "\\" + m_subfolderName : m_path)}";
        if (m_isAdvancedMode)
        {
            m_helper.fileName = $"{ConvertPrefixSuffixFormat(m_fileName)}";
            m_screenshotIdx++;
        }
        else
        {
            m_helper.fileName = $"{m_prefix}{m_fileName}{m_suffix}{m_screenshotIdx++}";
        }

        m_helper.fileFormat = m_currentEFileFormat.ToString().ToLower();

        switch (m_currentECaptureMode)
        {
            case ECaptureMode.IncludeUI:
                m_helper.CaptureWithUI();
                break;
            case ECaptureMode.ExcludeUI:
                m_helper.CaptureWithoutUI();
                break;
        }
    }
}