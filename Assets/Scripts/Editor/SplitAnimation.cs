using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO; 
using System.Text; 
/*
* 
* Copyright (c) 2016 Luis Santos AKA DJOKER
 * 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/
public class SplitAnimation : ScriptableWizard
{

    private   string rootPath;
    private   string importingAssetsDir;
    private string filename;

    //public string AssetFolder = "Assets";
    public int StartFrame;
    public int EndFrame;
    public float FrameRate;
    public int TotalOfFrames;
    public bool legacy = true;
    public string Clipname;
    public WrapMode WrapMode=WrapMode.Loop;

    [MenuItem("Assets/Djoker Tools/SplitAnimation")]
    static void CreateWizard()
    {
  
        ScriptableWizard.DisplayWizard("SplitAnimation",typeof(SplitAnimation));
    }
    void OnEnable()
    {
        OnSelectionChange();
    }
    void OnSelectionChange()
    {
        //Check user selection in editor - check for folder selection
        if (Selection.objects != null && Selection.objects.Length == 1)
        {
            
            if (Selection.activeObject != null && Selection.activeObject is AnimationClip)
            {
                AssetDatabase.StartAssetEditing();
                AnimationClip selectedClip = (AnimationClip)Selection.activeObject;
                AnimationClipCurveData[] curves = AnimationUtility.GetAllCurves(selectedClip);

                FrameRate = selectedClip.frameRate;
                legacy = selectedClip.legacy;
                WrapMode = selectedClip.wrapMode;
                Clipname = selectedClip.name;

                int maxTime = -1;
                for (int i = 0; i < curves.Count(); i++)
                {
                    AnimationCurve curve = curves[i].curve;

                    for (int k = 0; k < curve.length; k++)
                    {
                        int newTime = (int)(curve.keys[k].time * selectedClip.frameRate);
                        maxTime = Math.Max(maxTime, newTime);
                    }
                }
                TotalOfFrames = maxTime;
                AssetDatabase.StopAssetEditing();
            }
        }
    }
   
    void OnWizardCreate()
    {
        init();
    }

     void init()
    {

       
            filename = Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject));
            rootPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject));



    
        string nm = Path.GetFileNameWithoutExtension(filename);
        string importingAssetsDir =rootPath+"/";

        if (Selection.activeObject != null && Selection.activeObject is AnimationClip) 
        {
            AssetDatabase.StartAssetEditing();
            AnimationClip selectedClip = (AnimationClip) Selection.activeObject;
            AnimationClipCurveData[] curves = AnimationUtility.GetAllCurves(selectedClip);

            //EditorCurveBinding[] curves = AnimationUtility.GetCurveBindings(selectedClip);

           
           
            if (EndFrame > TotalOfFrames)
            {
                EndFrame = TotalOfFrames;
            }
      
            float fps =  selectedClip.frameRate;
            bool firstCopy=false;
            float startTime=0;
         
            AnimationClip clip = new AnimationClip();
            clip.name = Clipname;
            clip.wrapMode = WrapMode;
            clip.legacy = legacy;
            clip.frameRate = FrameRate;


          
            for (int i = 0; i < curves.Count(); i++)
            {
                AnimationCurve curve = curves[i].curve;
                AnimationCurve newCurve = new AnimationCurve();

                for (int k = 0; k < curve.keys.Length; k++)
                {
                    Keyframe key = curve.keys[k];

                    float frame =(key.time * fps );

           
                    float startFrame =(StartFrame  / fps );
                    float endFrame   =(EndFrame / fps );

                  
            


                    if (key.time >=startFrame && key.time <= endFrame)
                    {
                        if (!firstCopy)
                        {
                            startTime = key.time;
                        }
                        firstCopy = true;

                     //    Debug.Log("Frame:" + frame + ", real time:" + key.time+", Start:"+startFrame+", End:"+endFrame); 
                         key.time = key.time - startTime ;

                         newCurve.AddKey(key);
                    }


                }

          
                string path = curves[i].path;
                string propertyName = curves[i].propertyName;
                clip.SetCurve(path, typeof(Transform), propertyName, newCurve);

            }

            string clipAssetPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + clip.name + ".asset");
            AssetDatabase.CreateAsset(clip, clipAssetPath);

            AssetDatabase.StopAssetEditing();
        }
    }

   

  
}
