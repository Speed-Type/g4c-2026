using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Inherit from this to create a new computer application.
/// Place the subclass on the ContentArea child of an AppWindow prefab.
/// ComputerOS will call OnAppOpen / OnAppClose automatically.
/// </summary>

//Abstract base class that every other app will inherit from
public abstract class AppBase : MonoBehaviour
{
    //Reference to the OS, set when the app is launched
    protected ComputerOS OS {get; private set;}

    //Called by ComputerOS immediately after the window is instantiated
    public virtual void OnAppOpen(ComputerOS os)
    {
        OS = os;
    }

    //Called by ComputerOS just before the window is destroyed
    public virtual void OnAppClose() { }
}

//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

//App 1: EmailClient
//Read-only, has trivia questions for the player

/// Email client application.
///
/// ── ContentArea hierarchy ──────────────────────────────────────────────
///
///   ContentArea  (EmailClient.cs here)
///     ├── InboxPanel
///     │     └── EmailList  (VerticalLayoutGroup)  ← email rows spawn here
///     └── ReaderPanel
///           ├── SubjectLabel  (TMP_Text)
///           ├── FromLabel     (TMP_Text)
///           ├── BodyText      (TMP_Text, ScrollRect)
///           └── BackButton    (Button)
///

public class EmailClient : AppBase
{
    //Email datatypes

    [System.Serializable]
    public class Email
    {
        public string from = "unknown@example.com";
        public string subject = "(no subject)";
        [TextArea(3, 8)]
        public string body = "";
        [Tooltip("Objective ID to complete when this email is opened; Leave blank to skip")]
        public string completesObjective = "";
        [HideInInspector]
        public bool isRead = false;
    }

    //------------------------------------------------------------------------------------------

    [Header("Emails")]
    public List<Email> emails = new();

    [Header("Panels")]
    public CanvasGroup inboxPanel;
    public CanvasGroup readerPanel;

    [Header("Inbox")]
    public Transform emailList;
    public GameObject emailRowPrefab; // Button + TMP_Text (subject) + TMP_Text (from)

    [Header("Reader")]
    public TMP_Text subjectLabel;
    public TMP_Text fromLabel;
    public TMP_Text bodyText;
    public Button backButton;

    //------------------------------------------------------------------------------------------

    public override void OnAppOpen(ComputerOS os)
    {
        base.OnAppOpen(os);
        BuildInbox();
        ShowInbox();

        if(backButton != null)
            backButton.onClick.AddListener(ShowInbox);
    }

    public override void OnAppClose()
    {
        if(backButton != null)
            backButton.onClick.RemoveAllListeners();
    }

    //------------------------------------------------------------------------------------------

    //Inbox logic
    //Also pretty much ripped straight from The FACTory

    private void BuildInbox()
    {
        if(emailList == null) return;

        foreach(Transform child in emailList) Destroy(child.gameObject);

        for(int i = 0; i < emails.Count; i++)
        {
            int idx  = i; 
            Email mail = emails[i];

            GameObject row = emailRowPrefab != null
                ? Instantiate(emailRowPrefab, emailList)
                : CreateFallbackEmailRow(emailList);

            //Subject and from labels
            var texts = row.GetComponentsInChildren<TMP_Text>();
            if(texts.Length > 0) texts[0].text = mail.subject;
            if(texts.Length > 1) texts[1].text = mail.from;

            //Bold if unread
            if(texts.Length > 0)
                texts[0].fontStyle = mail.isRead ? FontStyles.Normal : FontStyles.Bold;

            var btn = row.GetComponent<Button>();
            if(btn == null) btn = row.AddComponent<Button>();
            btn.onClick.AddListener(() => OpenEmail(idx));
        }
    }

    private void OpenEmail(int index)
    {
        if(index < 0 || index >= emails.Count) return;

        Email mail = emails[index];
        mail.isRead = true;

        if (subjectLabel != null) subjectLabel.text = mail.subject;
        if (fromLabel != null) fromLabel.text    = "From: " + mail.from;
        if (bodyText != null) bodyText.text     = mail.body;

        //Complete linked objective
        if(!string.IsNullOrEmpty(mail.completesObjective))
            ObjectiveManager.instance?.CompleteObjective(mail.completesObjective);

        ShowReader();
        BuildInbox(); //Refresh bold state
    }

    private void ShowInbox()
    {
        SetGroup(inboxPanel, true);
        SetGroup(readerPanel, false);
    }

    private void ShowReader()
    {
        SetGroup(inboxPanel, false);
        SetGroup(readerPanel, true);
    }

    private void SetGroup(CanvasGroup g, bool on)
    {
        if(g == null) return;
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }

    private GameObject CreateFallbackEmailRow(Transform parent)
    {
        var go = new GameObject("EmailRow", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = 36f;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.14f, 0.18f);

        var lbl = new GameObject("Subject", typeof(RectTransform)).AddComponent<TMP_Text>();
        lbl.transform.SetParent(go.transform, false);
        lbl.fontSize = 13f;
        lbl.color = Color.white;

        return go;
    }
}

//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

//App 1: FileExplorer
//Virtual tree of folders and files

