using System;
using Colossal.Entities;
using Game.Buildings;
using Game.Areas;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Owner = Game.Common.Owner;

namespace SubwayFreedomAssetPack.System
{
    public partial class BuildingPickerToolSystem : ToolBaseSystem
    {
        private Entity m_PreviousRaycastedEntity;
        private EntityQuery m_HighlightedQuery;
        private ToolOutputBarrier m_Barrier;
        private PrefabSystem prefabSystem;

        public override string toolID => "SubwayFreedomPickerTool";
        private Entity m_TargetStationEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_HighlightedQuery = GetEntityQuery(ComponentType.ReadOnly<Highlighted>());
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.collisionMask = CollisionMask.Overground | CollisionMask.OnGround | CollisionMask.Underground;
            m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
            m_ToolRaycastSystem.areaTypeMask |= AreaTypeMask.Lots;
            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.BuildingLots;
            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubBuildings;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            applyAction.shouldBeEnabled = true;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
            EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
            m_PreviousRaycastedEntity = Entity.Null;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            
            if (!GetRaycastResult(out var currentRaycastEntity, out RaycastHit _))
            {
                buffer.AddComponent<BatchesUpdated>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                buffer.RemoveComponent<Highlighted>(m_HighlightedQuery, EntityQueryCaptureMode.AtPlayback);
                m_PreviousRaycastedEntity = Entity.Null;
                return inputDeps;
            }
            
            if (currentRaycastEntity != m_PreviousRaycastedEntity)
            {
                m_PreviousRaycastedEntity = currentRaycastEntity;
                NativeArray<Entity> entities = m_HighlightedQuery.ToEntityArray(Allocator.Temp);
                buffer.AddComponent<BatchesUpdated>(entities);
                buffer.RemoveComponent<Highlighted>(entities);
            }
            
            if (m_HighlightedQuery.IsEmptyIgnoreFilter && IsValidPrefab(currentRaycastEntity))
            {
                buffer.AddComponent<BatchesUpdated>(currentRaycastEntity);
                buffer.AddComponent<Highlighted>(currentRaycastEntity);
                m_PreviousRaycastedEntity = currentRaycastEntity;
            }
            
            if (!applyAction.WasReleasedThisFrame() || m_ToolSystem.selected == Entity.Null)
            {
                return inputDeps;
            }
            
            OnStationSelected(currentRaycastEntity, buffer);
            m_ToolSystem.activeTool = m_DefaultToolSystem;
            return base.OnUpdate(inputDeps);
        }

        private bool IsValidPrefab(Entity entity)
        {
            var owner = GetOwner(entity);
            return EntityManager.HasComponent<Building>(owner) && owner != m_TargetStationEntity;
        }

