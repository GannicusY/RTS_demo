using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateAfter(typeof(SpawnSelectionSystem))]
public partial class UnitMoveOrderSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var mouse = Mouse.current;

        if (!mouse.rightButton.wasPressedThisFrame ||
            !Physics.Raycast(Camera.main.ScreenPointToRay(new Vector2(mouse.position.x.ReadValue(), mouse.position.y.ReadValue())),
                out UnityEngine.RaycastHit hit)) return;

        var cellSize = Test.Instance.grid.CellSize;

        Test.Instance.grid.GetCellCoordinate(hit.point + new Vector3(1, 1) * cellSize * +.5f, out int endX, out int endY);

        ValidateGridPosition(ref endX, ref endY);

        var ecb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .WithAll<SelectionStateData>()
            .ForEach((Entity entity, ref Translation translation) =>
            {
                Test.Instance.grid.GetCellCoordinate(translation.Value + new float3(1, 1, 0) * cellSize * + 0.5f, out int startX, out int startZ);

                ValidateGridPosition(ref startX, ref startZ);

                ecb.AddComponent(entity, new NavigationData
                {
                    startPosition = new int2(startX, startZ),
                    endPosition = new int2(endX, endY)
                });

                ecb.AddComponent(entity, new PathFollow
                {
                    pathIndex = -1
                });
                
                ecb.AddBuffer<Path>(entity);

            }).Run();
    }

    private void ValidateGridPosition(ref int x, ref int z)
    {
        x = math.clamp(x, 0, Test.Instance.grid.Length - 1);
        z = math.clamp(z, 0, Test.Instance.grid.Width - 1);
    }
}
