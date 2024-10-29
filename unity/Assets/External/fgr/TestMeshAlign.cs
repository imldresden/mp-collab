using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Specific to Unity. Takes an existing object, pulls out a partial point cloud,
// adds a random transformation, and then tries to align. Will require a template
// (generic mesh) to display the clouds.
public class TestMeshAlign : MonoBehaviour
{
    public MeshFilter pointCloudSource;
    
    public GameObject cloudTemplate;
    
    private List <Vector3> extractRandomCloud (int n, bool scramble)
    {
        List <Vector3> p = new List <Vector3> ();

        Mesh m = pointCloudSource.GetComponent <MeshFilter> ().mesh;
        Vector3 [] v = m.vertices;
        int vlen = v.Length;
        
        Matrix4x4 randTrans = Matrix4x4.identity;
        if (scramble)
        {
            Vector3 t = Random.insideUnitSphere;
            float angle = Random.Range (0.0f, 360.0f);
            Vector3 direction = Random.insideUnitSphere;
            randTrans.SetTRS (t, Quaternion.AngleAxis (angle, direction), Vector3.one);
            Debug.Log ("Transformation: " + t.ToString ("F4") + "   " + angle.ToString ("F4") + " - " + direction.ToString ("F4") + "\n\n" + randTrans.ToString ("F4"));
        }
        
        for (int i = 0; i < n; i++)
        {
            p.Add (randTrans.MultiplyPoint (v[Random.Range (0, vlen)]));
        }
        
        return p;
    }
    
    private void displayCloud (List <Vector3> p)
    {
        Vector3 [] vertices = p.ToArray ();
        
        GameObject g = Instantiate (cloudTemplate);
        Mesh m = g.GetComponent <MeshFilter> ().mesh;
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = vertices;
        m.triangles = new int [0];
        m.RecalculateNormals();
    }
    
    void Start()
    {
        List <Vector3> p1 = extractRandomCloud (10000, false);
        List <Vector3> p2 = extractRandomCloud (10000, true);
        
        displayCloud (p1);
        displayCloud (p2);
        
        fgr.CApp app = new fgr.CApp ();
        app.AddFeature (p1);
        app.AddFeature (p2);
        app.NormalizePoints();
        app.AdvancedMatching();
        app.OptimizePairwise(true);
        app.WriteTrans ("Assets/cloudoutput.txt");

        List <Vector3> p3 = app.GetTransformedPoints (p2);
        displayCloud (p3);
    }

    void Update()
    {
        
    }
}
