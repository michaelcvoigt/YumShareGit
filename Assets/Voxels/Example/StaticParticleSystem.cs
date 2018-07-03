using UnityEngine;
using System;


// Component to store existing particles and reload them in every frame into the system
[ExecuteInEditMode]
public class StaticParticleSystem : MonoBehaviour
{
    [Serializable]
    public struct Particle
    {
        public float angularVelocity;
        public Vector3 axisOfRotation;
        public Color32 color;
        public float lifetime;
        public Vector3 position;
        public float rotation;
        public float size;
        public float startLifetime;
        public Vector3 velocity;
    }

    [SerializeField]
    protected Particle[] intrinsicParticles;
    protected ParticleSystem.Particle[] particles;

    public ParticleSystem.Particle[] Particles
    {
        get
        {
            return particles;
        }
    }

    // Process component once per frame
    protected virtual void Update()
    {
        if (particles == null)
        {
            if (intrinsicParticles != null && intrinsicParticles.Length != 0)
            {
                particles = new ParticleSystem.Particle[intrinsicParticles.Length];
                if (particles != null && particles.Length == intrinsicParticles.Length)
                {
                    for (int position = 0; position < intrinsicParticles.Length; ++position)
                    {
                        particles[position].angularVelocity = intrinsicParticles[position].angularVelocity;
                        particles[position].axisOfRotation = intrinsicParticles[position].axisOfRotation;
                        particles[position].startColor = intrinsicParticles[position].color;
                        particles[position].remainingLifetime = intrinsicParticles[position].lifetime;
                        particles[position].position = intrinsicParticles[position].position;
                        particles[position].rotation = intrinsicParticles[position].rotation;
                        particles[position].startSize = intrinsicParticles[position].size;
                        particles[position].startLifetime = intrinsicParticles[position].startLifetime;
                        particles[position].velocity = intrinsicParticles[position].velocity;
                    }
                }
            }
            else
            {
                ParticleSystem particleSystem = GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    particles = new ParticleSystem.Particle[particleSystem.particleCount];
                    if (particles != null)
                    {
                        particleSystem.GetParticles(particles);
                    }
                }
            }
        }

        if (particles != null && particles.Length != 0)
        {
            ParticleSystem particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                UnityEngine.ParticleSystem.EmissionModule emission = particleSystem.emission;

                emission.enabled = false;
                particleSystem.Stop();
                particleSystem.SetParticles(particles, particles.Length);

                particleSystem.startSize = 100.0f;
            }

            if (intrinsicParticles == null || intrinsicParticles.Length == 0 )
            {
                intrinsicParticles = new Particle[particles.Length];
                if (intrinsicParticles != null && intrinsicParticles.Length == particles.Length)
                {
                    for (int position = 0; position < particles.Length; ++position)
                    {
                        intrinsicParticles[position].angularVelocity = particles[position].angularVelocity;
                        intrinsicParticles[position].axisOfRotation = particles[position].axisOfRotation;
                        intrinsicParticles[position].color = particles[position].startColor;
                        intrinsicParticles[position].lifetime = particles[position].remainingLifetime;
                        intrinsicParticles[position].position = particles[position].position;
                        intrinsicParticles[position].rotation = particles[position].rotation;
                        intrinsicParticles[position].size = particles[position].startSize;
                        intrinsicParticles[position].startLifetime = particles[position].startLifetime;
                        intrinsicParticles[position].velocity = particles[position].velocity;
                    }
                }
            }
        }
    }
}
