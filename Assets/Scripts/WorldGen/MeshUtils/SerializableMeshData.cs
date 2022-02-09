using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class SerializableMeshData 
{
    public Vector3[] vertices;
    public Vector3[] normals;
    public int[] triangles;

    public SerializableMeshData(Mesh mesh)
    {
        vertices = mesh.vertices.ToArray();
        normals = mesh.normals.ToArray();
        triangles = mesh.triangles.ToArray();
    }
} 
