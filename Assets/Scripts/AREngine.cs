using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;
using System.Linq;
using UnityEngine.SceneManagement;

public class AREngine : MonoBehaviour
{
    public string StartMessage = "Tap near a yellow plus ...";
    public UnityARCamera MyARCamera;
    public UnityARVideo MyUnityARVideo;
    public ParticleSystem ParticleSystem;
    public PointCloudParticleExample MyPointCloud;
    public Text Message;
    public Text Logger;
    public GameObject ScreenBlocker;
    public GameObject BottomButtons;
    public GameObject TopButtons;
    public GameObject ModalButtons;
    public MalbersAnimations.Selector.SelectorController MySelectorController;
    public MalbersAnimations.Selector.SelectorManager MySelectorManager;
    public GameObject PartSelector;
    public GameObject Items;
    public MalbersAnimations.Selector.SelectorEditor MySelector;
    public GameObject SelectedMesh;
    public float MinScale = 0.15f;
    public float MaxScale = 3.00f;
    public float SelectionButtonScale = 175.0f;
    public GameObject HelpScreen;
    public GameObject AboutScreen;
    public Vector3 ObjectCreationScale = new Vector3(.5f, .5f, .5f);
    public GameObject CursorScale;
    public string LastTextString = "Yummy!";

    //private 
    private string updateMessage = "";
    private GameObject[] floatingListArray;
    private List<GameObject> floatingList;
    private GameObject MovingFloatObject;
    private bool TappedCloudPoint = false;
    private bool AddedAndReadyToScale = false;
    private GameObject SelectionMesh;
    private bool mySelectorOpen = false;
    private bool touchedScreen = false;
    private float PrevDisV;
    private float PrevScaleV;
    private List<GameObject> undoObjects;
    private ARPoint point;
    private Vector3 position;
    private float maxRayDistance = 30.0f;
    private LayerMask collisionLayer = 1 << 10;  //ARKitPlane layer
    private List<ARHitTestResult> hitResults;
    private Vector3 newScale = Vector3.one;
    public static AREngine instance;
    private bool checkForCameraPermission = true;

    void Awake()
    {
        instance = this;
    }

