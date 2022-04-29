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

    private Camera m_cam;
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

        bool isCameraRenderTextureSet = m_cam.targetTexture != null;

        var gameViewSize = UnityEditor.Handles.GetMainGameViewSize();
        if (!isCameraRenderTextureSet)
        {
            m_cam.targetTexture = RenderTexture.GetTemporary((int) gameViewSize.x, (int) gameViewSize.y);
        }

        RenderTexture renderTexture = m_cam.targetTexture;
        Texture2D result = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        if (fileFormat == "PNG")
        {
            File.WriteAllBytes($"{path}/{fileName}.png", result.EncodeToPNG());
            Debug.Log($"Saved as: {path}\\{fileName}.png");
        }
        else if (fileFormat == "JPG")
        {
            File.WriteAllBytes($"{path}/{fileName}.jpg", result.EncodeToJPG());
            Debug.Log($"Saved as: {path}\\{fileName}.jpg");
        }

        if (!isCameraRenderTextureSet)
        {
            m_cam.targetTexture = null;
        }
    }
}