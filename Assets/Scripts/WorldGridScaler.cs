using UnityEngine;

[ExecuteAlways]
public class WorldGridScaler : MonoBehaviour
{
    private MaterialPropertyBlock propBlock;
    private Renderer objRenderer;

    void OnValidate()
    {
        UpdateTiling();
    }

    void Start()
    {
        UpdateTiling();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            UpdateTiling();
        }
    }

    void UpdateTiling()
    {
        if (objRenderer == null) objRenderer = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        Vector3 scale = transform.localScale;

        // Determine orientation based on thinness
        bool isFloor = scale.y <= scale.x && scale.y <= scale.z;
        bool isZWall = scale.x <= scale.y && scale.x <= scale.z; // Wall running along Z-axis

        float tileX;
        float tileY;

        if (isFloor)
        {
            tileX = scale.x;
            tileY = scale.z;
        }
        else if (isZWall)
        {
            // Wall facing East/West (Length is Z, Height is Y)
            tileX = scale.z;
            tileY = scale.y;
        }
        else
        {
            // Wall facing North/South (Length is X, Height is Y)
            tileX = scale.x;
            tileY = scale.y;
        }

        objRenderer.GetPropertyBlock(propBlock);
        propBlock.SetVector("_BaseMap_ST", new Vector4(tileX, tileY, 0, 0));
        objRenderer.SetPropertyBlock(propBlock);
    }
}
