using UnityEngine;

/// <summary>
/// Phone-only startup settings: run at 60 fps (Unity's mobile default is 30) and keep the
/// screen awake while playing. Does nothing on PC.
/// </summary>
public static class MobileSettings
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        if (!Application.isMobilePlatform) return;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}
