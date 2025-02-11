﻿using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace ModularWeapons2 {
    public class MWCameraRenderer : MonoBehaviour {
        public static void Render(RenderTexture renderTexture, CompModularWeapon targetWeapon) {
            float orthographicSize = mwCamera.orthographicSize;
            mwCameraRenderer.targetWeaponInt = targetWeapon;
            mwCamera.SetTargetBuffers(renderTexture.colorBuffer, renderTexture.depthBuffer);
            mwCamera.Render();
            mwCameraRenderer.targetWeaponInt = null;
            mwCamera.orthographicSize = orthographicSize;
            mwCamera.targetTexture = null;
        }
        CompModularWeapon targetWeaponInt = null;
        public void OnPostRender() {
            foreach(var i in targetWeaponInt.GetRequestsForRenderCam().OrderBy(t=>t.layerOrder)) {
                var matrix = new Matrix4x4();
                matrix.SetTRS(i.offset, Quaternion.identity, Vector3.one);
                GenDraw.DrawMeshNowOrLater(MeshMakerPlanes.NewPlaneMesh(1f, false), Quaternion.Euler(90, 0, 0) * i.offset, Quaternion.identity, i.material, true);
            }
        }
        private static Camera mwCamera = InitCamera();
        private static MWCameraRenderer mwCameraRenderer;
        private static Camera InitCamera() {
            GameObject gameObject = new GameObject("MWCamera", new Type[]{typeof(Camera)});
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
            public Vector2 offset;
            public int layerOrder;
            public MWCameraRequest(Material material, Vector2 offset, int layerOrder) {
                this.material = material;
                this.offset = offset;
                this.layerOrder = layerOrder;
            }
        }
    }
}
