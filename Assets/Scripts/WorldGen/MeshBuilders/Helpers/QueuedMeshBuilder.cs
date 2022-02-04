using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class QueuedMeshBuilder
{
    public bool done { get; set; }

    public abstract IEnumerator Build();
}
