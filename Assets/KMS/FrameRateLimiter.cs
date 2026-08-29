using UnityEngine;

public static class FrameRateLimiter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}