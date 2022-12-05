using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine.InputSystem;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

[AlwaysUpdateSystem]
public partial class SelectionSystem : SystemBase
{
    public float2 MouseStartPos { get; private set; }
    public bool IsDragging { get; private set; }

    private Camera mainCamera;
    private BuildPhysicsWorld buildPhysicsWorld;

    private CollisionWorld collisionWorld;
    private EntityArchetype selectionArchetype;

    protected override void OnCreate()
    {
        mainCamera = Camera.main;
        buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        
        selectionArchetype = EntityManager.CreateArchetype(typeof(PhysicsCollider), typeof(LocalToWorld),
            typeof(SelectionColliderTag), typeof(PhysicsWorldIndex));
    }

    protected override void OnUpdate()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) MouseStartPos = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.isPressed && !IsDragging)
        {
            if (math.distance(MouseStartPos, Mouse.current.position.ReadValue()) > 25f) IsDragging = true;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (!Keyboard.current.leftShiftKey.isPressed) DeselectUnits();

            if (IsDragging) SelectMultipleUnits();

            SelectOrDeselectSingleUnit();
        }
    }

    private void SelectMultipleUnits()
    {
        IsDragging = false;

        var topLeft = math.min(MouseStartPos, Mouse.current.position.ReadValue());
        var botRight = math.max(MouseStartPos, Mouse.current.position.ReadValue());

        var rect = Rect.MinMaxRect(topLeft.x, topLeft.y, botRight.x, botRight.y);

        var cornerRays = new UnityEngine.Ray[]
        {
            mainCamera.ScreenPointToRay(rect.min),
            mainCamera.ScreenPointToRay(rect.max),
            mainCamera.ScreenPointToRay(new Vector2(rect.xMin, rect.yMax)),
            mainCamera.ScreenPointToRay(new Vector2(rect.xMax, rect.yMin))
        };

        var vertices = new NativeArray<float3>(5, Allocator.Temp);

        for (int i = 0; i < cornerRays.Length; i++)
        {
            vertices[i] = cornerRays[i].GetPoint(2000f);
        }

        vertices[4] = mainCamera.transform.position;
        
        var collisionFilter = new CollisionFilter
        {
            BelongsTo = (uint)CollisionLayers.Selection,
            CollidesWith = (uint)CollisionLayers.Units
        };

        var physicsMaterial = Unity.Physics.Material.Default;
        physicsMaterial.CollisionResponse = CollisionResponsePolicy.RaiseTriggerEvents;

        var convexHullGenerationParameters = new ConvexHullGenerationParameters
        {
            SimplificationTolerance = 0f,
            BevelRadius = 0f,
            MinimumAngle = 0f
        };

        var selectionCollider = ConvexCollider.Create(vertices, convexHullGenerationParameters, collisionFilter, physicsMaterial);
        
        var newSelectionEntity = EntityManager.CreateEntity(selectionArchetype);
        EntityManager.SetComponentData(newSelectionEntity, new PhysicsCollider { Value = selectionCollider });
        
        vertices.Dispose();
    }


    private void DeselectUnits()
    {
        EntityManager.RemoveComponent<SelectedEntityTag>(GetEntityQuery(typeof(SelectedEntityTag)));
    }

    private void SelectOrDeselectSingleUnit()
    {
        collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld;

        var ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        var rayStart = ray.origin;
        var rayEnd = ray.GetPoint(2000f);

        if (Raycast(rayStart, rayEnd, out var raycastHit))
        {
            var hitEntity = raycastHit.Entity;

            if (!EntityManager.HasComponent<SelectableUnitTag>(hitEntity)) return;

            if (!EntityManager.HasComponent<SelectedEntityTag>(hitEntity)) EntityManager.AddComponent<SelectedEntityTag>(hitEntity);

            else EntityManager.RemoveComponent<SelectedEntityTag>(hitEntity);
        }
    }

    private bool Raycast(float3 rayStart, float3 rayEnd, out RaycastHit raycastHit)
    {
        var racastInput = new RaycastInput
        {
            Start = rayStart,
            End = rayEnd,
            Filter = new CollisionFilter
            {
                BelongsTo = (uint)CollisionLayers.Selection,
                CollidesWith = (uint)(CollisionLayers.Ground | CollisionLayers.Units)
            }
        };

        return collisionWorld.CastRay(racastInput, out raycastHit);
    }
}
