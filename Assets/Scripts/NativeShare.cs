using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;

/*
 * https://github.com/ChrisMaire/unity-native-sharing 
 */

public class NativeShare : MonoBehaviour
{
    public string ScreenshotName = "screenshot.png";
    public GameObject BlockScreen;
    public Text Message;
    public Camera MyCamera;
    public ParticleSystem Particles;
    public PointCloudParticleExample MyPointCloud;

    public void ShareScreenshotWithText()
    {
        MyPointCloud.Hide(true);

        //todo: change this to listen to when the image has saved, not hardcoded to 3 seconds.
        string screenShotPath = Application.persistentDataPath + "/" + ScreenshotName;

        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        MyCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        MyCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);

        MyCamera.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();

        System.IO.File.WriteAllBytes(screenShotPath, bytes);

        Debug.Log(string.Format("Took screenshot to: {0}", screenShotPath));


        StartCoroutine(WaitForAJiffy());
        StartCoroutine(WaitForSomeTime());
    }

    IEnumerator WaitForAJiffy()
    {

        yield return new WaitForSeconds(.5f);
        Message.text = "Saving...";
        BlockScreen.SetActive(true);

    }
    IEnumerator WaitForSomeTime()
    {

        yield return new WaitForSeconds(.5f);

        Message.text = "";
        BlockScreen.SetActive(false);
        string screenShotPath = Application.persistentDataPath + "/" + ScreenshotName;
        Share("YumShare", screenShotPath, "");
        MyPointCloud.IsActive = true;
        MyPointCloud.Hide(false);

    }
    public void Share(string shareText, string imagePath, string url, string subject = "")
    {

#if UNITY_ANDROID
         AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
         AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
         
         intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
         AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
         AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + imagePath);
         intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
         intentObject.Call<AndroidJavaObject>("setType", "image/png");
         
         intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), shareText);
         
         AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
         AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
         
         AndroidJavaObject jChooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, subject);
         currentActivity.Call("startActivity", jChooser);
#elif UNITY_IOS
        CallSocialShareAdvanced(shareText, subject, url, imagePath);
#else
         Debug.Log("No sharing set up for this platform.");
#endif
    }

#if UNITY_IOS
    public struct ConfigStruct
    {
        public string title;
        public string message;
    }

    [DllImport("__Internal")] private static extern void showAlertMessage(ref ConfigStruct conf);

    public struct SocialSharingStruct
    {
        public string text;
        public string url;
        public string image;
        public string subject;
    }

    [DllImport("__Internal")] private static extern void showSocialSharing(ref SocialSharingStruct conf);

    public static void CallSocialShare(string title, string message)
    {
        Debug.Log("Called share");
        ConfigStruct conf = new ConfigStruct();
        //conf.title = title;
        //conf.message = message;
        showAlertMessage(ref conf);
    }

    public static void CallSocialShareAdvanced(string defaultTxt, string subject, string url, string img)
    {
        Debug.Log("Called share advanced");
        SocialSharingStruct conf = new SocialSharingStruct();
        //conf.text = defaultTxt;
        conf.url = url;
        conf.image = img;
        //conf.subject = subject;

        showSocialSharing(ref conf);
    }
#endif
}