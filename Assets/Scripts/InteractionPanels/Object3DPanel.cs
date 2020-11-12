﻿using AsImpL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

//TODO(Jitse): Some objects have the wrong pivot/center point which messes with rotation.
//TODO(cont.): In some cases, the pivot point is so far off from the object that the object is too far away from the preview panel to see.
//TODO(cont.): Lay the responsibility of a correct pivot point on the user? Or is there a workaround?
public class Object3DPanel : MonoBehaviour
{
	public Text title;
	public GameObject object3d;
	public Material transparent;

	private GameObject objectRenderer;
	private GameObject objectHolder;

	[SerializeField]
	private string filePath = "";
	[SerializeField]
	private string objectName = "";
	[SerializeField]
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;
	private int valueX;
	private int valueY;
	private bool rotate;
	private Renderer rend;

	public void Init(string newTitle, List<string> newPaths, float[] parameters)
	{
		title.text = newTitle;
		valueX = Convert.ToInt32(parameters[1]);
		valueY = Convert.ToInt32(parameters[2]);

		objectRenderer = GameObject.Find("ObjectRenderer");
		objImporter = objectRenderer.GetComponent<ObjectImporter>();
		if (objImporter == null)
		{
			objImporter = objectRenderer.AddComponent<ObjectImporter>();
		}

		objImporter.ImportingComplete += SetObjectProperties;

		if (newPaths.Count > 0)
		{
			filePath = newPaths[0];
			objectName = Path.GetFileName(Path.GetDirectoryName(filePath));

			//NOTE(Jitse): Create a parent object for the 3D object, to ensure it has the correct position for rotation
			if (GameObject.Find("/ObjectRenderer/holder_" + objectName) == null)
			{
				objectHolder = new GameObject("holder_" + objectName);
				objectHolder.transform.parent = objectRenderer.transform;
				objImporter.ImportModelAsync(objectName, filePath, objectHolder.transform, importOptions);
			}
		}
	}

	private void SetObjectProperties()
	{
		var objects3d = objectRenderer.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < objects3d.Length; i++)
		{
			var currentObject = objects3d[i];
			if (currentObject.name == objectName)
			{
				object3d = currentObject.gameObject;

				var transforms = object3d.GetComponentsInChildren<Transform>();
				//NOTE(Jitse): If the object consists of more than 1 child objects, we want to combine the meshes
				if (transforms.Length > 2)
				{
					rend = object3d.AddComponent<MeshRenderer>();
					//NOTE(Jitse): We don't want to see the combined "parent" mesh, because we already see the separate children meshes with their respective materials, so we assign a transparent material
					rend.material = transparent;
					var mainMesh = object3d.AddComponent<MeshFilter>();

					//NOTE(Jitse): Combine the meshes of the object into one mesh, to correctly calculate the bounds
					MeshFilter[] meshFilters = object3d.GetComponentsInChildren<MeshFilter>();
					CombineInstance[] combine = new CombineInstance[meshFilters.Length];

					int k = 1;
					while (k < meshFilters.Length)
					{
						combine[k].mesh = meshFilters[k].sharedMesh;
						combine[k].transform = meshFilters[k].transform.localToWorldMatrix;

						k++;
					}

					mainMesh.mesh = new Mesh();
					mainMesh.mesh.CombineMeshes(combine);
				}
				else
				{
					rend = transforms[1].gameObject.GetComponent<MeshRenderer>();
				}

				//NOTE(Jitse): Set the scaling value; 100f was chosen by testing which size would be most appropriate.
				//NOTE(cont.): Lowering or raising this value respectively decreases or increases the object size.
				var scale = 100f / Math.Max(Math.Max(rend.bounds.size.x, rend.bounds.size.y), rend.bounds.size.x);

				//NOTE(Jitse): Ensure every child object has the correct position within the object.
				//NOTE(cont.): Set object position to the bounding box center, this fixes when objects have an offset from their pivot point.
				var children = object3d.GetComponentsInChildren<Transform>();
				for (int j = 1; j < children.Length; j++)
				{
					children[j].localPosition = -rend.bounds.center;
				}

				//NOTE(Jitse): Setting correct parameters of the object.
				var rotation = object3d.transform.localRotation.eulerAngles;
				rotation.x = -90;
				object3d.transform.localRotation = Quaternion.Euler(rotation);
				object3d.transform.localScale = new Vector3(scale, scale, scale);
				object3d.SetLayer(12);
				object3d.SetActive(false);
				objectHolder.transform.localPosition = new Vector3(valueX, valueY, 0);

				break;
			}
		}

		//NOTE(Jitse): Makes sure to not call this again when importing a new object.
		objImporter.ImportingComplete -= SetObjectProperties;
	}

	private void Update()
	{
		if (object3d != null && rotate)
		{
			objectHolder.transform.Rotate(new Vector3(0, 0.1f, 0), Space.Self);
		}
	}

	private void OnEnable()
	{
		if (object3d != null)
		{
			object3d.SetActive(true);
		}
	}

	private void OnDisable()
	{
		if (object3d != null)
		{
			object3d.SetActive(false);
		}
	}

	public void ToggleRotate()
	{
		rotate = !rotate;
	}
}
