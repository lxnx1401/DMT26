using UnityEngine;

public class GrassInstancer : MonoBehaviour
{
    public Mesh grassMesh;
    public Material grassMaterial;
    public int instanceCount = 5000;
    public float areaSize = 50f;
    public Transform ground;

    private Matrix4x4[] matrices;

    void Start()
    {
        matrices = new Matrix4x4[instanceCount];

        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-ground.localScale.x * 5f, ground.localScale.x * 5f),
                0f,
                Random.Range(-ground.localScale.z * 5f, ground.localScale.z * 5f)
            );

            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
            float scale = Random.Range(0.8f, 1.5f);

            matrices[i] = Matrix4x4.TRS(pos, rot, Vector3.one * scale);
        }
    }

    void Update()
    {
        Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, matrices);
    }
}