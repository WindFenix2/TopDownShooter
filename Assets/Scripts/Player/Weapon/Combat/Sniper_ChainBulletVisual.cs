using System.Collections.Generic;
using UnityEngine;

public class Sniper_ChainBulletVisual : MonoBehaviour
{
    private List<Vector3> path;
    private float speed;

    private int segmentIndex;
    private Vector3 currentTarget;

    private TrailRenderer tr;
    private MeshRenderer mr;
    private MeshFilter mf;

    public void Play(List<Vector3> worldPath, float moveSpeed)
    {
        if (worldPath == null || worldPath.Count < 2)
        {
            Destroy(gameObject);
            return;
        }

        path = new List<Vector3>(worldPath);
        speed = Mathf.Max(1f, moveSpeed);

        SetupVisuals();
        transform.position = path[0];

        segmentIndex = 1;
        currentTarget = path[segmentIndex];

        SpawnImpact(transform.position);
    }

    private void SetupVisuals()
    {
        tr = gameObject.AddComponent<TrailRenderer>();
        tr.time = 0.12f;
        tr.startWidth = 0.08f;
        tr.endWidth = 0.0f;
        tr.minVertexDistance = 0.02f;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;

        Shader trailShader = Shader.Find("Sprites/Default");
        if (trailShader == null)
            trailShader = Shader.Find("Unlit/Color");
        tr.material = new Material(trailShader);

        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Shader bulletShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (bulletShader == null)
            bulletShader = Shader.Find("Sprites/Default");
        Material bulletMat = new Material(bulletShader);
        if (bulletMat.HasProperty("_BaseColor"))
            bulletMat.SetColor("_BaseColor", new Color(0f, 1f, 0.85f, 1f));
        else if (bulletMat.HasProperty("_Color"))
            bulletMat.SetColor("_Color", new Color(0f, 1f, 0.85f, 1f));
        mr.material = bulletMat;

        mf.mesh = CreateTinySphereMesh();
        transform.localScale = Vector3.one * 0.08f;
    }

    private Mesh CreateTinySphereMesh()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh m = sphere.GetComponent<MeshFilter>().sharedMesh;
        Destroy(sphere);
        return m;
    }

    private void Update()
    {
        if (path == null || path.Count < 2)
            return;

        transform.position = Vector3.MoveTowards(transform.position, currentTarget, speed * Time.deltaTime);

        Vector3 dir = (currentTarget - transform.position);
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (Vector3.SqrMagnitude(transform.position - currentTarget) < 0.001f)
            Advance();
    }

    private void Advance()
    {
        SpawnImpact(currentTarget);

        segmentIndex++;
        if (segmentIndex >= path.Count)
        {
            Destroy(gameObject, 0.15f);
            return;
        }

        currentTarget = path[segmentIndex];
    }

    private void SpawnImpact(Vector3 pos)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.transform.position = pos;
        g.transform.localScale = Vector3.one * 0.12f;

        Collider c = g.GetComponent<Collider>();
        if (c != null)
            Destroy(c);

        MeshRenderer r = g.GetComponent<MeshRenderer>();
        if (r != null)
        {
            Shader fxShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (fxShader == null)
                fxShader = Shader.Find("Sprites/Default");
            r.material = new Material(fxShader);

            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", new Color(1f, 0.9f, 0.3f, 1f));
            else if (r.material.HasProperty("_Color"))
                r.material.SetColor("_Color", new Color(1f, 0.9f, 0.3f, 1f));
        }

        Destroy(g, 0.08f);
    }
}
