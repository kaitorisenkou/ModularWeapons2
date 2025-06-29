using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;
using static RimWorld.MechClusterSketch;

namespace ModularWeapons2 {
    public class MWCameraRenderer : MonoBehaviour {
        public static void Render(RenderTexture renderTexture, CompModularWeapon targetWeapon) {
            mwCameraRenderer.requests = targetWeapon.GetRequestsForRenderCam()?.OrderBy(t => t.layerOrder) ?? null;
            if (mwCameraRenderer.requests == null) {
                mwCameraRenderer.requests = new MWCameraRequest[] {
                    new MWCameraRequest(targetWeapon.parent.Graphic.MatSingle,Vector2.zero,0)
                };
            }
            RenderInt(renderTexture);
        }
        public static void Render(RenderTexture renderTexture, params MWCameraRequest[] requests) {
            mwCameraRenderer.requests = requests?.OrderBy(t => t.layerOrder).ToArray() ?? null;
            RenderInt(renderTexture);
        }
        static void RenderInt(RenderTexture renderTexture) {
            float orthographicSize = mwCamera.orthographicSize;
            mwCamera.SetTargetBuffers(renderTexture.colorBuffer, renderTexture.depthBuffer);
            mwCamera.Render();
            mwCameraRenderer.requests = null;
            mwCamera.orthographicSize = orthographicSize;
            mwCamera.targetTexture = null;
        }
        //CompModularWeapon targetWeaponInt = null;
        IEnumerable<MWCameraRequest> requests = null;
        public void OnPostRender() {
            if (requests == null) return;
            foreach (var i in requests.ToArray()) {
                if (i.material == null) continue;
                var matrix = new Matrix4x4();
                matrix.SetTRS(i.offset, i.rotation, i.scale);
                GenDraw.DrawMeshNowOrLater(i.mesh, matrix, i.material, true);
            }
        }
        private static Camera mwCamera = InitCamera();
        private static MWCameraRenderer mwCameraRenderer;
        private static Camera InitCamera() {
            GameObject gameObject = new GameObject("MWCamera", new Type[] { typeof(Camera) });
            gameObject.SetActive(false);
            gameObject.AddComponent<MWCameraRenderer>();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            Camera component = gameObject.GetComponent<Camera>();
            component.transform.position = new Vector3(0f, 10f, 0f);
            component.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            component.orthographic = true;
            component.cullingMask = 0;
            component.orthographicSize = 1f;
            component.clearFlags = CameraClearFlags.Color;
            component.backgroundColor = new Color(0f, 0f, 0f, 0f);
            component.useOcclusionCulling = false;
            component.renderingPath = RenderingPath.Forward;
            component.nearClipPlane = 5f;
            component.farClipPlane = 12f;

            mwCameraRenderer = gameObject.GetComponent<MWCameraRenderer>();
            return component;
        }
        public struct MWCameraRequest {
            public Material material;
            public Vector3 offset;
            public int layerOrder;
            public Vector3 scale;
            public Quaternion rotation;
            public Mesh mesh;

            public MWCameraRequest(Material material, Vector2 offset, int layerOrder, Vector2 scale, float angle = 0) {
                this.material = material;
                this.offset = new Vector3(offset.x, 0, offset.y);
                this.layerOrder = layerOrder;
                this.scale = new Vector3(Mathf.Abs(scale.x), 1, Mathf.Abs(scale.y));
                this.rotation = Quaternion.AngleAxis(angle, Vector3.up);
                this.mesh = Meshes[(scale.x < 0 ? 1 : 0) + (scale.y < 0 ? 2 : 0)];
            }
            public MWCameraRequest(Material material, Vector2 offset, int layerOrder)
                : this(material, offset, layerOrder, Vector2.one, 0) {
            }
        }


        static Lazy<Mesh[]> meshes = new Lazy<Mesh[]>(
            () => {
                var normal = MeshMakerPlanes.NewPlaneMesh(1f, false);
                var vertFlipped = new Mesh();
                vertFlipped.name = "MW2Mesh_vertf";
                vertFlipped.vertices = normal.vertices;
                vertFlipped.uv = new Vector2[] { new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), };
                vertFlipped.SetTriangles(normal.triangles, 0);
                vertFlipped.RecalculateNormals();
                vertFlipped.RecalculateBounds();
                var bothFlipped = new Mesh();
                bothFlipped.name = "MW2Mesh_bothf";
                bothFlipped.vertices = normal.vertices;
                bothFlipped.uv = new Vector2[] { new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), };
                bothFlipped.SetTriangles(normal.triangles, 0);
                bothFlipped.RecalculateNormals();
                bothFlipped.RecalculateBounds();
                return new Mesh[]{
                normal,
                MeshMakerPlanes.NewPlaneMesh(1f, true),
                vertFlipped,
                bothFlipped
                };
            });
        static Mesh[] Meshes => meshes.Value;
        public static Mesh NormalMesh => Meshes[0];
        public static Mesh MeshHoriFlipped => Meshes[1];
        public static Mesh MeshVertFlipped => Meshes[2];
        public static Mesh MeshBothFlipped => Meshes[3];
    }
}
