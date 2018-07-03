using System;
using UnityEngine;
#if PLATFORM_IOS
using UnityEngine.iOS;
using UnityEngine.Apple.ReplayKit;
using UnityEngine.XR.iOS;
using UnityEngine.UI;
public class Replay : MonoBehaviour
{
    public GameObject MainButtons;
    public GameObject ShareButtons;
    public GameObject StopIcon;
    public GameObject RecordIcon;
    public UnityARGeneratePlane MyUnityARGeneratePlane;

    void Start(){
    }
    void Update()
    {
        if (ReplayKit.recordingAvailable)
        {
            ShareButtons.SetActive(true);
            MainButtons.SetActive(false);
        }else{
            ShareButtons.SetActive(false);
            MainButtons.SetActive(true);
        }

    }

    public void Review()
    {
        if (ReplayKit.recordingAvailable)
        {
            ReplayKit.Preview();
            MyUnityARGeneratePlane.ShowPlanes();
        }
    }
    public void Discard()
    {

        if (ReplayKit.recordingAvailable)
        {

            ReplayKit.Discard();
            MyUnityARGeneratePlane.ShowPlanes();

        }
    }
    public void StartStop()
    {

        if (!ReplayKit.APIAvailable)
        {
            return;
        }
        var recording = ReplayKit.isRecording;
        try
        {
            recording = !recording;
            if (recording)
            {
                ReplayKit.StartRecording();
                StopIcon.SetActive(true);
                RecordIcon.SetActive(false);
                MyUnityARGeneratePlane.HidePlanes();
                
            }
            else
            {
                ReplayKit.StopRecording();
                StopIcon.SetActive(false);
                RecordIcon.SetActive(true);
                MyUnityARGeneratePlane.ShowPlanes();
            }
        }
        catch (Exception e)
        {
            //lastError = e.ToString();
        }

    }    
}
#endif