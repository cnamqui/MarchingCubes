using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public interface IAsyncMeshBuilder
{
    public bool isAsyncBuildDone { get; set; } 
    public Mesh asyncMeshResult { get; }
    public abstract IEnumerator StartBuild();
    public abstract IEnumerator StartBuildAndUpdate(ChunkData chunk);
}
