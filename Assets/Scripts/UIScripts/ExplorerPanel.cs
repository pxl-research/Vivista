﻿using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ExplorerPanel : MonoBehaviour
{
	private class ExplorerEntry
	{
		public string fullPath;
		public string name;
		public DateTime date;
		public Sprite sprite;
		public GameObject filenameIconItem;
		public EntryType entryType;
	}

	private enum EntryType
	{
		File,
		Directory,
		Drive,
	}

	public bool answered;
	public string answerFilePath;
	public string searchPattern;

	public InputField currentPath;
	public Button upButton;
	public ScrollRect directoryContent;
	public GameObject filenameIconItemPrefab;
	public Sprite iconDirectory, iconFile, iconDrive, iconArrowUp;
	public Button sortDateButton;
	public Button sortNameButton;
	public InputField FilenameField;
	public Button OpenButton;
	public Text title;

	private DirectoryInfo[] directories;
	private string[] drives;

	private string currentDirectory;
	private string osType;
	private bool sortByDate;
	private bool sortByName = true;
	private bool sortAscending = true;

	private float timeSinceLastClick;
	private int lastClickIndex;

	private List<ExplorerEntry> entries;

	public void Update()
	{
		if (RectTransformUtility.RectangleContainsScreenPoint(directoryContent.GetComponent<RectTransform>(), Input.mousePosition))
		{
			//TODO(Simon): Figure out current scroll position, and only check x items before and after position
			for (int i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				if (entry.filenameIconItem != null)
				{
					entry.filenameIconItem.GetComponent<Image>().color = new Color(255, 255, 255);

					if (RectTransformUtility.RectangleContainsScreenPoint(entry.filenameIconItem.GetComponent<RectTransform>(), Input.mousePosition))
					{
						entry.filenameIconItem.GetComponent<Image>().color = new Color(210 / 255f, 210 / 255f, 210 / 255f);

						if (Input.GetMouseButtonDown(0))
						{
							if (lastClickIndex == i && timeSinceLastClick < 0.5f)
							{
								if (entry.entryType == EntryType.Directory)
								{
									OnDirectoryClick(entry.fullPath);
									break;
								}

								if (entry.entryType == EntryType.File)
								{
									Answer(entry.fullPath);
									break;
								}

								if (entry.entryType == EntryType.Drive)
								{
									DriveClick(entry.fullPath);
									break;
								}
							}

							timeSinceLastClick = 0;
							lastClickIndex = i;
						}
					}
				}
			}
		}

		timeSinceLastClick += Time.deltaTime;
	}
	/// <summary>
	/// Initialiase the explorepanel
	/// </summary>
	/// <param name="startDirectory"></param>
	/// <param name="searchPattern">Separate filename patterns with ';' </param>
	/// <param name="title"></param>
	public void Init(string startDirectory = "", string searchPattern = "*", string title = "Select file")
	{
		currentDirectory = startDirectory != "" ? startDirectory : Directory.GetCurrentDirectory();

		answered = false;
		osType = Environment.OSVersion.Platform.ToString();
		this.searchPattern = searchPattern;
		sortNameButton.GetComponentInChildren<Text>().text = "Name ↓";
		this.title.text = title;

		UpdateDir();
	}

	public void DirUp()
	{
		var parent = Directory.GetParent(currentDirectory);
		//TODO(Simon): Test with other OSes
		if (parent == null)
		{
			if (osType == "Win32NT")
			{
				SelectDisk();
			}
			return;
		}

		currentDirectory = parent.ToString();
		UpdateDir();
	}

	public void OnSortNameClick()
	{
		if (sortByName && sortAscending)
		{
			sortNameButton.GetComponentInChildren<Text>().text = "Name ↑";
			sortAscending = false;
		}
		else
		{
			sortDateButton.GetComponentInChildren<Text>().text = "Date";
			sortNameButton.GetComponentInChildren<Text>().text = "Name ↓";
			sortAscending = true;
			sortByName = true;
			sortByDate = false;
		}

		UpdateDir();
	}

	public void OnSortDateClick()
	{
		if (sortByDate && sortAscending)
		{
			sortDateButton.GetComponentInChildren<Text>().text = "Date ↑";
			sortAscending = false;
		}
		else
		{
			sortNameButton.GetComponentInChildren<Text>().text = "Name";
			sortDateButton.GetComponentInChildren<Text>().text = "Date ↓";
			sortAscending = true;
			sortByDate = true;
			sortByName = false;
		}

		UpdateDir();
	}

	public void OnPathSubmit(InputField inputField)
	{
		string path = inputField.text;
		if (Directory.Exists(path))
		{
			currentDirectory = path;
			UpdateDir();
			currentPath.GetComponentInChildren<Text>().color = Color.black;
		}
		else if (File.Exists(path))
		{
			Answer(path);
		}
		else
		{
			currentPath.GetComponentInChildren<Text>().color = Color.red;
		}
	}

	private void UpdateDir()
	{
		var dirinfo = new DirectoryInfo(currentDirectory);
		var filteredFiles = new List<FileInfo>();
		var patterns = searchPattern.Split(';');

		foreach (string p in patterns)
		{
			var infos = dirinfo.GetFiles(p);

			for (int j = 0; j < infos.Length; j++)
			{
				filteredFiles.Add(infos[j]);
			}
		}

		directories = dirinfo.GetDirectories();
		currentPath.text = currentDirectory;

		int direction = sortAscending ? 1 : -1;

		if (sortByName)
		{
			Array.Sort(directories, (x, y) => direction * String.Compare(x.Name, y.Name, StringComparison.Ordinal));
			filteredFiles.Sort((x, y) => direction * String.Compare(x.Name, y.Name, StringComparison.Ordinal));
		}
		if (sortByDate)
		{
			Array.Sort(directories, (x, y) => direction * x.LastWriteTime.CompareTo(y.LastWriteTime));
			filteredFiles.Sort((x, y) => direction * x.LastWriteTime.CompareTo(y.LastWriteTime));
		}

		ClearItems();

		foreach (var directory in directories)
		{
			var entry = new ExplorerEntry
			{
				name = directory.Name,
				sprite = iconDirectory,
				fullPath = directory.FullName,
				date = directory.LastWriteTime,
				entryType = EntryType.Directory
			};

			entries.Add(entry);
		}

		foreach (var file in filteredFiles)
		{
			var entry = new ExplorerEntry
			{
				name = file.Name,
				sprite = iconFile,
				fullPath = file.FullName,
				date = file.LastWriteTime,
				entryType = EntryType.File
			};

			entries.Add(entry);
		}

		FillItems();
	}

	private void ClearItems()
	{
		if (entries != null)
		{
			foreach (var item in entries)
			{
				Destroy(item.filenameIconItem);
			}

			entries.Clear();
		}
		else
		{
			entries = new List<ExplorerEntry>();
		}
	}

	private void FillItems()
	{
		foreach (var entry in entries)
		{
			var filenameIconItem = Instantiate(filenameIconItemPrefab);
			filenameIconItem.transform.SetParent(directoryContent.content, false);
			filenameIconItem.GetComponentsInChildren<Text>()[0].text = entry.name;
			filenameIconItem.GetComponentsInChildren<Text>()[1].text = entry.entryType == EntryType.Drive ? "" : entry.date.ToString();
			filenameIconItem.GetComponentsInChildren<Image>()[1].sprite = entry.sprite;

			entry.filenameIconItem = filenameIconItem;
		}

		// scroll to top
		Canvas.ForceUpdateCanvases();
		directoryContent.verticalNormalizedPosition = 1;
	}

	private void OnDirectoryClick(string path)
	{
		currentDirectory = path;
		UpdateDir();
	}

	private void SelectDisk()
	{
		upButton.enabled = false;
		ClearItems();

		drives = Directory.GetLogicalDrives();

		currentPath.text = "Select Drive";

		foreach (string drive in drives)
		{
			entries.Add(new ExplorerEntry
			{
				fullPath = drive,
				name = drive,
				sprite = iconDrive,
				entryType = EntryType.Drive
			});
		}
		FillItems();
	}

	private void DriveClick(string path)
	{
		currentDirectory = path;
		UpdateDir();
		upButton.enabled = true;
	}

	private void Answer(string path)
	{
		answered = true;
		answerFilePath = path;
	}

	public void OpenButtonClicked()
	{
		if (FilenameField.text != "")
		{
			string fullName = currentPath.text + "\\" + FilenameField.text;
			if (File.Exists(fullName))
			{
				Answer(fullName);
			}
			else
			{
				Debug.LogError("file does not exist!");
			}
		}
	}
}
