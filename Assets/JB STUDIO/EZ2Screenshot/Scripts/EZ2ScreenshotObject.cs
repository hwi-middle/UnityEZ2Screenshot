using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class EZ2ScreenshotObject : MonoBehaviour
{
    [HideInInspector] public string path;
    [HideInInspector] public string fileName;
    [HideInInspector] public string fileFormat;
    [HideInInspector] public bool isFileConflictOccured;
    [HideInInspector] public string conflictedFileFullPath;

    private Texture2D m_textureToSave;

    private Camera m_cam;
    private bool m_isCameraRenderTextureSet;
    private bool m_takeScreenshot;


    // Defined by assembly
#if USING_URP || USING_HDRP
    private void Awake()
    {
        RenderPipelineManager.endContextRendering += OnEndCameraRendering;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        if (m_takeScreenshot)
        {
            PerformCapture();
            m_takeScreenshot = false;
        }
    }

    private void OnDestroy()
    {
        RenderPipelineManager.endContextRendering -= OnEndCameraRendering;
    }
#else
    // Run as a last post-processing script, but do nothing and take a screenshot.
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (m_takeScreenshot)
        {
            PerformCapture();
            m_takeScreenshot = false;
        }

        // Yup, this is meaningless, but exist for avoid warning
        Graphics.Blit(src, dest);
    }
#endif

    public void CaptureWithUI()
    {
        StartCoroutine(WaitForEndOfFrameAndCapture());
    }

    public void CaptureWithoutUI()
    {
        m_takeScreenshot = true;
    }

    private IEnumerator WaitForEndOfFrameAndCapture()
    {
        yield return new WaitForEndOfFrame();
        PerformCapture();
    }

    private void PerformCapture()
    {
        m_cam = Camera.main;
        m_isCameraRenderTextureSet = m_cam.targetTexture != null;

        var gameViewSize = UnityEditor.Handles.GetMainGameViewSize();
        if (!m_isCameraRenderTextureSet)
        {
            m_cam.targetTexture = RenderTexture.GetTemporary((int) gameViewSize.x, (int) gameViewSize.y);
        }

        RenderTexture renderTexture = m_cam.targetTexture;
        m_textureToSave = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        m_textureToSave.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);

        if (!m_isCameraRenderTextureSet)
        {
            m_cam.targetTexture = null;
        }

        if (File.Exists($"{path}\\{fileName}.{fileFormat}"))
        {
            isFileConflictOccured = true;
            conflictedFileFullPath = $"{path}\\{fileName}.{fileFormat}";
            string msg = GetLocalizedString("log_error_already_exist")
                .Replace("{fileName}", $"{fileName}")
                .Replace("{fileFormat}", $"{fileFormat}");
            Debug.LogError(msg);
            return;
        }

        Save();
    }

    public void Save()
    {
        isFileConflictOccured = false; // This function called when the error has been resolved

        byte[] result = null;
        if (fileFormat == "png")
        {
            result = m_textureToSave.EncodeToPNG();
        }
        else if (fileFormat == "jpg")
        {
            result = m_textureToSave.EncodeToJPG();
        }

        if (result != null)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            File.WriteAllBytes($"{path}\\{fileName}.{fileFormat}", result);
        }

        RemoveComponentFromCamera();
        string msg = GetLocalizedString("log_info_saved_as")
            .Replace("{path}", $"{path}")
            .Replace("{fileName}", $"{fileName}")
            .Replace("{fileFormat}", $"{fileFormat}");
        Debug.Log(msg);
    }

    public void RemoveComponentFromCamera()
    {
        Destroy(this);
    }

    private string GetLocalizedString(string key)
    {
        return EZ2ScreenshotLocalizer.TranslateText(key);
    }
}