using UnityEditor;
using UnityEngine;

// Turns off the "Made with Unity" splash so the WebGL build opens straight into the room.
// The loading screen before it lives in the portfoliOS WebGL template (Assets/WebGLTemplates/portfoliOS).
// Called from Apply Latest Change. Revert: Player Settings > Splash Image > tick Show Splash Screen.
public static class PortfoliOSSplash
{
    public static void Apply()
    {
        if (!PlayerSettings.SplashScreen.show && !PlayerSettings.SplashScreen.showUnityLogo) return;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        AssetDatabase.SaveAssets();
        if (PlayerSettings.SplashScreen.show)
            Debug.LogWarning("portfoliOS: Unity kept the splash screen on (license may not allow turning it off).");
        else
            Debug.Log("portfoliOS: Made with Unity splash turned off.");
    }
}
