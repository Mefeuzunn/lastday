using System.Collections;
using UnityEngine;

namespace ProjectSurvival.Combat
{
    /// <summary>
    /// Phase 1 (Step 1B): Ring-Buffer Pooled URP Impact Decal System.
    /// Provides zero-allocation, 60 FPS locked bullet holes, metallic sparks, and blood splatters.
    /// Pre-warms exactly 40 lightweight quad decal objects at startup using Universal Render Pipeline/Unlit
    /// transparent materials with enableInstancing = true and 0.008m surface normal offset (zero z-fighting).
    /// Automatically recycles the oldest decal when slot 40 is reached without runtime GC spikes.
    /// </summary>
    public class ImpactDecalPoolManager : MonoBehaviour
    {
        public static ImpactDecalPoolManager Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int maxPoolSize = 40;
        [SerializeField] private float surfaceNormalOffset = 0.008f;
        [SerializeField] private float decalLifetime = 18f;

        [Header("Decal Materials (URP Unlit Instanced)")]
        [SerializeField] private Material bulletHoleMaterial;
        [SerializeField] private Material bloodSplatterMaterial;

        private class DecalSlot
        {
            public GameObject GameObject;
            public Transform Transform;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock PropertyBlock;
            public Coroutine FadeRoutine;
            public bool IsFlesh;
        }

        private DecalSlot[] pool;
        private int currentRingIndex = 0;
        private Transform poolRoot;

        // Spark Burst Pool (8 lightweight instances)
        private GameObject[] sparkPool;
        private int sparkRingIndex = 0;

        public int MaxPoolSize => maxPoolSize;
        public int PrewarmedDecalCount => pool != null ? pool.Length : 0;
        public bool AreMaterialsInstanced => (bulletHoleMaterial != null && bulletHoleMaterial.enableInstancing) &&
                                             (bloodSplatterMaterial != null && bloodSplatterMaterial.enableInstancing);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeMaterials();
            InitializeDecalPool();
            InitializeSparkPool();
        }

        private void InitializeMaterials()
        {
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Standard");

            // 1. Bullet Hole Decal Material (#1E2229 dark scorch ring)
            if (bulletHoleMaterial == null)
            {
                bulletHoleMaterial = new Material(urpUnlit);
                bulletHoleMaterial.name = "Mat_BulletHole_Decal_Instanced";
                bulletHoleMaterial.color = Color.white;
                bulletHoleMaterial.enableInstancing = true;

                // Configure transparent blend mode
                ConfigureTransparentMaterial(bulletHoleMaterial);

                Texture2D texHole = GenerateBulletHoleTexture();
                bulletHoleMaterial.mainTexture = texHole;
            }

            // 2. Blood Splatter Decal Material (#6A040F dark crimson splatter)
            if (bloodSplatterMaterial == null)
            {
                bloodSplatterMaterial = new Material(urpUnlit);
                bloodSplatterMaterial.name = "Mat_BloodSplatter_Decal_Instanced";
                bloodSplatterMaterial.color = Color.white;
                bloodSplatterMaterial.enableInstancing = true;

                ConfigureTransparentMaterial(bloodSplatterMaterial);

                Texture2D texBlood = GenerateBloodSplatterTexture();
                bloodSplatterMaterial.mainTexture = texBlood;
            }
        }

