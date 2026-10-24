using UnityEngine;
using UnityEngine.Rendering;

namespace SchoolEscape
{
    /// <summary>
    /// A vertical, south-facing school mirror. The reflected camera renders
    /// the same active scene, so inactive floor/room roots cannot appear in it.
    /// The component owns its camera, texture and projective material.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlanarMirror : MonoBehaviour
    {
        const int MirrorLayer = 30;
        const int TextureSize = 512;
        const float ClipOffset = .025f;
        static bool renderingReflection;
        static readonly int ReflectionTexture = Shader.PropertyToID("_ReflectionTex");
        static readonly int ReflectionProjection = Shader.PropertyToID("_ReflectionVP");
        Camera reflectionCamera;
        RenderTexture reflectionTexture;
        Material mirrorMaterial;
        Renderer surface;
        int lastFrame = -1, lastSourceId;

        public int RenderCount { get; private set; }
        public Vector3 LastReflectedCameraPosition { get; private set; }

        public static void Attach(GameObject surfaceObject)
        {
            surfaceObject.AddComponent<PlanarMirror>();
        }

        void Awake()
        {
            surface = GetComponent<Renderer>();
            gameObject.layer = MirrorLayer;
            // A projective shader samples the camera image at each world-space
            // mirror point, rather than stretching an entire image into it.
            var shader = Resources.Load<Shader>("PlanarMirror");
            if (surface == null || shader == null)
            {
                Debug.LogError("School Escape mirror shader or renderer is missing.", this);
                enabled = false;
                return;
            }
            mirrorMaterial = new Material(shader) { name = "School mirror · " + name };
            mirrorMaterial.SetTexture(ReflectionTexture, Texture2D.blackTexture);
            surface.sharedMaterial = mirrorMaterial;
            surface.shadowCastingMode = ShadowCastingMode.Off;
            surface.receiveShadows = false;
        }

        void EnsureResources()
        {
            if (reflectionTexture == null)
            {
                reflectionTexture = new RenderTexture(TextureSize, TextureSize, 16,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    name = "School mirror reflection · " + name,
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false,
                    antiAliasing = 1
                };
                reflectionTexture.Create();
                mirrorMaterial.SetTexture(ReflectionTexture, reflectionTexture);
            }
            if (reflectionCamera == null)
            {
                var cameraObject = new GameObject("School mirror camera · " + name)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                reflectionCamera = cameraObject.AddComponent<Camera>();
                reflectionCamera.enabled = false;
                reflectionCamera.cameraType = CameraType.Reflection;
                // No AudioListener or GUI component belongs to this camera.
            }
        }

        void OnWillRenderObject()
        {
            var source = Camera.current;
            if (!enabled || !gameObject.activeInHierarchy || surface == null ||
                mirrorMaterial == null || renderingReflection || source == null ||
                source.cameraType != CameraType.Game || !source.CompareTag("MainCamera")) return;
            if (lastFrame == Time.frameCount && lastSourceId == source.GetInstanceID()) return;

            EnsureResources();
            reflectionCamera.CopyFrom(source);
            reflectionCamera.enabled = false;
            reflectionCamera.cameraType = CameraType.Reflection;
            reflectionCamera.targetTexture = reflectionTexture;
            reflectionCamera.cullingMask = source.cullingMask & ~(1 << MirrorLayer);
            reflectionCamera.useOcclusionCulling = false;
            reflectionCamera.allowMSAA = false;
            reflectionCamera.allowHDR = false;
            reflectionCamera.depthTextureMode = DepthTextureMode.None;

            // Fixture boxes have their south face at local z=-0.5. Placing the
            // mathematical plane there keeps reflected edges on the surface.
            Vector3 planePoint = transform.TransformPoint(new Vector3(0, 0, -.5f));
            Vector3 normal = transform.TransformDirection(Vector3.back).normalized;
            var plane = new Vector4(normal.x, normal.y, normal.z,
                -Vector3.Dot(normal, planePoint));
            Matrix4x4 reflection = ReflectionMatrix(plane);
            LastReflectedCameraPosition = reflection.MultiplyPoint(source.transform.position);
            reflectionCamera.transform.position = LastReflectedCameraPosition;
            reflectionCamera.transform.rotation = Quaternion.LookRotation(
                reflection.MultiplyVector(source.transform.forward),
                reflection.MultiplyVector(source.transform.up));
            reflectionCamera.worldToCameraMatrix = source.worldToCameraMatrix * reflection;

            Vector3 clipPoint = reflectionCamera.worldToCameraMatrix.MultiplyPoint(
                planePoint + normal * ClipOffset);
            Vector3 clipNormal = reflectionCamera.worldToCameraMatrix.MultiplyVector(normal).normalized;
            var clip = new Vector4(clipNormal.x, clipNormal.y, clipNormal.z,
                -Vector3.Dot(clipPoint, clipNormal));
            reflectionCamera.projectionMatrix = source.CalculateObliqueMatrix(clip);
            mirrorMaterial.SetMatrix(ReflectionProjection,
                GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix, true) *
                reflectionCamera.worldToCameraMatrix);

            bool previousCulling = GL.invertCulling;
            renderingReflection = true;
            try
            {
                GL.invertCulling = !previousCulling;
                reflectionCamera.Render();
                RenderCount++;
                lastFrame = Time.frameCount;
                lastSourceId = source.GetInstanceID();
            }
            finally
            {
                // Restore global render state even if a rendering callback
                // throws, and prevent reflections rendering other mirrors.
                GL.invertCulling = previousCulling;
                renderingReflection = false;
            }
        }

        static Matrix4x4 ReflectionMatrix(Vector4 p)
        {
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = 1 - 2 * p.x * p.x; m.m01 = -2 * p.x * p.y; m.m02 = -2 * p.x * p.z; m.m03 = -2 * p.w * p.x;
            m.m10 = -2 * p.y * p.x; m.m11 = 1 - 2 * p.y * p.y; m.m12 = -2 * p.y * p.z; m.m13 = -2 * p.w * p.y;
            m.m20 = -2 * p.z * p.x; m.m21 = -2 * p.z * p.y; m.m22 = 1 - 2 * p.z * p.z; m.m23 = -2 * p.w * p.z;
            return m;
        }

        void ReleaseResources()
        {
            if (mirrorMaterial != null)
                mirrorMaterial.SetTexture(ReflectionTexture, Texture2D.blackTexture);
            if (reflectionCamera != null)
            {
                reflectionCamera.targetTexture = null;
                Destroy(reflectionCamera.gameObject);
                reflectionCamera = null;
            }
            if (reflectionTexture != null)
            {
                reflectionTexture.Release();
                Destroy(reflectionTexture);
                reflectionTexture = null;
            }
            lastFrame = -1;
        }

        void OnDisable() => ReleaseResources();
        void OnDestroy()
        {
            ReleaseResources();
            if (mirrorMaterial != null) Destroy(mirrorMaterial);
        }
    }
}
