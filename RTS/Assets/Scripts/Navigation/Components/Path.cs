using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(100)]
public struct Path : IBufferElementData
{
    public int2 position;    
}
