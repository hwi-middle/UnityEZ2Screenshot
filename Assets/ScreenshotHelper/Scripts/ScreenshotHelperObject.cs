using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class ScreenshotHelperObject : MonoBehaviour
{
    [HideInInspector] public string path;
    [HideInInspector] public string fileName;
    [HideInInspector] public string fileFormat;
    [HideInInspector] public bool isFileConflictOccured;
    [HideInInspector] public string conflictedFileFullPath;

    private Texture2D m_textureToSave;

    private Camera m_cam;
    private bool m_isCameraRenderTextureSet;
    private bool m_takeScreenshotOnPostRender;

    public void CaptureWithUI()
    {
        StartCoroutine(WaitForEndOfFrameAndCapture());
    }

    public void CaptureWithoutUI()
    {
        m_takeScreenshotOnPostRender = true;
    }

    // Run as a last post-processing script, but do nothing and take a screenshot.
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (m_takeScreenshotOnPostRender)
        {
            PerformCapture();
            m_takeScreenshotOnPostRender = false;
        }

        // Yup, this is meaningless, but exist for avoid warning
        Graphics.Blit(src, dest);
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
            Debug.LogError($"{fileName}.{fileFormat} already exists!");
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
        Debug.Log($"Saved as: {path}\\{fileName}.{fileFormat}");
    }

    public void RemoveComponentFromCamera()
    {
        Destroy(this);
    }
}