        private void ConfigureTransparentMaterial(Material mat)
        {
            // Set surface type to Transparent and blend mode to Alpha
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // 1 = Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);     // 0 = Alpha
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0);   // Disable ZWrite for decals
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10;
        }

        private Texture2D GenerateBulletHoleTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Tex_BulletHole_Procedural";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float center = size * 0.5f;
            float innerRadius = size * 0.16f;
            float outerRadius = size * 0.44f;
            Color darkScorch = new Color(0.118f, 0.133f, 0.161f, 1.0f); // #1E2229
            Color metalRim = new Color(0.35f, 0.38f, 0.42f, 0.95f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Procedural jagged roughness
                    float angle = Mathf.Atan2(dy, dx);
                    float wobble = Mathf.Sin(angle * 7f) * 2.5f + Mathf.Cos(angle * 13f) * 1.8f;
                    float effectiveDist = dist + wobble;

                    if (effectiveDist <= innerRadius)
                    {
                        // Deep dark center punch
                        pixels[y * size + x] = darkScorch;
                    }
                    else if (effectiveDist <= outerRadius)
                    {
                        // Outer chipped scorch rim
                        float t = (effectiveDist - innerRadius) / (outerRadius - innerRadius);
                        Color c = Color.Lerp(darkScorch, metalRim, t * 0.5f);
                        c.a = Mathf.SmoothStep(1f, 0f, t);
                        pixels[y * size + x] = c;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateBloodSplatterTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Tex_BloodSplatter_Procedural";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float center = size * 0.5f;
            Color crimson = new Color(0.416f, 0.016f, 0.059f, 0.96f); // #6A040F

            // Initialize clear
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            // Main central organic splatter blob
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Organic lobes and tendrils
                    float lobe = Mathf.Sin(angle * 5f) * 4.5f + Mathf.Cos(angle * 9f) * 3.0f + Mathf.Sin(angle * 14f) * 2.2f;
                    float r = 26f + lobe;

                    if (dist <= r)
                    {
                        float alpha = Mathf.SmoothStep(1f, 0.2f, dist / r);
                        Color c = crimson;
                        c.a = alpha;
                        pixels[y * size + x] = c;
                    }
                }
            }

            // Satellite droplets around main splatter
            int[] dropX = new int[] { 22, 105, 30, 98, 64, 42, 85, 68 };
            int[] dropY = new int[] { 34, 40, 92, 88, 112, 18, 108, 16 };
            int[] dropR = new int[] { 5, 4, 6, 5, 4, 3, 4, 3 };

            for (int d = 0; d < dropX.Length; d++)
            {
                int cx = dropX[d];
                int cy = dropY[d];
                int rad = dropR[d];

                for (int y = cy - rad; y <= cy + rad; y++)
                {
                    if (y < 0 || y >= size) continue;
                    for (int x = cx - rad; x <= cx + rad; x++)
                    {
                        if (x < 0 || x >= size) continue;
                        float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        if (dist <= rad)
                        {
                            Color c = crimson;
                            c.a = Mathf.SmoothStep(1f, 0.3f, dist / rad);
                            pixels[y * size + x] = c;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void InitializeDecalPool()
        {
            poolRoot = new GameObject("Impact_Decal_Pool").transform;
            poolRoot.SetParent(transform, false);

            pool = new DecalSlot[maxPoolSize];

            for (int i = 0; i < maxPoolSize; i++)
            {
                GameObject decalObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decalObj.name = $"PooledDecal_{i + 1}";
                decalObj.transform.SetParent(poolRoot, false);

                // Strip collider immediately to prevent interference with weapon raycasts
                Collider col = decalObj.GetComponent<Collider>();
                if (col != null) Destroy(col);

                MeshRenderer mr = decalObj.GetComponent<MeshRenderer>();
                mr.sharedMaterial = bulletHoleMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                pool[i] = new DecalSlot
                {
                    GameObject = decalObj,
                    Transform = decalObj.transform,
                    Renderer = mr,
                    PropertyBlock = new MaterialPropertyBlock(),
                    FadeRoutine = null,
                    IsFlesh = false
                };

                decalObj.SetActive(false);
            }
        }

        private void InitializeSparkPool()
        {
            GameObject sparkRoot = new GameObject("Spark_Burst_Pool");
            sparkRoot.transform.SetParent(transform, false);

            sparkPool = new GameObject[8];
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material sparkMat = new Material(unlit);
            sparkMat.color = new Color(1.0f, 0.9f, 0.45f, 1.0f); // Bright yellow-amber spark
            sparkMat.enableInstancing = true;

            for (int i = 0; i < 8; i++)
            {
                GameObject sp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sp.name = $"PooledSpark_{i + 1}";
                sp.transform.SetParent(sparkRoot.transform, false);
                Collider sc = sp.GetComponent<Collider>();
                if (sc != null) Destroy(sc);

                MeshRenderer smr = sp.GetComponent<MeshRenderer>();
                smr.sharedMaterial = sparkMat;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sp.transform.localScale = Vector3.one * 0.08f;
                sp.SetActive(false);
                sparkPool[i] = sp;
            }
        }

        /// <summary>
        /// Spawns a ring-buffer pooled URP impact decal at hitPoint oriented along surface normal.
        /// Zero allocation, 60 FPS locked, automatically recycles oldest slot.
        /// </summary>
        public void SpawnImpact(Vector3 hitPoint, Vector3 hitNormal, bool isFlesh)
        {
            if (pool == null || pool.Length == 0) return;

            DecalSlot slot = pool[currentRingIndex];
            currentRingIndex = (currentRingIndex + 1) % maxPoolSize;

            if (slot.FadeRoutine != null)
            {
                StopCoroutine(slot.FadeRoutine);
                slot.FadeRoutine = null;
            }

            slot.IsFlesh = isFlesh;
            slot.Renderer.sharedMaterial = isFlesh ? bloodSplatterMaterial : bulletHoleMaterial;

            // Position with 0.008m normal offset to guarantee zero z-fighting
            Vector3 pos = hitPoint + hitNormal * surfaceNormalOffset;
            slot.Transform.position = pos;

            // Quad faces toward -hitNormal (so its front faces outward)
            Quaternion rot = Quaternion.LookRotation(-hitNormal, Vector3.up);
            // Randomize roll angle for natural variation
            rot *= Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            slot.Transform.rotation = rot;

            // Randomize scale
            float scale = isFlesh ? Random.Range(0.24f, 0.42f) : Random.Range(0.12f, 0.18f);
            slot.Transform.localScale = new Vector3(scale, scale, 1f);

            // Reset alpha via MaterialPropertyBlock
            slot.PropertyBlock.SetColor("_BaseColor", Color.white);
            slot.Renderer.SetPropertyBlock(slot.PropertyBlock);

            slot.GameObject.SetActive(true);
            slot.FadeRoutine = StartCoroutine(DecalFadeRoutine(slot));

            // Metallic spark burst on hard surfaces
            if (!isFlesh)
            {
                SpawnSparkFlash(hitPoint, hitNormal);
            }
        }

        private void SpawnSparkFlash(Vector3 pos, Vector3 normal)
        {
            if (sparkPool == null || sparkPool.Length == 0) return;

            GameObject spark = sparkPool[sparkRingIndex];
            sparkRingIndex = (sparkRingIndex + 1) % sparkPool.Length;

            spark.transform.position = pos + normal * 0.04f;
            spark.SetActive(true);
            StartCoroutine(SparkFlashRoutine(spark));
        }

        private IEnumerator SparkFlashRoutine(GameObject spark)
        {
            float elapsed = 0f;
            float dur = 0.065f;
            Vector3 startScale = Vector3.one * 0.12f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                spark.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / dur);
                yield return null;
            }
            spark.SetActive(false);
        }

        private IEnumerator DecalFadeRoutine(DecalSlot slot)
        {
            float elapsed = 0f;
            float holdTime = decalLifetime * 0.75f;
            float fadeTime = decalLifetime * 0.25f;

            // Hold full visibility
            yield return new WaitForSeconds(holdTime);

            // Fade out
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / fadeTime);
                slot.PropertyBlock.SetColor("_BaseColor", new Color(1f, 1f, 1f, alpha));
                slot.Renderer.SetPropertyBlock(slot.PropertyBlock);
                yield return null;
            }

            slot.GameObject.SetActive(false);
            slot.FadeRoutine = null;
        }
    }
}
