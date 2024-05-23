using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public class RenderingPipelineDefines{
    private enum PipelineType{
        Unsupported,
        BuiltInPipeline,
        UniversalPipeline,
        HDPipeline
    }

    static RenderingPipelineDefines(){
        UpdateDefines();
    }

    /// <summary>
    /// Update the unity pipeline defines for URP
    /// </summary>
    static void UpdateDefines(){
        var pipeline = GetPipeline();
        if (pipeline == PipelineType.UniversalPipeline)
        { AddDefine("UNITY_PIPELINE_URP"); }
        else
        { RemoveDefine("UNITY_PIPELINE_URP"); }
            
        if (pipeline == PipelineType.HDPipeline)
        { AddDefine("UNITY_PIPELINE_HDRP"); }
        else
        { RemoveDefine("UNITY_PIPELINE_HDRP"); }
    }


    /// <summary>
    /// Returns the type of renderpipeline that is currently running
    /// </summary>
    /// <returns></returns>
    private static PipelineType GetPipeline(){
        #if UNITY_2019_1_OR_NEWER
        if (GraphicsSettings.renderPipelineAsset != null){
            var rendering = GraphicsSettings.renderPipelineAsset.GetType().ToString();
            if (rendering.Contains("HDRenderPipelineAsset"))
            { return PipelineType.HDPipeline; }

            if (rendering.Contains("UniversalRenderPipelineAsset") || rendering.Contains("LightweightRenderPipelineAsset"))
            { return PipelineType.UniversalPipeline; }
                
            return PipelineType.Unsupported;
        }
            
        #elif UNITY_2017_1_OR_NEWER
        if (GraphicsSettings.renderPipelineAsset != null)
            return PipelineType.Unsupported;
        #endif
        return PipelineType.BuiltInPipeline;
    }

    /// <summary>
    /// Add a custom define
    /// </summary>
    /// <param name="define"></param>
    private static void AddDefine(string define){
        var definesList = GetDefines();
        if (!definesList.Contains(define)){
            definesList.Add(define);
            SetDefines(definesList);
        }
    }

    /// <summary>
    /// Remove a custom define
    /// </summary>
    /// <param name="define"></param>
    public static void RemoveDefine(string define){
        var definesList = GetDefines();
        if (definesList.Contains(define)){
            definesList.Remove(define);
            SetDefines(definesList);
        }
    }

    public static List<string> GetDefines(){
        var target = EditorUserBuildSettings.activeBuildTarget;
        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        return defines.Split(';').ToList();
    }

    public static void SetDefines(List<string> definesList){
        var target = EditorUserBuildSettings.activeBuildTarget;
        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
        var defines = string.Join(";", definesList.ToArray());
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
    }
}