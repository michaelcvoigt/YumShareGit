//--------------------------------
//
// Voxels for Unity
//  Version: 1.10.3
//
// © 2014-17 by Ronny Burkersroda
//
//--------------------------------


using UnityEngine;


namespace Voxels
{

    // Processor class to convert voxels to particles
    public class ParticleSystem : Processor
    {
        // Template container including a particle system
        public GameObject template;
        // Sizing factor for a particle
        public float sizeFactor = 1;
        // Name of the target container to create
        public string targetName = "Voxel Particle System";

        // Current processing position
        int currentHeight = 0;
        int currentDepth = 0;

        // Offset, local and global scaling vectors
        Vector3 offset;
        Vector3 scaling;
        Vector3 globalScaling;

        // Array of particles
        UnityEngine.ParticleSystem.Particle[] particles;
        int particlesCount;
        // Size of a particle
        float particleSize;

        // Build voxel object
        public override float Build(Storage voxels, Bounds bounds, Informer informer, object parameter)
        {
            // Check for given array
            if (voxels != null)
            {
                int width = voxels.Width;
                int height = voxels.Height;
                int depth = voxels.Depth;
                int sides = voxels.FacesCount;
                int x, y, z;

                // Check for non-empty array
                if (width * height * depth * sides > 0)
                {
                    if (particles == null)
                    {
                        // Calculate total scaling for one block
                        globalScaling = new Vector3(2.0f * bounds.extents.x / (float)width, 2.0f * bounds.extents.y / (float)height, 2.0f * bounds.extents.z / (float)depth);

                        // Check for given template container containing a particle system and its renderer
                        if (template != null && template.GetComponent<UnityEngine.ParticleSystem>() != null && template.GetComponent<Renderer>() != null && template.GetComponent<Renderer>().GetType() == typeof(ParticleSystemRenderer) && ((ParticleSystemRenderer)template.GetComponent<Renderer>()).mesh != null)
                        {
                            // Calculate offset and scaling vector for a particle mesh
                            offset = -((ParticleSystemRenderer)template.GetComponent<Renderer>()).mesh.bounds.center;
                            scaling.x = 0.5f / ((ParticleSystemRenderer)template.GetComponent<Renderer>()).mesh.bounds.extents.x;
                            scaling.y = 0.5f / ((ParticleSystemRenderer)template.GetComponent<Renderer>()).mesh.bounds.extents.y;
                            scaling.z = 0.5f / ((ParticleSystemRenderer)template.GetComponent<Renderer>()).mesh.bounds.extents.z;
                            offset.x *= scaling.x;
                            offset.y *= scaling.y;
                            offset.z *= scaling.z;
                            scaling.x *= globalScaling.x;
                            scaling.y *= globalScaling.y;
                            scaling.z *= globalScaling.z;
                        }
                        else
                        {
                            // Unset translation und scaling
                            offset = Vector3.zero;
                            scaling = Vector3.one;
                        }

                        // Add offset for half voxel
                        offset += new Vector3(0.5f * globalScaling.x, 0.5f * globalScaling.y, 0.5f * globalScaling.z);

                        // Move to match position of the original object
                        offset += bounds.center - gameObject.transform.position - bounds.extents;

                        // Create array to store particles to
                        particles = new UnityEngine.ParticleSystem.Particle[voxels.Count];
                        particlesCount = 0;

                        // Calculate size of one particle
                        particleSize = Mathf.Max(globalScaling.x, Mathf.Max(globalScaling.y, globalScaling.z)) * sizeFactor;
                    }

                    // Check for main container
                    if (particles != null)
                    {
                        // Process all voxels
                        z = currentDepth;
                        //for (z = currentDepth; z < currentDepth + 1; ++z)
                        {
                            y = currentHeight;
                            //for (y = currentHeight; y < currentHeight + 1; ++y)
                            {
                                for (x = 0; x < width; ++x)
                                {
                                    // Retrieve material for current coordinate
                                    Material material = voxels.GetMaterial(x, y, z);

                                    // Check for valid voxel
                                    if (material != null)
                                    {
                                        // Calculate current voxel position
                                        Vector3 currentPosition = new Vector3((float)x * globalScaling.x + offset.x, (float)y * globalScaling.y + offset.y, (float)z * globalScaling.z + offset.z);

                                        // Set particle properties
                                        particles[particlesCount].position = currentPosition;
                                        particles[particlesCount].startColor = material.color;
                                        particles[particlesCount].remainingLifetime = 1000;
                                        particles[particlesCount].startLifetime = 0;
                                        particles[particlesCount].randomSeed = 0;
                                        particles[particlesCount].rotation = 0;
                                        particles[particlesCount].startSize = particleSize;
                                        particles[particlesCount].velocity = Vector3.zero;
                                        particles[particlesCount].angularVelocity = 0;

                                        // Increase number of particles
                                        ++particlesCount;
                                    }
                                }
                            }

                            // Increase to next height line
                            if (++currentHeight >= height)
                            {
                                // Increase to next depth slice
                                ++currentDepth;
                                currentHeight = 0;
                            }
                        }

                        // Return current progress when building has not been finished
                        if (currentDepth < depth)
                        {
                            return (float)currentDepth / (float)depth;
                        }
                    }
                }
            }

            // Check for particles
            if (particles != null && particlesCount > 0)
            {
                GameObject particlesContainer;

                // Clone existing or create new container
                if (template != null)
                {
                    particlesContainer = (GameObject)Instantiate(template);
                    particlesContainer.name = targetName;
                    particlesContainer.SetActive(true);
                }
                else
                {
                    particlesContainer = new GameObject(targetName);
                }

                if (particlesContainer != null)
                {
                    // Copy position and unset scale and rotation
                    particlesContainer.transform.localPosition = gameObject.transform.position;
                    particlesContainer.transform.localScale = Vector3.one;
                    particlesContainer.transform.localRotation = Quaternion.identity;

                    // Add particles system
                    UnityEngine.ParticleSystem particleSystem = particlesContainer.GetComponent<UnityEngine.ParticleSystem>();
                    if (particleSystem == null)
                    {
                        particleSystem = particlesContainer.AddComponent<UnityEngine.ParticleSystem>();
                    }

                    if (particleSystem != null)
                    {
                        // Add particle system renderer, if there is none
                        ParticleSystemRenderer particleSystemRenderer = particlesContainer.GetComponent<ParticleSystemRenderer>();
                        if (particleSystemRenderer == null)
                        {
                            particleSystemRenderer = particlesContainer.AddComponent<ParticleSystemRenderer>();

                            // Set render properties
                            if (particleSystemRenderer != null)
                            {
                                particleSystemRenderer.cameraVelocityScale = 0;
                                particleSystemRenderer.lengthScale = 0;
                                particleSystemRenderer.maxParticleSize = 1000;
                                particleSystemRenderer.velocityScale = 0;
                            }
                        }

                        UnityEngine.ParticleSystem.EmissionModule emission = particleSystem.emission;
                        emission.rateOverTime = new UnityEngine.ParticleSystem.MinMaxCurve(10, new AnimationCurve());
                        emission.enabled = true;

                        // Set properties for static particles
                        var mainModule = particleSystem.main;
                        mainModule.playOnAwake = false;
                        mainModule.maxParticles = particlesCount;
                        mainModule.simulationSpeed = 1;
                        mainModule.startSpeed = 0;
                        mainModule.startLifetime = 1;
                        mainModule.startSize = bounds.extents.magnitude * 2;
                        mainModule.startDelay = 0;
                        mainModule.gravityModifier = 0;
                        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;

                        // Force a simulation to calculate boundaries
                        particleSystem.Simulate(particleSystem.main.startLifetime.constant);

                        // Set particles
                        particleSystem.SetParticles(particles, particlesCount);

                        // Stop automatic simulation
                        particleSystem.Stop();
                        emission.enabled = false;
                    }

#if UNITY_EDITOR

                    // Add object creation undo operation
                    if (!Application.isPlaying)
                    {
                        UnityEditor.Undo.RegisterCreatedObjectUndo(particlesContainer, "\"" + targetName + "\" Creation");
                    }

#endif

                    // Execute informer callback
                    if (informer != null)
                    {
                        informer(new UnityEngine.Object[] { particlesContainer }, parameter);
                    }
                }
            }

            // Reset current processing data
            currentDepth = 0;
            currentHeight = 0;
            particles = null;

            return 1;
        }

    }

}