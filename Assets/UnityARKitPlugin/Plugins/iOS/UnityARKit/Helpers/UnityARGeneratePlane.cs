using System;
using System.Collections.Generic;

namespace UnityEngine.XR.iOS
{
    public class UnityARGeneratePlane : MonoBehaviour
    {
        public GameObject planePrefab;
        public Camera mainCamera;
        public GameObject MainLight;
        private UnityARAnchorManager unityARAnchorManager;

        // Use this for initialization
        void Start()
        {
            UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
            ShowPlanes();
        }

        public void HidePlanes()
        {
            mainCamera.cullingMask = 1 << 0;
        }

        public void ShowPlanes()
        {

            unityARAnchorManager = new UnityARAnchorManager();
            UnityARUtility.InitializePlanePrefab(planePrefab);
            mainCamera.cullingMask = -1;
        }

        void OnDestroy()
        {
            unityARAnchorManager.Destroy();
        }

        public void ARFrameUpdated(UnityARCamera camera)
        {
            foreach (ARPlaneAnchorGameObject arpag in unityARAnchorManager.GetCurrentPlaneAnchors())
            {

                MainLight.transform.position = arpag.gameObject.transform.position;
            }

        }
    }
}

