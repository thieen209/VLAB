// REVIEWABLE EDITOR SNIPPET ONLY. This text file has not been executed.
// Verified source: Engineering/9.asset has 48 readable vertices, 96 indices;
// world X columns -0.15, 1.05, 2.45, 4.35 (12 vertices each), Y 0,1.25,2.25,2.8,
// Z 3.60,3.75. Geometry plan confirms opening [1.05,2.45] x [1.25,2.25].
// Preserve the original asset; shift only the two aperture columns +0.35 world X.
// Paste into an existing editor execution bridge, or compile as an Editor class.
// shortcut: retained baked lightmaps describe the previous opening, rebake for final lighting approval.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EngineeringWindowCenter
{
    public static void RunAndBuild()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Labs/Scenes/Engineering.unity");Run();
            Vlab.Product.RoomLighting.Apply();Vlab.Product.RoomBranding.Apply(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),true);
            var view=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name.Contains("Learner"));
            view.transform.position=new Vector3(2.1f,1.65f,.65f);view.transform.LookAt(new Vector3(2.1f,1.65f,3.65f));view.fieldOfView=75;
            Directory.CreateDirectory("Evidence/FinalPass/Rooms");LabPanoramaCapture.CaptureReview(view,"Evidence/FinalPass/Rooms/Engineering-centered.png");
            File.WriteAllText("Evidence/FinalPass/Rooms/Engineering-centered-checks.txt","PASS guarded opening, jambs, sill/header, centered assembly and preserved bench. Baked lighting not rebaked; photo is editor evidence.");
            EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");FinalPassCandidateBuild.Run();
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void Require(bool condition,string message)
    {if(!condition)throw new InvalidOperationException(message);}

    public static void Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Require(!EditorApplication.isPlaying&&!Lightmapping.isRunning,"Saved, idle Editor required.");
        Require(scene.path=="Assets/Labs/Scenes/Engineering.unity"&&!scene.isDirty,"Open and save the intended Engineering scene first.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform One(string name)=>all.Single(t=>t.name==name);
        var wall=One("Wall-north.003");var filter=wall.GetComponent<MeshFilter>();var collider=wall.GetComponent<MeshCollider>();
        Require(filter&&collider&&filter.sharedMesh==collider.sharedMesh,"North wall render/collision mesh contract changed.");
        var source=filter.sharedMesh;
        Require(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))=="25df38b1fac76424c87163036d19a790","Unexpected wall mesh; stop rather than touch changed geometry.");
        Require(source.isReadable&&source.vertexCount==48&&source.triangles.Length==96,"Unexpected wall topology.");
        var positions=source.vertices;var shifted=(Vector3[])positions.Clone();int left=0,right=0;
        for(int i=0;i<positions.Length;i++)
        {
            var world=wall.TransformPoint(positions[i]);
            bool lo=Mathf.Abs(world.x-1.05f)<.001f,hi=Mathf.Abs(world.x-2.45f)<.001f;
            Require(lo||hi||Mathf.Abs(world.x+.15f)<.001f||Mathf.Abs(world.x-4.35f)<.001f,"Unexpected wall X column.");
            if(lo||hi)
            {
                Require(Mathf.Abs(world.y-1.25f)<.001f||Mathf.Abs(world.y-2.25f)<.001f,"Unexpected aperture vertex height.");
                if(lo)left++;else right++;
                world.x+=.35f;shifted[i]=wall.InverseTransformPoint(world);
            }
        }
        Require(left==12&&right==12,"Expected exactly 24 duplicated aperture boundary vertices.");
        var moving=new[]{One("Fitted Sketchfab window"),One("Working roller shade"),One("Window soft fill"),One("Baked window fill")};
        Require(moving.All(t=>Mathf.Abs(t.position.x-1.75f)<.001f),"Window assembly already moved or differs; stop.");
        var before=moving.Select(t=>t.position).ToArray();var renderer=wall.GetComponent<MeshRenderer>();var bounds=renderer.bounds;
        Require(Mathf.Abs(bounds.center.x-2.1f)<.001f&&Mathf.Abs(bounds.size.x-4.5f)<.002f,"Unexpected north wall bounds.");
        var bench=One("ENG-ASSEMBLY-top");var benchPosition=bench.position;
        bool WallAt(float x,float y)=>collider.Raycast(new Ray(new Vector3(x,y,3.3f),Vector3.forward),out var hit,1f);
        Require(!WallAt(1.75f,1.75f)&&WallAt(3.1f,1.75f),"Baseline wall raycast does not confirm the real opening.");
        string backup="Evidence/FinalPass/Rooms/Engineering-before-center-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+".unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(scene.path,backup,false);
        var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Engineering north wall centered opening";
        mesh.vertices=shifted;mesh.RecalculateBounds();
        string path=AssetDatabase.GenerateUniqueAssetPath("Assets/Labs/Upgrade/LightmapMeshes/Engineering/CenteredNorthWall.asset");
        AssetDatabase.CreateAsset(mesh,path);
        try
        {
            filter.sharedMesh=mesh;collider.sharedMesh=null;collider.sharedMesh=mesh;
            for(int i=0;i<moving.Length;i++)
            {
                moving[i].position=before[i]+Vector3.right*.35f;
                if(PrefabUtility.IsPartOfPrefabInstance(moving[i]))PrefabUtility.RecordPrefabInstancePropertyModifications(moving[i]);
            }
            Physics.SyncTransforms();
            Require(!WallAt(1.42f,1.75f)&&!WallAt(2.10f,1.75f)&&!WallAt(2.78f,1.75f),"Centered aperture must remain physically open.");
            Require(WallAt(1.38f,1.75f)&&WallAt(2.82f,1.75f)&&WallAt(1.20f,1.75f),"Both new jambs and the old left gap must be solid.");
            Require(WallAt(2.10f,1.23f)&&WallAt(2.10f,2.27f),"Sill and header must remain solid.");
            Require((renderer.bounds.center-bounds.center).sqrMagnitude<.000001f&&(renderer.bounds.size-bounds.size).sqrMagnitude<.000001f,"Outer wall bounds changed.");
            Require(bench.position==benchPosition&&moving.All(t=>Mathf.Abs(t.position.x-2.1f)<.001f),"Window center or bench preservation check failed.");
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
            Require(EditorSceneManager.SaveScene(scene),"Scene save failed.");
            Debug.Log("PASS Engineering opening/frame/glass/shade/daylight centered X=2.10; bench unchanged. Backup: "+backup+"; new wall mesh: "+path+". Baked lighting and final screenshots still require verification.");
        }
        catch
        {
            filter.sharedMesh=source;collider.sharedMesh=null;collider.sharedMesh=source;
            for(int i=0;i<moving.Length;i++)
            {
                moving[i].position=before[i];
                if(PrefabUtility.IsPartOfPrefabInstance(moving[i]))PrefabUtility.RecordPrefabInstancePropertyModifications(moving[i]);
            }
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);
            throw;
        }
    }
}