        private Entity GetOwner(Entity currentEntity)
        {
            return World.DefaultGameObjectInjectionWorld.EntityManager.TryGetComponent<Owner>(currentEntity,
                out var owner) ? GetOwner(owner.m_Owner) : currentEntity;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        public override PrefabBase GetPrefab()
        {
            return null;
        }
        
        public void StartPicking(Entity targetStationEntity)
        {
            m_TargetStationEntity = targetStationEntity;
            m_ToolSystem.activeTool = this;
        }
        
        private void OnStationSelected(Entity selectedStation, EntityCommandBuffer buffer)
        {
            var targetOwner = GetOwner(selectedStation);
            if (m_TargetStationEntity == Entity.Null || targetOwner == Entity.Null || targetOwner == m_TargetStationEntity)
            {
                return;
            }

            // 1. Prefab-level upgrades linking
            if (EntityManager.TryGetComponent<PrefabRef>(m_TargetStationEntity, out var mainPrefabRef) &&
                EntityManager.TryGetComponent<PrefabRef>(targetOwner, out var upgradePrefabRef))
            {
                var mainPrefabEntity = mainPrefabRef.m_Prefab;
                var upgradePrefabEntity = upgradePrefabRef.m_Prefab;

                // Set ServiceUpgradeBuilding buffer on upgradePrefabEntity
                DynamicBuffer<ServiceUpgradeBuilding> upgradeBuildingBuffer;
                if (EntityManager.HasBuffer<ServiceUpgradeBuilding>(upgradePrefabEntity))
                {
                    upgradeBuildingBuffer = EntityManager.GetBuffer<ServiceUpgradeBuilding>(upgradePrefabEntity);
                }
                else
                {
                    upgradeBuildingBuffer = EntityManager.AddBuffer<ServiceUpgradeBuilding>(upgradePrefabEntity);
                }

                bool linkExists = false;
                for (int i = 0; i < upgradeBuildingBuffer.Length; i++)
                {
                    if (upgradeBuildingBuffer[i].m_Building == mainPrefabEntity)
                    {
                        linkExists = true;
                        break;
                    }
                }
                if (!linkExists)
                {
                    upgradeBuildingBuffer.Add(new ServiceUpgradeBuilding { m_Building = mainPrefabEntity });
                }

                // Set BuildingUpgradeElement buffer on mainPrefabEntity
                DynamicBuffer<BuildingUpgradeElement> mainBuildingUpgradeBuffer;
                if (EntityManager.HasBuffer<BuildingUpgradeElement>(mainPrefabEntity))
                {
                    mainBuildingUpgradeBuffer = EntityManager.GetBuffer<BuildingUpgradeElement>(mainPrefabEntity);
                }
                else
                {
                    mainBuildingUpgradeBuffer = EntityManager.AddBuffer<BuildingUpgradeElement>(mainPrefabEntity);
                }

                bool mainLinkExists = false;
                for (int i = 0; i < mainBuildingUpgradeBuffer.Length; i++)
                {
                    if (mainBuildingUpgradeBuffer[i].m_Upgrade == upgradePrefabEntity)
                    {
                        mainLinkExists = true;
                        break;
                    }
                }
                if (!mainLinkExists)
                {
                    mainBuildingUpgradeBuffer.Add(new BuildingUpgradeElement { m_Upgrade = upgradePrefabEntity });
                }

                if (!EntityManager.HasComponent<ServiceUpgradeData>(upgradePrefabEntity))
                {
                    EntityManager.AddComponentData(upgradePrefabEntity, new ServiceUpgradeData
                    {
                        m_UpgradeCost = 100,
                        m_XPReward = 10,
                        m_MaxPlacementOffset = 2,
                        m_MaxPlacementDistance = 80f,
                        m_ForbidMultiple = false
                    });
                }
            }

            // 2. Entity-level upgrades linking
            // Ensure owner on targetOwner points to m_TargetStationEntity
            if (EntityManager.HasComponent<Owner>(targetOwner))
            {
                buffer.SetComponent(targetOwner, new Owner { m_Owner = m_TargetStationEntity });
            }
            else
            {
                buffer.AddComponent(targetOwner, new Owner { m_Owner = m_TargetStationEntity });
            }

            // Ensure targetOwner has ServiceUpgrade component
            if (!EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(targetOwner))
            {
                buffer.AddComponent<Game.Buildings.ServiceUpgrade>(targetOwner);
            }

            // Ensure m_TargetStationEntity has InstalledUpgrade buffer
            DynamicBuffer<InstalledUpgrade> installedUpgrades;
            if (EntityManager.HasBuffer<InstalledUpgrade>(m_TargetStationEntity))
            {
                installedUpgrades = EntityManager.GetBuffer<InstalledUpgrade>(m_TargetStationEntity);
            }
            else
            {
                installedUpgrades = buffer.AddBuffer<InstalledUpgrade>(m_TargetStationEntity);
            }

            bool installedExists = false;
            for (int i = 0; i < installedUpgrades.Length; i++)
            {
                if (installedUpgrades[i].m_Upgrade == targetOwner)
                {
                    installedExists = true;
                    break;
                }
            }
            if (!installedExists)
            {
                buffer.AppendToBuffer(m_TargetStationEntity, new InstalledUpgrade(targetOwner, 0u));
            }

            // 3. Ensure targetOwner is in m_TargetStationEntity's SubObjects buffer
            DynamicBuffer<Game.Objects.SubObject> subObjects;
            if (EntityManager.HasBuffer<Game.Objects.SubObject>(m_TargetStationEntity))
            {
                subObjects = EntityManager.GetBuffer<Game.Objects.SubObject>(m_TargetStationEntity);
            }
            else
            {
                subObjects = buffer.AddBuffer<Game.Objects.SubObject>(m_TargetStationEntity);
            }

            bool subObjectExists = false;
            for (int i = 0; i < subObjects.Length; i++)
            {
                if (subObjects[i].m_SubObject == targetOwner)
                {
                    subObjectExists = true;
                    break;
                }
            }
            if (!subObjectExists)
            {
                buffer.AppendToBuffer(m_TargetStationEntity, new Game.Objects.SubObject(targetOwner));
            }

            // 4. Copy SpawnLocationElements from targetOwner to m_TargetStationEntity
            if (EntityManager.HasBuffer<SpawnLocationElement>(targetOwner))
            {
                var targetSpawnLocations = EntityManager.GetBuffer<SpawnLocationElement>(targetOwner);
                DynamicBuffer<SpawnLocationElement> mainSpawnLocations;
                if (EntityManager.HasBuffer<SpawnLocationElement>(m_TargetStationEntity))
                {
                    mainSpawnLocations = EntityManager.GetBuffer<SpawnLocationElement>(m_TargetStationEntity);
                }
                else
                {
                    mainSpawnLocations = buffer.AddBuffer<SpawnLocationElement>(m_TargetStationEntity);
                }

                for (int i = 0; i < targetSpawnLocations.Length; i++)
                {
                    bool spawnLocationExists = false;
                    for (int j = 0; j < mainSpawnLocations.Length; j++)
                    {
                        if (mainSpawnLocations[j].m_SpawnLocation == targetSpawnLocations[i].m_SpawnLocation && mainSpawnLocations[j].m_Type == targetSpawnLocations[i].m_Type)
                        {
                            spawnLocationExists = true;
                            break;
                        }
                    }
                    if (!spawnLocationExists)
                    {
                        buffer.AppendToBuffer(m_TargetStationEntity, targetSpawnLocations[i]);
                    }
                }
            }

            // Trigger updates to pathfinding and rendering
            buffer.AddComponent<BatchesUpdated>(m_TargetStationEntity);
            buffer.AddComponent<BatchesUpdated>(targetOwner);
        }
    }
}
