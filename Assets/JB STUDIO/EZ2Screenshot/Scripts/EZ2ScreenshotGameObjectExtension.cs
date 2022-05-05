using UnityEngine;

public static class EZ2ScreenshotGameObjectExtension
{
    public static T JB_GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        
        if (component == null)
            component = go.AddComponent<T>();
        
        return component;
    }
}