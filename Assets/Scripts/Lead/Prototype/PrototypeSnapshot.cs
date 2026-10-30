using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GameUp.Lead.Prototype
{
    /// <summary>Adapted from PigeonSimulator5's UIDesignSnapshotRenderer for hidden Windows players.</summary>
    public static class PrototypeSnapshot
    {
        public static void Capture(Canvas canvas, Camera worldCamera, string path, int width, int height)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Visual verification requires a graphics device.");
            var rect = (RectTransform)canvas.transform;
            var scaler = canvas.GetComponent<CanvasScaler>();
            var mode = canvas.renderMode; var renderCamera = canvas.worldCamera;
            var position = rect.position; var rotation = rect.rotation; var scale = rect.localScale; var size = rect.sizeDelta;
            bool scalerEnabled = scaler && scaler.enabled;
            var objects = canvas.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
            var layers = objects.Select(o => o.layer).ToArray();
            int worldMask = worldCamera.cullingMask;
            var previous = RenderTexture.active;
            var worldTarget = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var cameraObject = new GameObject("__LeadCaptureCamera", typeof(Camera));
            var captureCamera = cameraObject.GetComponent<Camera>();
            captureCamera.enabled = false;
            GameObject background = null;
            Texture2D pixels = null;
            try
            {
                float uiScale = 1;
                if (scaler && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    uiScale = Mathf.Pow(2, Mathf.Lerp(Mathf.Log(width / scaler.referenceResolution.x, 2),
                        Mathf.Log(height / scaler.referenceResolution.y, 2), scaler.matchWidthOrHeight));
                if (scaler) scaler.enabled = false;
                canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = captureCamera;
                rect.sizeDelta = new Vector2(width / uiScale, height / uiScale);
                rect.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                rect.localScale = Vector3.one * (10.8f / rect.sizeDelta.y);
                foreach (var item in objects) item.layer = 31;
                worldCamera.cullingMask &= ~(1 << 31);
                Render(worldCamera, worldTarget);

                background = new GameObject("__LeadCaptureBackground", typeof(RectTransform), typeof(RawImage));
                var backgroundRect = (RectTransform)background.transform;
                backgroundRect.SetParent(canvas.transform, false); backgroundRect.SetAsFirstSibling();
                backgroundRect.anchorMin = Vector2.zero; backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;
                background.layer = 31;
                background.GetComponent<RawImage>().texture = worldTarget;
                background.GetComponent<RawImage>().raycastTarget = false;
                captureCamera.transform.position = new Vector3(0, 0, -30);
                captureCamera.orthographic = true; captureCamera.orthographicSize = 5.4f;
                captureCamera.aspect = (float)width / height; captureCamera.cullingMask = 1 << 31;
                captureCamera.clearFlags = CameraClearFlags.SolidColor; captureCamera.backgroundColor = Color.black;
                captureCamera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                Canvas.ForceUpdateCanvases();
                Render(captureCamera, target);
                RenderTexture.active = target;
                pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                var colors = pixels.GetPixels32();
                var first = colors[0]; bool varied = false;
                for (int i = 0; i < colors.Length; i += 97)
                    if (!colors[i].Equals(first)) { varied = true; break; }
                if (!varied) throw new InvalidOperationException("Captured frame was blank: " + path);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                if (background) { background.SetActive(false); UnityEngine.Object.Destroy(background); }
                canvas.renderMode = mode; canvas.worldCamera = renderCamera;
                rect.sizeDelta = size; rect.SetPositionAndRotation(position, rotation); rect.localScale = scale;
                if (scaler) scaler.enabled = scalerEnabled;
                for (int i = 0; i < objects.Length; i++) objects[i].layer = layers[i];
                worldCamera.cullingMask = worldMask; captureCamera.targetTexture = null;
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(worldTarget); RenderTexture.ReleaseTemporary(target);
                if (pixels) UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(cameraObject);
            }
        }

        private static void Render(Camera camera, RenderTexture target)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            else
            {
                var previous = camera.targetTexture;
                camera.targetTexture = target; camera.Render(); camera.targetTexture = previous;
            }
        }
    }
}
