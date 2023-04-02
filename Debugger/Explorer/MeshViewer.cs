using System;
using ColossalFramework;
using ColossalFramework.UI;
using ModTools.UI;
using ModTools.Utils;
using UnityEngine;

namespace ModTools.Explorer
{
    internal sealed class MeshViewer : GUIWindow, IGameObject
    {

        private const int RenderResolution = 1080;
        private readonly RenderTexture targetRT;
        private readonly Camera meshViewerCamera;
        private Mesh previewMesh;
        private Material previewMaterial;
        private string assetName;

        private float distance = 4f;
        private Vector2 previewDir = new (120f, -20f);
        private Bounds bounds;
        private Vector2 lastLeftMousePos = Vector2.zero;
        private Vector2 lastRightMousePos = Vector2.zero;

        private Material material;

        private bool useOriginalShader;

        public MeshViewer()
            : base("Mesh Viewer", new Rect(512, 128, 512, 512))
        {

            // Set up camera.
            meshViewerCamera = new GameObject("Camera").AddComponent<Camera>();
            meshViewerCamera.transform.SetParent(transform);
            targetRT = new RenderTexture(RenderResolution, RenderResolution, 24, RenderTextureFormat.ARGB32);
            meshViewerCamera.targetTexture = targetRT;
            meshViewerCamera.allowHDR = true;
            meshViewerCamera.enabled = false;
            meshViewerCamera.clearFlags = CameraClearFlags.Color;

            // Basic defaults.
            meshViewerCamera.pixelRect = new Rect(0f, 0f, RenderResolution, RenderResolution);
            meshViewerCamera.backgroundColor = new Color(0, 0, 0, 0);
            meshViewerCamera.fieldOfView = 30f;
            meshViewerCamera.nearClipPlane = 1f;
            meshViewerCamera.farClipPlane = 1000f;
        }

        public static MeshViewer CreateMeshViewer(string assetName, Mesh mesh, Material material, bool calculateBounds = true)
        {
            var go = new GameObject("MeshViewer");
            go.transform.parent = MainWindow.Instance.transform;
            var meshViewer = go.AddComponent<MeshViewer>();
            meshViewer.assetName = assetName;
            meshViewer.previewMesh = mesh;
            meshViewer.material = material;

            meshViewer.previewMaterial = new Material(Shader.Find("Diffuse"));
            if (material != null)
            {
                meshViewer.previewMaterial.mainTexture = material.mainTexture;
            }

            meshViewer.Setup(calculateBounds);
            meshViewer.Visible = true;
            meshViewer.SetCitizenInfoObjects(false);
            return meshViewer;
        }

        public void Update()
        {
            if (previewMesh == null)
            {
                return;
            }

            // Set background.
            meshViewerCamera.clearFlags = CameraClearFlags.Color;
            meshViewerCamera.backgroundColor = new Color32(33, 151, 199, 255);

            // Back up current game InfoManager mode.
            InfoManager infoManager = Singleton<InfoManager>.instance;
            InfoManager.InfoMode currentMode = infoManager.CurrentMode;
            InfoManager.SubInfoMode currentSubMode = infoManager.CurrentSubMode;

            // Set current game InfoManager to default (don't want to render with an overlay mode).
            infoManager.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            infoManager.UpdateInfoMode();

            // Backup current exposure and sky tint.
            float gameExposure = DayNightProperties.instance.m_Exposure;
            Color gameSkyTint = DayNightProperties.instance.m_SkyTint;

            // Backup current game lighting.
            Light gameMainLight = RenderManager.instance.MainLight;

            // Set exposure and sky tint for render.
            DayNightProperties.instance.m_Exposure = 0.75f;
            DayNightProperties.instance.m_SkyTint = new Color(0, 0, 0);
            DayNightProperties.instance.Refresh();

            // Set up our render lighting settings.
            Light renderLight = DayNightProperties.instance.sunLightSource;
            RenderManager.instance.MainLight = renderLight;

            // Light settings.
            renderLight.transform.eulerAngles = new Vector3(45f, 180f, 0f);
            renderLight.intensity = 2f;
            renderLight.color = Color.white;

            // Set zoom to encapsulate entire model.
            var magnitude = bounds.extents.magnitude;
            var num1 = (magnitude + 16f) * 1.5f;
            var num2 = magnitude * distance;
            meshViewerCamera.transform.position = Vector3.forward * num2;
            meshViewerCamera.transform.rotation = Quaternion.identity;
            meshViewerCamera.nearClipPlane = Mathf.Max(num2 - num1, 0.01f);
            meshViewerCamera.farClipPlane = num2 + num1;
            var q = Quaternion.Euler(previewDir.y, 0f, 0f) * Quaternion.Euler(0f, previewDir.x, 0f);
            var trs = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

            // Aim camera at middle of bounds.
            meshViewerCamera.transform.LookAt(bounds.center);

            // If game is currently in nighttime, enable sun and disable moon lighting.
            if (gameMainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = true;
                DayNightProperties.instance.moonLightSource.enabled = false;
            }

            var material1 = (useOriginalShader && material != null) ? material : previewMaterial;
            Graphics.DrawMesh(previewMesh, trs, material1, 0, meshViewerCamera, 0, null, true, true);
            meshViewerCamera.RenderWithShader(material1.shader, string.Empty);

            // Restore game lighting.
            RenderManager.instance.MainLight = gameMainLight;

            // Reset to moon lighting if the game is currently in nighttime.
            if (gameMainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = false;
                DayNightProperties.instance.moonLightSource.enabled = true;
            }

            // Restore game exposure and sky tint.
            DayNightProperties.instance.m_Exposure = gameExposure;
            DayNightProperties.instance.m_SkyTint = gameSkyTint;
            DayNightProperties.instance.Refresh();

            // Restore game InfoManager mode.
            infoManager.SetCurrentMode(currentMode, currentSubMode);
            infoManager.UpdateInfoMode();
        }

