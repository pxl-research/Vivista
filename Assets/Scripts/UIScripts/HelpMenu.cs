﻿using System.Diagnostics;
using System.IO;
using UnityEngine;

public class HelpMenu : MonoBehaviour
{
	public GameObject bugReportPanelPrefab;
	public GameObject aboutPanelPrefab;

	//NOTE(Simon): For now this shows the log file in the appropriate explorer-like application on the various OSes
	public void ExportLog()
	{
		string path;
#if UNITY_STANDALONE_WIN
		path = Path.Combine(Application.persistentDataPath, "Player.log");
		path = path.Replace('/', '\\');
		Process.Start("explorer.exe", $"/select,\"{path}\"");
#elif UNITY_STANDALONE_OSX
		path = "~/Library/Logs/Unity/Player.log";
		Process.Start("open", "-R " + path);
#elif UNITY_STANDALONE_LINUX
		var path = Path.Combine(Application.persistentDataPath, "Player.log");
		Process.Start("xdg-open", path);
#else
#error Function not defined for this platform
#endif
	}

	public void ReportBug()
	{
		Canvass.modalBackground.SetActive(true);
		Instantiate(bugReportPanelPrefab, Canvass.main.transform, false);
	}

	public void About()
	{
		Canvass.modalBackground.SetActive(true);
		Instantiate(aboutPanelPrefab, Canvass.main.transform, false);
	}
}
