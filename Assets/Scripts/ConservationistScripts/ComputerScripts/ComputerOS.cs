using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Boot Screen")]
    public RectTransform progressBar;
    private float progressBarOriginalWidth;

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
    //appName to taskbar button
    private Dictionary<string, GameObject>  taskbarButtons = new();

    private bool isPowered = false;

    //------------------------------------------------------------------------------------------

    void Awake()
    {
        SetGroup(bootScreen, false, instant: true);
        SetGroup(desktop, false, instant: true);
        SetGroup(taskbar, false, instant: true);

        progressBarOriginalWidth = progressBar.sizeDelta.x;
    }

    void Update()
    {
        //Fun little clock label
        if(isPowered) clockLabel.text = System.DateTime.Now.ToString("HH:mm tt");
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
        progressBar.sizeDelta = new Vector2(0, progressBar.sizeDelta.y);
        yield return StartCoroutine(FadeGroup(bootScreen, 1f, 0.3f));

        //Simulate loading
        LeanTween.value(0, 1, bootDuration - 0.5f)
        .setOnUpdate((float val) =>
        {
            progressBar.sizeDelta = new Vector2(val * progressBarOriginalWidth, progressBar.sizeDelta.y);
        });
        yield return new WaitForSeconds(bootDuration);

        yield return StartCoroutine(FadeGroup(bootScreen, 0f, 0.3f));
        SetGroup(bootScreen, false, instant: true);

        //Show desktop
        BuildDesktopIcons();
        SetGroup(desktop, true, instant: true);
        desktop.alpha = 0f;
        SetGroup(taskbar, true, instant: true);
        taskbar.alpha = 0f;

        StartCoroutine(FadeGroup(desktop, 1f, 0.3f));
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
        //Clear existing
        foreach(Transform child in iconGrid) Destroy(child.gameObject);

        foreach(var app in apps)
        {
            var appCopy = app;
            GameObject icon = Instantiate(desktopIconPrefab, iconGrid);
            DesktopIcon desktopIcon = icon.GetComponent<DesktopIcon>();

            //Icon image
            if(app.icon != null) desktopIcon.SetIcon(app.icon);

            //Label
            desktopIcon.SetLabel(app.appName);

            //Add click event
            desktopIcon.AddButtonListener(() => LaunchApp(appCopy));
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

        //Instantiate the window
        GameObject win = Instantiate(entry.windowPrefab, windowLayer);
        AppWindow appWindow = win.GetComponent<AppWindow>();
        win.SetActive(true);

        //Set title bar label
        appWindow.SetLabel(entry.appName);

        //Wire the close button
        appWindow.AddCloseButtonListener(() => CloseApp(entry.appName));

        //Wire title bar click to bring to front function
        appWindow.AddTitleBarButtonListener(() => BringToFront(win));

        //Slight cascade offset so windows don't stack exactly
        int count = openWindows.Count;
        var rect = win.GetComponent<RectTransform>();
        if(rect != null)
            rect.anchoredPosition += new Vector2(count * 20f, -count * 20f);

        openWindows[entry.appName] = win;

        //Notify the app's logic component
        var appBase = appWindow.GetAppBase();
        appBase?.OnAppOpen(this);

        //Add taskbar button
        AddTaskbarButton(entry, win);

        BringToFront(win);
    }

    private void CloseWindow(string appName, GameObject win)
    {
        var appBase = win.GetComponent<AppWindow>().GetAppBase();
        appBase?.OnAppClose();

        Destroy(win);
        openWindows.Remove(appName);

        //Remove taskbar button
        if(taskbarButtons.TryGetValue(appName, out var btn))
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

    private void AddTaskbarButton(AppEntry app, GameObject window)
    {
        GameObject btnGO = Instantiate(taskbarButtonPrefab, openAppList);
        TaskbarButton taskbarButton = btnGO.GetComponent<TaskbarButton>();
        string name = app.appName;

        //Set image
        if(app.icon != null) taskbarButton.SetIcon(app.icon);

        //Set label
        taskbarButton.SetLabel(name);

        //Set button
        taskbarButton.AddButtonListener(() =>
        {
            if(openWindows.ContainsKey(name))
                BringToFront(window);
        });

        taskbarButtons[name] = btnGO;
    }

    //------------------------------------------------------------------------------------------

    //Helper functions

    private void SetGroup(CanvasGroup group, bool on, bool instant)
    {
        if(group == null) return;
        if(instant) group.alpha = on ? 1f : 0f;
        group.interactable = on;
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