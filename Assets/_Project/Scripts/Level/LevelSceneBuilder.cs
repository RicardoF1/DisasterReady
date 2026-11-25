using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DisasterReady.Level
{
    /// <summary>
    /// Builds a primitive-based, two-storey level-design blockout beneath a House root.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class LevelSceneBuilder : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Level - DisasterReady";
        private const string MaterialResourceFolder = "DisasterReadyLevel/";
        private const float HouseWidth = 16f;
        private const float HouseDepth = 12f;
        private const float HalfHouseWidth = HouseWidth * 0.5f;
        private const float HalfHouseDepth = HouseDepth * 0.5f;
        private const float FirstFloorY = 0f;
        private const float SecondFloorY = 3.2f;
        private const float StoreyHeight = 2.9f;
        private const float FloorThickness = 0.2f;
        private const float WallThickness = 0.2f;
        private const float StairOpeningHalfWidth = 1.1f;
        private const float StairOpeningFrontZ = 0.4f;
        private const float StairOpeningBackZ = 5.8f;
        private const float StairWidth = 1.3f;
        private const int StairStepCount = 18;
        private const float StairRun = 0.27f;
        private const float StairRise = SecondFloorY / StairStepCount;
        private const float EastServiceExitCenterZ = 4.65f;
        private const float EastServiceExitClearWidth = 1.2f;

        private const string SafeZoneRationale =
            "Level-design refuge candidate only; it is a blockout marker, not an engineering or safety certification. Educational/design validation remains pending.";
        private const string DangerZoneRationale =
            "Level-design hazard candidate for visual review only; it is not a prediction or engineering assessment. Educational/design validation remains pending.";

        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private bool isBuilding;

        private struct WallOpening
        {
            public readonly float Center;
            public readonly float Width;
            public readonly float Bottom;
            public readonly float Top;

            public WallOpening(float center, float width, float bottom, float top)
            {
                Center = center;
                Width = width;
                Bottom = bottom;
                Top = top;
            }
        }

        private void OnEnable()
        {
            if (!CanGenerateInCurrentScene() || isBuilding || HasGeneratedRoot())
            {
                return;
            }

            BuildGeneratedLevel();
        }

        /// <summary>
        /// Replaces this House prototype's children with a fresh generated blockout.
        /// This is available from the component context menu for intentional rebuilds.
        /// </summary>
        [ContextMenu("Rebuild Generated Level")]
        public void RebuildGeneratedLevel()
        {
            if (!CanGenerateInCurrentScene() || isBuilding)
            {
                return;
            }

            BuildGeneratedLevel();
        }

        private bool CanGenerateInCurrentScene()
        {
            if (gameObject.name != "House")
            {
                return false;
            }

            Scene scene = gameObject.scene;
            return scene.IsValid() && scene.isLoaded &&
                   (scene.name == "Level" || scene.name == "House");
        }

        private bool HasGeneratedRoot()
        {
            for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
            {
                if (transform.GetChild(childIndex).name == GeneratedRootName)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildGeneratedLevel()
        {
            isBuilding = true;
            try
            {
                ClearHouseChildren();
                RemoveLegacyRootStairs();

                Transform generatedRoot = CreateContainer(transform, GeneratedRootName);
                BuildLevel(generatedRoot);
                LevelEarthquakeEffects earthquakeEffects = generatedRoot.gameObject.AddComponent<LevelEarthquakeEffects>();
                earthquakeEffects.Configure();
            }
            finally
            {
                isBuilding = false;
            }
        }

        private void ClearHouseChildren()
        {
            for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                DestroySafely(transform.GetChild(childIndex).gameObject);
            }
        }

        private void RemoveLegacyRootStairs()
        {
            Scene scene = gameObject.scene;
            if (scene.name != "Level" || transform.parent != null)
            {
                return;
            }

            GameObject[] sceneRoots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < sceneRoots.Length; rootIndex++)
            {
                GameObject sceneRoot = sceneRoots[rootIndex];
                if (sceneRoot != gameObject && sceneRoot.name == "Stairs")
                {
                    DestroySafely(sceneRoot);
                }
            }
        }

        private static void DestroySafely(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                target.SetActive(false);
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void BuildLevel(Transform generatedRoot)
        {
            Transform exterior = CreateContainer(generatedRoot, "Exterior and Roof");
            Transform firstFloor = CreateContainer(generatedRoot, "First Floor - Ground Walk Surface y=0");
            Transform secondFloor = CreateContainer(generatedRoot, "Second Floor - Walk Surface y=3.2m");
            Transform firstStructure = CreateContainer(firstFloor, "Structure");
            Transform secondStructure = CreateContainer(secondFloor, "Structure");

            BuildFloorSlabs(firstStructure, secondStructure);
            BuildExteriorWalls(firstStructure, secondStructure);
            BuildFirstFloorPartitions(firstStructure);
            BuildSecondFloorPartitions(secondStructure);
            BuildRoof(exterior);

            Transform sala = CreateContainer(firstFloor, "Sala");
            Transform comedor = CreateContainer(firstFloor, "Comedor");
            Transform cocina = CreateContainer(firstFloor, "Cocina");
            Transform bano = CreateContainer(firstFloor, "Baño");
            Transform hallStairs = CreateContainer(firstFloor, "Hall and Stairwell");
            Transform entrance = CreateContainer(firstFloor, "Main Entrance");
            Transform firstMarkers = CreateContainer(firstFloor, "Safe and Danger Zone Markers");

            Transform upperCorridor = CreateContainer(secondFloor, "Corridor and Stair Landing");
            Transform bedroomOne = CreateContainer(secondFloor, "Bedroom 1 - Front West");
            Transform bedroomTwo = CreateContainer(secondFloor, "Bedroom 2 - Front East");
            Transform bedroomThree = CreateContainer(secondFloor, "Bedroom 3 - Rear West");
            Transform bedroomFour = CreateContainer(secondFloor, "Bedroom 4 - Rear East");
            Transform secondMarkers = CreateContainer(secondFloor, "Safe and Danger Zone Markers");

            BuildEntrance(entrance);
            BuildExteriorEvacuation(exterior);
            BuildSala(sala);
            BuildComedor(comedor);
            BuildCocina(cocina);
            BuildBano(bano);
            BuildHallAndStairs(hallStairs, upperCorridor);
            BuildUpperCorridor(upperCorridor);
            BuildBedroom(bedroomOne, "Bed 1", new Vector3(-5.5f, SecondFloorY, -3.0f), false);
            BuildBedroom(bedroomTwo, "Bed 2", new Vector3(5.5f, SecondFloorY, -3.0f), false);
            BuildBedroom(bedroomThree, "Bed 3", new Vector3(-5.5f, SecondFloorY, 3.0f), true);
            BuildBedroom(bedroomFour, "Bed 4", new Vector3(5.5f, SecondFloorY, 3.0f), true);
            BuildFirstFloorMarkers(firstMarkers);
            BuildSecondFloorMarkers(secondMarkers);
        }

        private void BuildFloorSlabs(Transform firstStructure, Transform secondStructure)
        {
            CreateBlock(firstStructure, "Ground Floor Slab - 16x12m", new Vector3(0f, FirstFloorY - FloorThickness * 0.5f, 0f),
                new Vector3(HouseWidth, FloorThickness, HouseDepth), "MD_FloorWood");

            CreateBlock(secondStructure, "Upper Floor - Front Landing", new Vector3(0f, SecondFloorY - FloorThickness * 0.5f,
                    (-HalfHouseDepth + StairOpeningFrontZ) * 0.5f),
                new Vector3(HouseWidth, FloorThickness, StairOpeningFrontZ + HalfHouseDepth), "MD_FloorWood");

            float rearDepth = HalfHouseDepth - StairOpeningFrontZ;
            float sideWidth = HalfHouseWidth - StairOpeningHalfWidth;
            float sideCenterX = (HalfHouseWidth + StairOpeningHalfWidth) * 0.5f;
            float rearCenterZ = (StairOpeningFrontZ + HalfHouseDepth) * 0.5f;
            CreateBlock(secondStructure, "Upper Floor - Rear West Walkway and Bedroom Wing",
                new Vector3(-sideCenterX, SecondFloorY - FloorThickness * 0.5f, rearCenterZ),
                new Vector3(sideWidth, FloorThickness, rearDepth), "MD_FloorWood");
            CreateBlock(secondStructure, "Upper Floor - Rear East Walkway and Bedroom Wing",
                new Vector3(sideCenterX, SecondFloorY - FloorThickness * 0.5f, rearCenterZ),
                new Vector3(sideWidth, FloorThickness, rearDepth), "MD_FloorWood");
            CreateBlock(secondStructure, "Upper Floor - Stairwell Rear Bridge", new Vector3(0f, SecondFloorY - FloorThickness * 0.5f,
                    (StairOpeningBackZ + HalfHouseDepth) * 0.5f),
                new Vector3(StairOpeningHalfWidth * 2f, FloorThickness, HalfHouseDepth - StairOpeningBackZ), "MD_FloorWood");

            CreateBlock(firstStructure, "Kitchen Tile Finish", new Vector3(5.45f, 0.012f, -3.0f),
                new Vector3(4.9f, 0.024f, 5.8f), "MD_Tile");
            CreateBlock(firstStructure, "Bathroom Tile Finish", new Vector3(5.5f, 0.025f, 1.5f),
                new Vector3(4.8f, 0.05f, 3.0f), "MD_Tile");
        }

        private void BuildExteriorWalls(Transform firstStructure, Transform secondStructure)
        {
            WallOpening[] groundFrontOpenings =
            {
                new WallOpening(-5.7f, 1.35f, 0.85f, 2.15f),
                new WallOpening(0f, 1.6f, 0f, 2.2f),
                new WallOpening(5.7f, 1.35f, 0.85f, 2.15f)
            };
            WallOpening[] groundBackOpenings =
            {
                new WallOpening(-5.3f, 1.25f, 0.85f, 2.15f),
                new WallOpening(5.3f, 1.25f, 0.85f, 2.15f)
            };
            WallOpening[] groundWestSideOpenings =
            {
                new WallOpening(-2.5f, 1.35f, 0.85f, 2.15f),
                new WallOpening(3.25f, 1.35f, 0.85f, 2.15f)
            };
            WallOpening[] groundEastSideOpenings =
            {
                new WallOpening(-2.5f, 1.35f, 0.85f, 2.15f),
                new WallOpening(EastServiceExitCenterZ, EastServiceExitClearWidth, 0f, 2.2f)
            };
            WallOpening[] upperFrontOpenings =
            {
                new WallOpening(-5.5f, 1.4f, 0.85f, 2.15f),
                new WallOpening(0f, 1.4f, 0.85f, 2.15f),
                new WallOpening(5.5f, 1.4f, 0.85f, 2.15f)
            };
            WallOpening[] upperBackOpenings =
            {
                new WallOpening(-5.3f, 1.3f, 0.85f, 2.15f),
                new WallOpening(5.3f, 1.3f, 0.85f, 2.15f)
            };
            WallOpening[] upperSideOpenings =
            {
                new WallOpening(-2.5f, 1.35f, 0.85f, 2.15f),
                new WallOpening(3.25f, 1.35f, 0.85f, 2.15f)
            };

            CreateWallAlongX(firstStructure, "Ground Front Exterior Wall", -HalfHouseDepth, -HalfHouseWidth, HalfHouseWidth,
                FirstFloorY, StoreyHeight, WallThickness, groundFrontOpenings);
            CreateWallAlongX(firstStructure, "Ground Rear Exterior Wall", HalfHouseDepth, -HalfHouseWidth, HalfHouseWidth,
                FirstFloorY, StoreyHeight, WallThickness, groundBackOpenings);
            CreateWallAlongZ(firstStructure, "Ground West Exterior Wall", -HalfHouseWidth, -HalfHouseDepth, HalfHouseDepth,
                FirstFloorY, StoreyHeight, WallThickness, groundWestSideOpenings);
            CreateWallAlongZ(firstStructure, "Ground East Exterior Wall", HalfHouseWidth, -HalfHouseDepth, HalfHouseDepth,
                FirstFloorY, StoreyHeight, WallThickness, groundEastSideOpenings);

            CreateWallAlongX(secondStructure, "Upper Front Exterior Wall", -HalfHouseDepth, -HalfHouseWidth, HalfHouseWidth,
                SecondFloorY, StoreyHeight, WallThickness, upperFrontOpenings);
            CreateWallAlongX(secondStructure, "Upper Rear Exterior Wall", HalfHouseDepth, -HalfHouseWidth, HalfHouseWidth,
                SecondFloorY, StoreyHeight, WallThickness, upperBackOpenings);
            CreateWallAlongZ(secondStructure, "Upper West Exterior Wall", -HalfHouseWidth, -HalfHouseDepth, HalfHouseDepth,
                SecondFloorY, StoreyHeight, WallThickness, upperSideOpenings);
            CreateWallAlongZ(secondStructure, "Upper East Exterior Wall", HalfHouseWidth, -HalfHouseDepth, HalfHouseDepth,
                SecondFloorY, StoreyHeight, WallThickness, upperSideOpenings);

            AddWindowAlongX(firstStructure, "Ground Front West Window", -HalfHouseDepth, -5.7f, 1.35f, FirstFloorY + 0.85f, "MD_Glass");
            AddWindowAlongX(firstStructure, "Ground Front East Window", -HalfHouseDepth, 5.7f, 1.35f, FirstFloorY + 0.85f, "MD_Glass");
            AddWindowAlongX(firstStructure, "Ground Rear West Window", HalfHouseDepth, -5.3f, 1.25f, FirstFloorY + 0.85f, "MD_Glass");
            AddWindowAlongX(firstStructure, "Ground Rear East Window", HalfHouseDepth, 5.3f, 1.25f, FirstFloorY + 0.85f, "MD_Glass");
            AddWindowAlongZ(firstStructure, "Ground West Window South", -HalfHouseWidth, -2.5f, 1.35f, FirstFloorY + 0.85f, "MD_Glass");
            AddWindowAlongZ(firstStructure, "Ground West Window North", -HalfHouseWidth, 3.25f, 1.35f, FirstFloorY + 0.85f, "MD_Glass");
            AddWindowAlongZ(firstStructure, "Ground East Window South", HalfHouseWidth, -2.5f, 1.35f, FirstFloorY + 0.85f, "MD_Glass");

            AddWindowAlongX(secondStructure, "Upper Front West Window", -HalfHouseDepth, -5.5f, 1.4f, SecondFloorY + 0.85f, "MD_Glass");
            AddWindowAlongX(secondStructure, "Upper Front Center Window", -HalfHouseDepth, 0f, 1.4f, SecondFloorY + 0.85f, "MD_Glass");
            AddWindowAlongX(secondStructure, "Upper Front East Window", -HalfHouseDepth, 5.5f, 1.4f, SecondFloorY + 0.85f, "MD_Glass");
            AddWindowAlongX(secondStructure, "Upper Rear West Window", HalfHouseDepth, -5.3f, 1.3f, SecondFloorY + 0.85f, "MD_Glass");
            AddWindowAlongX(secondStructure, "Upper Rear East Window", HalfHouseDepth, 5.3f, 1.3f, SecondFloorY + 0.85f, "MD_Glass");
            AddWindowAlongZ(secondStructure, "Upper West Window South", -HalfHouseWidth, -2.5f, 1.35f, SecondFloorY + 0.85f, "MD_Glass");
            AddWindowAlongZ(secondStructure, "Upper West Window North", -HalfHouseWidth, 3.25f, 1.35f, SecondFloorY + 0.85f, "MD_Glass");
            AddWindowAlongZ(secondStructure, "Upper East Window South", HalfHouseWidth, -2.5f, 1.35f, SecondFloorY + 0.85f, "MD_Glass");
            AddWindowAlongZ(secondStructure, "Upper East Window North", HalfHouseWidth, 3.25f, 1.35f, SecondFloorY + 0.85f, "MD_Glass");

            AddDoorFrameAlongX(firstStructure, "Main Entrance Door Frame", -HalfHouseDepth, 0f, 1.6f, FirstFloorY);
            AddDoorFrameAlongZ(firstStructure, "Secondary East Service Exit Door Frame - Blocked Route", HalfHouseWidth,
                EastServiceExitCenterZ, EastServiceExitClearWidth, FirstFloorY);
            CreateBlock(firstStructure, "Secondary East Service Exit Threshold", new Vector3(HalfHouseWidth + 0.08f, 0.04f, EastServiceExitCenterZ),
                new Vector3(0.32f, 0.08f, EastServiceExitClearWidth), "MD_Tile");
        }

        private void BuildFirstFloorPartitions(Transform structure)
        {
            WallOpening[] salaDoor = { new WallOpening(-3.1f, 1.2f, 0f, 2.15f) };
            WallOpening[] kitchenDoor = { new WallOpening(-3.0f, 1.2f, 0f, 2.15f) };
            WallOpening[] salaRearDoor = { new WallOpening(-4.5f, 1.2f, 0f, 2.15f) };
            WallOpening[] kitchenRearWallDoor = { new WallOpening(5.3f, 1.2f, 0f, 2.15f) };
            WallOpening[] bathEntry = { new WallOpening(1.55f, 1.2f, 0f, 2.15f) };
            WallOpening[] bathRearDoor = { new WallOpening(4.6f, 1.2f, 0f, 2.15f) };

            CreateWallAlongZ(structure, "Sala and Dining Partition", -1.5f, -HalfHouseDepth, 0f,
                FirstFloorY, StoreyHeight, WallThickness, salaDoor);
            CreateWallAlongZ(structure, "Dining and Kitchen Partition", 3f, -HalfHouseDepth, 0f,
                FirstFloorY, StoreyHeight, WallThickness, kitchenDoor);
            CreateWallAlongX(structure, "Sala Rear Partition", 0f, -HalfHouseWidth, -1.5f,
                FirstFloorY, StoreyHeight, WallThickness, salaRearDoor);
            CreateWallAlongX(structure, "Kitchen Rear Partition", 0f, 3f, HalfHouseWidth,
                FirstFloorY, StoreyHeight, WallThickness, kitchenRearWallDoor);
            CreateWallAlongZ(structure, "Bath and Hall Partition", 3f, 0f, 6f,
                FirstFloorY, StoreyHeight, WallThickness, bathEntry);
            CreateWallAlongX(structure, "Bath Rear Partition", 3f, 3f, HalfHouseWidth,
                FirstFloorY, StoreyHeight, WallThickness, bathRearDoor);

            AddDoorFrameAlongZ(structure, "Sala Door Frame", -1.5f, -3.1f, 1.2f, FirstFloorY);
            AddDoorFrameAlongZ(structure, "Kitchen Door Frame", 3f, -3.0f, 1.2f, FirstFloorY);
            AddDoorFrameAlongX(structure, "Sala Rear Door Frame", 0f, -4.5f, 1.2f, FirstFloorY);
            AddDoorFrameAlongZ(structure, "Bathroom Door Frame", 3f, 1.55f, 1.2f, FirstFloorY);

            CreateBlock(structure, "Interior Frame Junction - Hall-Sala", new Vector3(-1.5f, FirstFloorY + StoreyHeight * 0.5f, 0f),
                new Vector3(0.24f, StoreyHeight, 0.24f), "MD_Wood");
            CreateBlock(structure, "Interior Frame Junction - Hall-Sala Upper", new Vector3(-1.5f, SecondFloorY + StoreyHeight * 0.5f, 0f),
                new Vector3(0.24f, StoreyHeight, 0.24f), "MD_Wood");
        }

        private void BuildSecondFloorPartitions(Transform structure)
        {
            WallOpening[] westCorridorDoors =
            {
                new WallOpening(-2.4f, 1.2f, 0f, 2.15f),
                new WallOpening(2.5f, 1.2f, 0f, 2.15f)
            };
            WallOpening[] eastCorridorDoors =
            {
                new WallOpening(-2.4f, 1.2f, 0f, 2.15f),
                new WallOpening(2.5f, 1.2f, 0f, 2.15f)
            };
            WallOpening[] westWingDividerDoor = { new WallOpening(-5.4f, 1.2f, 0f, 2.15f) };
            WallOpening[] eastWingDividerDoor = { new WallOpening(5.4f, 1.2f, 0f, 2.15f) };

            CreateWallAlongZ(structure, "Upper West Corridor Partition", -3f, -HalfHouseDepth, HalfHouseDepth,
                SecondFloorY, StoreyHeight, WallThickness, westCorridorDoors);
            CreateWallAlongZ(structure, "Upper East Corridor Partition", 3f, -HalfHouseDepth, HalfHouseDepth,
                SecondFloorY, StoreyHeight, WallThickness, eastCorridorDoors);
            CreateWallAlongX(structure, "Upper West Bedroom Divider", 0.25f, -HalfHouseWidth, -3f,
                SecondFloorY, StoreyHeight, WallThickness, westWingDividerDoor);
            CreateWallAlongX(structure, "Upper East Bedroom Divider", 0.25f, 3f, HalfHouseWidth,
                SecondFloorY, StoreyHeight, WallThickness, eastWingDividerDoor);

            AddDoorFrameAlongZ(structure, "Bedroom 1 Door Frame", -3f, -2.4f, 1.2f, SecondFloorY);
            AddDoorFrameAlongZ(structure, "Bedroom 2 Door Frame", 3f, -2.4f, 1.2f, SecondFloorY);
            AddDoorFrameAlongZ(structure, "Bedroom 3 Door Frame", -3f, 2.5f, 1.2f, SecondFloorY);
            AddDoorFrameAlongZ(structure, "Bedroom 4 Door Frame", 3f, 2.5f, 1.2f, SecondFloorY);
            AddDoorFrameAlongX(structure, "West Bedroom Divider Door Frame", 0.25f, -5.4f, 1.2f, SecondFloorY);
            AddDoorFrameAlongX(structure, "East Bedroom Divider Door Frame", 0.25f, 5.4f, 1.2f, SecondFloorY);
        }

        private void BuildRoof(Transform exterior)
        {
            CreateBlock(exterior, "Roof West Slope", new Vector3(-4.15f, 6.92f, 0f),
                new Vector3(8.55f, 0.2f, HouseDepth + 0.7f), "MD_Roof", new Vector3(0f, 0f, 10f));
            CreateBlock(exterior, "Roof East Slope", new Vector3(4.15f, 6.92f, 0f),
                new Vector3(8.55f, 0.2f, HouseDepth + 0.7f), "MD_Roof", new Vector3(0f, 0f, -10f));
            CreateBlock(exterior, "Roof Ridge Cap", new Vector3(0f, 7.67f, 0f),
                new Vector3(0.22f, 0.24f, HouseDepth + 0.85f), "MD_Roof");
        }

        private void BuildEntrance(Transform entrance)
        {
            CreateBlock(entrance, "Main Entrance Threshold", new Vector3(0f, 0.04f, -5.82f),
                new Vector3(1.6f, 0.08f, 0.35f), "MD_Tile");
            CreateContainer(entrance, "Main Entrance - 1.6m Clear Opening");
            CreateBlock(entrance, "Entry Rug", new Vector3(0f, 0.025f, -5.05f),
                new Vector3(1.25f, 0.04f, 1.15f), "MD_Upholstery");
            CreateCeilingLight(entrance, "Entry Ceiling Light", new Vector3(0f, 2.82f, -5.05f));
        }

        private void BuildExteriorEvacuation(Transform exterior)
        {
            Transform route = CreateContainer(exterior, "Exterior Evacuation Route, Assembly Plaza, and Road");
            CreateBlock(route, "Front Approach Walkway - Clear Main Door Route", new Vector3(0f, -0.12f, -12.3f),
                new Vector3(4.6f, 0.24f, 12.3f), "MD_Sidewalk");
            CreateBlock(route, "Front Approach Landing at Main Door", new Vector3(0f, -0.02f, -6.65f),
                new Vector3(3.4f, 0.12f, 1.5f), "MD_Sidewalk");
            CreateBlock(route, "Sidewalk West Curb", new Vector3(-2.32f, -0.02f, -12.3f),
                new Vector3(0.12f, 0.12f, 12.3f), "MD_White");
            CreateBlock(route, "Sidewalk East Curb", new Vector3(2.32f, -0.02f, -12.3f),
                new Vector3(0.12f, 0.12f, 12.3f), "MD_White");
            CreateBlock(route, "Evacuation Route Guidance - West Green Edge", new Vector3(-1.45f, 0.015f, -12.25f),
                new Vector3(0.09f, 0.03f, 11.9f), "MD_Safe");
            CreateBlock(route, "Evacuation Route Guidance - East Green Edge", new Vector3(1.45f, 0.015f, -12.25f),
                new Vector3(0.09f, 0.03f, 11.9f), "MD_Safe");

            float[] arrowPositions = { -7.5f, -9.35f, -11.2f, -13.05f, -14.9f };
            for (int arrowIndex = 0; arrowIndex < arrowPositions.Length; arrowIndex++)
            {
                CreateEvacuationArrow(route, arrowPositions[arrowIndex], arrowIndex + 1);
            }

            CreateBlock(route, "Green Assembly Point Plaza", new Vector3(0f, -0.07f, -17.1f),
                new Vector3(6f, 0.14f, 4.2f), "MD_Safe");
            CreateBlock(route, "Assembly Plaza White Cross - Vertical", new Vector3(0f, 0.018f, -17.1f),
                new Vector3(0.24f, 0.025f, 1.45f), "MD_White");
            CreateBlock(route, "Assembly Plaza White Cross - Horizontal", new Vector3(0f, 0.018f, -17.1f),
                new Vector3(1.45f, 0.025f, 0.24f), "MD_White");
            CreateBlock(route, "Assembly Point Sign Post West", new Vector3(1.35f, 0.92f, -15.05f),
                new Vector3(0.12f, 1.84f, 0.12f), "MD_Metal");
            CreateBlock(route, "Assembly Point Sign Post East", new Vector3(2.65f, 0.92f, -15.05f),
                new Vector3(0.12f, 1.84f, 0.12f), "MD_Metal");
            CreateBlock(route, "ASSEMBLY POINT - Green Physical Sign", new Vector3(2f, 1.65f, -15.05f),
                new Vector3(1.55f, 0.9f, 0.12f), "MD_Safe");
            CreateBlock(route, "Assembly Sign White Cross - Vertical", new Vector3(2f, 1.65f, -14.975f),
                new Vector3(0.18f, 0.56f, 0.025f), "MD_White");
            CreateBlock(route, "Assembly Sign White Cross - Horizontal", new Vector3(2f, 1.65f, -14.975f),
                new Vector3(0.56f, 0.18f, 0.025f), "MD_White");

            GameObject markerObject = new GameObject("Assembly Point - Evacuation Marker (Local Data Only)");
            markerObject.transform.SetParent(route, false);
            markerObject.transform.localPosition = new Vector3(0f, 1f, -17.1f);
            BoxCollider markerTrigger = markerObject.AddComponent<BoxCollider>();
            markerTrigger.isTrigger = true;
            markerTrigger.size = new Vector3(5.2f, 2f, 3.4f);
            LevelEvacuationMarker marker = markerObject.AddComponent<LevelEvacuationMarker>();
            marker.Configure("assembly-point-front-plaza", "Assembly Point", "Follow the green route guidance from the main entrance to the green front plaza.");

            CreateBlock(route, "Front Road - Asphalt", new Vector3(0f, -0.16f, -22.1f),
                new Vector3(26f, 0.2f, 4.4f), "MD_Road");
            for (int markingIndex = 0; markingIndex < 6; markingIndex++)
            {
                float markingX = -10f + markingIndex * 4f;
                CreateBlock(route, "Front Road Center Marking " + (markingIndex + 1), new Vector3(markingX, -0.052f, -22.1f),
                    new Vector3(1.8f, 0.02f, 0.12f), "MD_White");
            }

            BuildBlockedSideExitDebris(exterior);
        }

        private void CreateEvacuationArrow(Transform parent, float centerZ, int arrowNumber)
        {
            string arrowName = "Evacuation Route Arrow " + arrowNumber;
            CreateBlock(parent, arrowName + " Shaft", new Vector3(0f, 0.025f, centerZ + 0.16f),
                new Vector3(0.12f, 0.035f, 0.62f), "MD_Safe");
            CreateBlock(parent, arrowName + " Head Left", new Vector3(-0.16f, 0.025f, centerZ - 0.12f),
                new Vector3(0.1f, 0.035f, 0.42f), "MD_Safe", new Vector3(0f, -42f, 0f));
            CreateBlock(parent, arrowName + " Head Right", new Vector3(0.16f, 0.025f, centerZ - 0.12f),
                new Vector3(0.1f, 0.035f, 0.42f), "MD_Safe", new Vector3(0f, 42f, 0f));
        }

        private void BuildBlockedSideExitDebris(Transform exterior)
        {
            Transform debris = CreateContainer(exterior, "Secondary East Service Exit - BLOCKED by Exterior Debris Pile");
            CreateBlock(debris, "Blocked Side Exit Debris - Large Concrete Chunk A", new Vector3(8.55f, 0.55f, 4.3f),
                new Vector3(0.8f, 1.1f, 0.8f), "MD_White", new Vector3(8f, 12f, 13f));
            CreateBlock(debris, "Blocked Side Exit Debris - Large Concrete Chunk B", new Vector3(9.0f, 0.65f, 4.95f),
                new Vector3(0.85f, 1.3f, 0.72f), "MD_White", new Vector3(-9f, 18f, -12f));
            CreateBlock(debris, "Blocked Side Exit Debris - Fallen Timber A", new Vector3(9.35f, 0.28f, 4.35f),
                new Vector3(1.25f, 0.34f, 0.36f), "MD_Wood", new Vector3(0f, 0f, 17f));
            CreateBlock(debris, "Blocked Side Exit Debris - Fallen Timber B", new Vector3(8.65f, 0.72f, 5.0f),
                new Vector3(0.9f, 0.28f, 0.42f), "MD_Wood", new Vector3(0f, 21f, -23f));
            CreateBlock(debris, "Blocked Side Exit Debris - Small Concrete Chunk C", new Vector3(9.7f, 0.25f, 4.75f),
                new Vector3(0.62f, 0.5f, 0.64f), "MD_White", new Vector3(14f, -9f, 19f));
            CreateBlock(debris, "Blocked Side Exit Debris - Small Concrete Chunk D", new Vector3(8.35f, 0.22f, 5.45f),
                new Vector3(0.62f, 0.44f, 0.55f), "MD_White", new Vector3(-7f, 11f, 8f));
        }

        private void BuildSala(Transform sala)
        {
            CreateSofa(sala, "Sala Sofa", new Vector3(-5.1f, 0f, -4.55f));
            CreateSofa(sala, "Sala Loveseat", new Vector3(-3.0f, 0f, -1.25f), new Vector3(1.8f, 0.78f, 0.82f));
            CreateCoffeeTable(sala, "Sala Coffee Table", new Vector3(-5.0f, 0f, -2.85f));
            CreateTelevisionConsole(sala, new Vector3(-7.1f, 0f, -3.05f));
            CreateLaptop(sala, new Vector3(-4.55f, 0.53f, -2.85f));
            CreatePlant(sala, "Sala Decorative Plant", new Vector3(-7.0f, 0f, -0.9f));
            CreatePainting(sala, "Sala Wall Painting", new Vector3(-1.68f, 1.8f, -4.55f), false);
            CreateCeilingLight(sala, "Sala Ceiling Light", new Vector3(-5.0f, 2.82f, -3.0f));
        }

        private void BuildComedor(Transform comedor)
        {
            Transform diningTable = CreateDiningTable(comedor, new Vector3(0.45f, 0f, -3.35f));
            CreateDiningChair(comedor, "Dining Chair West", new Vector3(-0.72f, 0f, -3.35f), 90f);
            CreateDiningChair(comedor, "Dining Chair East", new Vector3(1.62f, 0f, -3.35f), -90f);
            CreateDiningChair(comedor, "Dining Chair North", new Vector3(0.45f, 0f, -4.12f), 0f);
            CreateDiningChair(comedor, "Dining Chair South", new Vector3(0.45f, 0f, -2.58f), 180f);
            CreateSupplyWater(comedor, new Vector3(0.95f, 0.84f, -3.25f));
            CreateCeilingLight(comedor, "Comedor Ceiling Light", new Vector3(0.3f, 2.82f, -3.1f));
            CreatePainting(comedor, "Comedor Wall Painting", new Vector3(2.88f, 1.8f, -4.6f), false);
            AddZoneMarker(diningTable, "Safe Refuge - Under Sturdy Dining Table", new Vector3(0f, 0.39f, 0f),
                new Vector3(1.65f, 0.68f, 1.15f), LevelZoneKind.SafeRefuge, SafeZoneRationale);
        }

        private void BuildCocina(Transform cocina)
        {
            CreateKitchenCounter(cocina, "Kitchen Counter Run", new Vector3(6.75f, 0f, -3.25f));
            CreateRefrigerator(cocina, new Vector3(4.15f, 0f, -5.0f));
            CreateStove(cocina, new Vector3(6.1f, 0f, -4.2f));
            CreateSink(cocina, new Vector3(7.0f, 0f, -3.25f));
            CreateSupplyRadio(cocina, new Vector3(5.9f, 1.06f, -3.4f));
            CreateSupplyBatteries(cocina, new Vector3(6.45f, 0.97f, -3.15f));
            CreateSupplyWhistle(cocina, new Vector3(6.95f, 0.97f, -3.45f));
            CreateSupplyFood(cocina, new Vector3(7.45f, 0.93f, -3.15f));
            CreateBlock(cocina, "Kitchen Emergency Shelf", new Vector3(4.35f, 0.8f, -2.3f),
                new Vector3(1.0f, 0.1f, 0.6f), "MD_Wood");
            CreateSupplyDocuments(cocina, new Vector3(4.35f, 0.85f, -2.3f));
            BuildOptionalGasValve(cocina);
            CreateCeilingLight(cocina, "Cocina Ceiling Light", new Vector3(5.3f, 2.82f, -2.8f));
        }

        private void BuildOptionalGasValve(Transform kitchen)
        {
            CreateBlock(kitchen, "Optional Gas Shutoff Valve - Prop Only (No Core Interaction) Mount",
                new Vector3(7.72f, 1.25f, -1.15f), new Vector3(0.12f, 0.7f, 0.58f), "MD_Metal");
            CreatePrimitive(kitchen, "Optional Gas Shutoff Valve - Prop Only (No Core Interaction) Pipe",
                PrimitiveType.Cylinder, new Vector3(7.48f, 1.08f, -1.15f), new Vector3(0.09f, 0.34f, 0.09f),
                "MD_Metal", new Vector3(0f, 0f, 90f));
            CreatePrimitive(kitchen, "Optional Gas Shutoff Valve - Prop Only (No Core Interaction) Wheel",
                PrimitiveType.Cylinder, new Vector3(7.38f, 1.35f, -1.15f), new Vector3(0.28f, 0.045f, 0.28f),
                "MD_SupplyRed");
            CreatePrimitive(kitchen, "Optional Gas Shutoff Valve - Prop Only (No Core Interaction) Handle",
                PrimitiveType.Cylinder, new Vector3(7.38f, 1.41f, -1.15f), new Vector3(0.045f, 0.13f, 0.045f),
                "MD_Metal");
        }

        private void BuildBano(Transform bano)
        {
            CreateToilet(bano, new Vector3(6.9f, 0f, 2.25f));
            CreateBathroomSink(bano, new Vector3(4.15f, 0f, 1.0f));
            CreateBathtub(bano, new Vector3(6.1f, 0f, 0.45f));
            CreateBathroomMirror(bano, new Vector3(3.15f, 1.45f, 1.65f));
            CreateCeilingLight(bano, "Bathroom Ceiling Light", new Vector3(5.5f, 2.82f, 1.5f));
        }

        private void BuildHallAndStairs(Transform hallStairs, Transform upperCorridor)
        {
            CreateStairFlight(hallStairs);
            CreateBlock(hallStairs, "Stairwell Upper Landing", new Vector3(0f, SecondFloorY - 0.1f, 0.2f),
                new Vector3(1.35f, FloorThickness, 0.55f), "MD_FloorWood");
            CreateBlock(hallStairs, "Stairwell West Guard Rail", new Vector3(-0.98f, 3.67f, 3.1f),
                new Vector3(0.08f, 0.9f, 5.35f), "MD_Wood");
            CreateBlock(hallStairs, "Stairwell East Guard Rail", new Vector3(0.98f, 3.67f, 3.1f),
                new Vector3(0.08f, 0.9f, 5.35f), "MD_Wood");
            CreateSupplyCoat(hallStairs, new Vector3(2.25f, 1.15f, 4.25f));
            CreateCoatPeg(hallStairs, new Vector3(2.2f, 1.2f, 4.25f));
            BuildOptionalBreakerPanel(hallStairs);
            CreateTallShelf(hallStairs, "Hall Low Storage Shelves", new Vector3(-1.9f, 0f, 4.8f), false);
            CreateCeilingLight(hallStairs, "Hall Ceiling Light", new Vector3(2.2f, 2.82f, 3.0f));
            CreateCeilingLight(upperCorridor, "Upper Corridor Ceiling Light", new Vector3(2.2f, 6.02f, -1.5f));
        }

        private void BuildOptionalBreakerPanel(Transform hall)
        {
            CreateBlock(hall, "Optional Electrical Breaker Panel - Prop Only (No Core Interaction) Housing",
                new Vector3(2.82f, 1.5f, 1.15f), new Vector3(0.12f, 0.72f, 0.56f), "MD_Metal");
            CreateBlock(hall, "Optional Electrical Breaker Panel - Prop Only (No Core Interaction) Door",
                new Vector3(2.74f, 1.5f, 1.15f), new Vector3(0.045f, 0.62f, 0.46f), "MD_White");
            CreateBlock(hall, "Optional Electrical Breaker Panel - Prop Only (No Core Interaction) Red Switch",
                new Vector3(2.70f, 1.52f, 1.15f), new Vector3(0.04f, 0.2f, 0.12f), "MD_SupplyRed");
        }

        private void BuildUpperCorridor(Transform corridor)
        {
            CreateSofa(corridor, "Upper Landing Bench", new Vector3(2.05f, SecondFloorY, -4.65f),
                new Vector3(1.45f, 0.7f, 0.7f));
            CreatePlant(corridor, "Upper Hall Decorative Plant", new Vector3(-2.0f, SecondFloorY, -5.15f));
            CreatePainting(corridor, "Upper Hall Painting", new Vector3(2.88f, 4.85f, -3.0f), false);
        }

        private void BuildBedroom(Transform bedroom, string bedName, Vector3 bedPosition, bool rearRoom)
        {
            CreateBed(bedroom, bedName, bedPosition);
            float wardrobeX = bedPosition.x < 0f ? -7.05f : 7.05f;
            float wardrobeZ = rearRoom ? 5.0f : -5.05f;
            CreateWardrobe(bedroom, "Bedroom Wardrobe", new Vector3(wardrobeX, SecondFloorY, wardrobeZ));
            float shelfX = bedPosition.x < 0f ? -3.75f : 3.75f;
            float shelfZ = rearRoom ? 4.75f : -4.75f;
            CreateTallShelf(bedroom, "Tall Bedroom Shelving", new Vector3(shelfX, SecondFloorY, shelfZ), true);
            CreateBedsideTable(bedroom, "Bedside Table", new Vector3(bedPosition.x + (bedPosition.x < 0f ? 1.35f : -1.35f), SecondFloorY, bedPosition.z));
            CreateCeilingLight(bedroom, "Bedroom Ceiling Light", new Vector3(bedPosition.x, SecondFloorY + 2.82f, bedPosition.z));
            CreatePainting(bedroom, "Bedroom Wall Painting", new Vector3(bedPosition.x, SecondFloorY + 1.8f, rearRoom ? 0.36f : -0.36f), true);
        }

        private void BuildFirstFloorMarkers(Transform markers)
        {
            AddZoneMarker(markers, "Safe Refuge - Near Interior Frame Junction - Hall-Sala", new Vector3(-2.45f, 1.0f, 0.55f),
                new Vector3(1.4f, 2.0f, 1.4f), LevelZoneKind.SafeRefuge, SafeZoneRationale +
                " Specifically designated level-design refuge near Interior Frame Junction - Hall-Sala.");

            AddZoneMarker(markers, "Danger - Ground Front West Window", new Vector3(-5.7f, 1.35f, -5.25f),
                new Vector3(2.2f, 2.3f, 1.5f), LevelZoneKind.Danger, DangerZoneRationale);
            AddZoneMarker(markers, "Danger - Ground East Kitchen Window", new Vector3(7.25f, 1.35f, -2.5f),
                new Vector3(1.5f, 2.3f, 2.2f), LevelZoneKind.Danger, DangerZoneRationale);
            AddZoneMarker(markers, "Danger - Ground Rear Window", new Vector3(5.3f, 1.35f, 5.25f),
                new Vector3(2.2f, 2.3f, 1.5f), LevelZoneKind.Danger, DangerZoneRationale);

            AddZoneMarker(markers, "Danger - Hall Shelving Topple Region", new Vector3(-1.9f, 1.25f, 4.8f),
                new Vector3(1.8f, 2.5f, 1.5f), LevelZoneKind.Danger, DangerZoneRationale);
        }

        private void BuildSecondFloorMarkers(Transform markers)
        {
            AddZoneMarker(markers, "Danger - Upper West Window Exposure", new Vector3(-7.25f, 4.55f, -2.5f),
                new Vector3(1.5f, 2.3f, 2.2f), LevelZoneKind.Danger, DangerZoneRationale);
            AddZoneMarker(markers, "Danger - Upper Front Window Exposure", new Vector3(5.5f, 4.55f, -5.25f),
                new Vector3(2.2f, 2.3f, 1.5f), LevelZoneKind.Danger, DangerZoneRationale);
            AddZoneMarker(markers, "Danger - Bedroom 3 Tall Shelving Topple Region", new Vector3(-3.75f, 4.55f, 4.75f),
                new Vector3(1.8f, 2.7f, 1.5f), LevelZoneKind.Danger, DangerZoneRationale);
            AddZoneMarker(markers, "Danger - Bedroom 4 Tall Shelving Topple Region", new Vector3(3.75f, 4.55f, 4.75f),
                new Vector3(1.8f, 2.7f, 1.5f), LevelZoneKind.Danger, DangerZoneRationale);
        }

        private void CreateStairFlight(Transform parent)
        {
            Transform stairs = CreateContainer(parent, "Stair Flight - 18 Treads and Risers");
            for (int stepIndex = 0; stepIndex < StairStepCount; stepIndex++)
            {
                float treadY = (stepIndex + 1) * StairRise - 0.04f;
                float treadZ = StairOpeningBackZ - 0.35f - stepIndex * StairRun;
                CreateBlock(stairs, "Stair Tread " + (stepIndex + 1).ToString("00"),
                    new Vector3(0f, treadY, treadZ), new Vector3(StairWidth, 0.08f, 0.31f), "MD_Wood");
                CreateBlock(stairs, "Stair Riser " + (stepIndex + 1).ToString("00"),
                    new Vector3(0f, stepIndex * StairRise + StairRise * 0.5f, treadZ - StairRun * 0.5f),
                    new Vector3(StairWidth, StairRise, 0.07f), "MD_Wood");
            }
        }

        private void CreateWallAlongX(Transform parent, string name, float z, float minX, float maxX,
            float baseY, float height, float thickness, params WallOpening[] openings)
        {
            if (openings == null)
            {
                openings = new WallOpening[0];
            }

            Array.Sort(openings, delegate(WallOpening left, WallOpening right)
            {
                return left.Center.CompareTo(right.Center);
            });

            float cursor = minX;
            int segmentIndex = 0;
            for (int openingIndex = 0; openingIndex < openings.Length; openingIndex++)
            {
                WallOpening opening = openings[openingIndex];
                float start = Mathf.Max(minX, opening.Center - opening.Width * 0.5f);
                float end = Mathf.Min(maxX, opening.Center + opening.Width * 0.5f);
                if (start > cursor)
                {
                    CreateWallBlock(parent, name + " Segment " + (++segmentIndex), (cursor + start) * 0.5f, z,
                        start - cursor, baseY, height, thickness, true);
                }

                float openingWidth = Mathf.Max(0f, end - start);
                if (openingWidth > 0f && opening.Bottom > 0f)
                {
                    CreateWallBlock(parent, name + " Opening Sill " + (++segmentIndex), (start + end) * 0.5f, z,
                        openingWidth, baseY, Mathf.Min(opening.Bottom, height), thickness, true);
                }
                if (openingWidth > 0f && opening.Top < height)
                {
                    float topBottom = Mathf.Max(0f, opening.Top);
                    CreateWallBlock(parent, name + " Opening Header " + (++segmentIndex), (start + end) * 0.5f, z,
                        openingWidth, baseY + topBottom, Mathf.Max(0f, height - topBottom), thickness, true);
                }

                cursor = Mathf.Max(cursor, end);
            }

            if (cursor < maxX)
            {
                CreateWallBlock(parent, name + " Segment " + (++segmentIndex), (cursor + maxX) * 0.5f, z,
                    maxX - cursor, baseY, height, thickness, true);
            }
        }

        private void CreateWallAlongZ(Transform parent, string name, float x, float minZ, float maxZ,
            float baseY, float height, float thickness, params WallOpening[] openings)
        {
            if (openings == null)
            {
                openings = new WallOpening[0];
            }

            Array.Sort(openings, delegate(WallOpening left, WallOpening right)
            {
                return left.Center.CompareTo(right.Center);
            });

            float cursor = minZ;
            int segmentIndex = 0;
            for (int openingIndex = 0; openingIndex < openings.Length; openingIndex++)
            {
                WallOpening opening = openings[openingIndex];
                float start = Mathf.Max(minZ, opening.Center - opening.Width * 0.5f);
                float end = Mathf.Min(maxZ, opening.Center + opening.Width * 0.5f);
                if (start > cursor)
                {
                    CreateWallBlock(parent, name + " Segment " + (++segmentIndex), x, (cursor + start) * 0.5f,
                        start - cursor, baseY, height, thickness, false);
                }

                float openingWidth = Mathf.Max(0f, end - start);
                if (openingWidth > 0f && opening.Bottom > 0f)
                {
                    CreateWallBlock(parent, name + " Opening Sill " + (++segmentIndex), x, (start + end) * 0.5f,
                        openingWidth, baseY, Mathf.Min(opening.Bottom, height), thickness, false);
                }
                if (openingWidth > 0f && opening.Top < height)
                {
                    float topBottom = Mathf.Max(0f, opening.Top);
                    CreateWallBlock(parent, name + " Opening Header " + (++segmentIndex), x, (start + end) * 0.5f,
                        openingWidth, baseY + topBottom, Mathf.Max(0f, height - topBottom), thickness, false);
                }

                cursor = Mathf.Max(cursor, end);
            }

            if (cursor < maxZ)
            {
                CreateWallBlock(parent, name + " Segment " + (++segmentIndex), x, (cursor + maxZ) * 0.5f,
                    maxZ - cursor, baseY, height, thickness, false);
            }
        }

        private void CreateWallBlock(Transform parent, string name, float alongX, float z, float length,
            float baseY, float height, float thickness, bool alongXDirection)
        {
            if (length <= 0.001f || height <= 0.001f)
            {
                return;
            }

            Vector3 position = new Vector3(alongX, baseY + height * 0.5f, z);
            Vector3 size = alongXDirection
                ? new Vector3(length, height, thickness)
                : new Vector3(thickness, height, length);
            CreateBlock(parent, name, position, size, "MD_Wall");
        }

        private void AddWindowAlongX(Transform parent, string name, float z, float centerX, float width, float baseY, string glassMaterial)
        {
            float windowHeight = 1.3f;
            float windowCenterY = baseY + 0.85f + windowHeight * 0.5f;
            float paneDepth = 0.035f;
            GameObject pane = CreateBlock(parent, name + " Glass", new Vector3(centerX, windowCenterY, z),
                new Vector3(width - 0.12f, windowHeight - 0.12f, paneDepth), glassMaterial);
            LevelBreakableWindow breakableWindow = pane.AddComponent<LevelBreakableWindow>();
            breakableWindow.Configure(parent);
            CreateBlock(parent, name + " Sill", new Vector3(centerX, baseY + 0.84f, z),
                new Vector3(width + 0.12f, 0.08f, 0.18f), "MD_White");
            CreateBlock(parent, name + " Header", new Vector3(centerX, baseY + 2.17f, z),
                new Vector3(width + 0.12f, 0.08f, 0.18f), "MD_White");
            CreateBlock(parent, name + " Left Frame", new Vector3(centerX - width * 0.5f, baseY + 1.5f, z),
                new Vector3(0.07f, 1.3f, 0.18f), "MD_White");
            CreateBlock(parent, name + " Right Frame", new Vector3(centerX + width * 0.5f, baseY + 1.5f, z),
                new Vector3(0.07f, 1.3f, 0.18f), "MD_White");
        }

        private void AddWindowAlongZ(Transform parent, string name, float x, float centerZ, float width, float baseY, string glassMaterial)
        {
            float windowHeight = 1.3f;
            float windowCenterY = baseY + 0.85f + windowHeight * 0.5f;
            float paneDepth = 0.035f;
            GameObject pane = CreateBlock(parent, name + " Glass", new Vector3(x, windowCenterY, centerZ),
                new Vector3(paneDepth, windowHeight - 0.12f, width - 0.12f), glassMaterial);
            LevelBreakableWindow breakableWindow = pane.AddComponent<LevelBreakableWindow>();
            breakableWindow.Configure(parent);
            CreateBlock(parent, name + " Sill", new Vector3(x, baseY + 0.84f, centerZ),
                new Vector3(0.18f, 0.08f, width + 0.12f), "MD_White");
            CreateBlock(parent, name + " Header", new Vector3(x, baseY + 2.17f, centerZ),
                new Vector3(0.18f, 0.08f, width + 0.12f), "MD_White");
            CreateBlock(parent, name + " South Frame", new Vector3(x, baseY + 1.5f, centerZ - width * 0.5f),
                new Vector3(0.18f, 1.3f, 0.07f), "MD_White");
            CreateBlock(parent, name + " North Frame", new Vector3(x, baseY + 1.5f, centerZ + width * 0.5f),
                new Vector3(0.18f, 1.3f, 0.07f), "MD_White");
        }

        private void AddDoorFrameAlongX(Transform parent, string name, float z, float centerX, float clearWidth, float baseY)
        {
            float sideOffset = clearWidth * 0.5f + 0.045f;
            CreateBlock(parent, name + " Left Jamb", new Vector3(centerX - sideOffset, baseY + 1.05f, z),
                new Vector3(0.07f, 2.1f, 0.14f), "MD_Wood");
            CreateBlock(parent, name + " Right Jamb", new Vector3(centerX + sideOffset, baseY + 1.05f, z),
                new Vector3(0.07f, 2.1f, 0.14f), "MD_Wood");
            CreateBlock(parent, name + " Header", new Vector3(centerX, baseY + 2.14f, z),
                new Vector3(clearWidth + 0.16f, 0.08f, 0.14f), "MD_Wood");
        }

        private void AddDoorFrameAlongZ(Transform parent, string name, float x, float centerZ, float clearWidth, float baseY)
        {
            float sideOffset = clearWidth * 0.5f + 0.045f;
            CreateBlock(parent, name + " South Jamb", new Vector3(x, baseY + 1.05f, centerZ - sideOffset),
                new Vector3(0.14f, 2.1f, 0.07f), "MD_Wood");
            CreateBlock(parent, name + " North Jamb", new Vector3(x, baseY + 1.05f, centerZ + sideOffset),
                new Vector3(0.14f, 2.1f, 0.07f), "MD_Wood");
            CreateBlock(parent, name + " Header", new Vector3(x, baseY + 2.14f, centerZ),
                new Vector3(0.14f, 0.08f, clearWidth + 0.16f), "MD_Wood");
        }

        private Transform CreateSofa(Transform parent, string name, Vector3 position, Vector3 dimensions = default(Vector3))
        {
            if (dimensions == Vector3.zero)
            {
                dimensions = new Vector3(2.4f, 0.82f, 0.9f);
            }

            Transform sofa = CreateContainer(parent, name);
            sofa.localPosition = position;
            CreateBlock(sofa, "Sofa Base", new Vector3(0f, dimensions.y * 0.32f, 0f),
                new Vector3(dimensions.x, dimensions.y * 0.42f, dimensions.z), "MD_Upholstery");
            CreateBlock(sofa, "Sofa Back", new Vector3(0f, dimensions.y * 0.72f, dimensions.z * 0.39f),
                new Vector3(dimensions.x, dimensions.y * 0.56f, dimensions.z * 0.22f), "MD_Upholstery");
            CreateBlock(sofa, "Sofa Left Arm", new Vector3(-dimensions.x * 0.46f, dimensions.y * 0.55f, 0f),
                new Vector3(dimensions.x * 0.08f, dimensions.y * 0.56f, dimensions.z), "MD_Upholstery");
            CreateBlock(sofa, "Sofa Right Arm", new Vector3(dimensions.x * 0.46f, dimensions.y * 0.55f, 0f),
                new Vector3(dimensions.x * 0.08f, dimensions.y * 0.56f, dimensions.z), "MD_Upholstery");
            for (int legIndex = 0; legIndex < 4; legIndex++)
            {
                float x = legIndex % 2 == 0 ? -dimensions.x * 0.4f : dimensions.x * 0.4f;
                float z = legIndex < 2 ? -dimensions.z * 0.36f : dimensions.z * 0.36f;
                CreateBlock(sofa, "Sofa Leg " + (legIndex + 1), new Vector3(x, 0.12f, z),
                    new Vector3(0.1f, 0.24f, 0.1f), "MD_Wood");
            }
            return sofa;
        }

        private Transform CreateCoffeeTable(Transform parent, string name, Vector3 position)
        {
            Transform table = CreateContainer(parent, name);
            table.localPosition = position;
            CreateBlock(table, "Coffee Table Top", new Vector3(0f, 0.48f, 0f), new Vector3(1.35f, 0.1f, 0.75f), "MD_Wood");
            CreateTableLegs(table, 1.2f, 0.65f, 0.43f, "Coffee Table");
            CreateSupplyFlashlight(table, new Vector3(-0.35f, 0.61f, 0.06f));
            CreateSupplyFirstAidKit(table, new Vector3(0.32f, 0.68f, -0.06f));
            return table;
        }

        private Transform CreateDiningTable(Transform parent, Vector3 position)
        {
            Transform table = CreateContainer(parent, "Sturdy Dining Table");
            table.localPosition = position;
            CreateBlock(table, "Dining Table Top", new Vector3(0f, 0.78f, 0f), new Vector3(1.95f, 0.12f, 1.05f), "MD_Wood");
            CreateTableLegs(table, 1.78f, 0.7f, 0.72f, "Dining Table");
            return table;
        }

        private void CreateTableLegs(Transform parent, float width, float depth, float legHeight, string name)
        {
            for (int legIndex = 0; legIndex < 4; legIndex++)
            {
                float x = legIndex % 2 == 0 ? -width * 0.5f : width * 0.5f;
                float z = legIndex < 2 ? -depth * 0.5f : depth * 0.5f;
                CreateBlock(parent, name + " Leg " + (legIndex + 1), new Vector3(x, legHeight * 0.5f, z),
                    new Vector3(0.11f, legHeight, 0.11f), "MD_Wood");
            }
        }

        private void CreateDiningChair(Transform parent, string name, Vector3 position, float yaw)
        {
            Transform chair = CreateContainer(parent, name);
            chair.localPosition = position;
            chair.localRotation = Quaternion.Euler(0f, yaw, 0f);
            CreateBlock(chair, "Chair Seat", new Vector3(0f, 0.48f, 0f), new Vector3(0.48f, 0.1f, 0.48f), "MD_Wood");
            CreateBlock(chair, "Chair Back", new Vector3(0f, 0.88f, 0.2f), new Vector3(0.48f, 0.78f, 0.09f), "MD_Wood");
            CreateTableLegs(chair, 0.38f, 0.38f, 0.46f, "Chair");
        }

        private void CreateTelevisionConsole(Transform parent, Vector3 position)
        {
            Transform console = CreateContainer(parent, "Sala TV Console and Television");
            console.localPosition = position;
            CreateBlock(console, "TV Console Cabinet", new Vector3(0f, 0.38f, 0f), new Vector3(1.5f, 0.72f, 0.48f), "MD_Wood");
            CreateBlock(console, "Television Screen", new Vector3(0f, 1.28f, 0.17f), new Vector3(1.3f, 0.76f, 0.09f), "MD_Metal");
            CreateGameConsole(console, new Vector3(0.35f, 0.8f, -0.12f));
        }

        private void CreateLaptop(Transform parent, Vector3 position)
        {
            Transform laptop = CreateContainer(parent, "Laptop - Non-supply Decor");
            laptop.localPosition = position;
            CreateBlock(laptop, "Laptop Keyboard Base", new Vector3(0f, 0.035f, 0f), new Vector3(0.42f, 0.06f, 0.3f), "MD_Metal");
            CreateBlock(laptop, "Laptop Display", new Vector3(0f, 0.2f, 0.135f), new Vector3(0.42f, 0.31f, 0.045f), "MD_Metal", new Vector3(-15f, 0f, 0f));
            CreateBlock(laptop, "Laptop Screen", new Vector3(0f, 0.2f, 0.109f), new Vector3(0.35f, 0.23f, 0.012f), "MD_Glass", new Vector3(-15f, 0f, 0f));
        }

        private void CreateKitchenCounter(Transform parent, string name, Vector3 position)
        {
            Transform counter = CreateContainer(parent, name);
            counter.localPosition = position;
            CreateBlock(counter, "Kitchen Cabinet Base", new Vector3(0f, 0.42f, 0f), new Vector3(2.0f, 0.84f, 0.62f), "MD_Wood");
            CreateBlock(counter, "Kitchen Worktop", new Vector3(0f, 0.88f, 0f), new Vector3(2.08f, 0.1f, 0.7f), "MD_Ceramic");
            CreateBlock(counter, "Kitchen Upper Shelf", new Vector3(0f, 1.82f, 0.2f), new Vector3(1.8f, 0.16f, 0.4f), "MD_Wood");
        }

        private void CreateRefrigerator(Transform parent, Vector3 position)
        {
            Transform fridge = CreateContainer(parent, "Kitchen Refrigerator");
            fridge.localPosition = position;
            CreateBlock(fridge, "Refrigerator Body", new Vector3(0f, 1.0f, 0f), new Vector3(0.9f, 2.0f, 0.82f), "MD_White");
            CreateBlock(fridge, "Refrigerator Handle", new Vector3(0.34f, 1.1f, -0.43f), new Vector3(0.045f, 0.65f, 0.06f), "MD_Metal");
        }

        private void CreateStove(Transform parent, Vector3 position)
        {
            Transform stove = CreateContainer(parent, "Kitchen Stove and Oven");
            stove.localPosition = position;
            CreateBlock(stove, "Oven Body", new Vector3(0f, 0.44f, 0f), new Vector3(0.75f, 0.88f, 0.68f), "MD_Metal");
            CreateBlock(stove, "Stove Top", new Vector3(0f, 0.91f, 0f), new Vector3(0.8f, 0.08f, 0.72f), "MD_Ceramic");
            for (int burner = 0; burner < 4; burner++)
            {
                float x = burner % 2 == 0 ? -0.2f : 0.2f;
                float z = burner < 2 ? -0.18f : 0.18f;
                CreatePrimitive(stove, "Stove Burner " + (burner + 1), PrimitiveType.Cylinder,
                    new Vector3(x, 0.97f, z), new Vector3(0.17f, 0.025f, 0.17f), "MD_Metal");
            }
        }

        private void CreateSink(Transform parent, Vector3 position)
        {
            Transform sink = CreateContainer(parent, "Kitchen Sink");
            sink.localPosition = position;
            CreateBlock(sink, "Sink Basin", new Vector3(0f, 0.91f, 0f), new Vector3(0.62f, 0.12f, 0.45f), "MD_Ceramic");
            CreatePrimitive(sink, "Sink Faucet", PrimitiveType.Cylinder, new Vector3(0f, 1.1f, 0.2f),
                new Vector3(0.045f, 0.16f, 0.045f), "MD_Metal");
        }

        private void CreateToilet(Transform parent, Vector3 position)
        {
            Transform toilet = CreateContainer(parent, "Bathroom Toilet");
            toilet.localPosition = position;
            CreatePrimitive(toilet, "Toilet Bowl", PrimitiveType.Cylinder, new Vector3(0f, 0.23f, 0f),
                new Vector3(0.42f, 0.24f, 0.52f), "MD_Ceramic");
            CreateBlock(toilet, "Toilet Tank", new Vector3(0f, 0.62f, 0.33f), new Vector3(0.62f, 0.72f, 0.22f), "MD_Ceramic");
            CreateBlock(toilet, "Toilet Seat", new Vector3(0f, 0.47f, -0.06f), new Vector3(0.48f, 0.07f, 0.57f), "MD_White");
        }

        private void CreateBathroomSink(Transform parent, Vector3 position)
        {
            Transform sink = CreateContainer(parent, "Bathroom Sink and Vanity");
            sink.localPosition = position;
            CreateBlock(sink, "Bathroom Vanity", new Vector3(0f, 0.42f, 0f), new Vector3(0.8f, 0.84f, 0.52f), "MD_Wood");
            CreateBlock(sink, "Bathroom Basin", new Vector3(0f, 0.89f, -0.02f), new Vector3(0.82f, 0.12f, 0.56f), "MD_Ceramic");
            CreatePrimitive(sink, "Bathroom Faucet", PrimitiveType.Cylinder, new Vector3(0f, 1.06f, 0.16f),
                new Vector3(0.04f, 0.14f, 0.04f), "MD_Metal");
        }

        private void CreateBathtub(Transform parent, Vector3 position)
        {
            Transform bathtub = CreateContainer(parent, "Bathroom Bathtub");
            bathtub.localPosition = position;
            CreateBlock(bathtub, "Bathtub Outer Shell", new Vector3(0f, 0.34f, 0f), new Vector3(1.55f, 0.68f, 0.82f), "MD_White");
            CreateBlock(bathtub, "Bathtub Inner Basin", new Vector3(0f, 0.69f, 0f), new Vector3(1.32f, 0.08f, 0.62f), "MD_Ceramic");
        }

        private void CreateBathroomMirror(Transform parent, Vector3 position)
        {
            Transform mirror = CreateContainer(parent, "Bathroom Mirror");
            mirror.localPosition = position;
            mirror.localRotation = Quaternion.Euler(0f, 90f, 0f);
            CreateBlock(mirror, "Bathroom Mirror Glass", Vector3.zero, new Vector3(0.72f, 0.82f, 0.05f), "MD_Glass");
            CreateBlock(mirror, "Bathroom Mirror Frame Top", new Vector3(0f, 0.46f, 0.04f), new Vector3(0.86f, 0.08f, 0.08f), "MD_Wood");
            CreateBlock(mirror, "Bathroom Mirror Frame Bottom", new Vector3(0f, -0.46f, 0.04f), new Vector3(0.86f, 0.08f, 0.08f), "MD_Wood");
            CreateBlock(mirror, "Bathroom Mirror Frame Left", new Vector3(-0.39f, 0f, 0.04f), new Vector3(0.08f, 0.84f, 0.08f), "MD_Wood");
            CreateBlock(mirror, "Bathroom Mirror Frame Right", new Vector3(0.39f, 0f, 0.04f), new Vector3(0.08f, 0.84f, 0.08f), "MD_Wood");
        }

        private void CreateBed(Transform parent, string name, Vector3 position)
        {
            Transform bed = CreateContainer(parent, name);
            bed.localPosition = position;
            CreateBlock(bed, "Bed Frame", new Vector3(0f, 0.28f, 0f), new Vector3(2.0f, 0.42f, 1.6f), "MD_Wood");
            CreateBlock(bed, "Mattress", new Vector3(0f, 0.55f, -0.03f), new Vector3(1.92f, 0.26f, 1.52f), "MD_Upholstery");
            CreateBlock(bed, "Pillow", new Vector3(0f, 0.72f, -0.58f), new Vector3(0.68f, 0.14f, 0.4f), "MD_White");
            CreateBlock(bed, "Bed Headboard", new Vector3(0f, 0.72f, -0.82f), new Vector3(2.05f, 0.95f, 0.12f), "MD_Wood");
        }

        private void CreateWardrobe(Transform parent, string name, Vector3 position)
        {
            Transform wardrobe = CreateContainer(parent, name);
            wardrobe.localPosition = position;
            CreateBlock(wardrobe, "Wardrobe Body", new Vector3(0f, 1.15f, 0f), new Vector3(1.35f, 2.3f, 0.62f), "MD_Wood");
            CreateBlock(wardrobe, "Wardrobe Left Door", new Vector3(-0.34f, 1.15f, -0.33f), new Vector3(0.65f, 2.18f, 0.045f), "MD_Wood");
            CreateBlock(wardrobe, "Wardrobe Right Door", new Vector3(0.34f, 1.15f, -0.33f), new Vector3(0.65f, 2.18f, 0.045f), "MD_Wood");
            CreatePrimitive(wardrobe, "Wardrobe Handle Left", PrimitiveType.Sphere, new Vector3(-0.05f, 1.15f, -0.39f),
                new Vector3(0.045f, 0.09f, 0.045f), "MD_Metal");
            CreatePrimitive(wardrobe, "Wardrobe Handle Right", PrimitiveType.Sphere, new Vector3(0.05f, 1.15f, -0.39f),
                new Vector3(0.045f, 0.09f, 0.045f), "MD_Metal");
            AddEarthquakeProp(wardrobe.gameObject, 7.5f, 1.0f, 0.35f, 3.5f, 3.0f);
        }

        private void CreateTallShelf(Transform parent, string name, Vector3 position, bool tall)
        {
            Transform shelf = CreateContainer(parent, name);
            shelf.localPosition = position;
            float height = tall ? 2.25f : 1.55f;
            float depth = 0.42f;
            CreateBlock(shelf, "Shelf Left Side", new Vector3(-0.52f, height * 0.5f, 0f), new Vector3(0.08f, height, depth), "MD_Wood");
            CreateBlock(shelf, "Shelf Right Side", new Vector3(0.52f, height * 0.5f, 0f), new Vector3(0.08f, height, depth), "MD_Wood");
            CreateBlock(shelf, "Shelf Back", new Vector3(0f, height * 0.5f, depth * 0.42f), new Vector3(1.08f, height, 0.06f), "MD_Wood");
            int shelfCount = tall ? 4 : 3;
            for (int shelfIndex = 0; shelfIndex < shelfCount; shelfIndex++)
            {
                float y = 0.12f + shelfIndex * (height - 0.2f) / (shelfCount - 1);
                CreateBlock(shelf, "Shelf Board " + (shelfIndex + 1), new Vector3(0f, y, 0f), new Vector3(1.08f, 0.08f, depth), "MD_Wood");
            }
            Transform detachableBooks = CreateContainer(parent, name + " Detachable Books");
            detachableBooks.localPosition = position;
            CreateDecorBooks(detachableBooks, new Vector3(-0.28f, 0.32f, -0.03f));
            if (tall)
            {
                AddEarthquakeProp(shelf.gameObject, 5f, 0.85f, 0.28f, 3.5f, 3.0f);
            }
        }

        private void CreateBedsideTable(Transform parent, string name, Vector3 position)
        {
            Transform table = CreateContainer(parent, name);
            table.localPosition = position;
            CreateBlock(table, "Bedside Cabinet", new Vector3(0f, 0.34f, 0f), new Vector3(0.56f, 0.68f, 0.5f), "MD_Wood");
            CreateBlock(table, "Bedside Lamp Base", new Vector3(0f, 0.76f, 0f), new Vector3(0.22f, 0.08f, 0.22f), "MD_Metal");
            CreatePrimitive(table, "Bedside Lamp Shade", PrimitiveType.Cylinder, new Vector3(0f, 0.96f, 0f),
                new Vector3(0.22f, 0.2f, 0.22f), "MD_White");
        }

        private void CreatePainting(Transform parent, string name, Vector3 position, bool facesNegativeZ)
        {
            Transform painting = CreateContainer(parent, name);
            painting.localPosition = position;
            if (facesNegativeZ)
            {
                painting.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            CreateBlock(painting, "Painting Frame", Vector3.zero, new Vector3(0.95f, 0.62f, 0.08f), "MD_Wood");
            CreateBlock(painting, "Painting Canvas", new Vector3(0f, 0f, -0.05f), new Vector3(0.81f, 0.48f, 0.025f), "MD_Upholstery");
            CreateBlock(painting, "Painting Accent", new Vector3(0.12f, 0.04f, -0.068f), new Vector3(0.24f, 0.22f, 0.02f), "MD_SupplyYellow");
            AddEarthquakeProp(painting.gameObject, 1.8f, 0.55f, 0.2f, 3.0f, 3.0f);
        }

        private void CreateCeilingLight(Transform parent, string name, Vector3 position)
        {
            GameObject ceilingLight = CreatePrimitive(parent, name, PrimitiveType.Cylinder, position, new Vector3(0.42f, 0.055f, 0.42f), "MD_White");
            AddEarthquakeProp(ceilingLight, 0.8f, 0.4f, 0.18f, 2.5f, 3.0f);
        }

        private void CreatePlant(Transform parent, string name, Vector3 position)
        {
            Transform plant = CreateContainer(parent, name);
            plant.localPosition = position;
            CreatePrimitive(plant, "Plant Pot", PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f),
                new Vector3(0.38f, 0.22f, 0.38f), "MD_Ceramic");
            CreatePrimitive(plant, "Plant Foliage", PrimitiveType.Sphere, new Vector3(0f, 0.78f, 0f),
                new Vector3(0.62f, 0.82f, 0.62f), "MD_Safe");
        }

        private void CreateDecorBooks(Transform parent, Vector3 position)
        {
            for (int bookIndex = 0; bookIndex < 3; bookIndex++)
            {
                GameObject book = CreateBlock(parent, "Decor Book " + (bookIndex + 1), position + new Vector3(bookIndex * 0.17f, 0f, 0f),
                    new Vector3(0.14f, 0.3f + (bookIndex % 2) * 0.08f, 0.22f), bookIndex == 1 ? "MD_SupplyBlue" : "MD_SupplyRed");
                AddEarthquakeProp(book, 0.22f, 0.16f, 0.08f, 3.0f, 3.5f);
            }
        }

        private void CreateGameConsole(Transform parent, Vector3 position)
        {
            Transform console = CreateContainer(parent, "Game Console - Non-supply Decor");
            console.localPosition = position;
            CreateBlock(console, "Console Body", Vector3.zero, new Vector3(0.5f, 0.14f, 0.36f), "MD_Metal");
            CreatePrimitive(console, "Controller", PrimitiveType.Cube, new Vector3(0.36f, 0.1f, 0.3f),
                new Vector3(0.28f, 0.09f, 0.18f), "MD_Metal");
        }

        private void CreateSupplyWater(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "Water");
            supply.localPosition = position;
            CreatePrimitive(supply, "Water Bottle", PrimitiveType.Cylinder, new Vector3(0f, 0.17f, 0f),
                new Vector3(0.14f, 0.17f, 0.14f), "MD_SupplyBlue");
            CreatePrimitive(supply, "Bottle Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.37f, 0f),
                new Vector3(0.07f, 0.06f, 0.07f), "MD_White");
            CreatePrimitive(supply, "Bottle Cap", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f),
                new Vector3(0.08f, 0.035f, 0.08f), "MD_SupplyYellow");
        }

        private void CreateSupplyFlashlight(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "Flashlight");
            supply.localPosition = position;
            CreatePrimitive(supply, "Flashlight Barrel", PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(0.08f, 0.28f, 0.08f), "MD_SupplyBlue", new Vector3(0f, 0f, 90f));
            CreatePrimitive(supply, "Flashlight Lens", PrimitiveType.Sphere, new Vector3(0.28f, 0f, 0f),
                new Vector3(0.11f, 0.11f, 0.11f), "MD_White");
        }

        private void CreateSupplyFirstAidKit(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "First Aid Kit");
            supply.localPosition = position;
            CreateBlock(supply, "First Aid Kit Case", Vector3.zero, new Vector3(0.42f, 0.28f, 0.3f), "MD_SupplyRed");
            CreateBlock(supply, "First Aid Cross Vertical", new Vector3(0f, 0.03f, -0.16f), new Vector3(0.08f, 0.2f, 0.025f), "MD_White");
            CreateBlock(supply, "First Aid Cross Horizontal", new Vector3(0f, 0.03f, -0.16f), new Vector3(0.2f, 0.08f, 0.025f), "MD_White");
        }

        private void CreateSupplyRadio(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "Battery Radio");
            supply.localPosition = position;
            CreateBlock(supply, "Radio Body", Vector3.zero, new Vector3(0.42f, 0.26f, 0.2f), "MD_SupplyYellow");
            CreateBlock(supply, "Radio Speaker", new Vector3(-0.09f, 0f, -0.11f), new Vector3(0.17f, 0.16f, 0.025f), "MD_Metal");
            CreatePrimitive(supply, "Radio Tuning Dial", PrimitiveType.Cylinder, new Vector3(0.12f, 0f, -0.13f),
                new Vector3(0.045f, 0.035f, 0.045f), "MD_White", new Vector3(90f, 0f, 0f));
            CreatePrimitive(supply, "Radio Antenna", PrimitiveType.Cylinder, new Vector3(0.16f, 0.24f, 0.02f),
                new Vector3(0.018f, 0.24f, 0.018f), "MD_Metal", new Vector3(0f, 0f, -16f));
        }

        private void CreateSupplyBatteries(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "Batteries");
            supply.localPosition = position;
            CreatePrimitive(supply, "Battery 1", PrimitiveType.Cylinder, new Vector3(-0.1f, 0.09f, 0f),
                new Vector3(0.07f, 0.11f, 0.07f), "MD_SupplyBlue", new Vector3(0f, 0f, 90f));
            CreatePrimitive(supply, "Battery 2", PrimitiveType.Cylinder, new Vector3(0.1f, 0.09f, 0f),
                new Vector3(0.07f, 0.11f, 0.07f), "MD_SupplyBlue", new Vector3(0f, 0f, 90f));
        }

        private void CreateSupplyFood(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "Nonperishable Food");
            supply.localPosition = position;
            CreatePrimitive(supply, "Food Can 1", PrimitiveType.Cylinder, new Vector3(-0.12f, 0.12f, 0f),
                new Vector3(0.11f, 0.12f, 0.11f), "MD_SupplyRed");
            CreatePrimitive(supply, "Food Can 2", PrimitiveType.Cylinder, new Vector3(0.12f, 0.12f, 0f),
                new Vector3(0.11f, 0.12f, 0.11f), "MD_SupplyYellow");
        }

        private void CreateSupplyWhistle(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "Whistle");
            supply.localPosition = position;
            CreatePrimitive(supply, "Whistle Body", PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(0.055f, 0.14f, 0.055f), "MD_SupplyYellow", new Vector3(0f, 0f, 90f));
            CreatePrimitive(supply, "Whistle Lanyard Loop", PrimitiveType.Cylinder, new Vector3(-0.17f, 0f, 0f),
                new Vector3(0.04f, 0.06f, 0.04f), "MD_Metal");
        }

        private void CreateSupplyDocuments(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "Documents in Bag");
            supply.localPosition = position;
            CreateBlock(supply, "Document Bag", new Vector3(0f, 0.2f, 0f), new Vector3(0.48f, 0.38f, 0.2f), "MD_SupplyRed");
            CreateBlock(supply, "Documents", new Vector3(0f, 0.43f, -0.02f), new Vector3(0.34f, 0.03f, 0.28f), "MD_White");
            CreateBlock(supply, "Document Page Accent", new Vector3(0.05f, 0.45f, -0.03f), new Vector3(0.17f, 0.012f, 0.02f), "MD_SupplyBlue");
        }

        private void CreateSupplyCoat(Transform parent, Vector3 position)
        {
            Transform supply = CreateContainer(parent, "Coat");
            supply.localPosition = position;
            CreateBlock(supply, "Coat Torso", Vector3.zero, new Vector3(0.72f, 0.92f, 0.22f), "MD_SupplyBlue");
            CreateBlock(supply, "Coat Left Sleeve", new Vector3(-0.42f, 0.08f, 0f), new Vector3(0.2f, 0.72f, 0.2f), "MD_Upholstery", new Vector3(0f, 0f, -18f));
            CreateBlock(supply, "Coat Right Sleeve", new Vector3(0.42f, 0.08f, 0f), new Vector3(0.2f, 0.72f, 0.2f), "MD_Upholstery", new Vector3(0f, 0f, 18f));
            CreateBlock(supply, "Coat Collar", new Vector3(0f, 0.47f, -0.02f), new Vector3(0.28f, 0.16f, 0.25f), "MD_White");
        }

        private void CreateCoatPeg(Transform parent, Vector3 position)
        {
            Transform peg = CreateContainer(parent, "Hall Coat Peg");
            peg.localPosition = position;
            CreatePrimitive(peg, "Coat Peg Base", PrimitiveType.Sphere, Vector3.zero,
                new Vector3(0.1f, 0.1f, 0.1f), "MD_Metal");
            CreatePrimitive(peg, "Coat Peg Hook", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.12f),
                new Vector3(0.04f, 0.17f, 0.04f), "MD_Metal", new Vector3(90f, 0f, 0f));
        }

        private static void AddEarthquakeProp(GameObject target, float mass, float impulseMagnitude, float torqueMagnitude,
            float maximumLinearSpeed, float maximumAngularSpeed)
        {
            LevelEarthquakeProp earthquakeProp = target.AddComponent<LevelEarthquakeProp>();
            earthquakeProp.Configure(mass, impulseMagnitude, torqueMagnitude, maximumLinearSpeed, maximumAngularSpeed);
        }

        private void AddZoneMarker(Transform parent, string name, Vector3 position, Vector3 size,
            LevelZoneKind kind, string rationale)
        {
            GameObject zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            zone.transform.localPosition = position;
            BoxCollider trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;
            LevelZoneMarker marker = zone.AddComponent<LevelZoneMarker>();
            marker.Configure(kind, rationale);
        }

        private static Transform CreateContainer(Transform parent, string name)
        {
            GameObject container = new GameObject(name);
            Transform containerTransform = container.transform;
            containerTransform.SetParent(parent, false);
            containerTransform.localPosition = Vector3.zero;
            containerTransform.localRotation = Quaternion.identity;
            containerTransform.localScale = Vector3.one;
            return containerTransform;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 size,
            string materialName, Vector3? eulerRotation = null)
        {
            return CreatePrimitive(parent, name, PrimitiveType.Cube, position, size, materialName, eulerRotation);
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType primitiveType,
            Vector3 position, Vector3 size, string materialName, Vector3? eulerRotation = null)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            Transform primitiveTransform = primitive.transform;
            primitiveTransform.SetParent(parent, false);
            primitiveTransform.localPosition = position;
            primitiveTransform.localRotation = eulerRotation.HasValue
                ? Quaternion.Euler(eulerRotation.Value)
                : Quaternion.identity;
            primitiveTransform.localScale = size;

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetMaterial(materialName);
            }
            return primitive;
        }

        private static Material GetMaterial(string materialName)
        {
            Material material;
            if (MaterialCache.TryGetValue(materialName, out material) && material != null)
            {
                return material;
            }

            material = Resources.Load<Material>(MaterialResourceFolder + materialName);
            if (material != null)
            {
                MaterialCache[materialName] = material;
                return material;
            }

            Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
            if (fallbackShader == null)
            {
                fallbackShader = Shader.Find("Standard");
            }
            if (fallbackShader == null)
            {
                return null;
            }

            material = new Material(fallbackShader);
            material.name = "Fallback - " + materialName;
            Color fallbackColor = GetFallbackColor(materialName);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", fallbackColor);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", fallbackColor);
            }
            MaterialCache[materialName] = material;
            return material;
        }

        private static Color GetFallbackColor(string materialName)
        {
            switch (materialName)
            {
                case "MD_Wall": return new Color(0.78f, 0.75f, 0.68f);
                case "MD_FloorWood": return new Color(0.43f, 0.27f, 0.14f);
                case "MD_Tile": return new Color(0.73f, 0.76f, 0.74f);
                case "MD_Wood": return new Color(0.34f, 0.19f, 0.09f);
                case "MD_Upholstery": return new Color(0.48f, 0.3f, 0.24f);
                case "MD_Ceramic": return new Color(0.83f, 0.84f, 0.81f);
                case "MD_Metal": return new Color(0.3f, 0.33f, 0.35f);
                case "MD_Glass": return new Color(0.38f, 0.72f, 0.82f, 0.55f);
                case "MD_Roof": return new Color(0.28f, 0.15f, 0.12f);
                case "MD_Safe": return new Color(0.18f, 0.62f, 0.3f);
                case "MD_Danger": return new Color(0.82f, 0.18f, 0.08f);
                case "MD_Road": return new Color(0.18f, 0.2f, 0.22f);
                case "MD_Sidewalk": return new Color(0.58f, 0.59f, 0.56f);
                case "MD_SupplyRed": return new Color(0.78f, 0.1f, 0.08f);
                case "MD_SupplyBlue": return new Color(0.08f, 0.28f, 0.76f);
                case "MD_SupplyYellow": return new Color(0.92f, 0.69f, 0.08f);
                default: return new Color(0.88f, 0.88f, 0.84f);
            }
        }
    }
}
