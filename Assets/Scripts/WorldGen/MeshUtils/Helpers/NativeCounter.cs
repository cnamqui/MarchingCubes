using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public unsafe struct NativeCounter : IDisposable
{ 
    private readonly Allocator _allocator;
     
    [NativeDisableUnsafePtrRestriction] private readonly int* _counter;
     
    public int Count
    {
        get => *_counter;
        set => (*_counter) = value;
    } 
    public NativeCounter(Allocator allocator)
    {
        _allocator = allocator;
        _counter = (int*)UnsafeUtility.Malloc(sizeof(int), 4, allocator);
        Count = 0;
    }  
    public int Increment()
    {
        return Interlocked.Increment(ref *_counter) - 1;
    } 
    public void Dispose()
    {
        UnsafeUtility.Free(_counter, _allocator);
    }
}
