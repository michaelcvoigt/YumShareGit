using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;

public class PointCloudParticleExample : MonoBehaviour
{
    public bool IsActive = true;
    public bool Disabled = false;
    public GameObject HideIcon;
    public GameObject ShowIcon;
    public bool Paused = false;
    public ParticleSystem Particles;
    public ParticleSystemRenderer MyParticleRender;
    public int maxPointsToShow;
    public Color ParticleColor;
    public float particleSize = 1.0f;
    public Text Message;
    private Vector3[] m_PointCloudData;
    private bool frameUpdated = false;
    private ParticleSystem currentPS;
    private ParticleSystem.Particle[] particles;

    // Use this for initialization
    void Start()
    {
        UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
        frameUpdated = false;


    }
    public void DisableToggle()
    {
        if (!Disabled)
        {
            Message.text = "Tap points hidden! \r\n Tap again to show.";
            Hide(true);
            Disabled = true;
            HideIcon.SetActive(false);
            ShowIcon.SetActive(true);
        }else
        {
            Message.text = "Tap points shown! \r\n Tap again to hide.";
            Disabled = false;
            Hide(false);
            HideIcon.SetActive(true);
            ShowIcon.SetActive(false);
            
        }
        Debug.Log("disabled = " + Disabled);
    }
    public void Hide(bool hide)
    {
        if (Disabled)
        {
            return;
        }

        Material particleMarterial = MyParticleRender.material;
        if (hide)
        {
            particleMarterial.color = new Color(0f, 0f, 0f, 0f);
        }
        else
        {
            particleMarterial.color = ParticleColor;
        }
    }

    public void Pause(bool pause)
    {
        Paused = pause;
    }

    public void ARFrameUpdated(UnityARCamera camera)
    {
        if (!Paused)
        {

            m_PointCloudData = camera.pointCloudData;
            frameUpdated = true;

            if (frameUpdated)
            {
                if (m_PointCloudData != null && m_PointCloudData.Length > 0 && maxPointsToShow > 0)
                {
                    int numParticles = Mathf.Min(m_PointCloudData.Length, maxPointsToShow);

                    ParticleSystem.Particle[] particles = new ParticleSystem.Particle[numParticles];

                    int index = 0;
                    foreach (Vector3 currentPoint in m_PointCloudData)
                    {
                        particles[index].position = currentPoint;
                        particles[index].startColor = new Color(1.0f, 1.0f, 1.0f);
                        particles[index].startSize = particleSize;
                        index++;
                        if (index >= numParticles) break;
                    }
                    Particles.SetParticles(particles, numParticles);
                }
                else
                {
                    ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1];
                    particles[0].startSize = 0.0f;
                    Particles.SetParticles(particles, 1);
                }
                frameUpdated = false;
            }
        }
    }
}
