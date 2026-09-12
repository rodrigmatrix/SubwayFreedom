using System;
using System.Collections.Generic;
using Colossal.Entities;
using Game.Buildings;
using Game.Prefabs;
using Game.UI;
using SubwayFreedomAssetPack.Domain;
using SubwayFreedomAssetPack.Extensions;
using SubwayFreedomAssetPack.Utils;
using Unity.Entities;
using Game.Common;
using Colossal.UI.Binding;

namespace SubwayFreedomAssetPack.System
{
    public partial class SelectedBuildingUISystem : ExtendedInfoSectionBase
    {
        private NameSystem _nameSystem;
        private BuildingPickerToolSystem _buildingPickerToolSystem;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            _nameSystem ??= World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<NameSystem>();
            CreateTrigger("OnOpenPicker", OnOpenPicker);
        }

        protected override string group => "SubwayFreedomAssetPack";

        public override void OnWriteProperties(IJsonWriter writer)
        {
        }

        protected override void OnProcess()
        {
        }

        protected override void Reset()
        {
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            visible = false;
            
            if (selectedEntity != Entity.Null && EntityManager.HasComponent<PrefabRef>(selectedEntity))
            {
                var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
                var prefabRef = EntityManager.GetComponentData<PrefabRef>(selectedEntity);
                if (prefabSystem.TryGetPrefab<PrefabBase>(prefabRef.m_Prefab, out var prefab))
                {
                    if (prefab.name.Contains("FreedomSubwayStationBuilding") || 
                        prefab.name.Contains("FreedomSubwayStation02") || 
                        prefab.name.Contains("FreedomSubwayStationElevated"))
                    {
                        visible = true;
                    }
                }
            }
      
            RequestUpdate();
        }
        
        private void OnOpenPicker()
        {
            _buildingPickerToolSystem ??= World.GetOrCreateSystemManaged<BuildingPickerToolSystem>();
            _buildingPickerToolSystem.StartPicking(selectedEntity);
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            
            try
            {
                var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
                var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<InstalledUpgrade>());
                using (var entities = query.ToEntityArray(Unity.Collections.Allocator.TempJob))
                {
                    foreach (var entity in entities)
                    {
                        if (EntityManager.TryGetComponent<PrefabRef>(entity, out var mainPrefabRef))
                        {
                            if (prefabSystem.TryGetPrefab<PrefabBase>(mainPrefabRef.m_Prefab, out var mainPrefabBase))
                            {
                                if (mainPrefabBase.name.Contains("FreedomSubwayStationBuilding") || 
                                    mainPrefabBase.name.Contains("FreedomSubwayStation02") || 
                                    mainPrefabBase.name.Contains("FreedomSubwayStationElevated"))
                                {
                                    var buffer = EntityManager.GetBuffer<InstalledUpgrade>(entity);
                                    foreach (var upgradeElem in buffer)
                                    {
                                        var upgradeEntity = upgradeElem.m_Upgrade;
                                        if (EntityManager.Exists(upgradeEntity) && EntityManager.TryGetComponent<PrefabRef>(upgradeEntity, out var upgradePrefabRef))
                                        {
                                            var mainPrefabEntity = mainPrefabRef.m_Prefab;
                                            var upgradePrefabEntity = upgradePrefabRef.m_Prefab;
                                            
                                            // Restore ServiceUpgradeBuilding buffer on upgradePrefabEntity
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
                                            
                                            // Restore BuildingUpgradeElement buffer on mainPrefabEntity
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

                                            // Restore SubObject buffer
                                            DynamicBuffer<Game.Objects.SubObject> subObjects;
                                            if (EntityManager.HasBuffer<Game.Objects.SubObject>(entity))
                                            {
                                                subObjects = EntityManager.GetBuffer<Game.Objects.SubObject>(entity);
                                            }
                                            else
                                            {
                                                subObjects = EntityManager.AddBuffer<Game.Objects.SubObject>(entity);
                                            }

                                            bool subObjectExists = false;
                                            for (int i = 0; i < subObjects.Length; i++)
                                            {
                                                if (subObjects[i].m_SubObject == upgradeEntity)
                                                {
                                                    subObjectExists = true;
                                                    break;
                                                }
                                            }
                                            if (!subObjectExists)
                                            {
                                                subObjects.Add(new Game.Objects.SubObject(upgradeEntity));
                                            }

                                            // Restore SpawnLocationElements
                                            if (EntityManager.HasBuffer<SpawnLocationElement>(upgradeEntity))
                                            {
                                                var targetSpawnLocations = EntityManager.GetBuffer<SpawnLocationElement>(upgradeEntity);
                                                DynamicBuffer<SpawnLocationElement> mainSpawnLocations;
                                                if (EntityManager.HasBuffer<SpawnLocationElement>(entity))
                                                {
                                                    mainSpawnLocations = EntityManager.GetBuffer<SpawnLocationElement>(entity);
                                                }
                                                else
                                                {
                                                    mainSpawnLocations = EntityManager.AddBuffer<SpawnLocationElement>(entity);
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
                                                        mainSpawnLocations.Add(targetSpawnLocations[i]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Mod.log.Error("Failed to restore prefab upgrade relationships: " + e);
            }
        }
    }
}
