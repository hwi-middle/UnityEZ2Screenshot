using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.Json;
using UnityEditor;

public static class EZ2ScreenshotLocalizer
{
    public enum EZ2ScreenshotLang
    {
        English,
        Korean,
        Japanese,
    }

    private static EZ2ScreenshotLang _currentLang = (EZ2ScreenshotLang) EditorPrefs.GetInt("CurrentLanguage", 0);

    public static EZ2ScreenshotLang CurrentLang
    {
        get => _currentLang;
        set
        {
            _currentLang = value;
            EditorPrefs.SetInt("CurrentLanguage", (int) value);
            Init();
        }
    }

    private static Dictionary<string, string> _data;

    public static string TranslateText(string key)
    {
        if (_data == null)
        {
            Init();
        }

        return _data[key];
    }

    private static void Init()
    {
        string fileName;
        switch (CurrentLang)
        {
            case EZ2ScreenshotLang.English:
                fileName = "EN";
                break;
            case EZ2ScreenshotLang.Korean:
                fileName = "KR";
                break;
            case EZ2ScreenshotLang.Japanese:
                fileName = "JP";
                break;
            default:
                Debug.Assert(false, CurrentLang);
                return;
        }

        string path = @"Assets\JB STUDIO\EZ2Screenshot\Lang\";
        string file = File.ReadAllText($"{path}{fileName}.json");
        _data = JsonSerializer.Deserialize<Dictionary<string, string>>(file);
    }
}