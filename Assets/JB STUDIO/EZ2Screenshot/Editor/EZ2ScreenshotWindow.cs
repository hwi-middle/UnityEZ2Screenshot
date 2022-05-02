using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.WebPages;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EZ2ScreenshotWindow : EditorWindow
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
    private EZ2ScreenshotObject m_helper;

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


    [MenuItem("Window/JB Studio/EZ2Screenshot")]
    private static void Init()
    {
        EZ2ScreenshotWindow window = (EZ2ScreenshotWindow) GetWindow(typeof(EZ2ScreenshotWindow));
        window.titleContent.text = "EZ2Screenshot";
    }

    private void Awake()
    {
        m_path = Path.GetDirectoryName(Application.dataPath);
    }

    private void OnGUI()
    {
        // Set helper object
        if (Camera.main == null)
        {
            Debug.LogError(GetLocalizedString("log_error_main_cam_not_exit"));
            return;
        }

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

        // Set Language
        SetLanguageOnGUI();

        // Perform Capture
        GUILayout.Space(10f);

        if (GUILayout.Button(GetLocalizedString("gui_take_screenshot"), GUILayout.Height(80)))
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

        GUILayout.Label(GetLocalizedString("gui_error_title"), EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(GetLocalizedString("gui_error_file_exist_question"), MessageType.Error);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(GetLocalizedString("gui_error_file_exist_cancel"), GUILayout.Height(60)))
        {
            m_helper.RemoveComponentFromCamera();
        }

        if (GUILayout.Button(GetLocalizedString("gui_error_file_exist_retry"), GUILayout.Height(60)))
        {
            // Error resolved
            if (!File.Exists($"{m_helper.conflictedFileFullPath}"))
            {
                m_helper.Save();
            }
        }

        if (GUILayout.Button(GetLocalizedString("gui_error_file_exist_replace"), GUILayout.Height(60)))
        {
            m_helper.Save();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(GetLocalizedString("gui_error_file_exist_open_existing_file"), GUILayout.Height(60)))
        {
            if (!Directory.Exists($"{m_path}\\{m_subfolderName}"))
            {
                Debug.Log(GetLocalizedString("log_error_file_not_exist_now"));
            }
            else
            {
                System.Diagnostics.Process.Start($"{m_path}\\{m_subfolderName}\\{m_fileName}.{m_currentEFileFormat}");
            }
        }

        if (GUILayout.Button(GetLocalizedString("gui_error_file_exist_open_existing_dir"), GUILayout.Height(60)))
        {
            if (!Directory.Exists($"{m_path}\\{m_subfolderName}"))
            {
                Debug.Log(GetLocalizedString("log_error_dir_not_exit_now"));
            }
            else
            {
                System.Diagnostics.Process.Start($"{m_path}\\{m_subfolderName}");
            }
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void SetLanguageOnGUI()
    {
        GUILayout.Space(10f);
        GUILayout.Label(GetLocalizedString("gui_language_title"), EditorStyles.boldLabel);

        string[] displayedLanguage =
        {
            "English",
            "한국어",
            "日本語"
        };
        EZ2ScreenshotLocalizer.CurrentLang =
            (EZ2ScreenshotLocalizer.EZ2ScreenshotLang) EditorGUILayout.Popup(GetLocalizedString("gui_language_yours"), (int) EZ2ScreenshotLocalizer.CurrentLang, displayedLanguage);

        if (EZ2ScreenshotLocalizer.CurrentLang == EZ2ScreenshotLocalizer.EZ2ScreenshotLang.Japanese)
        {
            GUILayout.Label("翻訳: Ryu Siyeong", EditorStyles.boldLabel);
        }
    }

    private void SetPathOnGUI()
    {
        GUILayout.Space(10f);
        GUILayout.Label(GetLocalizedString("gui_save_path_title"), EditorStyles.boldLabel);
        GUILayout.Label(
            m_createSubfolder
                ? $"{GetLocalizedString("gui_current_path")}: {m_path}<color=yellow>{Path.DirectorySeparatorChar}{m_subfolderName}</color>"
                : $"{GetLocalizedString("gui_current_path")}: {m_path}",
            EditorStyles.wordWrappedLabel);

        if (!IsPathValid())
        {
            GUILayout.Label($"<color=red><b>{GetLocalizedString("gui_error_set_valid_path")}</b></color>", EditorStyles.wordWrappedLabel);
        }

        GUILayout.Label(GetLocalizedString("gui_save_to"), EditorStyles.wordWrappedLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(GetLocalizedString("gui_dir_proj")))
        {
            m_path = Path.GetDirectoryName(Application.dataPath);
            Debug.Log(GetLocalizedString("log_info_new_path").Replace("{path}", $"{m_path}"));
        }

        if (GUILayout.Button(GetLocalizedString("gui_dir_desktop")))
        {
            m_path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Debug.Log(GetLocalizedString("log_info_new_path").Replace("{path}", $"{m_path}"));
        }

        if (GUILayout.Button(GetLocalizedString("gui_dir_my_pictures")))
        {
            m_path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            Debug.Log(GetLocalizedString("log_info_new_path").Replace("{path}", $"{m_path}"));
        }

        if (GUILayout.Button(GetLocalizedString("gui_dir_custom")))
        {
            SetCustomPath();
            Debug.Log(GetLocalizedString("log_info_new_path").Replace("{path}", $"{m_path}"));
        }

        GUILayout.EndHorizontal();
        if (GUILayout.Button(GetLocalizedString("gui_open_cur_path")))
        {
            if (!Directory.Exists($"{m_path}\\{m_subfolderName}"))
            {
                Directory.CreateDirectory($"{m_path}\\{m_subfolderName}");
            }

            System.Diagnostics.Process.Start($"{m_path}\\{m_subfolderName}");
        }

        m_createSubfolder = EditorGUILayout.Toggle(GetLocalizedString("gui_create_subfolder"), m_createSubfolder);
        m_subfolderName = m_createSubfolder ? EditorGUILayout.TextField(GetLocalizedString("gui_subfolder_name"), m_subfolderName) : "";
    }

    private void SetFileNameOnGUI()
    {
        GUILayout.Space(10f);
        GUILayout.Label(GetLocalizedString("gui_file_name_title"), EditorStyles.boldLabel);

        m_isAdvancedMode = EditorGUILayout.Toggle(GetLocalizedString("gui_advanced_mode"), m_isAdvancedMode);
        if (m_isAdvancedMode)
        {
            EditorGUILayout.HelpBox($"{GetLocalizedString("gui_final_file_name")}: <b>{ConvertPrefixSuffixFormat(m_fileName)}.{m_currentEFileFormat.ToString().ToLower()}</b>",
                MessageType.Info);
            m_fileName = EditorGUILayout.TextField(GetLocalizedString("gui_file_name"), m_fileName);
            m_showFormatGuide = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showFormatGuide, GetLocalizedString("gui_advanced_guide_title"), true);
            if (m_showFormatGuide)
            {
                GUILayout.Label(
                    "<b>{Date}:</b>" + $" {GetLocalizedString("gui_advanced_guide_date")}\n" +
                    "<b>{Time}:</b>" + $" {GetLocalizedString("gui_advanced_guide_time")}\n" +
                    "<b>{Idx}:</b>" + $" {GetLocalizedString("gui_advanced_guide_idx")}\n" +
                    "<b>{Product}:</b>" + $" {GetLocalizedString("gui_advanced_guide_product")}\n" +
                    "<b>{Scene}:</b>" + $" {GetLocalizedString("gui_advanced_guide_scene")}\n" +
                    $"{GetLocalizedString("gui_advanced_guide_example").Replace("{productName}", $"{Application.productName}")}", EditorStyles.wordWrappedLabel);
            }

            // Date Settings
            m_showDateSettings = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showDateSettings, GetLocalizedString("gui_advanced_date_title"), true);
            if (m_showDateSettings)
            {
                EditorGUILayout.HelpBox($"{GetLocalizedString("gui_advanced_date_current_format")}: {m_dateFormat}", MessageType.None);

                string[] dateFormat =
                {
                    $"{GetLocalizedString("gui_advanced_date_item_mdy")}",
                    $"{GetLocalizedString("gui_advanced_date_item_dmy")}",
                    $"{GetLocalizedString("gui_advanced_date_item_ymd")}",
                };
                m_dateType = (EDateType) EditorGUILayout.Popup(GetLocalizedString("gui_advanced_date_date_format"), (int) m_dateType, dateFormat);
                m_useTwoDigitYear = EditorGUILayout.Toggle(GetLocalizedString("gui_advanced_date_2digit_year"), m_useTwoDigitYear);
                m_dateSeparator = EditorGUILayout.TextField(GetLocalizedString("gui_advanced_date_separator"), m_dateSeparator);
                m_useZerofillDate = EditorGUILayout.Toggle(GetLocalizedString("gui_advanced_date_leading_zero"), m_useZerofillDate);
            }

            // Time Settings
            m_showTimeSettings = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showTimeSettings, GetLocalizedString("gui_advanced_time_title"), true);
            if (m_showTimeSettings)
            {
                EditorGUILayout.HelpBox($"{GetLocalizedString("gui_advanced_time_current_format")}: {m_timeFormat}", MessageType.None);

                string[] timeFormat =
                {
                    $"{GetLocalizedString("gui_advanced_time_item_12h")}",
                    $"{GetLocalizedString("gui_advanced_time_item_24h")}",
                };

                m_timeType = (ETimeType) EditorGUILayout.Popup(GetLocalizedString("gui_advanced_time_12h_24h"), (int) m_timeType, timeFormat);
                m_useSeconds = EditorGUILayout.Toggle(GetLocalizedString("gui_advanced_time_show_sec"), m_useSeconds);
                m_timeSeparator = EditorGUILayout.TextField(GetLocalizedString("gui_advanced_time_separator"), m_timeSeparator);
                m_useZerofillTime = EditorGUILayout.Toggle(GetLocalizedString("gui_advanced_time_leading_zero"), m_useZerofillTime);
            }

            // Index Settings
            m_showIndexSettings = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showIndexSettings, GetLocalizedString("gui_advanced_index_title"), true);
            if (m_showIndexSettings)
            {
                EditorGUILayout.HelpBox($"{GetLocalizedString("gui_advanced_index_info_current_index")}: {m_screenshotIdx.ToString($"D{m_minDigits}")}\n" +
                                        $"{GetLocalizedString("gui_advanced_index_info_next_index")}: {(m_screenshotIdx + m_incrementalValue).ToString($"D{m_minDigits}")}",
                    MessageType.None);

                m_screenshotIdx = EditorGUILayout.IntField(GetLocalizedString("gui_advanced_index_current_index"), m_screenshotIdx);
                m_incrementalValue = EditorGUILayout.IntField(GetLocalizedString("gui_advanced_index_incremental_value"), m_incrementalValue);
                m_minDigits = EditorGUILayout.IntSlider(GetLocalizedString("gui_advanced_index_minimum_digits"), m_minDigits, 1, 10);
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
                $"{GetLocalizedString("gui_final_file_name")}: <color=yellow>{m_prefix}</color>{m_fileName}<color=yellow>{m_suffix}{m_screenshotIdx:D4}</color>.{m_currentEFileFormat.ToString().ToLower()}",
                MessageType.Info);

            m_fileName = EditorGUILayout.TextField(GetLocalizedString("gui_file_name"), m_fileName);
            if (GUILayout.Button(GetLocalizedString("gui_reset_index")))
            {
                m_screenshotIdx = 0;
            }

            m_showPrefixSuffix = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_showPrefixSuffix, GetLocalizedString("gui_prefix_and_suffix"), true);
            if (m_showPrefixSuffix)
            {
                string[] displayedPrefixSuffixFormat =
                {
                    $"{GetLocalizedString("gui_prefix_suffix_item_none")}",
                    $"{GetLocalizedString("gui_prefix_suffix_item_date")} (MM.dd.yyyy)",
                    $"{GetLocalizedString("gui_prefix_suffix_item_date")} (dd.MM.yyyy)",
                    $"{GetLocalizedString("gui_prefix_suffix_item_date")} (yyyy.MM.dd)",
                    $"{GetLocalizedString("gui_prefix_suffix_item_time")} (hh_mm_ss, 12H)",
                    $"{GetLocalizedString("gui_prefix_suffix_item_time")} (HH_mm_ss, 24H)",
                };
                m_currentPrefixFormat = (EPrefixSuffixFormat) EditorGUILayout.Popup(GetLocalizedString("gui_prefix"), (int) m_currentPrefixFormat, displayedPrefixSuffixFormat);
                m_currentSuffixFormat = (EPrefixSuffixFormat) EditorGUILayout.Popup(GetLocalizedString("gui_suffix"), (int) m_currentSuffixFormat, displayedPrefixSuffixFormat);
            }

            string[] prefixSuffixSeparator =
            {
                "_",
                "-",
                ".",
                @":",
                $"({GetLocalizedString("gui_separator_item_space")})",
                $"{GetLocalizedString("gui_separator_item_none")}",
            };
            m_selectedSeparator = EditorGUILayout.Popup(GetLocalizedString("gui_separator"), m_selectedSeparator, prefixSuffixSeparator);
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
        GUILayout.Label(GetLocalizedString("gui_miscellaneous"), EditorStyles.boldLabel);
        m_currentEFileFormat = (EFileFormat) EditorGUILayout.EnumPopup(GetLocalizedString("gui_format"), m_currentEFileFormat);
        string[] displayedCaptureOptions =
        {
            $"{GetLocalizedString("gui_item_include_ui")}",
            $"{GetLocalizedString("gui_item_exclude_ui")}",
        };
        m_currentECaptureMode = (ECaptureMode) EditorGUILayout.Popup(GetLocalizedString("gui_capture_mode"), (int) m_currentECaptureMode, displayedCaptureOptions);

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

    private string GetLocalizedString(string key)
    {
        return EZ2ScreenshotLocalizer.TranslateText(key);
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

        m_helper = Camera.main.gameObject.GetOrAddComponent<EZ2ScreenshotObject>();
        m_helper.path = $"{(m_createSubfolder ? m_path + "\\" + m_subfolderName : m_path)}";
        if (m_isAdvancedMode)
        {
            m_helper.fileName = $"{ConvertPrefixSuffixFormat(m_fileName)}";
            m_screenshotIdx += m_incrementalValue;
        }
        else
        {
            m_helper.fileName = $"{m_prefix}{m_fileName}{m_suffix}{m_screenshotIdx:D4}";
            m_screenshotIdx++;
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