    // Use this for initialization
    void Start()
    {
        UnityARSessionNativeInterface.ARSessionShouldAttemptRelocalization = true;

        // user finished the intro screens : 
        PlayerPrefs.SetInt("LoadedMain_version_1.5", 1);

        UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;

        Message.text = "Look around with camera and tap on yellow plus...";
        Help(false);
        About(false);
        undoObjects = new List<GameObject>();
        PartSelector.SetActive(false);
        CursorScale.SetActive(false);
        ModalButtons.SetActive(false);

        floatingListArray = Resources.LoadAll<GameObject>("Floating");
        floatingList = floatingListArray.ToList();

        // set selected item to first

        Destroy(SelectionMesh);
        SelectionMesh = Instantiate(floatingList[0], new Vector3(0, 0, 0), Quaternion.identity);
        SelectionMesh.transform.parent = SelectedMesh.transform;

        SetupSelectionObject(SelectionMesh, SelectionButtonScale);

        foreach (GameObject item in floatingListArray)
        {
            GameObject NewObject = Instantiate(item, new Vector3(0, 0, 0), Quaternion.identity);
            NewObject.transform.parent = Items.transform;

            SetupSelectionObject(NewObject, 3.75f);

            foreach (Animation animation in NewObject.GetComponentsInChildren(typeof(Animation)))
            {
                animation.playAutomatically = false;
            }

            MySelector.UpdateItemsList();

            MalbersAnimations.Selector.MItem NewMItem = NewObject.GetComponent<MalbersAnimations.Selector.MItem>();

            NewMItem.originalItem = item;

            // set selected item to the first one 
            if (!MySelectorController.SelectedGameObject)
            {
                MySelectorController.SelectedGameObject = NewMItem.originalItem;
            }
        }

    }
    public void Help(bool Active)
    {
        Message.text = "";
        HelpScreen.SetActive(Active);

    }
    public void About(bool Active)
    {
        Message.text = "";
        AboutScreen.SetActive(Active);

    }
    public void LinkToHTML()
    {
        Application.OpenURL("https://www.linkedin.com/in/michaelvoigt/");
    }
    public void SetupSelectionObject(GameObject myObject, float myScaleFactor)
    {

        SelectorCustomTransform custom = myObject.GetComponent<SelectorCustomTransform>();

        if (custom)
        {

            float customScale = myScaleFactor * custom.CustomScaleForSelector;
            myObject.transform.localScale = new Vector3(customScale, customScale, customScale);
        }
        else
        {
            myObject.transform.localScale = new Vector3(myScaleFactor, myScaleFactor, myScaleFactor);
        }

        foreach (StuffToDestroyForSelector script in myObject.GetComponents(typeof(StuffToDestroyForSelector)))
        {
            script.DestroyStuff();
        }

        foreach (MonoBehaviour monoBehaviour in myObject.GetComponentsInChildren(typeof(MonoBehaviour)))
        {
            monoBehaviour.enabled = false;
        }

        myObject.transform.localPosition = new Vector3(0, 0, 0);
        myObject.transform.eulerAngles = new Vector3(0, 180, 0);
        myObject.SetLayer(5, true);

    }
    public void SetSelection()
    {
        PartSelector.SetActive(false);
        mySelectorOpen = false;
        BottomButtons.SetActive(true);

        if (MySelectorController.SelectedGameObject)
        {
            Destroy(SelectionMesh);

            SelectionMesh = Instantiate(MySelectorController.SelectedGameObject, new Vector3(0, 0, 0), Quaternion.identity);

            SelectionMesh.transform.parent = SelectedMesh.transform;
            Debug.Log("selected");
            SetupSelectionObject(SelectionMesh, SelectionButtonScale);

            foreach (Animation animation in SelectionMesh.GetComponentsInChildren(typeof(Animation)))
            {
                animation.enabled = false;
                animation.playAutomatically = false;
            }

        }
        MySelectorManager.EnableSelector = false;

    }

    public void ShowSelector()
    {
        Debug.Log("Show Selector");

        MySelectorManager.ToggleSelector();

        mySelectorOpen = MySelectorManager.isActiveAndEnabled;
        PartSelector.SetActive(mySelectorOpen);

        if (MySelectorManager)
        {
            BottomButtons.SetActive(true);
        }
        else
        {
            BottomButtons.SetActive(false);
        }
        Message.text = "";
    }

    public void MoveObject(Vector3 newPosition, GameObject objectToMove)
    {
        objectToMove.transform.localPosition = new Vector3(newPosition.x, newPosition.y, newPosition.z);

    }

    public void TouchedScreen(bool start)
    {
        Debug.Log("tapped background = " + start);
        if (ScreenBlocker.active == true) ScreenBlocker.SetActive(false);
        touchedScreen = start;
    }

    public void CheckRotate(GameObject scalingObject)
    {

        // Store both touches.
        Touch touchZero = Input.GetTouch(0);
        // Find the position in the previous frame of each touch.
        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;

        Debug.Log("Rotate function = " + touchZeroPrevPos.x);

        scalingObject.transform.localRotation = Quaternion.Euler(0f, scalingObject.transform.localRotation.y + touchZeroPrevPos.x, 0f);

        Debug.Log("Rotate function value = " + scalingObject.transform.localRotation);
    }

    public void CheckLocation(GameObject scalingObject)
    {
        Debug.Log("CheckedLocation");

        Touch touchZero = Input.GetTouch(0);

        float originalYValue = scalingObject.transform.localPosition.y;

        Vector3 newLocation = scalingObject.transform.localPosition + (Camera.main.transform.forward * (touchZero.deltaPosition.y * .00075f));

        newLocation = newLocation + (Camera.main.transform.right * (touchZero.deltaPosition.x * .00075f));

        scalingObject.transform.localPosition = new Vector3(newLocation.x, originalYValue, newLocation.z);

    }

