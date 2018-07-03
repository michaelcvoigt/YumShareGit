using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Loading : MonoBehaviour
{
    public Renderer RendererToFade;
    public float FadePerSecond = 1.0f;
    private bool tapped = false;
    private bool startFade = false;
    private bool loadingNextScreen = false;

    void Awake(){

        // skip the intro
        if ( PlayerPrefs.GetInt("LoadedMain_version_1.5") == 1 ){

            loadingNextScreen = true;
            SceneManager.LoadScene(5);
        }
    }
    void Start()
    {
        Debug.Log("Started Loading Screen");
    }
    void Update()
    {
        if (!loadingNextScreen)
        {

            if ( (Input.touchCount == 1) && (Input.GetTouch(0).phase == TouchPhase.Began)  || (Input.GetMouseButton(0)) )
            {
                if (!tapped)
                {
                    Debug.Log("Tapped");
                    startFade = true;
                    tapped = true;
                }
            }

            if (startFade)
            {

                var material = RendererToFade.material;
                var color = material.color;

                if (color.a > 0.0f)
                {
                    material.color = new Color(color.r, color.g, color.b, color.a - (FadePerSecond * Time.deltaTime));
                }
                if (color.a <= 0.0f)
                {
                    loadingNextScreen = true;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                    Debug.Log("Loading next scene = " + (SceneManager.GetActiveScene().buildIndex + 1));
                }
            }
        }

    }



}