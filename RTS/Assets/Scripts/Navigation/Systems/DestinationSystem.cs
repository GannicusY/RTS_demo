using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateBefore(typeof(BuildPhysicsWorld))]
[DisableAutoCreation]
public partial class DestinationSystem : SystemBase
{
    
    BuildPhysicsWorld buildPhysicsWorld => World.GetExistingSystem<BuildPhysicsWorld>();
    EntityCommandBufferSystem barrier => World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

    

    CollisionFilter filter = CollisionFilter.Default;

    protected override void OnCreate()
    {
        //filter = new CollisionFilter
        //{
        //    BelongsTo = Util.ToBitMask(settings.SurfaceLayer),
        //    CollidesWith = Util.ToBitMask(settings.SurfaceLayer)
        //};
    }

    protected override void OnUpdate()
    {
        var physicsWorld = buildPhysicsWorld.PhysicsWorld;
        //var settings = navSystem.Settings;
        var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
       
        var renderBoundsFromEntity = GetComponentDataFromEntity<RenderBounds>(true);
        //var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

        var mouse = Mouse.current;

        if (!mouse.rightButton.wasPressedThisFrame ||
            !Physics.Raycast(Camera.main.ScreenPointToRay(new Vector2(mouse.position.x.ReadValue(), mouse.position.y.ReadValue())),
                out UnityEngine.RaycastHit hit)
        ) return;               

        Entities            
            .WithAll<SelectedEntityTag>()            
            .ForEach((Entity entity, int entityInQueryIndex, int nativeThreadIndex) =>
            {
                //if (surface.Value.Equals(Entity.Null) || !jumpableBufferFromEntity.HasComponent(surface.Value)) return;

                //var jumpableSurfaces = jumpableBufferFromEntity[surface.Value];
                //var random = randomArray[nativeThreadIndex];
                //var aabb = renderBoundsFromEntity[surface.Value].Value;
                
                //if (physicsWorld.GetPointOnSurfaceLayer(localToWorld, NavUtil.GetRandomPointInBounds(
                //            ref random, aabb, 99, aabb.Center), out var validDestination,
                //        settings.ObstacleRaycastDistanceMax,
                //        settings.ColliderLayer,
                //        settings.SurfaceLayer
                //    )
                //)
                //{
                    //commandBuffer.AddComponent(entityInQueryIndex, entity, new NavDestination
                    //{
                    //    WorldPoint = hit.point
                    //});
                //}
                

                //randomArray[nativeThreadIndex] = random;
            })
            .WithName("NavTerrainDestinationJob")
            .ScheduleParallel();

        barrier.AddJobHandleForProducer(Dependency);
        buildPhysicsWorld.AddInputDependencyToComplete(Dependency);
    }
}