    public void CheckScale(GameObject scalingObject)
    {
        // Store both touches.
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        // Find the position in the previous frame of each touch.
        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        // Find the magnitude of the vector (the distance) between the touches in each frame.
        float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

        // Find the difference in the distances between each frame.
        float deltaMagnitudeDiff = (prevTouchDeltaMag - touchDeltaMag) * .001f;


        float NewX = Mathf.Abs(scalingObject.transform.localScale.x - deltaMagnitudeDiff);
        float NewY = Mathf.Abs(scalingObject.transform.localScale.y - deltaMagnitudeDiff);
        float NewZ = Mathf.Abs(scalingObject.transform.localScale.z - deltaMagnitudeDiff);

        //scale
        Vector3 newScale = new Vector3(Mathf.Clamp(NewX, MinScale, MaxScale), Mathf.Clamp(NewY, MinScale, MaxScale), Mathf.Clamp(NewZ, MinScale, MaxScale));

        scalingObject.transform.localScale = newScale;

    }
    public void CancelEdit()
    {
        Message.text = "Object removed, tap to add more...";
        Undo();
        FinishedEditing();
    }


    public void StartEditing()
    {

        Renderer MyRenderer = MovingFloatObject.GetComponentInChildren<Renderer>();
        MovingObjectScript MyScript = MyRenderer.gameObject.AddComponent<MovingObjectScript>();

        Debug.Log(MyScript);

        CursorScale.SetActive(true);
        CursorScale.transform.localPosition = MovingFloatObject.transform.localPosition;

        BottomButtons.SetActive(false);
        TopButtons.SetActive(false);
        ModalButtons.SetActive(true);

        MyPointCloud.Hide(true);

        CursorScale.SetActive(true);
        CursorScale.transform.localPosition = MovingFloatObject.transform.localPosition;
    }
    public void FinishedEditing()
    {
        MovingObjectScript MYMovingObjectScript = MovingFloatObject.GetComponentInChildren<MovingObjectScript>();

        if (MYMovingObjectScript) Destroy(MYMovingObjectScript);

        AddedAndReadyToScale = false;
        CursorScale.SetActive(false);

        BottomButtons.SetActive(true);
        TopButtons.SetActive(true);
        ModalButtons.SetActive(false);

        MovingFloatObject = null;

        MyPointCloud.Hide(false);

        Message.text = "Tap near a yellow plus to add more stuff";
    }

    void Update()
    {
        if (checkForCameraPermission)
        {

            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                ScreenBlocker.SetActive(true);
                Message.text = "You didn't allow this App to use your camera, AR requires a camera. Please allow camera usage from settings.";
            }
            else
            {
                ScreenBlocker.SetActive(false);
                Message.text = StartMessage;
                checkForCameraPermission = false;
            }
        }

        //CheckLocation(Test);
        // if two fingers scale that object
        if (Input.touchCount > 1 && AddedAndReadyToScale && MovingFloatObject)
        {
            Message.text = "Tap to move, pinch to scale and tap Ok.";
            CheckScale(MovingFloatObject);
            CursorScale.transform.localPosition = MovingFloatObject.transform.localPosition;
            return;
        }