/// File explorer application.
///
/// ── ContentArea hierarchy ──────────────────────────────────────────────
///
///   ContentArea  (FileExplorer.cs here)
///     ├── PathLabel     (TMP_Text)          — breadcrumb
///     ├── FileList      (VerticalLayoutGroup) ← entries spawn here
///     └── FileViewer    (CanvasGroup)
///           ├── ViewerTitle  (TMP_Text)
///           ├── ViewerBody   (TMP_Text)
///           └── CloseViewer  (Button)
///

public class FileExplorer : AppBase
{
    //File datatype

    [System.Serializable]
    public class FileEntry
    {
        public string name = "file.txt";
        public bool isFolder  = false;
        [TextArea(3, 8)]
        public string content = "";  //shown when opened (files only)
        [Tooltip("Objective ID completed when this file is opened; Leave blank to skip")]
        public string completesObjective = "";
        public List<FileEntry> children = new(); //populated for folders only
    }

    //------------------------------------------------------------------------------------------

    [Header("File System")]
    [Tooltip("Top-level entries (think root C:\\)")]
    public List<FileEntry> rootEntries = new();

    [Header("UI")]
    public TMP_Text pathLabel;
    public Transform fileList;
    public GameObject fileRowPrefab;

    [Header("Viewer")]
    public CanvasGroup viewerPanel;
    public TMP_Text viewerTitle;
    public TMP_Text viewerBody;
    public Button closeViewerButton;

    //------------------------------------------------------------------------------------------

    private readonly Stack<(string path, List<FileEntry> entries)> history = new(); //never thought i'd actually use what i learned in data structs
    private List<FileEntry> current;
    private string currentPath = "C:\\";

    //------------------------------------------------------------------------------------------

    public override void OnAppOpen(ComputerOS os)
    {
        base.OnAppOpen(os);

        SetGroup(viewerPanel, false);

        if(closeViewerButton != null)
            closeViewerButton.onClick.AddListener(() => SetGroup(viewerPanel, false));

        Navigate(rootEntries, "C:\\");
    }

    public override void OnAppClose()
    {
        history.Clear();
        if(closeViewerButton != null)
            closeViewerButton.onClick.RemoveAllListeners();
    }

    //------------------------------------------------------------------------------------------

    //File system logic

    private void Navigate(List<FileEntry> entries, string path)
    {
        current = entries;
        currentPath = path;

        if(pathLabel != null) pathLabel.text = path;
        BuildFileList();
    }

    private void BuildFileList()
    {
        if(fileList == null) return;
        foreach(Transform child in fileList) Destroy(child.gameObject);

        //Back button if we've navigated inside a folder
        if(history.Count > 0)
        {
            var backRow = CreateFileRow("← Back", isFolder: true, fileList);
            var backBtn = backRow.GetComponent<Button>();
            if(backBtn == null) backBtn = backRow.AddComponent<Button>();
            backBtn.onClick.AddListener(GoBack);
        }

        foreach(var entry in current)
        {
            var e = entry;
            var row = CreateFileRow(
                (e.isFolder ? "📁 " : "📄 ") + e.name,
                e.isFolder, fileList);

            var btn = row.GetComponent<Button>();
            if(btn == null) btn = row.AddComponent<Button>();

            //If it's a folder, go deeper
            if(e.isFolder)
            {
                btn.onClick.AddListener(() =>
                {
                    history.Push((currentPath, current));
                    Navigate(e.children, currentPath + e.name + "\\");
                });
            }
            else
            {
                btn.onClick.AddListener(() => OpenFile(e));
            }
        }
    }

    private void GoBack()
    {
        if(history.Count == 0) return;
        var (path, entries) = history.Pop();
        Navigate(entries, path);
    }

    private void OpenFile(FileEntry entry)
    {
        if(viewerTitle != null) viewerTitle.text = entry.name;
        if(viewerBody  != null) viewerBody.text  = entry.content;
        SetGroup(viewerPanel, true);

        if(!string.IsNullOrEmpty(entry.completesObjective))
            ObjectiveManager.instance?.CompleteObjective(entry.completesObjective);
    }

    //------------------------------------------------------------------------------------------

    //Helper methods

    private GameObject CreateFileRow(string label, bool isFolder, Transform parent)
    {
        GameObject row = fileRowPrefab != null
            ? Instantiate(fileRowPrefab, parent)
            : new GameObject("Row", typeof(RectTransform));

        if(fileRowPrefab == null)
        {
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().minHeight = 30f;
            var img = row.AddComponent<Image>();
            img.color = new Color(0.1f, 0.12f, 0.16f);
        }

        var txt = row.GetComponentInChildren<TMP_Text>();
        if(txt == null)
        {
            txt = new GameObject("Label", typeof(RectTransform)).AddComponent<TMP_Text>();
            txt.transform.SetParent(row.transform, false);
            txt.fontSize = 13f;
            txt.color    = isFolder ? new Color(0.6f, 0.85f, 1f) : Color.white;
        }
        txt.text = label;

        return row;
    }

    private void SetGroup(CanvasGroup g, bool on)
    {
        if(g == null) return;
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }
}
