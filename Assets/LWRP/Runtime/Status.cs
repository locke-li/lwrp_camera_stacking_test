using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;
using UnityEngine.UI;

[ExecuteInEditMode]
public class Status : MonoBehaviour
{
    private static Status Instance { get; set; }
    public static string CodePath { get; set; }
    public static bool Valid => Instance != null && Blit0?.texture != null && Blit1?.texture != null && Blit2?.texture != null;
    public static RawImage Blit0 => Instance.Blit0Obj;
    public static RawImage Blit1 => Instance.Blit1Obj;
    public static RawImage Blit2 => Instance.Blit2Obj;
    public RawImage Blit0Obj;
    public RawImage Blit1Obj;
    public RawImage Blit2Obj;
    public LightweightRenderPipelineAsset none;
    public LightweightRenderPipelineAsset msaa;
    public LightweightRenderPipelineAsset hdr;
    public LightweightRenderPipelineAsset msaa_hdr;
    Vector2 scrollPos;

    void Awake()
    {
        Instance = this;
        if (Blit0Obj.texture == null) Blit0Obj.texture = RenderTexture.GetTemporary(Screen.width, Screen.height);
        if (Blit1Obj.texture == null) Blit1Obj.texture = RenderTexture.GetTemporary(400, 200);
        if (Blit2Obj.texture == null) Blit2Obj.texture = RenderTexture.GetTemporary(400, 200);
    }

    private void OnGUI() {
        var styleBox = new GUIStyle("Box");
        var styleLabel = new GUIStyle("Label") {
#if !UNITY_EDITOR
            fontSize = 24
#endif
        };
        var pipelineSetting = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
        var supportHDR = pipelineSetting == null ? "unknown" : pipelineSetting.supportsHDR.ToString();
        var width = Screen.width;
        var height = Screen.height;
        GUILayout.BeginArea(new Rect(width * 0.6f, height * 0.25f, width * 0.4f, height * 0.75f), styleBox);
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.BeginHorizontal();
        var normalStyle = new GUIStyle("Button");
        var pressedStyle = new GUIStyle("Button");
        pressedStyle.normal = pressedStyle.active;
        void SettingButton(LightweightRenderPipelineAsset setting, string name) {
            if (GUILayout.Button(name, pipelineSetting == setting ? pressedStyle : normalStyle)) {
                GraphicsSettings.renderPipelineAsset = setting;
            }
        }
        SettingButton(none, nameof(none));
        SettingButton(msaa, nameof(msaa));
        SettingButton(hdr, nameof(hdr));
        SettingButton(msaa_hdr, nameof(msaa_hdr));
        GUILayout.EndHorizontal();
        GUILayout.Label($"CodePath={CodePath}", styleLabel);
        GUILayout.Label($"GPU={SystemInfo.graphicsDeviceName}", styleLabel);
        GUILayout.Label($"API={SystemInfo.graphicsDeviceType}", styleLabel);
        GUILayout.Label($"ColorSpace={QualitySettings.activeColorSpace}", styleLabel);
        GUILayout.Label($"DesiredColorSpace={QualitySettings.desiredColorSpace}", styleLabel);
        GUILayout.Label($"UVStartsAtTop={SystemInfo.graphicsUVStartsAtTop}", styleLabel);
        GUILayout.Label($"TextureCopy={SystemInfo.copyTextureSupport}", styleLabel);
        GUILayout.Label($"MSAA={QualitySettings.antiAliasing}", styleLabel);
        GUILayout.Label($"MSAA_AutoResolve={SystemInfo.supportsMultisampleAutoResolve}", styleLabel);
        GUILayout.Label($"HDR={supportHDR}", styleLabel);
        GUILayout.Label($"HDR_RT_Supported={SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR)}", styleLabel);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
