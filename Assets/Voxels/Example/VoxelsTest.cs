using UnityEngine;


public class VoxelsTest : MonoBehaviour
{

	// Use this for initialization
	void Start()
    {
        // Scan this game object by Voxels converter engine and transfer result data to attached processor instances
        bool success = Voxels.Rasterizer.Engine.Process(
            gameObject,
            new Voxels.Rasterizer.Settings(
                Voxels.Rasterizer.Engine.ComputeBounds(gameObject),
                new Vector3(0.05f, 0.05f, 0.05f),
                0,
                1,
                Voxels.BakingOperation.MostFrequentColor,
                null,
                null,
                true,
                false,
                false,
                1.0f,
                1.0f
                ),
            null
            );

        Debug.Log("Voxels.Rasterizer.Engine.Process() returned: " + success.ToString());

        // Hide original object
        gameObject.SetActive(false);
	}

}
