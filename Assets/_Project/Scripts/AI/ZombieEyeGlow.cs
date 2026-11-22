using UnityEngine;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Bioluminescent Night Eye-Glow VFX.
    /// Lightweight twin additive eye-glint sprites (0.06m) at Visual_Model eye level.
    /// Colors:
    /// - Standard & Runner: #FF3B30 (Crimson-amber)
    /// - Screamer: #B5179E (Violet)
    /// - Tank: #FF9E00 (Molten orange)
    /// Fades in smoothly at night (20:00 - 05:30) and fades out at dawn or death.
    /// Zero real-time lights, zero GC allocs, 100% SRP batcher compatible.
    /// </summary>
    public class ZombieEyeGlow : MonoBehaviour
    {
        [Header("Glow Configuration")]
        [SerializeField] private Color glowColor = new Color(1f, 0.23f, 0.19f, 1f); // #FF3B30 default
        [SerializeField] private float targetAlpha = 0f;
        [SerializeField] private float currentAlpha = 0f;
        [SerializeField] private float fadeSpeed = 3.5f;

        private GameObject leftEyeQuad;
        private GameObject rightEyeQuad;
        private Renderer leftRenderer;
        private Renderer rightRenderer;
        private MaterialPropertyBlock propBlock;
        private ZombieBrain brain;

        private static Material sharedEyeGlowMat;

        private void Awake()
        {
            brain = GetComponentInParent<ZombieBrain>();
            propBlock = new MaterialPropertyBlock();
            ConfigureColorByZombieType();
            SetupEyeQuads();
        }

        private void ConfigureColorByZombieType()
        {
            if (brain == null) brain = GetComponentInParent<ZombieBrain>();
            if (brain != null)
            {
                switch (brain.Type)
                {
                    case ZombieType.Runner:
                    case ZombieType.Standard:
                        glowColor = new Color(1f, 0.23f, 0.19f, 1f); // #FF3B30 Crimson
                        break;
                    case ZombieType.Screamer:
                        glowColor = new Color(0.71f, 0.09f, 0.62f, 1f); // #B5179E Violet
                        break;
                    case ZombieType.Tank:
                        glowColor = new Color(1f, 0.62f, 0.0f, 1f); // #FF9E00 Molten orange
                        break;
                }
            }
        }

        private void SetupEyeQuads()
        {
            Transform vm = transform.Find("Visual_Model");
            if (vm == null && gameObject.name == "Visual_Model") vm = transform;
            if (vm == null) vm = transform;

            if (sharedEyeGlowMat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                sharedEyeGlowMat = new Material(s);
                sharedEyeGlowMat.name = "Mat_ZombieEyeGlow_Additive";
                // Additive / transparent blend
                sharedEyeGlowMat.SetFloat("_Surface", 1); // Transparent
                sharedEyeGlowMat.SetFloat("_Blend", 1);   // Additive / Alpha
                sharedEyeGlowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                sharedEyeGlowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                sharedEyeGlowMat.SetInt("_ZWrite", 0);
                sharedEyeGlowMat.renderQueue = 3000;
            }

            float eyeHeight = 1.62f;
            float eyeForward = 0.22f;
            float eyeSpacing = 0.075f;

            if (brain != null && brain.Type == ZombieType.Tank)
            {
                eyeHeight = 2.05f;
                eyeForward = 0.28f;
                eyeSpacing = 0.10f;
            }

            // Left Eye
            leftEyeQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            leftEyeQuad.name = "EyeGlow_Left";
            leftEyeQuad.transform.SetParent(vm, false);
            leftEyeQuad.transform.localPosition = new Vector3(-eyeSpacing, eyeHeight, eyeForward);
            leftEyeQuad.transform.localRotation = Quaternion.identity;
            leftEyeQuad.transform.localScale = new Vector3(0.06f, 0.06f, 1f);
            DestroyImmediate(leftEyeQuad.GetComponent<Collider>());
            leftRenderer = leftEyeQuad.GetComponent<Renderer>();
            leftRenderer.sharedMaterial = sharedEyeGlowMat;
            leftRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            leftRenderer.receiveShadows = false;

            // Right Eye
            rightEyeQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rightEyeQuad.name = "EyeGlow_Right";
            rightEyeQuad.transform.SetParent(vm, false);
            rightEyeQuad.transform.localPosition = new Vector3(eyeSpacing, eyeHeight, eyeForward);
            rightEyeQuad.transform.localRotation = Quaternion.identity;
            rightEyeQuad.transform.localScale = new Vector3(0.06f, 0.06f, 1f);
            DestroyImmediate(rightEyeQuad.GetComponent<Collider>());
            rightRenderer = rightEyeQuad.GetComponent<Renderer>();
            rightRenderer.sharedMaterial = sharedEyeGlowMat;
            rightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rightRenderer.receiveShadows = false;

            ApplyAlpha(0f);
        }

        public void SetNightState(bool isNight)
        {
            if (brain != null && brain.IsDead)
            {
                targetAlpha = 0f;
            }
            else
            {
                targetAlpha = isNight ? 0.88f : 0f;
            }
        }

        public void ResetGlow()
        {
            currentAlpha = 0f;
            targetAlpha = 0f;
            ApplyAlpha(0f);
        }

        private void Update()
        {
            if (brain != null && brain.IsDead)
            {
                targetAlpha = 0f;
            }

            if (Mathf.Abs(currentAlpha - targetAlpha) > 0.005f)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
                ApplyAlpha(currentAlpha);
            }
        }

        private void ApplyAlpha(float alpha)
        {
            if (leftRenderer == null || rightRenderer == null) return;

            bool isVisible = alpha > 0.01f;
            if (leftEyeQuad.activeSelf != isVisible) leftEyeQuad.SetActive(isVisible);
            if (rightEyeQuad.activeSelf != isVisible) rightEyeQuad.SetActive(isVisible);

            if (!isVisible) return;

            Color c = glowColor;
            c.a = alpha;
            propBlock.SetColor("_BaseColor", c);
            propBlock.SetColor("_Color", c);

            leftRenderer.SetPropertyBlock(propBlock);
            rightRenderer.SetPropertyBlock(propBlock);
        }

        private void OnDestroy()
        {
            if (leftEyeQuad != null) Destroy(leftEyeQuad);
            if (rightEyeQuad != null) Destroy(rightEyeQuad);
        }
    }
}
