using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Xml; 
using System.Xml.Serialization; 
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
public static class ImportB3D
{
    public static bool saveAssets = true;
    public static bool saveKeysFrames = false;//teste
    public static bool useTangents = true;
    public static bool isStatic = true;


    private static BinaryReader file;
    private static int b3d_tos;
    private static int VerticesStart;
    private static int[] b3d_stack;
    private static float framesPerSecond;
    private static int NumFrames;
    private static List<VertexBone> listVertex ;
    private static List<CoreJoint> listJoints;
    private static List<B3DBone> Bones ;
    private static List<B3dTexture> textures ;
  
    private static List<B3Brush> brushes;
    private static List<int>AnimatedVertices_VertexID;
    private static List<int>AnimatedVertices_BufferID;
    private static List<CoreMesh>surfaces;
    private static  string rootPath;
    private static  string importingAssetsDir;

    [MenuItem("Assets/Djoker Tools/Import/Blitz3D")]
    static void init()
    {

        string filename = Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject));
          rootPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject));


        if (Path.GetExtension(filename).ToUpper() != ".B3D")
        {
            Debug.LogError("File unknow:"+Path.GetExtension(filename).ToUpper());
            return;     
        }
          listVertex = new List<VertexBone>();
          listJoints = new List<CoreJoint>();
          Bones = new List<B3DBone>();

          textures = new List<B3dTexture>();
        brushes = new List<B3Brush>();
          AnimatedVertices_VertexID=new List<int>();
         AnimatedVertices_BufferID=new List<int>();
         surfaces=new List<CoreMesh>();

        b3d_tos = 0;
        b3d_stack = new int[100];
        VerticesStart = 0;
        file = null;

        readMesh(rootPath, filename, "");

        listVertex.Clear();
        listVertex = null;
        listJoints.Clear();
        listJoints =null;
        Bones.Clear();
        Bones = null;
        textures.Clear();
        textures = null;
        brushes.Clear();
        brushes = null;
        AnimatedVertices_VertexID.Clear();
        AnimatedVertices_VertexID=null;
        AnimatedVertices_BufferID.Clear();
        AnimatedVertices_BufferID=null;
        for (int i = 0; i < surfaces.Count; i++)
        {
            surfaces[i].dispose();
        }
        surfaces=null;
        file = null;
        b3d_tos = 0;
        b3d_stack = null;

    }

    private static string readUTFBytes(int count)
    {

        int i = 0;
        byte[] b = file.ReadBytes (count);
        char[] namestr = new char[count];
        while ((i < b.Length) && (b[i] != 0)) 
        {
            namestr[i]=Convert.ToChar(b[i]);
            i++;
        }

        string name = new string (namestr);
        name.Trim ();

        return name;
    }
    private static string readstring()
    {
        string str = "";
        while (true)
        {
            byte b = file.ReadByte();
            if (b == 0)
            {
                break;
            }
            str += (char)b;
        }

        return str;
    }

    public static Color ReadColor()
    {
        var color = new Color();
        color.r = file.ReadSingle();
        color.g = file.ReadSingle();
        color.b = file.ReadSingle();
        color.a = file.ReadSingle();
        return color;
    }

    public static Vector2 ReadVector2()
    {
        var vector2 = new Vector2();
        vector2.x = file.ReadSingle();
        vector2.y = file.ReadSingle();
        return vector2;
    }

    public static Vector3 ReadVector3()
    {
        var v3 = new Vector3();
        v3.x = file.ReadSingle();
        v3.y = file.ReadSingle();
        v3.z = file.ReadSingle();
        return v3;
    }

    public static Quaternion ReadQuaternion()
    {
        var q = new Quaternion();
        q.w = file.ReadSingle();
        q.x = file.ReadSingle();
        q.y = file.ReadSingle();
        q.z = file.ReadSingle();
        q = Quaternion.Inverse(q);
        return q;
    }
    private static int   getChunkSize()
    { 
        return (int)(b3d_stack[b3d_tos] - (int)file.BaseStream.Position);
    }

    private static void breakChunk()
    {
      //  file.BaseStream.Seek(b3d_stack[b3d_tos], SeekOrigin.Begin);
        file.BaseStream.Position = b3d_stack[b3d_tos];
        b3d_tos--;
    }


    private static string  ReadChunk()
    {
        string tag = readUTFBytes(4);
        int size = file.ReadInt32();
        b3d_tos++;
        b3d_stack[b3d_tos] =(int) (file.BaseStream.Position + size);
        return tag;
    }

    private static void trace(string msg)
    {
        Debug.LogWarning(msg);
    }

   
   
    public class VertexWight
    {
        public int vertex_id;
        public int boneId;
        public float Weight;
        public VertexWight()
        {
            boneId = 0;
            Weight = 0;
            vertex_id=0;
        }


    }

   

    public class VertexBone
    {
        public Vector3 Pos;
        public Vector3 Normal;
        public Vector2 TCoords0;
        public Vector2 TCoords1;
        public Color color;
        public bool useUv1;
        public bool useUv2;
        public bool useNormals;
        public VertexWight[] bones;
        public int numBones;
        public VertexBone()
        {
            bones = new VertexWight[4];
            for (int i = 0; i < 4; i++)
            {
                bones[i] = new VertexWight();
            }
            Pos=new Vector3();
            Normal=new Vector3();
            TCoords0=new Vector2();
            TCoords1=new Vector2();
            color=Color.white;
            numBones = 0;
            useUv1=false;
            useUv2=false;
            useNormals=false;
        }

      

    }

    public class B3DBone
    {
        public List<VertexBone> vertex;
        public B3DBone()
        {
            vertex=new List<VertexBone>();
        }

    }
    public class B3dTexture
    {
        public Texture2D text;
        public Vector2 Scale;
        public Vector2 Position;
        public float Rotation;
        public float Blend;
        public B3dTexture()
        {
            Scale=Vector2.zero;
            Position=Vector2.zero;
            text=null;
        }
        
    }
    public class B3Brush
    {
        public string name;
        public float alpha;
        public float shiness;
        public int blend;
        public int numTextures;
        public Color color = new Color(1, 1, 1, 1);
        public List<B3dTexture> textures;


        public B3Brush()
        {
            textures=new List<B3dTexture>();
        }

    }

    public class CoreJoint
    {

      public  AnimationCurve scaleXcurve;
      public  AnimationCurve scaleYcurve;
      public   AnimationCurve scaleZcurve;

        public  AnimationCurve posXcurve;
        public AnimationCurve posYcurve;
        public AnimationCurve posZcurve;

        public AnimationCurve rotXcurve;
        public AnimationCurve rotYcurve;
        public AnimationCurve rotZcurve;
        public AnimationCurve rotWcurve;

        public List<VertexWight> weights;
      
        public string Name;
        public string Path;
        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Orientation;
        public CoreJoint parent;
        public GameObject joint;
        public int index;
        public bool containsPosition;
        public bool containsScale;
        public bool containsRotation;


        public CoreJoint()
        {
            
            index=0;
            Name = "";
            parent = null;
            Path = "";

            Position=new Vector3(0,0,0);
            Scale=new Vector3(1,1,1);
            Orientation=Quaternion.identity;
            weights = new List<VertexWight>();


            //joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            joint = new GameObject("");

             scaleXcurve = new AnimationCurve();
             scaleYcurve = new AnimationCurve();
             scaleZcurve = new AnimationCurve();

             posXcurve = new AnimationCurve();
             posYcurve = new AnimationCurve();
             posZcurve = new AnimationCurve();

             rotXcurve = new AnimationCurve();
             rotYcurve = new AnimationCurve();
             rotZcurve = new AnimationCurve();
             rotWcurve = new AnimationCurve();

        }

        public void addCurves(AnimationClip clip)
        {
            if (containsPosition)//position
            {
               clip.SetCurve(Path, typeof(Transform), "localPosition.x", posXcurve);
               clip.SetCurve(Path, typeof(Transform),   "localPosition.y", posYcurve);
               clip.SetCurve( Path, typeof(Transform), "localPosition.z", posZcurve);

            }
            if (containsScale)//scale
            {
             
                clip.SetCurve(Path, typeof(Transform), "m_LocalScale.x", scaleXcurve);
                clip.SetCurve(Path, typeof(Transform), "m_LocalScale.y", scaleYcurve);
                clip.SetCurve(Path, typeof(Transform), "m_LocalScale.z", scaleZcurve);
            }

            if (containsRotation)//rotation
            {
                clip.SetCurve( Path, typeof(Transform), "localRotation.x",rotXcurve);
                clip.SetCurve( Path, typeof(Transform), "localRotation.y", rotYcurve);
                clip.SetCurve( Path, typeof(Transform), "localRotation.z", rotZcurve);
                clip.SetCurve( Path, typeof(Transform), "localRotation.w", rotWcurve);

            }
        }

        public void addPositionCurve(float x,float y ,float z,float time)
        {
            posXcurve.AddKey(time, x);
            posYcurve.AddKey(time, y);
            posZcurve.AddKey(time, z);
        }
        public void addScaleCurve(float x,float y ,float z,float time)
        {
            scaleXcurve.AddKey(time, x);
            scaleYcurve.AddKey(time, y);
            scaleZcurve.AddKey(time, z);
        }

        public void addRotationCurve(Quaternion rotation,float time)
        {
            rotXcurve.AddKey(time, rotation.x);
            rotYcurve.AddKey(time, rotation.y);
            rotZcurve.AddKey(time, rotation.z);
            rotWcurve.AddKey(time, rotation.w);
        }




    }

    public class CoreMesh
    {
        public Mesh geometry;
        public string name;

       
        public List<BoneWeight> boneWeightsList;
        public Material material;
        public List<Vector3> vertices;
        public List<Color> colors;
        public List<Vector3> normals;
        public List<Vector2> uvcoords0;
        public List<Vector2> uvcoords1;
        public List<int> faces;
        public GameObject meshContainer;
        public SkinnedMeshRenderer meshRenderer;
        public GameObject Root;
        public B3Brush brush;


        public CoreMesh(GameObject meshRoot, GameObject root, string name)
        {
            this.name = name;
            this.Root = root;
            meshContainer = new GameObject(name);
            meshContainer.transform.parent = meshRoot.transform;
            meshRenderer = (SkinnedMeshRenderer)meshContainer.AddComponent(typeof(SkinnedMeshRenderer));
            meshRenderer.sharedMesh = new Mesh();
            geometry = meshRenderer.sharedMesh;
         
            boneWeightsList = new List<BoneWeight>();
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            uvcoords0 = new List<Vector2>();
            uvcoords1 = new List<Vector2>();
            colors=new List<Color>();
            brush=null;
           
            faces = new List<int>();
          
        }
        public void addVertex(Vector3 pos, Vector3 normal,Color c, Vector2 uv0,Vector2 uv1)
        {
            vertices.Add(pos);
            normals.Add(normal);
            colors.Add(c);
            uvcoords0.Add(uv0);
            uvcoords1.Add(uv1);
   

        }
        public void addTexCoords(Vector2 uv,int layer)
        {
            if (layer == 0)
            {
                uvcoords0.Add(uv);
            }
            else
            {
                uvcoords1.Add(uv);
            }



        }
        public void addVertexColor(Color c)
        {
            colors.Add(c);

        }

        public void addVertex(Vector3 pos)
        {
            vertices.Add(pos);
      
        }
        public void addNormal( Vector3 normal)
        {
            normals.Add(normal);
        
        }
 

        public void addFace(int a, int b, int c)
        {
            faces.Add(a);
            faces.Add(b);
            faces.Add(c);
        }
       
        public void addBone(BoneWeight b)
        {
            boneWeightsList.Add(b);

        }
      

       
        private void SolveTangentsForMesh()
        {
            int vertexCount = geometry.vertexCount;
            Vector3[] vertices = geometry.vertices;
            Vector3[] normals = geometry.normals;
            Vector2[] texcoords = geometry.uv;
            int[] triangles = geometry.triangles;
            int triangleCount = triangles.Length / 3;

            Vector4[] tangents = new Vector4[vertexCount];
            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            int tri = 0;

            for (int i = 0; i < (triangleCount); i++)
            {
                int i1 = triangles[tri];
                int i2 = triangles[tri + 1];
                int i3 = triangles[tri + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = texcoords[i1];
                Vector2 w2 = texcoords[i2];
                Vector2 w3 = texcoords[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float r = 1.0f / (s1 * t2 - s2 * t1);
                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;

                tri += 3;
            }

            for (int i = 0; i < (vertexCount); i++)
            {
                Vector3 n = normals[i];
                Vector3 t = tan1[i];

                // Gram-Schmidt orthogonalize
                Vector3.OrthoNormalize(ref n, ref t);

                tangents[i].x = t.x;
                tangents[i].y = t.y;
                tangents[i].z = t.z;

                // Calculate handedness
                tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
            }

            geometry.tangents = tangents;
        }

       
        public void build()
        {
            if (!isStatic)
            {
                List<Transform> bones = new List<Transform>();
                List<Matrix4x4> bindposes = new List<Matrix4x4>();

                for (int i = 0; i < listJoints.Count; i++)
                {
                    CoreJoint joint = listJoints[i];
                    Transform bone = joint.joint.transform;
                    bones.Add(bone);
                    bindposes.Add(bone.worldToLocalMatrix * Root.transform.localToWorldMatrix);
                }

                geometry.bindposes = bindposes.ToArray();
                meshRenderer.bones = bones.ToArray();

            }

            int numVertices = vertices.Count;
     
            geometry.vertices = vertices.ToArray();
            geometry.triangles = faces.ToArray();
          
            if (colors.Count == numVertices)
            {
                geometry.colors = colors.ToArray();
            }


            if (normals.Count == numVertices)
            {
                geometry.normals = normals.ToArray();
            }
            else
            {
                geometry.RecalculateNormals();
            }
            if (uvcoords0.Count == numVertices)
            {
                geometry.uv = uvcoords0.ToArray();
            }
            if (uvcoords1.Count == numVertices)
            {
                geometry.uv2 = uvcoords1.ToArray();
            }
            if(useTangents) SolveTangentsForMesh();


            if (!isStatic)
            {
                geometry.boneWeights = boneWeightsList.ToArray();
            }

            geometry.RecalculateBounds();
            geometry.Optimize();
            meshRenderer.sharedMesh = geometry;

       
            if (brush != null)
            {
                var material = new Material(Shader.Find("Diffuse"));
                material.name = brush.name;
                material.color = brush.color;
                if(brush.textures[0]!=null)
                {
                    material.mainTexture = brush.textures[0].text;
                    material.mainTextureOffset = brush.textures[0].Position;
                    material.mainTextureScale = brush.textures[0].Scale;
                }
                if (saveAssets)
                {
                    string meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + material.name + ".asset");
                    Debug.LogWarning("Save material:" + meshAssetPath);
                    AssetDatabase.CreateAsset(material, meshAssetPath);
                }
                meshRenderer.sharedMaterial = material;
            }
            else
            {
                var material = new Material(Shader.Find("Diffuse"));
                meshRenderer.sharedMaterial = material;
                if (saveAssets)
                {
                    string meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + material.name + ".asset");
                    Debug.LogWarning("Save material:" + meshAssetPath);
                    AssetDatabase.CreateAsset(material, meshAssetPath);
                }
            }
      


        }
        public void dispose()
        {
            
            boneWeightsList.Clear();
            vertices.Clear();
            normals.Clear();
            faces.Clear();

            uvcoords0.Clear();
            uvcoords1.Clear();

            boneWeightsList = null;
            vertices = null;
            normals = null;
            faces = null;
            uvcoords0 = null;
            uvcoords1 = null;
        }


    }

    private static void readMesh(string path, string filename, string texturepath)
    {
        

        if (File.Exists(path + "/" + filename))
        {
                string nm = Path.GetFileNameWithoutExtension(filename);
                importingAssetsDir = "Assets/Prefabs/" + nm + "/";

                if (saveAssets)
                {
                if (!Directory.Exists(importingAssetsDir))
                {
                    Directory.CreateDirectory(importingAssetsDir);
                }
               
                }


            trace("load file :"+path + "/" + filename);

            using (FileStream fs = File.OpenRead(path + "/" + filename))
            {

              
                file = new BinaryReader(fs);
                file.BaseStream.Position = 0;



                GameObject ObjectRoot = new GameObject(nm);
                GameObject meshContainer = new GameObject("Surfaces");
                meshContainer.transform.parent = ObjectRoot.transform;
                //   GameObject Skeleton = new GameObject("Skeleton");
                //   Skeleton.transform.parent= ObjectRoot.transform;
               



                string tag = ReadChunk();
                int version = file.ReadInt32();

                trace(tag + ",  Version :" + version);

                while (getChunkSize() != 0)
                {
                    string ChunkName = ReadChunk();
   
                    if (ChunkName == "TEXS")
                    {
                        readTEX(); 
                    }
                    else if (ChunkName == "BRUS")
                    {
              
                        readBRUS();
                    }
                    else if (ChunkName == "NODE")
                    {
              
                        readNODE(ObjectRoot, meshContainer, null, importingAssetsDir);
                    }
                    breakChunk();
                }
                breakChunk();
               

                if (!isStatic)
                {
                    AnimationClip clip = new AnimationClip();
                    clip.name = nm + "_anim";
                    clip.wrapMode = WrapMode.Loop;
                    clip.frameRate = framesPerSecond;
        
                    for (int b = 0; b < listJoints.Count; b++)
                    {
                        CoreJoint joint = listJoints[b];
                        joint.addCurves(clip);
                        // trace("PATH:Skeleton" + joint.Path);
                    }

                    clip.legacy = true;
                    Animation anim = (UnityEngine.Animation)ObjectRoot.AddComponent(typeof(Animation));
                    anim.AddClip(clip, clip.name);
                    anim.clip = clip;
                    anim.playAutomatically = true;


                    if (saveAssets)
                    {
                  
                        string clipAssetPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + clip.name + ".asset");
                        AssetDatabase.CreateAsset(clip, clipAssetPath);
          
                    }
                }

                for (int i = 0; i < surfaces.Count; i++)
                {
                    CoreMesh mesh = surfaces[i];

                    if (!isStatic)
                    {
                        B3DBone bone = Bones[i];

                
                   

                   
                        for (int j = 0; j < bone.vertex.Count; j++)
                        {
                        
                            VertexBone vertex = bone.vertex[j];
                            BoneWeight b = new BoneWeight();
                            b.boneIndex0 = vertex.bones[0].boneId;
                            b.weight0 = vertex.bones[0].Weight;
                     
                            b.boneIndex1 = vertex.bones[1].boneId;
                            b.weight1 = vertex.bones[1].Weight;

                            b.boneIndex2 = vertex.bones[2].boneId;
                            b.weight2 = vertex.bones[2].Weight;

                            b.boneIndex3 = vertex.bones[3].boneId;
                            b.weight3 = vertex.bones[3].Weight;
                     
                            mesh.addBone(b);

                    
                        }
                    }
                  
                    mesh.build();

                    if (saveAssets)
                    {
                        string meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + mesh.name + "_" + i + ".asset");
                        AssetDatabase.CreateAsset(mesh.geometry, meshAssetPath);
                    }
                }


                if (saveAssets)
                {

                    string prefabPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + Path.GetFileNameWithoutExtension(filename) + ".prefab");
                    var prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
                    PrefabUtility.ReplacePrefab(ObjectRoot, prefab, ReplacePrefabOptions.ConnectToPrefab);
                    AssetDatabase.Refresh();
                }

                if(saveKeysFrames)
                {
                    /*
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;

                XmlWriter textWriter = XmlWriter.Create(importingAssetsDir + "keyframes.xml", settings);
                textWriter.WriteStartDocument();
                textWriter.WriteComment("by luis santos aka djoker");
                textWriter.WriteStartElement("KeyFrames");

                    textWriter.WriteStartAttribute("Fps");
                    textWriter.WriteValue(framesPerSecond);
                    textWriter.WriteEndAttribute();
                    textWriter.WriteStartAttribute("NumFrames");
                    textWriter.WriteValue(NumFrames);
                    textWriter.WriteEndAttribute();


                // BinaryWriter writer = new BinaryWriter(new FileStream("out.lvl", FileMode.Create));
                //  writer.Close();


                for (int b = 0; b < listJoints.Count; b++)
                {
                    CoreJoint joint = listJoints[b];
                     

                    textWriter.WriteStartElement("Joint");

                    textWriter.WriteStartAttribute("ContainsPosition");
                    textWriter.WriteValue(joint.containsPosition);
                    textWriter.WriteEndAttribute();
                      
                    textWriter.WriteStartAttribute("ContainsScale");
                    textWriter.WriteValue(joint.containsScale);
                    textWriter.WriteEndAttribute();

                    textWriter.WriteStartAttribute("ContainsRotation");
                    textWriter.WriteValue(joint.containsRotation);
                    textWriter.WriteEndAttribute();


                    textWriter.WriteElementString("Name", joint.Name);
                    textWriter.WriteElementString("Path", joint.Path);
            
                    if (joint.containsPosition)
                    {
                        textWriter.WriteStartElement("Positions");

                        for (int i = 0; i < joint.rotXcurve.length; i++)
                        {
                            Keyframe xkey = joint.posXcurve.keys[i];
                            Keyframe ykey = joint.posYcurve.keys[i];
                            Keyframe zkey = joint.posZcurve.keys[i];

                            float time = xkey.time;
                            float x = xkey.value;
                            float y = ykey.value;
                            float z = zkey.value;


                            textWriter.WriteStartElement("Poition");
                            textWriter.WriteAttributeString("Time", time.ToString());
                            textWriter.WriteAttributeString("X", x.ToString());
                            textWriter.WriteAttributeString("Y", y.ToString());
                            textWriter.WriteAttributeString("Z", z.ToString());

                            textWriter.WriteEndElement();


                        }
                        textWriter.WriteEndElement();
                    }

                    if (joint.containsScale)
                    {
                        textWriter.WriteStartElement("Scales");
                        for (int i = 0; i < joint.rotXcurve.length; i++)
                        {
                            Keyframe xkey = joint.scaleXcurve.keys[i];
                            Keyframe ykey = joint.scaleYcurve.keys[i];
                            Keyframe zkey = joint.scaleZcurve.keys[i];

                            float time = xkey.time;
                            float x = xkey.value;
                            float y = ykey.value;
                            float z = zkey.value;


                            textWriter.WriteStartElement("Scale");
                            textWriter.WriteAttributeString("Time", time.ToString());
                            textWriter.WriteAttributeString("X", x.ToString());
                            textWriter.WriteAttributeString("Y", y.ToString());
                            textWriter.WriteAttributeString("Z", z.ToString());

                            textWriter.WriteEndElement();


                        }
                        textWriter.WriteEndElement();
                    }

                    if (joint.containsRotation)
                    {
                      
                        textWriter.WriteStartElement("Rotations");
                        for (int i = 0; i < joint.rotXcurve.length; i++)
                        {
                            Keyframe xkey = joint.rotXcurve.keys[i];
                            Keyframe ykey = joint.rotYcurve.keys[i];
                            Keyframe zkey = joint.rotZcurve.keys[i];
                            Keyframe wkey = joint.rotWcurve.keys[i];

                                var fps = 1f *  framesPerSecond;

                            float time = xkey.time ;

                            float x = xkey.value;
                            float y = ykey.value;
                            float z = zkey.value;
                            float w = wkey.value;

                            textWriter.WriteStartElement("Rotation");
                            textWriter.WriteAttributeString("Time", time.ToString());
                            textWriter.WriteAttributeString("X", x.ToString());
                            textWriter.WriteAttributeString("Y", y.ToString());
                            textWriter.WriteAttributeString("Z", z.ToString());
                            textWriter.WriteAttributeString("W", w.ToString());
                            textWriter.WriteEndElement();


                        }
                        textWriter.WriteEndElement();
                    }


                    textWriter.WriteEndElement();
                 
                }
      
                textWriter.WriteEndElement();
                textWriter.WriteEndDocument();
                textWriter.Flush();
                textWriter.Close();



                    AnimationClip clip = new AnimationClip();

                    for (int b = 0; b < listJoints.Count; b++)
                    {
                        CoreJoint joint = listJoints[b];

                        if (joint.containsRotation)
                        {
                         
                            AnimationCurve rotXcurve = new AnimationCurve();
                            AnimationCurve rotYcurve = new AnimationCurve();
                            AnimationCurve rotZcurve = new AnimationCurve();
                            AnimationCurve rotWcurve = new AnimationCurve();

                            int to = 3;// (int)( 14 /  framesPerSecond) % NumFrames;

                            Debug.LogWarning(" to frame:" + to);
                            for (int i = 0; i < to; i++)
                            {
                                
                                Keyframe xkey = joint.rotXcurve.keys[i];
                                Keyframe ykey = joint.rotYcurve.keys[i];
                                Keyframe zkey = joint.rotZcurve.keys[i];
                                Keyframe wkey = joint.rotWcurve.keys[i];
                                float time = xkey.time ;

                                rotXcurve.AddKey(time, xkey.value);
                                rotYcurve.AddKey(time, ykey.value);
                                rotZcurve.AddKey(time, zkey.value);
                                rotWcurve.AddKey(time, wkey.value);
                             
                            }
                            clip.SetCurve( joint.Path, typeof(Transform), "localRotation.x",rotXcurve);
                            clip.SetCurve( joint.Path, typeof(Transform), "localRotation.y", rotYcurve);
                            clip.SetCurve( joint.Path, typeof(Transform), "localRotation.z", rotZcurve);
                            clip.SetCurve( joint.Path, typeof(Transform), "localRotation.w", rotWcurve);
                        }

                    }
                    clip.name = nm + "_walk";
                    clip.wrapMode = WrapMode.Loop;
                    clip.legacy = true;
                    string clipAssetPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + clip.name + ".asset");
                    AssetDatabase.CreateAsset(clip, clipAssetPath);
*/

            }

            }//file open

            Debug.LogWarning(path + "/" + filename + " Imported ;) ");
        }

    }
    private static void readTEX()
    {
        while (getChunkSize()!=0) 
        {
            string fileName = Path.GetFileName(readstring());



            B3dTexture text = new B3dTexture();
            trace("Texture name:"+rootPath+"/"+fileName);

            text.text = loadexture(rootPath+"/"+fileName);

            file.ReadInt32();//flags
            file.ReadInt32();//blend
            text.Position=ReadVector2();
            text.Scale=ReadVector2();
            text.Rotation=file.ReadSingle();//rotation

            textures.Add(text);
        }

    }
    private static void readBRUS()
    {
        int n_texes = file.ReadInt32();
     

        while (getChunkSize()!=0) 
        {

            B3Brush brush = new B3Brush();


            brush.name="brush_"+ readstring();
            brush.color = ReadColor();
            brush.shiness=file.ReadSingle();//shiness
            brush.blend= file.ReadInt32();//blend
            int  fx = file.ReadInt32();//fx
            brush.numTextures=n_texes;

            trace("num textures in brush:" + n_texes+" name :"+ brush.name);

            for (int i=0; i < n_texes ; i++)
            {
                int textid = file.ReadInt32();//texid
                if (textures.Count > 0)
                {
                    brush.textures.Add(textures[textid]);
                }
            }


          brushes.Add(brush);
        }
    }

    private static  void readKEYS( CoreJoint bone)
    {
   
        int Flags = file.ReadInt32();


        bool containsPosition=(Flags & 1) != 0;
        bool containsScale   =(Flags & 2) != 0;
        bool containsRotation=(Flags & 4) != 0;


        bone.containsPosition = containsPosition;
        bone.containsScale    = containsScale;
        bone.containsRotation = containsRotation;
       
        var fps = 1f /  framesPerSecond;

        while (getChunkSize()!=0) 
        {
            int frame = file.ReadInt32();

           

            if (containsPosition)//position
            {
                Vector3 position = ReadVector3();
                bone.addPositionCurve(position.x, position.y, position.z, frame*fps );

          
            }
            if (containsScale)//scale
            {
                Vector3 scale = ReadVector3();
                bone.addScaleCurve(scale.x, scale.y, scale.z, frame*fps );
        
            }
            if (containsRotation)//rotation
            {
                Quaternion rotation = ReadQuaternion();
                bone.addRotationCurve(rotation, frame*fps );

//                Debug.Log("Frame:" + frame + " , time:" + frame * fps);
            }


        }

    }

    private static void readANIM()
    {
      //  trace("READ ANIMAION");
         isStatic=false;

         int flags=  file.ReadInt32();//flags
         NumFrames=  file.ReadInt32();//ketframecount
         framesPerSecond =  file.ReadSingle();//fps
        if (framesPerSecond < 1f)
        {
            framesPerSecond = 1;
        }
     
        float duration =  (NumFrames / framesPerSecond);
        trace("Animation - duration :" +duration +",flags:" + flags + ", totalframes:" + NumFrames + ", fps:" + framesPerSecond);

    }

    private static void readBone( CoreJoint bone)
    {
        int vertex_id = 0;
        int buffer_id = 0;

   
        while (getChunkSize()!=0) 
        {
            int globalVertexID  =file.ReadInt32();//vertexid
            float strength =  file.ReadSingle();//wight



            globalVertexID += VerticesStart;

            if (AnimatedVertices_VertexID[globalVertexID]==-1)
            {
                trace(" Weight has bad vertex id (no link to meshbuffer index found)");
            } else 
            {

                vertex_id = AnimatedVertices_VertexID[globalVertexID];
                buffer_id = AnimatedVertices_BufferID[globalVertexID];

         

                if (strength > Bones[buffer_id].vertex[vertex_id].bones[0].Weight)
                {
                    Bones[buffer_id].vertex[vertex_id].bones[3].boneId = Bones[buffer_id].vertex[vertex_id].bones[2].boneId;
                    Bones[buffer_id].vertex[vertex_id].bones[3].Weight = Bones[buffer_id].vertex[vertex_id].bones[2].Weight;

                    Bones[buffer_id].vertex[vertex_id].bones[1].boneId = Bones[buffer_id].vertex[vertex_id].bones[0].boneId;
                    Bones[buffer_id].vertex[vertex_id].bones[1].Weight = Bones[buffer_id].vertex[vertex_id].bones[0].Weight;

                    Bones[buffer_id].vertex[vertex_id].bones[0].boneId = bone.index;
                    Bones[buffer_id].vertex[vertex_id].bones[0].Weight = strength;
                    Bones[buffer_id].vertex[vertex_id].numBones = 1;

                  

                } else

                    if (strength > Bones[buffer_id].vertex[vertex_id].bones[1].Weight)
                    {
                        Bones[buffer_id].vertex[vertex_id].bones[3].boneId = Bones[buffer_id].vertex[vertex_id].bones[2].boneId;
                        Bones[buffer_id].vertex[vertex_id].bones[3].Weight = Bones[buffer_id].vertex[vertex_id].bones[2].Weight;

                        Bones[buffer_id].vertex[vertex_id].bones[2].boneId = Bones[buffer_id].vertex[vertex_id].bones[1].boneId;
                        Bones[buffer_id].vertex[vertex_id].bones[2].Weight = Bones[buffer_id].vertex[vertex_id].bones[1].Weight;

                        Bones[buffer_id].vertex[vertex_id].bones[1].boneId = bone.index;
                        Bones[buffer_id].vertex[vertex_id].bones[1].Weight = strength;
                        Bones[buffer_id].vertex[vertex_id].numBones = 2;

                      

                  //  Debug.LogError("num bones 2");
                    } else
                        if (strength > Bones[buffer_id].vertex[vertex_id].bones[2].Weight)
                        {


                            Bones[buffer_id].vertex[vertex_id].bones[3].boneId = Bones[buffer_id].vertex[vertex_id].bones[2].boneId;
                            Bones[buffer_id].vertex[vertex_id].bones[3].Weight = Bones[buffer_id].vertex[vertex_id].bones[2].Weight;
                            Bones[buffer_id].vertex[vertex_id].bones[2].boneId = bone.index;
                            Bones[buffer_id].vertex[vertex_id].bones[2].Weight = strength;
                            Bones[buffer_id].vertex[vertex_id].numBones = 3;
                          //  Debug.LogError("num bones 3");


                        } else
                            if (strength > Bones[buffer_id].vertex[vertex_id].bones[3].Weight)
                            {
                                Bones[buffer_id].vertex[vertex_id].bones[3].boneId = bone.index;
                                Bones[buffer_id].vertex[vertex_id].bones[3].Weight = strength;
                                Bones[buffer_id].vertex[vertex_id].numBones = 4;
                               // Debug.LogError("num bones 4");

                            }

            }
           




        }

        //trace(buffer_id+ " ," +VerticesStart);
    }
    private static CoreJoint readNODE(GameObject ObjectRoot ,GameObject MeshContainer,CoreJoint parent,string path)
    {
        
        string name = readstring();

   


        CoreJoint  lastBone= new CoreJoint();
       


        lastBone.Position    = ReadVector3();
        lastBone.Scale       = ReadVector3();
        lastBone.Orientation = ReadQuaternion();

        lastBone.Name = name;
        lastBone.joint.name = name;
        lastBone.index = listJoints.Count;
    
     
        /*
        trace("Name:" + name + " , Position:" +
            lastBone.Position.ToString() +
            ", Rotation :" + lastBone.Orientation.ToString() +
            ", Scale :" + lastBone.Scale.ToString());

       */

        if (parent != null)
        {
            lastBone.parent = parent;
            lastBone.joint.transform.parent = parent.joint.transform;
            lastBone.Path += parent.Path + "/"; 
    
        } else
        {
            lastBone.joint.transform.parent = ObjectRoot.transform;

        }



           
        lastBone.joint.transform.localPosition = lastBone.Position;
        lastBone.joint.transform.localRotation = lastBone.Orientation;
        lastBone.joint.transform.localScale = lastBone.Scale;

     
        lastBone.Path += name;   


       


        listJoints.Add(lastBone);



      


        while (getChunkSize() != 0)
        {
            var ChunkName = ReadChunk();
            if(ChunkName=="MESH") 
            {
                VerticesStart = listVertex.Count;
                readMESH(MeshContainer,ObjectRoot,"mesh");
            } else  
                if(ChunkName=="BONE") 
                {
                    readBone(lastBone);
                } 
            if(ChunkName=="ANIM") 
            {
                readANIM();

            }  else  
                if(ChunkName=="KEYS") 
                {
                    readKEYS(lastBone); 
                } else
                    if(ChunkName=="NODE") 
                    {
                        CoreJoint child = readNODE(ObjectRoot,MeshContainer,lastBone,path);
                       

                       
                    }
            breakChunk();
        }
  
        return lastBone;
    }
    private static void readVTS()
    {


      //  trace("READ VERTEX");

        int flags = file.ReadInt32();
        int tex_coord = file.ReadInt32();
        int texsize = file.ReadInt32();


        bool containsNormals=  (flags & 1) != 0;
        bool containsColors =  (flags & 2) != 0;
            
        int Size = 12 + tex_coord * texsize * 4;
        if(containsNormals) Size += 12;
        if (containsColors) Size += 16;


        int VertexCount = (int)(getChunkSize() / Size);
  

        while (getChunkSize() > 0)
        {
            VertexBone vertex = new VertexBone();
            vertex.Pos = ReadVector3();

            if (containsNormals)
            {
                vertex.Normal= ReadVector3();
                vertex.useNormals=true;
            }

            if (containsColors)
            {
                vertex.color =  ReadColor();

             }

            if (tex_coord == 1)
            {
                if (texsize == 2)
                {
                    vertex.TCoords0.x=file.ReadSingle();//u
                    vertex.TCoords0.y =1*- file.ReadSingle();//v
                    vertex.useUv1=true;
                } else
                {
                    vertex.TCoords0.x =file.ReadSingle();//u
                    vertex.TCoords0.y =1*- file.ReadSingle();//v
                    file.ReadSingle();//w
                    vertex.useUv1=true;
                }
            } else
            {
                if (texsize == 2)
                {
                    vertex.TCoords0.x=file.ReadSingle();//u
                    vertex.TCoords0.y=1*-file.ReadSingle();//v
                    vertex.TCoords1.x=file.ReadSingle();//u
                    vertex.TCoords1.y=1*-file.ReadSingle();//v
                    vertex.useUv1=true;
                    vertex.useUv2=true;
                } else
                {
                    vertex.TCoords0.x =file.ReadSingle();//u
                    vertex.TCoords0.y =1*- file.ReadSingle();//v
                    file.ReadSingle();//w
                    vertex.TCoords1.x =file.ReadSingle();//u
                    vertex.TCoords1.y =1*- file.ReadSingle();//v
                    file.ReadSingle();//w
                    vertex.useUv1=true;
                    vertex.useUv2=true;
                }
            }

            listVertex.Add(vertex);
            AnimatedVertices_VertexID.Add( -1);
            AnimatedVertices_BufferID.Add( -1);
        }



     //   trace("Num vertex:" + listVertex.Count +" , "+VertexCount);

    }
    private static void  readMESH(GameObject parent,GameObject root,string name)
    {

        int  brushID = file.ReadInt32();//brushID


        while (getChunkSize() != 0)
        {
            var ChunkName = ReadChunk();
            if(ChunkName=="VRTS") 
            {
                readVTS();
            } else  
                if(ChunkName=="TRIS") 
                {
                    CoreMesh surf = new CoreMesh(parent, root, name);
                    if (brushID == -1)  brushID = 0;
                    if (brushes.Count > 0)
                    {
                    surf.brush = brushes[brushID];
                    }
                    readTRIS(surf,surfaces.Count,VerticesStart);
                    surfaces.Add(surf);
                } 
            breakChunk();
        }  

    }

    public static  void readTRIS(CoreMesh surf, int surfaceId,int vtStar)
    {

     //   trace("READ TRIS");


        int brushid = file.ReadInt32();
        int TriangleCount =(int)(getChunkSize() / 12);
        bool showwarning = false;
        var vertex_id = new int[3];


        B3DBone bone= new B3DBone();

        while (getChunkSize() != 0)
        {
            vertex_id[0] = file.ReadInt32();
            vertex_id[1] = file.ReadInt32();
            vertex_id[2] = file.ReadInt32();

       
            vertex_id[0] += vtStar;
            vertex_id[1] += vtStar;
            vertex_id[2] += vtStar;

            for (int i=0; i<3;i++)
            {
                if (vertex_id[i] >= AnimatedVertices_VertexID.Count)
                {
                    trace("Illegal vertex index found");
                    return ;
                }

                if (AnimatedVertices_VertexID[ vertex_id[i] ] != -1)
                {
                    if ( AnimatedVertices_BufferID[ vertex_id[i] ] != surfaceId ) //If this vertex is linked in a different meshbuffer
                    {
                        AnimatedVertices_VertexID[ vertex_id[i] ] = -1;
                        AnimatedVertices_BufferID[ vertex_id[i] ] = -1;
                        showwarning = true;

                    }
                }

                if (AnimatedVertices_VertexID[ vertex_id[i] ] == -1) //If this vertex is not in the meshbuffer
                {

                    var vertex = listVertex[ vertex_id[i] ];
                    surf.addVertex(vertex.Pos);
                    surf.addVertexColor(vertex.color);

                    if (vertex.useUv1)
                        surf.addTexCoords(vertex.TCoords0, 0);
                    
                    if (vertex.useUv2)
                        surf.addTexCoords(vertex.TCoords1, 1);
                    
                    if (vertex.useNormals)
                        surf.addNormal(vertex.Normal);


                    
                    
                    bone.vertex.Add(vertex);


                    //create vertex id to meshbuffer index link:
                    AnimatedVertices_VertexID[ vertex_id[i] ] = surf.vertices.Count-1;
                    AnimatedVertices_BufferID[ vertex_id[i] ] = surfaceId;

                }




            }
            surf.addFace(
                AnimatedVertices_VertexID[ vertex_id[0] ],
                AnimatedVertices_VertexID[ vertex_id[1] ],
                AnimatedVertices_VertexID[ vertex_id[2] ]);


          

        }


        if (showwarning)
        {
            Debug.LogWarning("Warning, different meshbuffers linking to the same vertex, this will cause problems with animated meshes");
        }



       
      
        Bones.Add(bone);

      //  surf.material.clone(brushes[brushid]);
      

    }

  




    public static Texture2D loadexture(string texturename)
    {
        Texture2D tex = null;
        string fileName=rootPath+"/"+Path.GetFileNameWithoutExtension(texturename);




        if (File.Exists(texturename))
        {
            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(texturename, typeof(Texture2D));

        }else

            if (File.Exists(fileName + ".png"))
            {
                tex = (Texture2D)AssetDatabase.LoadAssetAtPath(fileName + ".png", typeof(Texture2D));
            }
            else
                if (File.Exists(fileName + ".tga"))
                {
                    tex = (Texture2D)AssetDatabase.LoadAssetAtPath(fileName + ".tga", typeof(Texture2D));
                }
                else

                    if (File.Exists(fileName + ".dds"))
                    {
                        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(fileName + ".dds", typeof(Texture2D));
                    }
                    else
                        if (File.Exists(fileName + ".jpg"))
                        {
                            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(fileName + ".jpg", typeof(Texture2D));
                        }
                        else
                            if (File.Exists(fileName + ".bmp"))
                            {
                                tex = (Texture2D)AssetDatabase.LoadAssetAtPath(fileName + ".bmp", typeof(Texture2D));
                            }

                            else
                            {
                                Debug.LogError(fileName+" , "+texturename +"  dont exits...");
                            }

        return tex;
    }



}


