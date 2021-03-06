using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    [CanEditMultipleObjects]
    // Disable the GUI for additional camera data
    [CustomEditor(typeof(LWRPAdditionalCameraData))]
    class AdditionalCameraDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var data = (LWRPAdditionalCameraData)target;
            data.requiresRenderedTexture = EditorGUILayout.Toggle("Rendered Texture", data.requiresRenderedTexture);
        }

        [MenuItem("CONTEXT/LWRPAdditionalCameraData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            if (EditorUtility.DisplayDialog("Remove Component?", "Are you sure you want to remove this component? If you do, you will lose some settings.", "Remove", "Cancel"))
            {
                Undo.DestroyObjectImmediate(command.context);
            }
        }
    }
}
