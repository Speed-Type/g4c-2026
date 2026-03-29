using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The desktop "operating system" living on the world-space monitor canvas.
///
/// Manages:
///   • Boot / shutdown sequence
///   • Desktop icons that open registered apps
///   • A taskbar showing open apps and a clock
///   • App window layering (bring-to-front on click)
///
/// ── Canvas hierarchy expected ───────────────────────────────────────────
///
///   ScreenCanvas  (World Space Canvas, ComputerOS here)
///     ├── BootScreen          (CanvasGroup)  — loading bar + logo
///     ├── Desktop             (CanvasGroup)
///     │     ├── Wallpaper     (Image)
///     │     ├── IconGrid      (GridLayoutGroup)  ← desktop icons spawn here
///     │     └── WindowLayer   (RectTransform)    ← app windows spawn here
///     └── Taskbar             (CanvasGroup)
///           ├── StartLabel    (TMP_Text)
///           ├── OpenAppList   (HorizontalLayoutGroup) ← taskbar buttons
///           └── ClockLabel    (TMP_Text)
///
/// ── App prefab expected ─────────────────────────────────────────────────
///
///   AppWindow  (RectTransform, CanvasGroup)
///     ├── TitleBar  (Image, draggable)
///     │     ├── TitleLabel  (TMP_Text)
///     │     └── CloseButton (Button)
///     └── ContentArea (the AppBase subclass goes here)
///
/// </summary>
public class ComputerOS : MonoBehaviour
{
    //------------------------------------------------------------------------------------------
    
    //App registration (setup in inspector)

    [System.Serializable]
    public class AppEntry
    {
        [Tooltip("Name shown on the icon and title bar")]
        public string appName = "App";

        [Tooltip("Icon sprite shown on the desktop")]
        public Sprite icon;

        [Tooltip("The root GameObject of the app's window (must be in WindowLayer or a prefab)")]
        public GameObject windowPrefab;

        [Tooltip("Start the app already open when the computer powers on")]
        public bool openOnBoot = false;
    }

    //------------------------------------------------------------------------------------------

    [Header("Apps")]
    public List<AppEntry> apps = new();

    [Header("UI Roots")]
    public CanvasGroup bootScreen;
    public CanvasGroup desktop;
    public CanvasGroup taskbar;

    [Header("Desktop")]
    public Transform iconGrid; //GridLayoutGroup parent for desktop icons
    public Transform windowLayer; //RectTransform parent for app windows

    [Header("Taskbar")]
    public Transform openAppList; //HorizontalLayoutGroup for taskbar buttons
    public TMP_Text clockLabel;

    [Header("Prefabs")]
    [Tooltip("Prefab for a desktop icon (Image + TMP_Text + Button)")]
    public GameObject desktopIconPrefab;

    [Tooltip("Prefab for a taskbar button (Button + TMP_Text)")]
    public GameObject taskbarButtonPrefab;

    [Header("Boot Settings")]
    public float bootDuration = 1.8f;
    public float shutdownDelay = 0.4f;

    //------------------------------------------------------------------------------------------

    //State

    //appName to live window instance
    private Dictionary<string, GameObject> openWindows = new();
    // appName → taskbar button
    private Dictionary<string, GameObject>  taskbarButtons = new();

    private bool isPowered = false;

