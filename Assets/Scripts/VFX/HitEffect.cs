using UnityEngine;

public class HitEffect : MonoBehaviour
{
    [Header("Decal")]
    [SerializeField] private GameObject decalPrefab;      
    [SerializeField] private float      decalLifetime = 8f;
    [SerializeField] private float      decalSize     = 0.12f;
    [SerializeField] private float      decalOffset   = 0.005f; 

    
    private static readonly System.Collections.Generic.Queue<GameObject> _decalPool
        = new System.Collections.Generic.Queue<GameObject>();
    private const int MaxDecals = 40;

    public void Trigger(Vector3 position, Vector3 normal)
    {
        
        SpawnDecal(position, normal);
    }

    
    private void SpawnDecal(Vector3 position, Vector3 normal)
    {
        GameObject decal;

        if (_decalPool.Count >= MaxDecals)
        {
            
            decal = _decalPool.Dequeue();
            StopAllCoroutines();
        }
        else
        {
            decal = decalPrefab != null
                ? Instantiate(decalPrefab)
                : CreateProceduralDecal();
        }

        
        decal.transform.position = position + normal * decalOffset;
        decal.transform.rotation = Quaternion.LookRotation(-normal);
        decal.transform.localScale = Vector3.one * decalSize;
        decal.SetActive(true);

        _decalPool.Enqueue(decal);
        StartCoroutine(FadeDecal(decal, decalLifetime));
    }

    private System.Collections.IEnumerator FadeDecal(GameObject decal, float lifetime)
    {
        yield return new UnityEngine.WaitForSeconds(lifetime * 0.7f);

        
        float elapsed = 0f;
        float fadeDur = lifetime * 0.3f;
        var rend = decal?.GetComponent<Renderer>();
        if (rend == null) yield break;

        Color startColor = rend.material.color;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDur);
            rend.material.color = new Color(startColor.r, startColor.g, startColor.b, a);
            yield return null;
        }

        if (decal != null) decal.SetActive(false);
    }

    
    private static GameObject CreateProceduralDecal()
    {
        var go   = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name  = "BulletDecal";

        
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        
        var rend = go.GetComponent<Renderer>();
        var mat  = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        rend.material = mat;

        go.SetActive(false);
        return go;
    }
}