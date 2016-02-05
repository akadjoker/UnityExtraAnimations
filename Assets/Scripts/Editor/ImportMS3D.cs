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
public static class ImportMS3D
{
    public static bool saveAssets = true;
    public static bool saveKeysFrames = false;
    public static bool useTangents = true;
    public static bool isStatic = true;


    private static BinaryReader file;

  
    private static List<CoreJoint> listJoints;



    private static float framesPerSecond;
    private static int NumFrames;


    private static List<CoreMesh>surfaces;
    private static  string rootPath;
    private static  string importingAssetsDir;

    [MenuItem("Assets/Djoker Tools/Import/MilkShape")]
    static void init()
    {

        string filename = Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject));
          rootPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject));

        if (Path.GetExtension(filename).ToUpper() != ".MS3D")
        {
            Debug.LogError("File unknow:"+Path.GetExtension(filename).ToUpper());
            return;     
        }

        Debug.ClearDeveloperConsole();
         
        listJoints = new List<CoreJoint>();
        surfaces=new List<CoreMesh>();

    
        file = null;

        readMesh(rootPath, filename, "");
       
    
        listJoints.Clear();
        listJoints =null;
      
   
     
        for (int i = 0; i < surfaces.Count; i++)
        {
            surfaces[i].dispose();
        }
        surfaces=null;
        file = null;
      

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
    private static float readFloat()
    {
        return file.ReadSingle();
    }
    private static int readInt()
    {
        return file.ReadInt32();
    }

    public static Vector3 ReadVector3()
    {
        var v3 = new Vector3();
        v3.x = file.ReadSingle();
        v3.y = file.ReadSingle();
        v3.z = file.ReadSingle();
        return v3;
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
    private static string readstring(int count)
    {
        string str = "";
        for(int i=0;i<count;i++)
        {
            byte b = file.ReadByte();
           
            if (b>32 && b <= 126)
            {
                str += (char)b;
            }

        }

        return str;
    }

  

    private static void trace(string msg)
    {
        Debug.LogWarning(msg);
    }

   
    class MS3DVertex
    {

        public int flags;
        public Vector3 Vertex;
        public int boneId;
        public int refCount;
        public MS3DVertex()
        {
            

            flags= (int)file.ReadByte();

            Vertex=ReadVector3();

            boneId=(int)file.ReadByte();

            refCount=(int)file.ReadByte();

        }

        
    }
    class MS3DTriangle
    {
        public int flags;
        public int indice0;
        public int indice1;
        public int indice2;
        public Vector3 normal0;
        public Vector3 normal1;
        public Vector3 normal2;
        public Vector3 s;
        public Vector3 t;
        public int smoothingGroup;
        public int groupIndex;



        public MS3DTriangle()
        {
            flags = (int)file.ReadInt16();

            indice0 =(int)file.ReadInt16();
            indice1 =(int)file.ReadInt16();
            indice2 =(int)file.ReadInt16();


            normal0 = ReadVector3();
            normal1 = ReadVector3();
            normal2 = ReadVector3();


            s = ReadVector3();

            t = ReadVector3();

       

            smoothingGroup = (int)file.ReadByte();
            groupIndex =(int)file.ReadByte();
        }
    }


    class MS3DMesh
    {
        public int flags;
        public string name;
        public int numTriangles;
        public List<int> TriangleIndices;
        public int MaterialIndex;
  

        public MS3DMesh()
        {
            flags = file.ReadByte();

            name=readstring( 32);

            numTriangles = file.ReadInt16();
            TriangleIndices =new List<int>();
            for (int i=0;i<numTriangles;i++)
            {
                int indice = file.ReadInt16();
                TriangleIndices.Add(indice);
            }
            MaterialIndex =(int) file.ReadSByte();

            Debug.Log("Mesh name:" + name + " , Num Triangles:" + numTriangles + " , material index:"+MaterialIndex);
        }

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

        public VertexWight[] bones;
        public int numBones;
        public VertexBone()
        {
            bones = new VertexWight[4];
            for (int i = 0; i < 4; i++)
            {
                bones[i] = new VertexWight();
            }
           
        }

        public void addBone(int bone, float w)
        {
            for (int i = 0; i < 4; i++)
            {
                if (bones[i].Weight == 0)
                {
                    bones[i].boneId = bone;
                    bones[i].Weight = w;
                    numBones++;
                    return;
                }
            }


        }

    }

 
    public class MS3DMaterial
    {
        public string name;
        public string textureMap;
        public string alphaMap;



        public float transparency;
        public float shininess;
        public int mode;


        public Color ambient;
        public Color diffuse;
        public Color specular;
        public Color emissive;
        public Texture2D texture;
        public Texture2D textureDetail;


        public MS3DMaterial()
        {
            string s = "";
            
            byte [] nameFN =file.ReadBytes(32);
            for (int j = 0; j < 32; j++) 
            {
                if (nameFN[j] == 0)                    break;
                s = s + (char)nameFN[j];
            }
            name=s;

            ambient=ReadColor();
            specular=ReadColor();
            diffuse=ReadColor();
            emissive=ReadColor();

            shininess=readFloat();
            transparency=readFloat();

            mode=(int)file.ReadChar();




           
            byte[] textureFN=file.ReadBytes(128);
            byte[] spheremapFN=file.ReadBytes(128);
            s="";
            for (int j = 0; j < 128; j++) 
            {
                if (textureFN[j] == 0)                    break;
                s = s + (char)textureFN[j];
            }
            textureMap =Path.GetFileName( s);

            s = "";
            for (int j = 0; j < 128; j++) 
            {
                if (spheremapFN[j] == 0)                    break;
                s = s + (char)spheremapFN[j];
            }
            alphaMap=Path.GetFileName( s);

          

            Debug.Log("Material :"+name+" , texture:"+textureMap+" , detail :"+alphaMap);
          
            if(textureMap!="")
            {
            texture=loadexture(rootPath+"/"+textureMap);
            }
            if(alphaMap!="")
            {
            textureDetail=loadexture(rootPath+"/"+alphaMap);
            }

           
        }

    }

    public class CoreJoint
    {

   
        public  AnimationCurve posXcurve;
        public AnimationCurve posYcurve;
        public AnimationCurve posZcurve;

        public AnimationCurve rotXcurve;
        public AnimationCurve rotYcurve;
        public AnimationCurve rotZcurve;
        public AnimationCurve rotWcurve;

             
        public int numRotKeyFrames;          
        public int numPosKeyFrames;           
     

        public string Name;
        public string ParentName;
        public string Path;
   
        public CoreJoint parent;
        public GameObject joint;
    

        public Vector3 position; 
        public Quaternion rotation; 


        public CoreJoint()
        {
            
       
            Name = "";
            parent = null;
            Path = "";

          
          
            //joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            joint =new  GameObject("");

        
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
            
               clip.SetCurve(Path, typeof(Transform), "localPosition.x", posXcurve);
               clip.SetCurve(Path, typeof(Transform),  "localPosition.y", posYcurve);
               clip.SetCurve( Path, typeof(Transform), "localPosition.z", posZcurve);

          

           
                clip.SetCurve( Path, typeof(Transform), "localRotation.x",rotXcurve);
                clip.SetCurve( Path, typeof(Transform), "localRotation.y", rotYcurve);
                clip.SetCurve( Path, typeof(Transform), "localRotation.z", rotZcurve);
                clip.SetCurve( Path, typeof(Transform), "localRotation.w", rotWcurve);

           
        }

        public void addPositionCurve(float x,float y ,float z,float time)
        {
            posXcurve.AddKey(time, x);
            posYcurve.AddKey(time, y);
            posZcurve.AddKey(time, z);
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
         
        public Material material;
       
        public List<BoneWeight> boneWeightsList;
    
        public List<Vector3> vertices;
        public List<Color> colors;
        public List<Vector3> normals;
        public List<Vector2> uvcoords0;
        public List<Vector2> uvcoords1;
        public List<int> faces;
        public GameObject meshContainer;
        public SkinnedMeshRenderer meshRenderer;
        public GameObject Root;
    
        private int no_verts;
        private int no_tris;

        public List<VertexBone> vertex;

        public CoreMesh(GameObject meshRoot, GameObject root, string name)
        {
            this.name = name;
            this.Root = root;
            meshContainer = new GameObject(name);
            meshContainer.transform.parent = meshRoot.transform;
            meshRenderer = (SkinnedMeshRenderer)meshContainer.AddComponent(typeof(SkinnedMeshRenderer));
           // meshRenderer.quality=SkinQuality.Bone1;
            meshRenderer.sharedMesh = new Mesh();
            geometry = meshRenderer.sharedMesh;
         
            boneWeightsList = new List<BoneWeight>();
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            uvcoords0 = new List<Vector2>();
            uvcoords1 = new List<Vector2>();
            colors=new List<Color>();
          
            vertex=new List<VertexBone>();
           
            faces = new List<int>();

            no_verts = 0;
            no_tris = 0;
          
        }
        public int addVertex(Vector3 pos, Vector3 normal,Color c, Vector2 uv0,Vector2 uv1)
        {
            no_verts++;
            vertices.Add(pos);
            normals.Add(normal);
            colors.Add(c);
            uvcoords0.Add(uv0);
            uvcoords1.Add(uv1);
            return no_verts-1;

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

        public int addVertex(Vector3 pos)
        {
            no_verts++;
            vertices.Add(pos);
            return no_verts-1;
      
        }
        public void addNormal( Vector3 normal)
        {
            normals.Add(normal);
        
        }
 

        public int addFace(int a, int b, int c)
        {
            no_tris++;
            faces.Add(a);
            faces.Add(b);
            faces.Add(c);
            return no_tris;
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
            if (material != null)
            {
                meshRenderer.sharedMaterial = material;
            }
 


        }
        public void dispose()
        {
            
            boneWeightsList.Clear();
            vertices.Clear();
            normals.Clear();
            faces.Clear();
            vertex.Clear();
            vertex = null;
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


                string id =readUTFBytes(10);
                int version = readInt();

                Debug.Log("ID:" + id + ", version:" + version);


                int numVerts = (int)file.ReadInt16();

                Debug.Log("Numer of vertex:" + numVerts);
                List<MS3DVertex> vertices = new List<MS3DVertex>();
                for (int i=0; i<numVerts; i++)
                {
                    MS3DVertex vertex = new MS3DVertex();
                    vertices.Add(vertex);
                    
                }

                List<MS3DTriangle> triangles = new List<MS3DTriangle>();
                int numTriangles = (int)file.ReadInt16();

                Debug.Log("Numer of triangles:" + numTriangles);
                for (int i=0; i<numTriangles; i++)
                {
                    MS3DTriangle tri = new MS3DTriangle();
                    triangles.Add(tri);

                }
                 

                int numMeshes=(int)file.ReadInt16();
                Debug.Log("Numer of meshes:" + numMeshes);

                List<MS3DMesh> meshes = new List<MS3DMesh>();
                for (int i = 0; i < numMeshes; i++)
                {
                    MS3DMesh mesh = new MS3DMesh();
                    meshes.Add(mesh);
                }

                int numMaterials=(int)file.ReadInt16();
                Debug.Log("Number  of Materials:" + numMaterials);
                List<MS3DMaterial> materials = new List<MS3DMaterial>();
                for (int i = 0; i < numMaterials; i++)
                {
                    MS3DMaterial material = new MS3DMaterial();

                    if (File.Exists(path + "/" + material.alphaMap))
                    {
                        material.textureDetail = loadexture(path + "/" + material.alphaMap);
                    }
                        
                    if (File.Exists(path + "/" + material.textureMap))
                    {
                        material.texture = loadexture(path + "/" + material.textureMap);
                    }



                    materials.Add(material);
                }

                framesPerSecond = file.ReadSingle();
                float currentTime =file.ReadSingle();
                NumFrames =file.ReadInt32();
                int numJoints = file.ReadInt16();


                Debug.Log("fps:"+framesPerSecond+", time:"+currentTime+", total frames:"+NumFrames+", num joints:"+numJoints);

              
                GameObject ObjectRoot = new GameObject(nm);
                GameObject meshContainer = new GameObject("Surfaces");
                meshContainer.transform.parent = ObjectRoot.transform;

                if (numJoints > 1)
                {
                    AnimationClip clip = new AnimationClip();
                    clip.name = nm + "take00";
                    clip.wrapMode = WrapMode.Loop;

               
                    for (int i = 0; i < numJoints; i++)
                    {
                        isStatic = false;

                        CoreJoint Joint = new CoreJoint();
                        byte flags = file.ReadByte();
                        char[] name = file.ReadChars(32);
                        char[] parentName = file.ReadChars(32);

                   
                        Joint.Name = "";
                        for (int k = 0; k < 32; k++)
                        {
                            if (name[k] == (char)0)
                                break;
                            Joint.Name += name[k];
                        }
                        Joint.ParentName = "";
                        for (int k = 0; k < 32; k++)
                        {
                            if (parentName[k] == (char)0)
                                break;
                            Joint.ParentName += parentName[k];
                        }

                        Joint.joint.name = Joint.Name;


                        //       Debug.Log("Joint name:" + Joint.Name + " , Join Parent:" + Joint.ParentName);

                  
                   
                        Vector3 rotation = Vector3.zero;
                        rotation.x = file.ReadSingle();
                        rotation.y = file.ReadSingle();
                        rotation.z = file.ReadSingle();

                        Joint.position = Vector3.zero;
                        Joint.position.x = file.ReadSingle();
                        Joint.position.y = file.ReadSingle();
                        Joint.position.z = file.ReadSingle();
                        Joint.rotation = QuaternionCreate(rotation);


                        CoreJoint parent = getBoneByName(Joint.ParentName);
                        if (parent != null)
                        {
                            Joint.parent = parent;
                            Joint.joint.transform.parent = parent.joint.transform;    
                            Joint.Path += parent.Path + "/";

                            //    Debug.Log("Bone:"+ Joint.Name+" Parent"+ Joint.ParentName); 

                        }
                        else
                        {
                            Joint.joint.transform.parent = ObjectRoot.transform;
                            //  Debug.LogWarning("Bone:"+ Joint.Name+" dont have parent"); 
                        }
                        Joint.Path += Joint.Name;

                        Joint.joint.transform.localPosition = Joint.position;
                        Joint.joint.transform.localRotation = Joint.rotation;


                 


                 

                        // Debug.Log("Joint: "+Joint.Name+", Position:" + Joint.position +", Rotation: "+rotation);
                        //    Debug.Log("Joint :"+Joint.Name+" , Path:"+Joint.Path);


           
              
                        Joint.numRotKeyFrames = file.ReadInt16();
                        Joint.numPosKeyFrames = file.ReadInt16();
                  
                        float fps = 1.0f/ framesPerSecond;
                    
                        for (int k = 0; k < Joint.numRotKeyFrames; k++)
                        {
                       
                            float time = file.ReadSingle();
                            Vector3 rot = ReadVector3();
                            Quaternion qrot = QuaternionCreate(rot) * Joint.rotation;
                            Joint.addRotationCurve(qrot, time );


                       //     Debug.Log("Frame:" + time + " , time:" + (time*fps));

                        }
                        for (int k = 0; k < Joint.numPosKeyFrames; k++)
                        {
                            float time = file.ReadSingle();
                            Vector3 pos = Joint.position + ReadVector3();
                            Joint.addPositionCurve(pos.x, pos.y, pos.z, time );

                

                           


                        }
                        Joint.addCurves(clip);
                       
                        if (Joint.numRotKeyFrames != Joint.numPosKeyFrames)
                        {
                            Debug.LogError(Joint.numPosKeyFrames + " != " + Joint.numRotKeyFrames);
                        }
                  



                  

               
                        listJoints.Add(Joint);



                   
                    }

                    clip.frameRate = framesPerSecond;
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
                }//JOINTS

             
                for (int i = 0; i < meshes.Count; i++)
                {
                    MS3DMesh mesh = meshes[i];

                    CoreMesh cmesh = new CoreMesh(meshContainer, ObjectRoot, mesh.name);


                    if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex <= materials.Count)
                    {
                        MS3DMaterial material = materials[mesh.MaterialIndex];    
                        bool isDetail = material.textureDetail != null;
                        if (isDetail)
                        {
                        }
                        else
                        {
                            cmesh.material=new Material(Shader.Find("Diffuse"));
                            cmesh.material.name = material.name;
                            if (material.texture != null)
                            {
                                cmesh.material.mainTexture = material.texture;
                            }

                            if (saveAssets)
                            {
                                string meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + cmesh.material.name + ".asset");
                       
                                AssetDatabase.CreateAsset(cmesh.material, meshAssetPath);
                            }

                        }


                    }



                    for (int j = 0; j < mesh.numTriangles; j++)
                    {
                        if(!isStatic)
                        {
                   
                            VertexBone vtx0 = new VertexBone();
                            cmesh.vertex.Add(vtx0);

                            VertexBone vtx1 = new VertexBone();
                  
                            cmesh.vertex.Add(vtx1);

                            VertexBone vtx2 = new VertexBone();
                     
                            cmesh.vertex.Add(vtx2);

                        }

                        
                        int index0 = triangles[mesh.TriangleIndices[j]].indice0;
                        Vector3 v0 = vertices[index0].Vertex;
                        Vector3 n0 = triangles[mesh.TriangleIndices[j]].normal0;
                        float u0 = triangles[mesh.TriangleIndices[j]].s.x;
                        float t0 = 1f * -triangles[mesh.TriangleIndices[j]].t.x;

                        int index1 = triangles[mesh.TriangleIndices[j]].indice1;
                        Vector3 v1 = vertices[index1].Vertex;
                        Vector3 n1 = triangles[mesh.TriangleIndices[j]].normal1;
                        float u1 = triangles[mesh.TriangleIndices[j]].s.y;
                        float t1 = 1f * -triangles[mesh.TriangleIndices[j]].t.y;


                        int index2 = triangles[mesh.TriangleIndices[j]].indice2;
                        Vector3 v2 = vertices[index2].Vertex;
                        Vector3 n2 = triangles[mesh.TriangleIndices[j]].normal2;
                        float u2 = triangles[mesh.TriangleIndices[j]].s.z;
                        float t2 = 1f * -triangles[mesh.TriangleIndices[j]].t.z;

                        int f0 = cmesh.addVertex(v0);
                                 cmesh.addNormal(n0);
                                 cmesh.addTexCoords(new Vector2(u0, t0), 0);


                        int f1 = cmesh.addVertex(v1);
                        cmesh.addNormal(n1);
                        cmesh.addTexCoords(new Vector2(u1, t1), 0);


                        int f2 = cmesh.addVertex(v2);
                        cmesh.addNormal(n2);
                        cmesh.addTexCoords(new Vector2(u2, t2), 0);

                        cmesh.addFace(f0, f1, f2);

                        if (!isStatic)
                        {
                            
                            int Bone0 = vertices[index0].boneId;
                            int Bone1 = vertices[index1].boneId;
                            int Bone2 = vertices[index2].boneId;

                            cmesh.vertex[f0].addBone(Bone0, 1);
                            cmesh.vertex[f1].addBone(Bone1, 1);
                            cmesh.vertex[f2].addBone(Bone2, 1);

                        }

                     

                    

                    }
                   
                    surfaces.Add(cmesh);
                }

                for (int i = 0; i < meshes.Count; i++)
                {
                    MS3DMesh mesh = meshes[i];
                    CoreMesh cmesh = surfaces[i];

                    if (!isStatic)
                    {
                        
                        for (int j = 0; j < cmesh.vertex.Count; j++)
                        {
                            isStatic = false;

                            VertexBone vertex = cmesh.vertex[j];
                            //  Debug.Log("Num Bone:" + vertex.numBones);

                            BoneWeight b = new BoneWeight();
                            b.boneIndex0 = vertex.bones[0].boneId;
                            b.weight0 = vertex.bones[0].Weight;

                      

                            cmesh.addBone(b);


                        }
                    }
                    cmesh.build();
                    if (saveAssets)
                    {
                        string meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + cmesh.name+"_"+i + ".asset");
                        AssetDatabase.CreateAsset(cmesh.geometry, meshAssetPath);
                    }
                }



                if (saveAssets)
                {

                    string prefabPath = AssetDatabase.GenerateUniqueAssetPath(importingAssetsDir + filename + ".prefab");
                    var prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
                    PrefabUtility.ReplacePrefab(ObjectRoot, prefab, ReplacePrefabOptions.ConnectToPrefab);
                    AssetDatabase.Refresh();
                }

                materials.Clear();
                meshes.Clear();
                triangles.Clear();
                vertices.Clear();

                materials = null;
                meshes = null;
                triangles = null;
                vertices = null;


               




            
            }//file open

            Debug.Log(path + "/" + filename + " Imported ;) ");
        }

    }
   


    private static Quaternion AngleQuaternion(Vector3 angles) {
        float angle;
        float sr, sp, sy, cr, cp, cy;
        Quaternion quaternion = Quaternion.identity;
        // FIXME: rescale the inputs to 1/2 angle
        angle = (float)angles.z * 0.5f;
        sy = (float)Math.Sin(angle);
        cy = (float)Math.Cos(angle);
        angle = (float)angles.y * 0.5f;
        sp = (float)Math.Sin(angle);
        cp = (float)Math.Cos(angle);
        angle = (float)angles.x * 0.5f;
        sr = (float)Math.Sin(angle);
        cr = (float)Math.Cos(angle);

        quaternion.x = sr * cp * cy - cr * sp * sy; // X
        quaternion.y = cr * sp * cy + sr * cp * sy; // Y
        quaternion.z = cr * cp * sy - sr * sp * cy; // Z
        quaternion.w = cr * cp * cy + sr * sp * sy; // W
        return quaternion;


    }

    private static Quaternion QuaternionCreate(Vector3 v) 
    {
   /*

        float angle = v.z * 0.5f;
        float sinZ = Mathf.Sin(angle);
        float cosZ = Mathf.Cos(angle);
        angle = v.y * 0.5f;
        float sinY = Mathf.Sin(angle);
        float cosY = Mathf.Cos(angle);
        angle = v.x * 0.5f;
        float sinX =Mathf.Sin(angle);
        float cosX =Mathf.Cos(angle);

        // variables used to reduce multiplication calls.
        float cosYXcosZ = cosY * cosZ;
        float sinYXsinZ = sinY * sinZ;
        float cosYXsinZ = cosY * sinZ;
        float sinYXcosZ = sinY * cosZ;

        float w = (cosYXcosZ * cosX - sinYXsinZ * sinX);
        float x = (cosYXcosZ * sinX + sinYXsinZ * cosX);
        float y = (sinYXcosZ * cosX + cosYXsinZ * sinX);
        float z = (cosYXsinZ * cosX - sinYXcosZ * sinX);
        return new Quaternion(w,x, y, z);
        */

        return Quaternion.EulerAngles(v);
        //return Quaternion.Euler(v);
    }

    private static CoreJoint getBoneByName(string name)
    {
        for (int i = 0; i < listJoints.Count; i++)
        {

            CoreJoint bone = listJoints[i];
            if (bone.Name == name)
            {
                return bone;

            }
        }
        return null;
    }
    private static int findBoneByName(string name)
    {
        for (int i = 0; i < listJoints.Count; i++)
        {

            CoreJoint bone = listJoints[i];
            if (bone.Name == name)
            {
                return i;

            }
        }
        return -1;
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


