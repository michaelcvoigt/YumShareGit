/* 
*   NatCorder
*   Copyright (c) 2018 Yusuf Olokoba
*/

namespace NatCorderU.Examples
{

    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using Core;
    using NatShareU;
    using UnityEngine.XR.iOS;

    public class ReplayCam : MonoBehaviour
    {

        /**
        * ReplayCam Example
        * -----------------
        * This example records the screen using the high-level `Replay` API
        * We simply call `Replay.StartRecording` to start recording, and `Replay.StopRecording` to stop recording
        * When we want mic audio, we play the mic to an AudioSource and pass the audio source to `Replay.StartRecording`
        * -----------------
        * Note that UI canvases in Overlay mode cannot be recorded, so we use a different mode (this is a Unity issue)
        */
        public ParticleSystem Particles;
        public PointCloudParticleExample MyPointCloud;
        public GameObject MainButtons;
        public GameObject TopButtons;
        public GameObject StopIcon;
        public GameObject RecordIcon;
        public GameObject UI;
        public Camera MyCamera;
        public bool recordMicrophoneAudio;
        private AudioSource audioSource;
        public AudioListener MyAudioListener;
        public GameObject RecordFade;
        public GameObject ScreenBlocker;
        public Text Message;
        private float videoStartTime;
        private float duration = 0f;

        void Start()
        {
            RecordFade.SetActive(false);

            audioSource = gameObject.AddComponent<AudioSource>();
            //audioSource.clip = Resources.Load(name) as AudioClip;

            Debug.Log(audioSource);
        }

        void Update()
        {
            if (Replay.IsRecording)
            {
                duration = Time.time - videoStartTime;
                Message.text = "Recording Video \r\n" + duration.ToString("F2") + " - Seconds \r\n Lift finger to stop.";
            }
        }
        public void StartRecording()
        {
            Debug.Log("Started Recording function called ");
            Debug.Log("Replay.IsRecording = " + Replay.IsRecording);
            if (!Replay.IsRecording)
            {
                duration = 0f;
                ScreenBlocker.SetActive(false);

                StopIcon.SetActive(true);
                RecordIcon.SetActive(false);
                MainButtons.SetActive(false);
                TopButtons.SetActive(false);
                UI.SetActive(false);
                RecordFade.SetActive(true);

                MyPointCloud.Hide(true);
                MyPointCloud.Pause(true);

                Debug.Log("Started Recording");

                // Create a recording configuration
                const float DownscaleFactor = 2f / 3;
                var configuration = new Configuration((int)(Screen.width * DownscaleFactor), (int)(Screen.height * DownscaleFactor));

                // Start recording with microphone audio
                if (recordMicrophoneAudio)
                {
                    if (audioSource.clip == null)
                    {
                        audioSource.clip = Microphone.Start(Microphone.devices[0], true, 1, 44100);
                        audioSource.loop = true;
                        audioSource.Play();
                    }

                    Debug.Log(audioSource.clip);
                    Replay.StartRecording(MyCamera, configuration, OnReplay, audioSource);

                }
                // Start recording without microphone audio
                else
                {
                    Replay.StartRecording(MyCamera, configuration, OnReplay);
                }

                videoStartTime = Time.time;

            }
        }

        private void StartMicrophone()
        {
#if !UNITY_WEBGL || UNITY_EDITOR // No `Microphone` API on WebGL :(
            // If the clip has not been set, set it now
            //if (audioSource.clip == null)
            {
                // audioSource.clip = Microphone.Start(null, true, 60, 48000);
                // while (Microphone.GetPosition(null) <= 0) ;
            }
            // Play through audio source
            //audioSource.timeSamples = Microphone.GetPosition(null);
            //audioSource.loop = true;
            //audioSource.Play();

#endif
        }

        public void StopRecording()
        {
            if (Replay.IsRecording)
            {

                ScreenBlocker.SetActive(true);
                StopIcon.SetActive(false);
                RecordIcon.SetActive(true);
                MainButtons.SetActive(true);
                TopButtons.SetActive(true);
                UI.SetActive(true);
                RecordFade.SetActive(false);

                MyPointCloud.Hide(false);
                MyPointCloud.Pause(false);

                Debug.Log("Stopped Recording");

                if (recordMicrophoneAudio) audioSource.Stop();

                Replay.StopRecording();

            }
        }
        void OnVideo(string path)
        {
            //Debug.Log("Share recording to: " + path);
            //NatShare.Share(path);
            //NatShare.SaveToCameraRoll(path);
        }

        void OnReplay(string path)
        {
            if (!Replay.IsRecording)
            {

                duration = Time.time - videoStartTime;
                // Share the video

                if (duration > 3.0f)
                {
                    NatShare.Share(path);
                    Debug.Log("Saved recording to: " + path + " Time Length = " + duration);
                    ScreenBlocker.SetActive(false);
                }
                else
                {
                    Message.text = "Video too short! \r\n Tap and hold longer please.";
                    Debug.Log("Video too short =" + duration);
                }

                //Handheld.PlayFullScreenMovie("file://" + path);
                //NatShare.SaveToCameraRoll(path);

            }

        }
    }
}