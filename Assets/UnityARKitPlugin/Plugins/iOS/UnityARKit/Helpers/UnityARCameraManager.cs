using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;

public class UnityARCameraManager : MonoBehaviour
{

    public Camera m_camera;
    public GameObject HideIcon;
    public GameObject ShowIcon;
	public Light FillLight;
	public Text Message;
    private UnityARSessionNativeInterface m_session;
    private Material savedClearMaterial;

    [Header("AR Config Options")]
    public UnityARAlignment startAlignment = UnityARAlignment.UnityARAlignmentGravity;
    public UnityARPlaneDetection planeDetection = UnityARPlaneDetection.Horizontal;
    public ARReferenceImagesSet detectionImages = null;
    public bool getPointCloud = true;
    public bool enableLightEstimation = true;
    public bool enableAutoFocus = true;
    private bool sessionStarted = false;
    private ARKitWorldTrackingSessionConfiguration config;

    // Use this for initialization
    void Start()
    {
        m_session = UnityARSessionNativeInterface.GetARSessionNativeInterface();

        Application.targetFrameRate = 60;
        config = new ARKitWorldTrackingSessionConfiguration();
        config.planeDetection = planeDetection;
        config.alignment = startAlignment;
        config.getPointCloudData = getPointCloud;
        config.enableLightEstimation = enableLightEstimation;
        config.enableAutoFocus = enableAutoFocus;

        if (detectionImages != null)
        {
            config.arResourceGroupName = detectionImages.resourceGroupName;
        }

        if (config.IsSupported)
        {
            m_session.RunWithConfig(config);
            UnityARSessionNativeInterface.ARFrameUpdatedEvent += FirstFrameUpdate;
        }

        if (m_camera == null)
        {
            m_camera = Camera.main;
        }
    }
    public void ToggleLighting()
    {
        Debug.Log("Lighting Pressed");
        config.enableLightEstimation = !config.enableLightEstimation;

		if (config.enableLightEstimation)
        {
            Message.text = "Natural lighting enabled! \r\n Tap again to change.";
            HideIcon.SetActive(true);
            ShowIcon.SetActive(false);
			FillLight.gameObject.SetActive(false);
        }else
        {
            Message.text = "Natural lighting disabled! \r\n Tap again to change.";
            HideIcon.SetActive(false);
            ShowIcon.SetActive(true);
			FillLight.gameObject.SetActive(true);
        }
    }
    void FirstFrameUpdate(UnityARCamera cam)
    {
        sessionStarted = true;
        UnityARSessionNativeInterface.ARFrameUpdatedEvent -= FirstFrameUpdate;
    }

    public void SetCamera(Camera newCamera)
    {
        if (m_camera != null)
        {
            UnityARVideo oldARVideo = m_camera.gameObject.GetComponent<UnityARVideo>();
            if (oldARVideo != null)
            {
                savedClearMaterial = oldARVideo.m_ClearMaterial;
                Destroy(oldARVideo);
            }
        }
        SetupNewCamera(newCamera);
    }

    private void SetupNewCamera(Camera newCamera)
    {
        m_camera = newCamera;

        if (m_camera != null)
        {
            UnityARVideo unityARVideo = m_camera.gameObject.GetComponent<UnityARVideo>();
            if (unityARVideo != null)
            {
                savedClearMaterial = unityARVideo.m_ClearMaterial;
                Destroy(unityARVideo);
            }
            unityARVideo = m_camera.gameObject.AddComponent<UnityARVideo>();
            unityARVideo.m_ClearMaterial = savedClearMaterial;
        }
    }

    // Update is called once per frame

    void Update()
    {

        if (m_camera != null && sessionStarted)
        {
            // JUST WORKS!
            Matrix4x4 matrix = m_session.GetCameraPose();
            m_camera.transform.localPosition = UnityARMatrixOps.GetPosition(matrix);
            m_camera.transform.localRotation = UnityARMatrixOps.GetRotation(matrix);

            m_camera.projectionMatrix = m_session.GetCameraProjection();
        }

    }

}