        if (Input.touchCount > 0 && AddedAndReadyToScale && MovingFloatObject)
        {
            Message.text = "Tap to move, pinch to scale and tap Ok.";
            CheckLocation(MovingFloatObject);
            CursorScale.transform.localPosition = MovingFloatObject.transform.localPosition;
            return;
        }

    }

    public void ARFrameUpdated(UnityARCamera camera)
    {
        if (AddedAndReadyToScale)
        {
            return;
        }

        // cast a ray to see if you tapped on an existing items, if so make that the current item to move around
        if (Input.touchCount > 0 && touchedScreen && !mySelectorOpen && MovingFloatObject == null && !TappedCloudPoint)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxRayDistance))
            {

                if (hit.collider.name != "plane" && hit.collider.name != "Plane")
                {
                    Message.text = "Tap to move, pinch to scale and tap Ok.";
                    AddedAndReadyToScale = true;
                    MovingFloatObject = hit.transform.gameObject;
                    StartEditing();
                    return;
                }
            }
        }

        // did we just finish adding a object, if so listen for a new tap 
        // this is to miss the tap on the okay button, I might be able to loose this 
        if (TappedCloudPoint && !MovingFloatObject)
        {
            TappedCloudPoint = false;
            return;
        }

        // didn't tap on anything
        // the touch code to create and move that object
        if (Input.touchCount > 0 && touchedScreen && !mySelectorOpen && MovingFloatObject == null && !TappedCloudPoint)
        {
            var touch = Input.GetTouch(0);
            //var touchFinger2 = Input.GetTouch(1);

            var screenPosition = Camera.main.ScreenToViewportPoint(touch.position);
            point = new ARPoint
            {
                x = screenPosition.x,
                y = screenPosition.y
            };

            // prioritize reults types
            // ARHitTestResultType[] resultTypes = {
            //ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingGeometry,
            //ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent, 
            // if you want to use infinite planes use this:
            //ARHitTestResultType.ARHitTestResultTypeExistingPlane,
            //ARHitTestResultType.ARHitTestResultTypeEstimatedHorizontalPlane, 
            //ARHitTestResultType.ARHitTestResultTypeEstimatedVerticalPlane, 
            //ARHitTestResultType.ARHitTestResultTypeFeaturePoint
            //}; 

            //hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface ().HitTest(point, resultTypes);

            // first try to mount to a feature point
            hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point, ARHitTestResultType.ARHitTestResultTypeFeaturePoint);

            // if no points use the plane that was found
            //if (hitResults.Count < 1)
            //{
            //hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point,ARHitTestResultType.ARHitTestResultTypeExistingPlane);
            //}

            //Debug.Log(hitResults.Count);

            if (hitResults.Count > 0)
            {
                TappedCloudPoint = true;
                ScreenBlocker.SetActive(false);

                foreach (var hitResult in hitResults)
                {
                    position = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
                    break;
                }

                if (MovingFloatObject == null)
                {
                    // create the object, there isn't one 
                    AddFloatObject(position);
                    Message.text = "Created Object, pinch to scale... Tap Ok.";
                    AddedAndReadyToScale = true;

                    StartEditing();

                }

                ScreenBlocker.SetActive(false);

            }

        }

        Logger.text = updateMessage;
    }

    public void AddFloatObject(Vector3 currentPositon)
    {

        GameObject partToBuild;

        if (MySelectorController.SelectedGameObject)
        {
            partToBuild = MySelectorController.SelectedGameObject;

            GameObject NewObject = Instantiate(partToBuild, new Vector3(0, 0, 0), Quaternion.identity);

            // ask for text if this item is a text object 
            // do this before we rotate
            CreateFont MyCreateFont = NewObject.GetComponentInChildren<CreateFont>();

            if (MyCreateFont)
            {
                MyCreateFont.EnterText();
            }

            // destroy proxy objectss
            GameObject[] DestroyInAr = null;

            if (DestroyInAr == null)
            {
                DestroyInAr = GameObject.FindGameObjectsWithTag("DestroyInAr");
            }

            foreach (GameObject destroyInAr in DestroyInAr)
            {
                Destroy(destroyInAr);
            }

            // place in world
            NewObject.transform.localPosition = new Vector3(currentPositon.x, currentPositon.y, currentPositon.z);
            NewObject.transform.localScale = ObjectCreationScale;
            MovingFloatObject = NewObject;
            undoObjects.Add(NewObject);

        }
    }
    public void Undo()
    {
        Debug.Log("Undo Pressed");
        if (undoObjects.Count > 0)
        {
            Destroy(undoObjects[undoObjects.Count - 1]);
            undoObjects.RemoveAt(undoObjects.Count - 1);
        }
    }

    public void DeleteAll()
    {
        Debug.Log("DeleteAll Pressed");
        Message.text = "Removed all objects, tap to add more...";

        for (int count = 0; count < undoObjects.Count; count++)
        {
            Destroy(undoObjects[count]);
        }
        undoObjects.Clear();

        //updateMessage = "Count :"+undoObjects.Count;
    }
    public void GetHelp()
    {
        SceneManager.LoadScene(0);
    }

}
