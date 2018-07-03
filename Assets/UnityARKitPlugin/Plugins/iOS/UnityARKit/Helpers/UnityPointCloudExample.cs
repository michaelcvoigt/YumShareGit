using System;
using UnityEngine;
using UnityEngine.XR.iOS;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class UnityPointCloudExample : MonoBehaviour
{
    public uint numPointsToShow = 100;
    public GameObject PointCloudPrefab = null;
    public GameObject MyParent;
    private List<GameObject> pointCloudObjects;
    private Vector3[] m_PointCloudData;
    private bool ShowPoints = true;

    public void Start()
    {
        UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;

        if (PointCloudPrefab != null)
        {
            pointCloudObjects = new List<GameObject>();
            for (int i = 0; i < numPointsToShow; i++)
            {
                pointCloudObjects.Add(Instantiate(PointCloudPrefab));
                pointCloudObjects[i].gameObject.SetLayer(5);
                //pointCloudObjects[i].transform.parent =  MyParent.transform;
            }
        }
    }

    public void ARFrameUpdated(UnityARCamera camera)
    {
        m_PointCloudData = camera.pointCloudData;

        if (PointCloudPrefab != null && m_PointCloudData != null)
        {
            for (int count = 0; count < Math.Min(m_PointCloudData.Length, numPointsToShow); count++)
            {
                Vector4 vert = m_PointCloudData[count];
                GameObject point = pointCloudObjects[count];

                //point.SetActive(ShowPoints);
                point.transform.position = new Vector3(vert.x, vert.y, vert.z);
            }
        }

    }
    public void Show(bool isActive)
    {
        ShowPoints = isActive;
    }
}