    // ──────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        SetGroup(bootScreen, false, instant: true);
        SetGroup(desktop, false, instant: true);
        SetGroup(taskbar, false, instant: true);
    }

    private void Update()
    {
        //Fun little clock label
        if(isPowered) clockLabel.text = System.DateTime.Now.ToString("HH:mm");
    }

    //------------------------------------------------------------------------------------------

    //Public methods

    public void PowerOn()
    {
        if(isPowered) return;
        isPowered = true;
        StartCoroutine(BootSequence());
    }

    public void PowerOff()
    {
        if(!isPowered) return;
        isPowered = false;
        StartCoroutine(ShutdownSequence());
    }

    //Open an app by name from code
    public void OpenApp(string appName)
    {
        var entry = apps.Find(a => a.appName == appName);
        if(entry != null) LaunchApp(entry);
    }

    //Close an app by name from code
    public void CloseApp(string appName)
    {
        if(openWindows.TryGetValue(appName, out var win))
            CloseWindow(appName, win);
    }

    //------------------------------------------------------------------------------------------

    //Boot/shutdown sequence

    private IEnumerator BootSequence()
    {
        //Show boot screen, fade in
        SetGroup(bootScreen, true, instant: true);
        bootScreen.alpha = 0f;
        yield return StartCoroutine(FadeGroup(bootScreen, 1f, 0.3f));

        //Simulate loading
        yield return new WaitForSeconds(bootDuration);

        yield return StartCoroutine(FadeGroup(bootScreen, 0f, 0.3f));
        SetGroup(bootScreen, false, instant: true);

        //Show desktop
        BuildDesktopIcons();
        SetGroup(desktop, true, instant: true);
        desktop.alpha = 0f;
        SetGroup(taskbar, true, instant: true);
        taskbar.alpha = 0f;

        yield return StartCoroutine(FadeGroup(desktop, 1f, 0.4f));
        yield return StartCoroutine(FadeGroup(taskbar, 1f, 0.3f));

        //Open any apps flagged as openOnBoot
        foreach(var app in apps)
            if(app.openOnBoot) LaunchApp(app);
    }

    private IEnumerator ShutdownSequence()
    {
        //Close all windows
        foreach(var kv in new Dictionary<string, GameObject>(openWindows))
            CloseWindow(kv.Key, kv.Value);

        yield return StartCoroutine(FadeGroup(taskbar, 0f, 0.2f));
        yield return StartCoroutine(FadeGroup(desktop, 0f, shutdownDelay));

        SetGroup(desktop, false, instant: true);
        SetGroup(taskbar, false, instant: true);

        //Clean up icons
        foreach(Transform child in iconGrid) Destroy(child.gameObject);
        foreach(Transform child in openAppList) Destroy(child.gameObject);
        openWindows.Clear();
        taskbarButtons.Clear();
    }

    //------------------------------------------------------------------------------------------

    //Building desktop icons

    private void BuildDesktopIcons()
    {
        if(desktopIconPrefab == null || iconGrid == null) return;

        //Clear existing
        foreach (Transform child in iconGrid) Destroy(child.gameObject);

        foreach(var app in apps)
        {
            var appCopy = app;
            GameObject icon = Instantiate(desktopIconPrefab, iconGrid);

            //Icon image
            var img = icon.GetComponentInChildren<Image>();
            if (img != null && app.icon != null) img.sprite = app.icon;

            //Label
            var lbl = icon.GetComponentInChildren<TMP_Text>();
            if(lbl != null) lbl.text = app.appName;

            //Click
            var btn = icon.GetComponent<Button>();
            if(btn == null) btn = icon.AddComponent<Button>();
            btn.onClick.AddListener(() => LaunchApp(appCopy));
        }
    }

    //------------------------------------------------------------------------------------------

    //Window management (pretty much ripped straight from The FACTory)

    private void LaunchApp(AppEntry entry)
    {
        //If already open, bring to front instead of opening twice
        if(openWindows.TryGetValue(entry.appName, out var existing))
        {
            BringToFront(existing);
            return;
        }

        if(entry.windowPrefab == null || windowLayer == null) return;

        //Instantiate the window
        GameObject win = Instantiate(entry.windowPrefab, windowLayer);
        win.SetActive(true);

        //Set title bar label
        var titleLabel = win.transform.Find("TitleBar/TitleLabel")?.GetComponent<TMP_Text>();
        if (titleLabel != null) titleLabel.text = entry.appName;

        //Wire the close button
        var closeBtn = win.transform.Find("TitleBar/CloseButton")?.GetComponent<Button>();
        if(closeBtn != null)
        {
            string name = entry.appName;
            closeBtn.onClick.AddListener(() => CloseApp(name));
        }

        //Wire title bar click to bring to front function
        var titleBar = win.transform.Find("TitleBar")?.GetComponent<Button>();
        if(titleBar != null)
        {
            GameObject winRef = win;
            titleBar.onClick.AddListener(() => BringToFront(winRef));
        }

        //Slight cascade offset so windows don't stack exactly
        int count = openWindows.Count;
        var rect = win.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition += new Vector2(count * 20f, -count * 20f);

        openWindows[entry.appName] = win;

        //Notify the app's logic component
        var appBase = win.GetComponentInChildren<AppBase>();
        appBase?.OnAppOpen(this);

        //Add taskbar button
        AddTaskbarButton(entry.appName, win);

        BringToFront(win);
    }

    private void CloseWindow(string appName, GameObject win)
    {
        var appBase = win.GetComponentInChildren<AppBase>();
        appBase?.OnAppClose();

        Destroy(win);
        openWindows.Remove(appName);

        //Remove taskbar button
        if (taskbarButtons.TryGetValue(appName, out var btn))
        {
            Destroy(btn);
            taskbarButtons.Remove(appName);
        }
    }

    private void BringToFront(GameObject win)
    {
        win.transform.SetAsLastSibling();
    }

    //------------------------------------------------------------------------------------------

    //Taskbar logic

    private void AddTaskbarButton(string appName, GameObject win)
    {
        if(taskbarButtonPrefab == null || openAppList == null) return;

        GameObject btnGO = Instantiate(taskbarButtonPrefab, openAppList);

        var lbl = btnGO.GetComponentInChildren<TMP_Text>();
        if(lbl != null) lbl.text = appName;

        var btn = btnGO.GetComponent<Button>();
        if(btn == null) btn = btnGO.AddComponent<Button>();

        string name = appName;
        GameObject w = win;
        btn.onClick.AddListener(() =>
        {
            if(openWindows.ContainsKey(name))
                BringToFront(w);
        });

        taskbarButtons[appName] = btnGO;
    }

    //------------------------------------------------------------------------------------------

    //Helper functions

    private void SetGroup(CanvasGroup group, bool on, bool instant)
    {
        if(group == null) return;
        if(instant) group.alpha = on ? 1f : 0f;
        group.interactable   = on;
        group.blocksRaycasts = on;
    }

    private IEnumerator FadeGroup(CanvasGroup group, float target, float duration)
    {
        if(group == null) yield break;
        float start = group.alpha;
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        group.alpha = target;
    }
}