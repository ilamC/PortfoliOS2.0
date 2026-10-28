using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

// Menu: portfoliOS > Apply Latest Change
// The one button for scene changes. Brings the open "our" scene up to the latest
// look (room, CRT, text, glow) from inside the editor and saves it, so nothing has
// to be reloaded from disk. Safe to run more than once. Undo works.
public static class PortfoliOSLayoutTool
{
    const string Art = "Assets/artassetsnew/";
    // room art with the painted rain taken off the window (RoomAlive.shader animates it)
    const string RoomArt = Art + "room_alive.png";
    const string RoomFxMask = Art + "room_fx_mask.png";
    const string RoomFlashMap = Art + "room_flash_map.png";
    const string RoomAliveMat = Art + "RoomAlive.mat";
    // bezel art with the painted blue rim repainted as dark plastic (the reflection lights it now)
    const string BezelOn = Art + "crt_bezel_on_dark.png";
    const string BezelOff = Art + "crt_bezel_off_dark.png";
    const string ReflectionMask = Art + "crt_reflection_mask.png";
    const string ReflectionMat = Art + "ScreenReflection.mat";
    const string GlassOverlay = Art + "crt_screen_overlay.png";

    // Layout numbers (Monitor-local units unless noted)
    const float MonitorScale = 0.68f;
    static readonly Vector2 MonitorPos = new Vector2(52f, 26f);   // canvas units
    static readonly Vector2 GlassSize = new Vector2(1210f, 830f);
    static readonly Vector2 BezelSize = new Vector2(2188.6f, 1307f);
    static readonly Vector2 BezelPos = new Vector2(0f, -57.6f);
    const float TaskbarY = 26.85f;