        public void Setup(bool calculateBounds)
        {
            if (previewMesh == null)
            {
                return;
            }

            if (calculateBounds && previewMesh.isReadable)
            {
                bounds = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var vertex in previewMesh.vertices)
                {
                    bounds.Encapsulate(vertex);
                }
            }
            else
            {
                bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(3, 3, 3));
            }

            // Expand bounds slightly.
            bounds.Expand(1f);
            distance = 4f;
        }

        public void SetCitizenInfoObjects(bool enabled)
        {
            foreach (var i in Resources.FindObjectsOfTypeAll<CitizenInfo>())
            {
                i.gameObject.SetActive(enabled);
            }
        }

        protected override void OnWindowClosed()
        {
            SetCitizenInfoObjects(true);
            Destroy(this);
        }

        protected override void OnWindowDestroyed()
        {
            Destroy(meshViewerCamera);
            Destroy(targetRT);
        }

        protected override void DrawWindow()
        {
            if (previewMesh == null)
            {
                Title = "Mesh Viewer";
                GUILayout.Label("Use the Scene Explorer to select a Mesh for preview");
                return;
            }

            Title = $"Previewing \"{assetName ?? previewMesh.name}\"";

            GUILayout.BeginHorizontal();

            if (material != null)
            {
                useOriginalShader = GUILayout.Toggle(useOriginalShader, "Original Shader");
                if (previewMesh.isReadable)
                {
                    if (GUILayout.Button("Dump mesh+textures", GUILayout.Width(160)))
                    {
                        DumpUtil.DumpMeshAndTextures(assetName ?? previewMesh.name, previewMesh, material);
                    }
                }
                else if (GUILayout.Button("Dump textures", GUILayout.Width(160)))
                {
                    DumpUtil.DumpTextures(assetName, material);
                }
            }
            else
            {
                useOriginalShader = false;
                if (previewMesh.isReadable && GUILayout.Button("Dump mesh", GUILayout.Width(160)))
                {
                    DumpUtil.DumpMeshAndTextures($"{previewMesh.name}", previewMesh);
                }
            }

            if (previewMesh.isReadable)
            {
                GUILayout.Label($"Triangles: {previewMesh.triangles.Length / 3}");
            }
            else
            {
                var oldColor = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label("Mesh isn't readable!");
                GUI.color = oldColor;
            }

            if (material?.mainTexture != null)
            {
                GUILayout.Label($"Texture size: {material.mainTexture.width}x{material.mainTexture.height}");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0 || Event.current.button == 2)
                {
                    lastLeftMousePos = Event.current.mousePosition;
                }
                else
                {
                    lastRightMousePos = Event.current.mousePosition;
                }
            }
            else if (Event.current.type == EventType.MouseDrag)
            {
                var pos = Event.current.mousePosition;
                if (Event.current.button == 0 || Event.current.button == 2)
                {
                    if (lastLeftMousePos != Vector2.zero)
                    {
                        var moveDelta = (pos - lastLeftMousePos) * 2.0f;
                        previewDir -= moveDelta / Mathf.Min(targetRT.width, targetRT.height)
                                             * UIView.GetAView().ratio * 140f;
                        previewDir.y = Mathf.Clamp(previewDir.y, -90f, 90f);
                    }

                    lastLeftMousePos = pos;
                }
                else
                {
                    if (lastRightMousePos != Vector2.zero)
                    {
                        var moveDelta1 = pos - lastRightMousePos;
                        distance += (float)(moveDelta1.y / (double)targetRT.height
                                                     * UIView.GetAView().ratio * 40.0);
                        const float num1 = 6f;
                        var magnitude = bounds.extents.magnitude;
                        var num2 = magnitude + 16f;
                        distance = Mathf.Min(distance, 4f, num1 * (num2 / magnitude));
                    }

                    lastRightMousePos = pos;
                }
            }

            GUI.DrawTexture(new Rect(0.0f, 64.0f, WindowRect.width, WindowRect.height - 64.0f), targetRT, ScaleMode.StretchToFill, false);
        }
    }
}