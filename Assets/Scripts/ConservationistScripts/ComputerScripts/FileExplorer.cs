using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
