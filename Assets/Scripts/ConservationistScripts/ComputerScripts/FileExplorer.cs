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
        public bool isFolder = false;
        [TextArea(3, 8)]
        public string content = ""; //shown when opened (files only)
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

        closeViewerButton.onClick.AddListener(() => SetGroup(viewerPanel, false));

        Navigate(rootEntries, "C:\\");
    }

    public override void OnAppClose()
    {
        history.Clear();
        closeViewerButton.onClick.RemoveAllListeners();
    }

    //------------------------------------------------------------------------------------------

    //File system logic

    private void Navigate(List<FileEntry> entries, string path)
    {
        current = entries;
        currentPath = path;

        pathLabel.text = path;
        BuildFileList();
    }

    private void BuildFileList()
    {
        foreach(Transform child in fileList) Destroy(child.gameObject);

        int parity = 0;

        //Back button if we've navigated inside a folder
        if(history.Count > 0)
        {
            FileRow backRow = CreateFileRow("Back", isFolder: true, parity, fileList);

            backRow.AddButtonListener(GoBack);

            parity++;
        }

        foreach(var entry in current)
        {
            var e = entry;
            FileRow fileRow = CreateFileRow(e.name, e.isFolder, parity, fileList);

            //If it's a folder, go deeper
            if(e.isFolder)
            {
                fileRow.AddButtonListener(() =>
                {
                    history.Push((currentPath, current));
                    Navigate(e.children, currentPath + e.name + "\\");
                });
            }
            else
            {
                //If it's a file just open it
                fileRow.AddButtonListener(() => {OpenFile(e);});
            }

            parity++;
        }
    }

    private void GoBack()
    {
        if(history.Count == 0) return;
        var(path, entries) = history.Pop();
        Navigate(entries, path);
    }

    private void OpenFile(FileEntry entry)
    {
        viewerTitle.text = entry.name;
        viewerBody.text = entry.content;
        SetGroup(viewerPanel, true);

        if(!string.IsNullOrEmpty(entry.completesObjective))
            ObjectiveManager.instance?.CompleteObjective(entry.completesObjective);
    }

    //------------------------------------------------------------------------------------------

    //Helper methods

    private FileRow CreateFileRow(string label, bool isFolder, int parity, Transform parent)
    {
        GameObject row = Instantiate(fileRowPrefab, parent);
        FileRow fileRow = row.GetComponent<FileRow>();

        fileRow.SetLabel(label);
        fileRow.SetIcon(isFolder);
        fileRow.SetShade(parity);

        return fileRow;
    }

    private void SetGroup(CanvasGroup g, bool on)
    {
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }
}