    [MenuItem("portfoliOS/Apply Latest Change")]
    public static void ApplyLatest()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("portfoliOS: exit Play mode first, then click Apply Latest Change."); return; }
        if (!ApplyLayout()) return;
        ApplyGameFixes();
        PortfoliOSGamesLibrary.Apply();   // My Games library layout (PortfoliOSGamesLibrary.cs)
        PortfoliOSCreditsNotepad.Apply();  // Credits window as Notepad (PortfoliOSCreditsNotepad.cs)
        PortfoliOSMinimizeButton.Apply();  // minimize button on every window (PortfoliOSMinimizeButton.cs)
        PortfoliOSVRLibrary.Apply();       // VR Projects library layout (PortfoliOSVRLibrary.cs)
        PortfoliOSButtonText.Apply();      // keep button labels inside the bevel (PortfoliOSButtonText.cs)
        ApplyBezelReflection();
        PortfoliOSPowerButton.Apply();     // power button next to the LED (PortfoliOSPowerButton.cs)
        ApplyRoomAlive();
        DarkenScreenText();
        PortfoliOSScreenZoom.Apply();      // zoom-to-screen camera + corner ROOM button (PortfoliOSScreenZoom.cs)
        PortfoliOSRoomDepth.Apply();       // REPLACED-style depth plates, fog and parallax (PortfoliOSRoomDepth.cs)
        PortfoliOSWebTemplate.Apply();     // WebGL template with the phone rotate screen (PortfoliOSWebTemplate.cs)
        PortfoliOSTaskbar.Apply();         // Win95 taskbar rework, toggle on Taskbar (PortfoliOSTaskbar.cs)
        PortfoliOSMusicDeck.Apply();       // music player as the Disc Deck: progress bar + new icons (PortfoliOSMusicDeck.cs)
        PortfoliOSSplash.Apply();          // no Made with Unity splash; loader lives in the WebGL template (PortfoliOSSplash.cs)
        // Glow and grain around half the original strength, bloom nudged up a touch (original: threshold 0.47,
        // intensity 2.64, scatter 0.473, pink tint, grain 0.468)
        // film grain 0.36 -> 0.30 on 2026-10-26 (to revert, put the last number back to 0.36f)
        SetGlow(0.71f, 1.95f, 0.36f, new Color(1f, 0.76f, 0.88f, 1f), 0.30f);

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("portfoliOS: latest change applied and scene saved.");
    }

    static bool ApplyLayout()
    {
        var canvas = GameObject.Find("Canvas");
        if (!canvas) { Debug.LogError("portfoliOS: no 'Canvas' in the open scene. Open the 'our' scene first."); return false; }
        Transform root = canvas.transform;
        Transform monitor = root.Find("Monitor");
        if (!monitor) { Debug.LogError("portfoliOS: Canvas/Monitor not found."); return false; }

        // Room background
        var bg = root.Find("Background");
        if (bg)
        {
            var img = bg.GetComponent<Image>();
            var rt = (RectTransform)bg;
            Undo.RecordObjects(new Object[] { img, rt }, "Apply Room Layout");
            img.sprite = Load(RoomArt);
            img.color = Color.white;
            rt.localScale = Vector3.one;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1920f, 1081f);
        }

        // Monitor: centred, scaled down, dark CRT glass behind the desktop
        {
            var rt = (RectTransform)monitor;
            var img = monitor.GetComponent<Image>();
            Undo.RecordObjects(new Object[] { img, rt }, "Apply Room Layout");
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = MonitorPos;
            rt.sizeDelta = GlassSize;
            rt.localScale = Vector3.one * MonitorScale;
            if (img) img.color = new Color(0.0196f, 0.0235f, 0.0392f, 1f);
        }

        // Old pink side strips off
        foreach (var n in new[] { "Image (1)", "Image (2)", "Image (3)" })
        {
            var t = monitor.Find(n);
            if (t) { Undo.RecordObject(t.gameObject, "Apply Room Layout"); t.gameObject.SetActive(false); }
        }

        // Black fade only covers the screen
        var fade = monitor.Find("BlackFade") as RectTransform;
        if (fade)
        {
            Undo.RecordObject(fade, "Apply Room Layout");
            fade.localScale = Vector3.one;
            fade.sizeDelta = GlassSize;
        }

        // CRT glass overlay
        var overlay = monitor.Find("CRTOverlay");
        if (overlay)
        {
            var img = overlay.GetComponent<Image>();
            Undo.RecordObject(img, "Apply Room Layout");
            img.sprite = Load(GlassOverlay);
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        // Bezel (old frame was called "Image")
        var bezel = monitor.Find("CRTBezel") ?? monitor.Find("Image");
        if (bezel)
        {
            var rt = (RectTransform)bezel;
            var img = bezel.GetComponent<Image>();
            Undo.RecordObjects(new Object[] { img, rt, bezel.gameObject }, "Apply Room Layout");
            bezel.gameObject.name = "CRTBezel";
            bezel.SetAsLastSibling();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = BezelPos;
            rt.sizeDelta = BezelSize;
            img.sprite = Load(BezelOn);
            img.color = Color.white;
            img.raycastTarget = false;

            var led = bezel.GetComponent<MonitorPowerLed>();
            if (!led) led = Undo.AddComponent<MonitorPowerLed>(bezel.gameObject);
            Undo.RecordObject(led, "Apply Room Layout");
            led.target = img;
            led.ledOn = Load(BezelOn);
            led.ledOff = Load(BezelOff);
        }

        // Taskbar flush with the bottom of the screen opening
        var taskbar = monitor.Find("Desktop/Taskbar") as RectTransform;
        if (taskbar)
        {
            Undo.RecordObject(taskbar, "Apply Room Layout");
            taskbar.anchoredPosition = new Vector2(taskbar.anchoredPosition.x, TaskbarY);
        }

        // Camera clears to dark plum instead of the skybox
        var cam = Camera.main;
        if (cam)
        {
            Undo.RecordObject(cam, "Apply Room Layout");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.102f, 0.043f, 0.118f, 1f);
        }

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        return true;
    }

    static Sprite Load(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (!s) Debug.LogWarning("portfoliOS: sprite not found at " + path + " (check its Texture Type is Sprite).");
        return s;
    }

    // Dark grey text on the screen -> pure black so it reads through the CRT effects
    static void DarkenScreenText()
    {
        var canvas = GameObject.Find("Canvas");
        var monitor = canvas ? canvas.transform.Find("Monitor") : null;
        if (!monitor) return;
        foreach (var txt in monitor.GetComponentsInChildren<TMP_Text>(true))
        {
            var c = txt.color;
            if (c.a > 0.5f && c.r < 0.3f && c.g < 0.3f && c.b < 0.3f && (c.r + c.g + c.b) > 0f)
            {
                Undo.RecordObject(txt, "Apply Latest Change");
                txt.color = new Color(0f, 0f, 0f, c.a);
            }
        }
        EditorSceneManager.MarkSceneDirty(monitor.gameObject.scene);
    }

    // Live screen reflection on the inner bezel (ScreenReflection.cs + portfoliOS/ScreenReflection shader)
    // Living room: animated rain, lightning, lamp, dust, neon, city lights,
    // headlight sweep and screen light spill, all in portfoliOS/RoomAlive
    static void ApplyRoomAlive()
    {
        var canvas = GameObject.Find("Canvas");
        var bg = canvas ? canvas.transform.Find("Background") : null;
        var img = bg ? bg.GetComponent<Image>() : null;
        if (!img) { Debug.LogWarning("portfoliOS: Canvas/Background not found, skipping room effects."); return; }

        var shader = Shader.Find("portfoliOS/RoomAlive");
        if (!shader) { Debug.LogWarning("portfoliOS: RoomAlive shader not found, skipping room effects."); return; }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(RoomAliveMat);
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, RoomAliveMat);
        }
        mat.shader = shader;
        mat.SetTexture("_FxMask", AssetDatabase.LoadAssetAtPath<Texture2D>(RoomFxMask));
        mat.SetTexture("_FlashMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RoomFlashMap));
        mat.SetVector("_ArtSize", new Vector4(1672f, 941f, 0f, 0f));
        mat.SetVector("_ScreenRect", new Vector4(0.329f, 0.262f, 0.396f, 0.524f));
        mat.SetFloat("_Rain", 1f);
        mat.SetFloat("_Lamp", 1f);
        mat.SetFloat("_Neon", 1f);
        mat.SetFloat("_ScreenSpill", 1.2f);
        mat.SetFloat("_Lightning", 1f);
        mat.SetFloat("_Sweep", 1f);
        EditorUtility.SetDirty(mat);
        Shader.SetGlobalTexture("_PortfoliOSScreenLight", Texture2D.blackTexture);

        Undo.RecordObject(img, "Apply Latest Change");
        img.sprite = Load(RoomArt);
        img.material = mat;
        EditorSceneManager.MarkSceneDirty(bg.gameObject.scene);
    }

    static void ApplyBezelReflection()
    {
        var canvas = GameObject.Find("Canvas");
        var monitor = canvas ? canvas.transform.Find("Monitor") : null;
        var bezel = monitor ? monitor.Find("CRTBezel") as RectTransform : null;
        if (!bezel) { Debug.LogWarning("portfoliOS: CRTBezel not found, skipping reflection."); return; }

        var shader = Shader.Find("portfoliOS/ScreenReflection");
        if (!shader) { Debug.LogWarning("portfoliOS: ScreenReflection shader not found, skipping reflection."); return; }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ReflectionMat);
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ReflectionMat);
        }
        mat.shader = shader;
        mat.SetTexture("_MaskTex", AssetDatabase.LoadAssetAtPath<Texture2D>(ReflectionMask));
        mat.SetTexture("_BezelTex", AssetDatabase.LoadAssetAtPath<Texture2D>(BezelOn));   // lightning only lights the plastic
        mat.SetFloat("_Intensity", 2.5f);
        mat.SetVector("_PixelGrid", new Vector4(176f, 99f, 0f, 0f));
        EditorUtility.SetDirty(mat);

        var t = monitor.Find("BezelReflection") as RectTransform;
        if (!t)
        {
            var go = new GameObject("BezelReflection", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            Undo.RegisterCreatedObjectUndo(go, "Apply Latest Change");
            t = (RectTransform)go.transform;
            t.SetParent(monitor, false);
            go.layer = bezel.gameObject.layer;
        }
        Undo.RecordObject(t, "Apply Latest Change");
        t.SetSiblingIndex(bezel.GetSiblingIndex() + 1);
        t.anchorMin = bezel.anchorMin;
        t.anchorMax = bezel.anchorMax;
        t.pivot = bezel.pivot;
        t.anchoredPosition = bezel.anchoredPosition;
        t.sizeDelta = bezel.sizeDelta;
        t.localScale = bezel.localScale;

        var raw = t.GetComponent<RawImage>();
        Undo.RecordObject(raw, "Apply Latest Change");
        raw.material = mat;
        raw.color = Color.white;
        raw.raycastTarget = false;

        var refl = t.GetComponent<ScreenReflection>();
        if (!refl) refl = Undo.AddComponent<ScreenReflection>(t.gameObject);
        Undo.RecordObject(refl, "Apply Latest Change");
        refl.target = raw;
        refl.bezel = bezel;
        EditorSceneManager.MarkSceneDirty(monitor.gameObject.scene);
    }

    // Game page fixes from the "Add new games" thread (formerly PortfoliOSGamesTool)
    const string GoatsUrl = "https://play.google.com/store/apps/details?id=com.ByIlan.GoatsVsTigers";
    const string KaiPingRole = "Role: Solo developer and designer (expanding to a bigger team)\n";

    static void ApplyGameFixes()
    {
        var pages = FindPagesRoot();
        if (!pages) { Debug.LogWarning("portfoliOS: Window_Games PagesRoot not found, skipping game fixes."); return; }

        var goats = pages.Find("Page_GoatsTigers");
        if (goats)
        {
            var open = goats.GetComponentInChildren<OpenURL>(true);
            if (open) { Undo.RecordObject(open, "Apply Latest Change"); open.url = GoatsUrl; }
        }
        else Debug.LogWarning("portfoliOS: Page_GoatsTigers not found.");

        var kai = pages.Find("Page_KaiPing");
        if (kai)
        {
            var text = FindText(kai, "DetailText");
            if (text && !text.text.Contains("Role:"))
            {
                Undo.RecordObject(text, "Apply Latest Change");
                int i = text.text.IndexOf("\n\n");
                text.text = i < 0 ? KaiPingRole + text.text : text.text.Insert(i + 1, KaiPingRole);
            }
        }
        else Debug.LogWarning("portfoliOS: Page_KaiPing not found.");

        EditorSceneManager.MarkSceneDirty(pages.gameObject.scene);
    }

    static Transform FindPagesRoot()
    {
        var win = GameObject.Find("Canvas/Monitor/Desktop/WindowsParent/Window_Games");
        if (!win) foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.name == "Window_Games" && t.gameObject.scene.IsValid()) { win = t.gameObject; break; }
        return win ? win.transform.Find("Content/Body/PagesRoot") : null;
    }

    static TextMeshProUGUI FindText(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.name == name) return t;
        return null;
    }

    static void SetGlow(float threshold, float intensity, float scatter, Color tint, float grainIntensity)
    {
        var volume = Object.FindFirstObjectByType<Volume>();
        if (!volume || !volume.sharedProfile) { Debug.LogWarning("portfoliOS: no Global Volume profile found."); return; }
        var profile = volume.sharedProfile;
        Undo.RecordObject(profile, "Apply Latest Change");
        if (profile.TryGet(out Bloom bloom))
        {
            Undo.RecordObject(bloom, "Apply Latest Change");
            bloom.threshold.Override(threshold);
            bloom.intensity.Override(intensity);
            bloom.scatter.Override(scatter);
            bloom.tint.Override(tint);
            EditorUtility.SetDirty(bloom);
        }
        if (profile.TryGet(out FilmGrain grain))
        {
            Undo.RecordObject(grain, "Apply Latest Change");
            grain.intensity.Override(grainIntensity);
            EditorUtility.SetDirty(grain);
        }
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }
}
