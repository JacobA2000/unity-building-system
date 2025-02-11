﻿using System.Collections.Generic;
using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    bool debugcreated1 = false;
    bool debugcreated2 = false;

    [Header("GENERAL")]
    public bool AllowSnapping;
    public float SnappingDistance;
    public LayerMask BuildableLayers;
    public Camera CharacterCamera;

    [Header("BUILDING COMPONENT CATALOGUE")]
    public List<BuildingComponent> BuildingComponents = new List<BuildingComponent>();

    [Header("SNAPPING:")]
    public float GridSize;
    public float GridOffset;

    private GameObject currentPreviewGameObject;
    private BuildingComponent currentlyPreviewedComponent;
    private Vector3 currentPosition;
    private RaycastHit raycastHit;
    private bool previewing;
    private Preview currentPreview;

    private static BuildingSystem instance;
    public static BuildingSystem Instance { get { return instance; } }
    [HideInInspector] public bool snapping;
    [HideInInspector] public GameObject objectToSnap;
    [HideInInspector] public Vector3 snappingOffset;
    [HideInInspector] public float heightOffset;

    private void Start()
    {
        instance = this;
        for (int i = 0; i < BuildingComponents.Count; i++)
        {
            UserInterface.Instance.MenuElements.Add(BuildingComponents[i].MenuElement);
        }
        UserInterface.Instance.Initialize();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (UserInterface.Instance.Active)
            {
                UserInterface.Instance.Deactivate();
            }
            else
            {
                UserInterface.Instance.Activate();
            }
        }

        if (previewing && !UserInterface.Instance.Active)
        {
            ShowPreview();
            if (Input.GetMouseButtonDown(1))
            {
                if (currentPreview != null && currentPreview.Buildable)
                {
                    Place();
                }
            }

            if (Input.mouseScrollDelta.y != 0)
            {
                Rotate();
            }
        }
    }

    private void InstantiatePreview()
    {
        if (currentPreviewGameObject != null)
        {
            GameObject temp = currentPreviewGameObject;
            Destroy(temp);
        }

        currentPreviewGameObject = Instantiate(currentlyPreviewedComponent.PreviewPrefab);
        currentPreview = currentPreviewGameObject.GetComponentInChildren<Preview>();
        previewing = true;
    }

    private void Rotate()
    {
        if (currentPreviewGameObject == null)
        {
            return;
        }

        currentPreviewGameObject.transform.Rotate(Vector3.up, 90f * Input.mouseScrollDelta.y);
    }

    public void SwitchToIndex(int _index)
    {
        currentlyPreviewedComponent = BuildingComponents[_index];
        InstantiatePreview();
    }

    private void ShowPreview()
    {
        if (Physics.Raycast(CharacterCamera.ScreenPointToRay(Input.mousePosition), out raycastHit, float.MaxValue, BuildableLayers,
        QueryTriggerInteraction.Ignore))
        {
            currentPosition = raycastHit.point;
            UpdatePreview();
        }
    }

    private void UpdatePreview()
    {
        if (!AllowSnapping || !snapping || objectToSnap == null)
        {
            currentPosition -= Vector3.one * GridOffset;
            currentPosition /= GridSize;
            currentPosition = new Vector3(Mathf.Round(currentPosition.x), Mathf.Round(currentPosition.y), Mathf.Round(currentPosition.z));
            currentPosition *= GridSize;
            currentPosition += Vector3.one * GridOffset;
        }
        else if (objectToSnap.tag == "Wall" || objectToSnap.tag == "Door")
        {
            MeshCollider objectCollider = objectToSnap.GetComponent<MeshCollider>();
            Vector3[] position = new Vector3[1];

            position[0] = objectCollider.bounds.center + snappingOffset;

            //DEBUG
            if (!debugcreated1)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position[0];
                sphere.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                debugcreated1 = true;
            }
            //END DEBUG

            FindBestSnap(position);
        }
        else
        {
            MeshCollider objectCollider = objectToSnap.GetComponent<MeshCollider>();
            Vector3[] position = new Vector3[4];
            position[0] = new Vector3((objectCollider.bounds.center.x + objectCollider.bounds.size.x / 2f),
                            objectCollider.bounds.center.y, objectCollider.bounds.center.z) + snappingOffset;

            position[1] = new Vector3((objectCollider.bounds.center.x - objectCollider.bounds.size.x / 2f),
                            objectCollider.bounds.center.y, objectCollider.bounds.center.z) + snappingOffset;

            position[2] = new Vector3(objectCollider.bounds.center.x, objectCollider.bounds.center.y,
                            (objectCollider.bounds.center.z + objectCollider.bounds.size.z / 2f)) + snappingOffset;

            position[3] = new Vector3(objectCollider.bounds.center.x, objectCollider.bounds.center.y,
                            (objectCollider.bounds.center.z - objectCollider.bounds.size.z / 2f)) + snappingOffset;

            //DEBUG STUFF
            if (!debugcreated2)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position[0];
                sphere.GetComponent<Renderer>().material.SetColor("_Color", Color.red);

                GameObject sphere1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere1.transform.position = position[1];
                sphere1.GetComponent<Renderer>().material.SetColor("_Color", Color.red);

                GameObject sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere2.transform.position = position[2];
                sphere2.GetComponent<Renderer>().material.SetColor("_Color", Color.red);

                GameObject sphere3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere3.transform.position = position[3];
                sphere3.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                debugcreated2 = true;
            }
            //END OF DSEBUG STUFF

            FindBestSnap(position);
        }
        currentPreviewGameObject.transform.position = currentPosition;
    }


    private void Place()
    {
        previewing = false;
        GameObject instanctiated = Instantiate(currentlyPreviewedComponent.BuildingElementPrefab, currentPosition, currentPreviewGameObject.transform.rotation);
        instanctiated.GetComponentInChildren<Rigidbody>().isKinematic = true;
        objectToSnap = null;
        snapping = false;

        if (currentPreviewGameObject != null)
        {
            GameObject temp = currentPreviewGameObject;
            Destroy(temp);
        }
    }

    private void FindBestSnap(Vector3[] positions)
    {
        Vector3 updatedPosition = currentPosition;
        float minDistance = float.MaxValue;
        int index = 0;

        for (int i = 0; i < positions.Length; i++)
        {
            if (Vector3.Distance(currentPosition, positions[i]) < minDistance)
            {
                minDistance = Vector3.Distance(currentPosition, positions[i]);
                updatedPosition = positions[i];
                index = i;
            }
        }

        switch (index)
        {
            case 0:
                currentPreviewGameObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            case 1:
                currentPreviewGameObject.transform.rotation = Quaternion.Euler(0f, -180f, 0f);
                break;

            case 2:
                currentPreviewGameObject.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                break;

            case 3:
                currentPreviewGameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                break;
        }

        updatedPosition += new Vector3(0f, heightOffset, 0f);
        currentPosition = (Vector3.Distance(currentPosition, updatedPosition) <= SnappingDistance) ? updatedPosition : currentPosition;
    }
}
