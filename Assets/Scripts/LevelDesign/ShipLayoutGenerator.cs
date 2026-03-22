using UnityEngine;
using UnityEngine.ProBuilder;

public class ShipLayoutGenerator : MonoBehaviour
{
    [Header("Ship Dimensions")]
    public float corridorWidth = 3f;
    public float corridorHeight = 2.8f;
    public float roomHeight = 3.2f;
    public float wallThickness = 0.15f;

    [Header("Detail Level")]
    [Range(0, 2)]
    public int detailLevel = 1;

    [Header("Material")]
    public Material prototypeMaterial;

    [Header("Procedural Generation")]
    public bool proceduralLayout = false;
    public int seed = -1; // -1 = random

    [Header("Trained Parameters")]
    public ShipLayoutTrainedParams trainedParams;

    // Last-generation statistics — written at end of each generation, read by ShipLayoutTrainer/ShipLayoutScorer
    [System.NonSerialized] public int LastRoomsPlaced;
    [System.NonSerialized] public int LastRoomsSkipped;
    [System.NonSerialized] public int LastOverlapCount;
    [System.NonSerialized] public int LastGapCount;
    [System.NonSerialized] public int LastCorridorOverlaps;
    [System.NonSerialized] public int LastZShapeCount;
    [System.NonSerialized] public int LastLShapeCount;
    [System.NonSerialized] public int LastStraightCount;
    [System.NonSerialized] public int LastBranchCount;
    [System.NonSerialized] public int LastVentCutsMade;
    [System.NonSerialized] public int LastTerminalsCapped;
    [System.NonSerialized] public int LastActualSeed;

    private float HalfCor { get { return corridorWidth / 2f; } }
    private float ventW = 0.9f;
    private float ventH = 0.7f;
    private const float kDoorWidth = 1.4f;
    private const float kDoorHeight = 2.4f;

    // Auto-retry state — _procRetries is reset to 0 at the end of each successful (or exhausted)
    // generation.  _procBaseSeed is set once on the first attempt and reused for all retries.
    private int _procRetries = 0;
    private int _procBaseSeed = 0;

    [ContextMenu("Generate Ship Layout")]
    public void GenerateShipLayout()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (proceduralLayout)
        {
            GenerateProceduralLayout();
            return;
        }

        float doorWidth = kDoorWidth;
        float doorHeight = kDoorHeight;
        float dockW = 16f, dockD = 12f, dockH = 5.5f;
        float storW = 6f, storD = 5f;
        float armoryW = 4f, armoryD = 4f;
        float cargoW = 8f, cargoD = 6f, cargoH = 4f;
        float lifeSupW = 5f, lifeSupD = 4f;
        float engW = 12f, engD = 10f, engH = 4.5f;
        float reactW = 6f, reactD = 6f;
        float labW = 6f, labD = 5f;
        float crewW = 6f, crewD = 5f;
        float secW = 4f, secD = 4f;
        float messW = 7f, messD = 5f;
        float medW = 5f, medD = 5f;
        float navW = 5f, navD = 4f;
        float bridgeW = 10f, bridgeD = 8f;

        float dockZ = 0f, dockFront = dockD / 2f;
        float corALen = 10f, corABack = dockFront;
        float corAZ = corABack + corALen / 2f, corAFront = corABack + corALen;
        float cargoBack = corAFront, cargoZ = cargoBack + cargoD / 2f, cargoFront = cargoBack + cargoD;
        float corBLen = 8f, corBBack = cargoFront;
        float corBZ = corBBack + corBLen / 2f, corBFront = corBBack + corBLen;
        float engBack = corBFront, engZ = engBack + engD / 2f, engFront = engBack + engD;

        float corCStrLen = 5f, corCStrX = -(engW / 4f);
        float corCStrBack = engFront;
        float corCStrZ = corCStrBack + corCStrLen / 2f, corCStrFront = corCStrBack + corCStrLen;
        float ccL1X = corCStrX, ccL1Z = corCStrFront + HalfCor;
        float corCsideLen = 8f;
        float corCsideRightEdge = ccL1X - HalfCor;
        float corCsideCenterX = corCsideRightEdge - corCsideLen / 2f;
        float corCsideLeftEdge = corCsideRightEdge - corCsideLen;
        float corCsideZ = ccL1Z;
        float ccL2X = corCsideLeftEdge - HalfCor, ccL2Z = corCsideZ;
        float corCfinLen = 8f, corCfinBack = ccL2Z + HalfCor, corCfinX = ccL2X;
        float corCfinZ = corCfinBack + corCfinLen / 2f, corCfinFront = corCfinBack + corCfinLen;
        float messX = corCfinX, messBackEdge = corCfinFront;
        float messZ = messBackEdge + messD / 2f;

        float corDStrLen = 5f, corDStrX = engW / 4f;
        float corDStrBack = engFront;
        float corDStrZ = corDStrBack + corDStrLen / 2f, corDStrFront = corDStrBack + corDStrLen;
        float ccR1X = corDStrX, ccR1Z = corDStrFront + HalfCor;
        float corDsideLen = 8f;
        float corDsideLeftEdge = ccR1X + HalfCor;
        float corDsideCenterX = corDsideLeftEdge + corDsideLen / 2f;
        float corDsideRightEdge = corDsideLeftEdge + corDsideLen;
        float corDsideZ = ccR1Z;
        float ccR2X = corDsideRightEdge + HalfCor, ccR2Z = corDsideZ;
        float corDfinLen = 8f, corDfinBack = ccR2Z + HalfCor, corDfinX = ccR2X;
        float corDfinZ = corDfinBack + corDfinLen / 2f, corDfinFront = corDfinBack + corDfinLen;
        float bridgeX = corDfinX, bridgeBackEdge = corDfinFront;
        float bridgeZ = bridgeBackEdge + bridgeD / 2f;

        float storX = -(HalfCor + storD / 2f);
        float armX = HalfCor + armoryD / 2f;
        float lifeSupX = -(HalfCor + lifeSupD / 2f);
        float reactX = engW / 2f + reactD / 2f;
        float labX = -(engW / 2f + labD / 2f);
        float crewX = corCfinX - HalfCor - crewD / 2f;
        float secX = corCfinX + HalfCor + secD / 2f;
        float medX = corDfinX + HalfCor + medD / 2f;
        float navX = corDfinX - HalfCor - navD / 2f;

        ShipModuleGenerator dockGen = AddRoom("DockingBay", 0, dockZ, dockW, dockH, dockD);
        DeleteChildWall(dockGen, "Wall_Front");
        CutVentInDoorWallTop(AddDoorWall("Door_Dock_Front", 0, dockFront - wallThickness / 2f, dockW, dockH).transform, dockH, kDoorHeight, roomHeight);

        ShipModuleGenerator corAGen = AddCorridor("CorridorA", 0, corAZ, corridorWidth, corridorHeight, corALen);
        ShipModuleGenerator storGen = AddRoom("StorageRoom", storX, corAZ, storW, roomHeight, storD);
        DeleteChildWall(storGen, "Wall_Right");
        DeleteChildWall(corAGen, "Wall_Left");
        CutWallForDoor(corAGen, "Wall_Left", true, corridorWidth, corridorHeight, corALen, 0f, doorWidth, doorHeight);
        AddDoorWallSide("Door_Storage", -HalfCor, corAZ, storW, roomHeight);

        ShipModuleGenerator armGen = AddRoom("Armory", armX, corAZ, armoryW, roomHeight, armoryD);
        DeleteChildWall(armGen, "Wall_Left");
        DeleteChildWall(corAGen, "Wall_Right");
        CutWallForDoor(corAGen, "Wall_Right", false, corridorWidth, corridorHeight, corALen, 0f, doorWidth, doorHeight);
        AddDoorWallSide("Door_Armory", HalfCor, corAZ, armoryW, roomHeight);

        ShipModuleGenerator cargoGen = AddRoom("CargoBay", 0, cargoZ, cargoW, cargoH, cargoD);
        DeleteChildWall(cargoGen, "Wall_Back");
        CutVentInDoorWallTop(AddDoorWall("Door_Cargo_Back", 0, cargoBack + wallThickness / 2f, cargoW, cargoH).transform, cargoH, kDoorHeight, roomHeight);
        DeleteChildWall(cargoGen, "Wall_Front");
        CutVentInDoorWallTop(AddDoorWall("Door_Cargo_Front", 0, cargoFront - wallThickness / 2f, cargoW, cargoH).transform, cargoH, kDoorHeight, roomHeight);

        ShipModuleGenerator corBGen = AddCorridor("CorridorB", 0, corBZ, corridorWidth, corridorHeight, corBLen);
        ShipModuleGenerator lifeGen = AddRoom("LifeSupport", lifeSupX, corBZ, lifeSupW, roomHeight, lifeSupD);
        DeleteChildWall(lifeGen, "Wall_Right");
        DeleteChildWall(corBGen, "Wall_Left");
        CutWallForDoor(corBGen, "Wall_Left", true, corridorWidth, corridorHeight, corBLen, 0f, doorWidth, doorHeight);
        AddDoorWallSide("Door_LifeSupport", -HalfCor, corBZ, lifeSupW, roomHeight);

        ShipModuleGenerator engGen = AddRoom("EngineeringHub", 0, engZ, engW, engH, engD);
        DeleteChildWall(engGen, "Wall_Back");
        CutVentInDoorWallTop(AddDoorWall("Door_Eng_Back", 0, engBack + wallThickness / 2f, engW, engH).transform, engH, kDoorHeight, roomHeight);
        DeleteChildWall(engGen, "Wall_Front");
        BuildEngFrontWall(engW, engH, engFront, corCStrX, corDStrX, corridorWidth, doorHeight, doorWidth);
        DeleteChildWall(engGen, "Wall_Right");
        CutVentInDoorWallTop(AddDoorWallSide("Door_Eng_Reactor", engW / 2f - wallThickness / 2f, engZ, engD, engH).transform, engH, kDoorHeight, roomHeight);
        ShipModuleGenerator reactGen = AddRoom("ReactorRoom", reactX, engZ, reactW, roomHeight, reactD);
        DeleteChildWall(reactGen, "Wall_Left");
        DeleteChildWall(engGen, "Wall_Left");
        CutVentInDoorWallTop(AddDoorWallSide("Door_Eng_Lab", -(engW / 2f) + wallThickness / 2f, engZ, engD, engH).transform, engH, kDoorHeight, roomHeight);
        ShipModuleGenerator labGen = AddRoom("ScienceLab", labX, engZ, labW, roomHeight, labD);
        DeleteChildWall(labGen, "Wall_Right");

        AddCorridor("CorC_Straight", corCStrX, corCStrZ, corridorWidth, corridorHeight, corCStrLen);
        AddCorner("CorC_Corner1", ccL1X, ccL1Z, corridorWidth, corridorHeight, corridorWidth, false, true, false, true);
        GameObject corCSideObj = new GameObject("CorC_Side");
        corCSideObj.transform.SetParent(transform);
        corCSideObj.transform.localPosition = new Vector3(corCsideCenterX, 0, corCsideZ);
        corCSideObj.transform.localRotation = Quaternion.Euler(0, 90, 0);
        ShipModuleGenerator csGen = corCSideObj.AddComponent<ShipModuleGenerator>();
        csGen.moduleType = ShipModuleGenerator.ModuleType.Corridor;
        csGen.width = corridorWidth; csGen.height = corridorHeight; csGen.depth = corCsideLen;
        csGen.wallThickness = wallThickness; csGen.detailLevel = detailLevel;
        csGen.overrideMaterial = prototypeMaterial; csGen.Generate();
        AddCorner("CorC_Corner2", ccL2X, ccL2Z, corridorWidth, corridorHeight, corridorWidth, true, false, true, false);

        ShipModuleGenerator corCfinGen = AddCorridor("CorC_Final", corCfinX, corCfinZ, corridorWidth, corridorHeight, corCfinLen);
        ShipModuleGenerator crewGen = AddRoom("CrewQuarters", crewX, corCfinZ, crewW, roomHeight, crewD);
        DeleteChildWall(crewGen, "Wall_Right");
        DeleteChildWall(corCfinGen, "Wall_Left");
        CutWallForDoor(corCfinGen, "Wall_Left", true, corridorWidth, corridorHeight, corCfinLen, 0f, doorWidth, doorHeight);
        AddDoorWallSide("Door_Crew", corCfinX - HalfCor, corCfinZ, crewW, roomHeight);
        ShipModuleGenerator secGen = AddRoom("SecurityOffice", secX, corCfinZ, secW, roomHeight, secD);
        DeleteChildWall(secGen, "Wall_Left");
        DeleteChildWall(corCfinGen, "Wall_Right");
        CutWallForDoor(corCfinGen, "Wall_Right", false, corridorWidth, corridorHeight, corCfinLen, 0f, doorWidth, doorHeight);
        AddDoorWallSide("Door_Security", corCfinX + HalfCor, corCfinZ, secW, roomHeight);
        ShipModuleGenerator messGen = AddRoom("MessHall", messX, messZ, messW, roomHeight, messD);
        DeleteChildWall(messGen, "Wall_Back");
        AddDoorWall("Door_Mess_Back", messX, messBackEdge + wallThickness / 2f, messW, roomHeight);

        AddCorridor("CorD_Straight", corDStrX, corDStrZ, corridorWidth, corridorHeight, corDStrLen);
        AddCorner("CorD_Corner1", ccR1X, ccR1Z, corridorWidth, corridorHeight, corridorWidth, false, true, true, false);
        GameObject corDSideObj = new GameObject("CorD_Side");
        corDSideObj.transform.SetParent(transform);
        corDSideObj.transform.localPosition = new Vector3(corDsideCenterX, 0, corDsideZ);
        corDSideObj.transform.localRotation = Quaternion.Euler(0, 90, 0);
        ShipModuleGenerator dsGen = corDSideObj.AddComponent<ShipModuleGenerator>();
        dsGen.moduleType = ShipModuleGenerator.ModuleType.Corridor;
        dsGen.width = corridorWidth; dsGen.height = corridorHeight; dsGen.depth = corDsideLen;
        dsGen.wallThickness = wallThickness; dsGen.detailLevel = detailLevel;
        dsGen.overrideMaterial = prototypeMaterial; dsGen.Generate();
        AddCorner("CorD_Corner2", ccR2X, ccR2Z, corridorWidth, corridorHeight, corridorWidth, true, false, false, true);

        ShipModuleGenerator corDfinGen = AddCorridor("CorD_Final", corDfinX, corDfinZ, corridorWidth, corridorHeight, corDfinLen);
        ShipModuleGenerator medGen = AddRoom("MedBay", medX, corDfinZ, medW, roomHeight, medD);
        DeleteChildWall(medGen, "Wall_Left");
        DeleteChildWall(corDfinGen, "Wall_Right");
        CutWallForDoor(corDfinGen, "Wall_Right", false, corridorWidth, corridorHeight, corDfinLen, 0f, doorWidth, doorHeight);
        AddDoorWallSide("Door_MedBay", corDfinX + HalfCor, corDfinZ, medW, roomHeight);
        ShipModuleGenerator navGen = AddRoom("NavigationRoom", navX, corDfinZ, navW, roomHeight, navD);
        DeleteChildWall(navGen, "Wall_Right");
        DeleteChildWall(corDfinGen, "Wall_Left");
        CutWallForDoor(corDfinGen, "Wall_Left", true, corridorWidth, corridorHeight, corDfinLen, 0f, doorWidth, doorHeight);
        AddDoorWallSide("Door_Navigation", corDfinX - HalfCor, corDfinZ, navW, roomHeight);
        ShipModuleGenerator bridgeGen = AddRoom("Bridge", bridgeX, bridgeZ, bridgeW, roomHeight, bridgeD);
        DeleteChildWall(bridgeGen, "Wall_Back");
        AddDoorWall("Door_Bridge_Back", bridgeX, bridgeBackEdge + wallThickness / 2f, bridgeW, roomHeight);

        float vY = roomHeight;
        float hvW = ventW / 2f;
        float dropW = ventW * 0.5f;
        float dropH = ventH + wallThickness + 0.15f;
        float dropY = vY - dropH / 2f;

        AddVentCross("VJ_CorA", 0, vY, corAZ);
        AddVentTee("VJ_CorB", 0, vY, corBZ, false, false, false, true);
        AddVentCross("VJ_EngCenter", 0, vY, engZ);
        AddVentTee("VJ_EngFront", 0, vY, engFront, false, true, false, false);

        ConnectVent("VS_DockToCorA", 0, vY, dockZ - dockD / 2f + 1f, 0, vY, dockZ - hvW);
        AddVentCap("VentCap_Dock", 0, vY, dockZ - dockD / 2f + 1f);
        AddVentElbow("VElbow_Dock", 0, vY, dockZ, false, false, true, true);
        ConnectVent("VS_DockToCorA_2", 0, vY, dockZ + hvW, 0, vY, corAZ - hvW);
        ConnectVent("VS_CorAToCargo", 0, vY, corAZ + hvW, 0, vY, cargoFront);
        ConnectVent("VS_CargoToCorB", 0, vY, cargoFront, 0, vY, corBZ - hvW);
        ConnectVent("VS_CorBToEng", 0, vY, corBZ + hvW, 0, vY, engZ - hvW);
        ConnectVent("VS_EngToFront", 0, vY, engZ + hvW, 0, vY, engFront - hvW);

        // ── Branch vents + elbows + vertical drops ──────────────────
        // Each horizontal branch is shortened by hvW so the elbow fits
        // flush at the junction point.  The elbow (VentElbow) has its
        // open side facing the incoming branch and its open bottom
        // connecting to the vertical drop below.

        ConnectVent("VB_Stor", -hvW, vY, corAZ, storX + hvW, vY, corAZ);
        AddVentElbow("VElbow_Stor", storX, vY, corAZ, true, true, true, false);
        AddVentVertical("VDrop_Stor", storX, dropY, corAZ, dropW, dropH, dropW);
        CutCeilingForVent(storGen, dropW, dropW);
        ConnectVent("VB_Arm", hvW, vY, corAZ, armX - hvW, vY, corAZ);
        AddVentElbow("VElbow_Arm", armX, vY, corAZ, true, true, false, true);
        AddVentVertical("VDrop_Arm", armX, dropY, corAZ, dropW, dropH, dropW);
        CutCeilingForVent(armGen, dropW, dropW);
        ConnectVent("VB_Life", -hvW, vY, corBZ, lifeSupX + hvW, vY, corBZ);
        AddVentElbow("VElbow_Life", lifeSupX, vY, corBZ, true, true, true, false);
        AddVentVertical("VDrop_Life", lifeSupX, dropY, corBZ, dropW, dropH, dropW);
        CutCeilingForVent(lifeGen, dropW, dropW);
        ConnectVent("VB_React", hvW, vY, engZ, reactX - hvW, vY, engZ);
        AddVentElbow("VElbow_React", reactX, vY, engZ, true, true, false, true);
        AddVentVertical("VDrop_React", reactX, dropY, engZ, dropW, dropH, dropW);
        CutCeilingForVent(reactGen, dropW, dropW);
        ConnectVent("VB_Lab", -hvW, vY, engZ, labX + hvW, vY, engZ);
        AddVentElbow("VElbow_Lab", labX, vY, engZ, true, true, true, false);
        AddVentVertical("VDrop_Lab", labX, dropY, engZ, dropW, dropH, dropW);
        CutCeilingForVent(labGen, dropW, dropW);
        AddVentVertical("VDrop_Dock", 0, dropY, dockZ, dropW, dropH, dropW);
        CutCeilingForVent(dockGen, dropW, dropW);

        ConnectVent("VL_EngToStr", -hvW, vY, engFront, corCStrX + hvW, vY, engFront);
        AddVentTee("VJ_CorCStart", corCStrX, vY, corCStrBack, true, false, true, false);
        ConnectVent("VL_CorCStr", corCStrX, vY, corCStrBack + hvW, corCStrX, vY, ccL1Z - hvW);
        AddVentCorner("VJ_L1", ccL1X, vY, ccL1Z, false, true, false, true);
        ConnectVent("VL_Side", ccL1X - hvW, vY, ccL1Z, ccL2X + hvW, vY, ccL2Z);
        AddVentCorner("VJ_L2", ccL2X, vY, ccL2Z, true, false, true, false);
        ConnectVent("VL_ToJunc", ccL2X, vY, ccL2Z + hvW, corCfinX, vY, corCfinZ - hvW);
        AddVentCross("VJ_CorCFin", corCfinX, vY, corCfinZ);
        ConnectVent("VL_ToMess_A", corCfinX, vY, corCfinZ + hvW, messX, vY, messZ - hvW);
        AddVentElbow("VElbow_Mess", messX, vY, messZ, false, false, true, true);
        ConnectVent("VL_ToMess_B", messX, vY, messZ + hvW, messX, vY, messBackEdge + messD - 1f);
        AddVentCap("VentCap_Mess", messX, vY, messBackEdge + messD - 1f);
        ConnectVent("VB_Crew", corCfinX - hvW, vY, corCfinZ, crewX + hvW, vY, corCfinZ);
        AddVentElbow("VElbow_Crew", crewX, vY, corCfinZ, true, true, true, false);
        AddVentVertical("VDrop_Crew", crewX, dropY, corCfinZ, dropW, dropH, dropW);
        CutCeilingForVent(crewGen, dropW, dropW);
        ConnectVent("VB_Sec", corCfinX + hvW, vY, corCfinZ, secX - hvW, vY, corCfinZ);
        AddVentElbow("VElbow_Sec", secX, vY, corCfinZ, true, true, false, true);
        AddVentVertical("VDrop_Sec", secX, dropY, corCfinZ, dropW, dropH, dropW);
        CutCeilingForVent(secGen, dropW, dropW);
        AddVentVertical("VDrop_Mess", messX, dropY, messZ, dropW, dropH, dropW);
        CutCeilingForVent(messGen, dropW, dropW);

        ConnectVent("VR_EngToStr", hvW, vY, engFront, corDStrX - hvW, vY, engFront);
        AddVentTee("VJ_CorDStart", corDStrX, vY, corDStrBack, true, false, false, true);
        ConnectVent("VR_CorDStr", corDStrX, vY, corDStrBack + hvW, corDStrX, vY, ccR1Z - hvW);
        AddVentCorner("VJ_R1", ccR1X, vY, ccR1Z, false, true, true, false);
        ConnectVent("VR_Side", ccR1X + hvW, vY, ccR1Z, ccR2X - hvW, vY, ccR2Z);
        AddVentCorner("VJ_R2", ccR2X, vY, ccR2Z, true, false, false, true);
        ConnectVent("VR_ToJunc", ccR2X, vY, ccR2Z + hvW, corDfinX, vY, corDfinZ - hvW);
        AddVentCross("VJ_CorDFin", corDfinX, vY, corDfinZ);
        ConnectVent("VR_ToBridge_A", corDfinX, vY, corDfinZ + hvW, bridgeX, vY, bridgeZ - hvW);
        AddVentElbow("VElbow_Bridge", bridgeX, vY, bridgeZ, false, false, true, true);
        ConnectVent("VR_ToBridge_B", bridgeX, vY, bridgeZ + hvW, bridgeX, vY, bridgeBackEdge + bridgeD - 1f);
        AddVentCap("VentCap_Bridge", bridgeX, vY, bridgeBackEdge + bridgeD - 1f);
        ConnectVent("VB_Med", corDfinX + hvW, vY, corDfinZ, medX - hvW, vY, corDfinZ);
        AddVentElbow("VElbow_Med", medX, vY, corDfinZ, true, true, false, true);
        AddVentVertical("VDrop_Med", medX, dropY, corDfinZ, dropW, dropH, dropW);
        CutCeilingForVent(medGen, dropW, dropW);
        ConnectVent("VB_Nav", corDfinX - hvW, vY, corDfinZ, navX + hvW, vY, corDfinZ);
        AddVentElbow("VElbow_Nav", navX, vY, corDfinZ, true, true, true, false);
        AddVentVertical("VDrop_Nav", navX, dropY, corDfinZ, dropW, dropH, dropW);
        CutCeilingForVent(navGen, dropW, dropW);
        AddVentVertical("VDrop_Bridge", bridgeX, dropY, bridgeZ, dropW, dropH, dropW);
        CutCeilingForVent(bridgeGen, dropW, dropW);

        AddProp("DockCrate_1", -5f, 0, dockZ - 3f, ShipModuleGenerator.ModuleType.Crate, 1.2f, 0.8f, 1f);
        AddProp("DockCrate_2", -5.5f, 0, dockZ - 1f, ShipModuleGenerator.ModuleType.Crate, 0.9f, 1f, 0.9f);
        AddProp("DockCrate_3", -4.5f, 0.8f, dockZ - 3f, ShipModuleGenerator.ModuleType.Crate, 0.6f, 0.5f, 0.6f);
        AddProp("DockCrate_4", 5f, 0, dockZ + 1f, ShipModuleGenerator.ModuleType.Crate, 1f, 0.7f, 1.2f);
        AddProp("DockCrate_5", 6f, 0, dockZ - 2f, ShipModuleGenerator.ModuleType.Crate, 1.3f, 0.9f, 1.1f);
        AddProp("StorCrate_1", storX - 1f, 0, corAZ - 1f, ShipModuleGenerator.ModuleType.Crate, 0.8f, 0.6f, 0.8f);
        AddProp("StorCrate_2", storX + 0.5f, 0, corAZ + 1f, ShipModuleGenerator.ModuleType.Crate, 1f, 0.9f, 0.7f);
        AddProp("StorCrate_3", storX - 1.5f, 0, corAZ, ShipModuleGenerator.ModuleType.Crate, 1.2f, 1.1f, 1f);
        AddProp("ArmoryConsole", armX, 0, corAZ, ShipModuleGenerator.ModuleType.Console, 1f, 0.9f, 0.4f);
        AddProp("CargoCrate_1", -2f, 0, cargoZ - 1.5f, ShipModuleGenerator.ModuleType.Crate, 1.5f, 1.2f, 1.2f);
        AddProp("CargoCrate_2", -2.5f, 0, cargoZ + 1f, ShipModuleGenerator.ModuleType.Crate, 1f, 0.8f, 1f);
        AddProp("CargoCrate_3", 2f, 0, cargoZ, ShipModuleGenerator.ModuleType.Crate, 1.3f, 1f, 1.1f);
        AddProp("LifeConsole", lifeSupX, 0, corBZ, ShipModuleGenerator.ModuleType.Console, 1.2f, 1f, 0.5f);
        AddProp("Console_Eng_L", -3f, 0, engZ - 2f, ShipModuleGenerator.ModuleType.Console, 1.5f, 1f, 0.5f);
        AddProp("Console_Eng_R", 3f, 0, engZ - 2f, ShipModuleGenerator.ModuleType.Console, 1.5f, 1f, 0.5f);
        AddProp("Console_Eng_C", 0f, 0, engZ + 1f, ShipModuleGenerator.ModuleType.Console, 2f, 1.1f, 0.6f);
        AddProp("Pillar_E1", -4f, 0, engZ - 3f, ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Pillar_E2", 4f, 0, engZ - 3f, ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Pillar_E3", -4f, 0, engZ + 3f, ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Pillar_E4", 4f, 0, engZ + 3f, ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("ReactorConsole", reactX, 0, engZ, ShipModuleGenerator.ModuleType.Console, 1.2f, 1.2f, 0.5f);
        AddProp("ReactorPillar", reactX, 0, engZ - 1.5f, ShipModuleGenerator.ModuleType.Pillar, 0.5f, roomHeight, 0.5f);
        AddProp("LabConsole_1", labX - 1f, 0, engZ - 1f, ShipModuleGenerator.ModuleType.Console, 1.5f, 1f, 0.5f);
        AddProp("LabConsole_2", labX + 1f, 0, engZ + 1f, ShipModuleGenerator.ModuleType.Console, 1.5f, 1f, 0.5f);
        AddProp("CrewConsole", crewX, 0, corCfinZ, ShipModuleGenerator.ModuleType.Console, 1f, 0.8f, 0.4f);
        AddProp("SecConsole", secX, 0, corCfinZ, ShipModuleGenerator.ModuleType.Console, 1.2f, 0.9f, 0.4f);
        AddProp("MessTable_1", messX - 1.5f, 0, messZ, ShipModuleGenerator.ModuleType.Crate, 2f, 0.75f, 1f);
        AddProp("MessTable_2", messX + 1.5f, 0, messZ, ShipModuleGenerator.ModuleType.Crate, 2f, 0.75f, 1f);
        AddProp("MedConsole", medX, 0, corDfinZ - 0.5f, ShipModuleGenerator.ModuleType.Console, 1.2f, 1f, 0.5f);
        AddProp("MedBed", medX, 0, corDfinZ + 1f, ShipModuleGenerator.ModuleType.Crate, 2f, 0.6f, 0.9f);
        AddProp("NavConsole", navX, 0, corDfinZ, ShipModuleGenerator.ModuleType.Console, 1.5f, 1f, 0.5f);
        AddProp("Console_Bridge_Main", bridgeX, 0, bridgeZ + 1.5f, ShipModuleGenerator.ModuleType.Console, 3f, 1f, 0.7f);
        AddProp("Console_Bridge_L", bridgeX - 2.5f, 0, bridgeZ, ShipModuleGenerator.ModuleType.Console, 1.5f, 1f, 0.5f);
        AddProp("Console_Bridge_R", bridgeX + 2.5f, 0, bridgeZ, ShipModuleGenerator.ModuleType.Console, 1.5f, 1f, 0.5f);
        AddProp("BridgePillar_L", bridgeX - 3.5f, 0, bridgeZ - 1f, ShipModuleGenerator.ModuleType.Pillar, 0.3f, roomHeight, 0.3f);
        AddProp("BridgePillar_R", bridgeX + 3.5f, 0, bridgeZ - 1f, ShipModuleGenerator.ModuleType.Pillar, 0.3f, roomHeight, 0.3f);

        Debug.Log("Ship generated! Place player at (0, 1.5, " + (dockZ - dockD / 2f + 1.5f) + ")");
    }

    private void BuildEngFrontWall(float engW, float engH, float engFrontZ, float leftDoorX, float rightDoorX, float doorOpeningW, float doorH, float doorW)
    {
        float wallZ = engFrontZ - wallThickness / 2f;
        float halfEngW = engW / 2f;
        float halfOpening = doorOpeningW / 2f;
        float trim = 0.06f;

        float lw = (halfEngW + leftDoorX) - halfOpening;
        if (lw > 0.01f)
            MakeBoxOnParent(transform, "EngFW_L", new Vector3(-halfEngW + lw / 2f, engH / 2f, wallZ), lw, engH, wallThickness);

        float cLeft = leftDoorX + halfOpening;
        float cRight = rightDoorX - halfOpening;
        float cw = cRight - cLeft;
        if (cw > 0.01f)
        {
            // EngFW_C is fully covered by VJ_EngFront + the starts of VL/VR_EngToStr;
            // split into below-vent and above-vent sections, omitting the vent band.
            float cx = cLeft + cw / 2f;
            float vBot = roomHeight;
            float vTop = roomHeight + ventH;
            float cBotH = vBot;
            float cTopH = engH - vTop;
            if (cBotH > 0.01f)
                MakeBoxOnParent(transform, "EngFW_C_Bot", new Vector3(cx, cBotH / 2f, wallZ), cw, cBotH, wallThickness);
            if (cTopH > 0.01f)
                MakeBoxOnParent(transform, "EngFW_C_Top", new Vector3(cx, vTop + cTopH / 2f, wallZ), cw, cTopH, wallThickness);
        }

        float rw = halfEngW - (rightDoorX + halfOpening);
        if (rw > 0.01f)
            MakeBoxOnParent(transform, "EngFW_R", new Vector3(halfEngW - rw / 2f, engH / 2f, wallZ), rw, engH, wallThickness);

        float topH = engH - doorH;
        if (topH > 0.01f)
        {
            float hvW = ventW / 2f;
            float holeW = halfOpening + hvW;
            float vBot = roomHeight;
            float vTop = roomHeight + ventH;
            float belowH = vBot - doorH;
            float aboveH = engH - vTop;

            // EngFW_LT: solid far-left section (no vent/junction) + Bot/Top of vent+junction section
            float ltSolidW = halfOpening - hvW;
            float ltSolidCX = leftDoorX - halfOpening + ltSolidW / 2f;
            float ltHoleCX = leftDoorX + (halfOpening - hvW) / 2f;
            if (ltSolidW > 0.01f)
                MakeBoxOnParent(transform, "EngFW_LT_L", new Vector3(ltSolidCX, doorH + topH / 2f, wallZ), ltSolidW, topH, wallThickness);
            if (holeW > 0.01f)
            {
                if (belowH > 0.01f)
                    MakeBoxOnParent(transform, "EngFW_LT_Bot", new Vector3(ltHoleCX, doorH + belowH / 2f, wallZ), holeW, belowH, wallThickness);
                if (aboveH > 0.01f)
                    MakeBoxOnParent(transform, "EngFW_LT_Top", new Vector3(ltHoleCX, vTop + aboveH / 2f, wallZ), holeW, aboveH, wallThickness);
            }

            // EngFW_RT: solid far-right section (no vent/junction) + Bot/Top of vent+junction section
            float rtSolidW = halfOpening - hvW;
            float rtSolidCX = rightDoorX + halfOpening - rtSolidW / 2f;
            float rtHoleCX = rightDoorX - (halfOpening - hvW) / 2f;
            if (rtSolidW > 0.01f)
                MakeBoxOnParent(transform, "EngFW_RT_R", new Vector3(rtSolidCX, doorH + topH / 2f, wallZ), rtSolidW, topH, wallThickness);
            if (holeW > 0.01f)
            {
                if (belowH > 0.01f)
                    MakeBoxOnParent(transform, "EngFW_RT_Bot", new Vector3(rtHoleCX, doorH + belowH / 2f, wallZ), holeW, belowH, wallThickness);
                if (aboveH > 0.01f)
                    MakeBoxOnParent(transform, "EngFW_RT_Top", new Vector3(rtHoleCX, vTop + aboveH / 2f, wallZ), holeW, aboveH, wallThickness);
            }
        }

        if (detailLevel >= 1)
        {
            float tz = wallZ + wallThickness / 2f + trim / 2f;
            MakeBoxOnParent(transform, "EngTr_LL", new Vector3(leftDoorX - halfOpening - trim / 2f, doorH / 2f, tz), trim, doorH, trim);
            MakeBoxOnParent(transform, "EngTr_LR", new Vector3(leftDoorX + halfOpening + trim / 2f, doorH / 2f, tz), trim, doorH, trim);
            MakeBoxOnParent(transform, "EngTr_LT", new Vector3(leftDoorX, doorH + trim / 2f, tz), doorOpeningW + trim * 2f, trim, trim);
            MakeBoxOnParent(transform, "EngTr_RL", new Vector3(rightDoorX - halfOpening - trim / 2f, doorH / 2f, tz), trim, doorH, trim);
            MakeBoxOnParent(transform, "EngTr_RR", new Vector3(rightDoorX + halfOpening + trim / 2f, doorH / 2f, tz), trim, doorH, trim);
            MakeBoxOnParent(transform, "EngTr_RT", new Vector3(rightDoorX, doorH + trim / 2f, tz), doorOpeningW + trim * 2f, trim, trim);
        }
    }

    private void ConnectVent(string name, float x1, float y1, float z1, float x2, float y2, float z2)
    {
        float cx = (x1 + x2) / 2f, cy = (y1 + y2) / 2f, cz = (z1 + z2) / 2f;
        float dx = x2 - x1, dz = z2 - z1;
        float length = Mathf.Sqrt(dx * dx + dz * dz);
        if (length < 0.01f) return;
        float angle = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(cx, cy, cz);
        obj.transform.localRotation = Quaternion.Euler(0, angle, 0);
        ShipModuleGenerator gen = obj.AddComponent<ShipModuleGenerator>();
        gen.moduleType = ShipModuleGenerator.ModuleType.VentShaft;
        gen.width = ventW; gen.height = ventH; gen.depth = length;
        gen.wallThickness = wallThickness; gen.detailLevel = detailLevel;
        gen.overrideMaterial = prototypeMaterial; gen.Generate();
    }

    private void AddVentVertical(string name, float x, float y, float z, float w, float h, float d)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        // After Quaternion.Euler(90,0,0) the shaft's local Y axis maps to World +Z.
        // The shaft is bottom-anchored (local Y spans 0..w), so without correction it
        // would extend from z to z+w.  Subtract w/2 to centre the cross-section at z.
        obj.transform.localPosition = new Vector3(x, y, z - w / 2f);
        obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        ShipModuleGenerator gen = obj.AddComponent<ShipModuleGenerator>();
        gen.moduleType = ShipModuleGenerator.ModuleType.VentShaft;
        // After 90° X rotation the shaft's depth axis becomes vertical; keep cross-section square (w×w).
        gen.width = w; gen.height = w; gen.depth = h;
        gen.wallThickness = wallThickness; gen.detailLevel = detailLevel;
        gen.overrideMaterial = prototypeMaterial; gen.Generate();
    }

    private void AddVentCorner(string name, float x, float y, float z, bool wB, bool wF, bool wL, bool wR)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(x, y, z);
        ShipModuleGenerator gen = obj.AddComponent<ShipModuleGenerator>();
        gen.moduleType = ShipModuleGenerator.ModuleType.VentCorner;
        gen.width = ventW; gen.height = ventH; gen.depth = ventW;
        gen.cornerWallBack = wB; gen.cornerWallFront = wF;
        gen.cornerWallLeft = wL; gen.cornerWallRight = wR;
        gen.wallThickness = wallThickness; gen.detailLevel = detailLevel;
        gen.overrideMaterial = prototypeMaterial; gen.Generate();
    }

    private void AddVentTee(string name, float x, float y, float z, bool wB, bool wF, bool wL, bool wR)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(x, y, z);
        ShipModuleGenerator gen = obj.AddComponent<ShipModuleGenerator>();
        gen.moduleType = ShipModuleGenerator.ModuleType.VentTee;
        gen.width = ventW; gen.height = ventH; gen.depth = ventW;
        gen.cornerWallBack = wB; gen.cornerWallFront = wF;
        gen.cornerWallLeft = wL; gen.cornerWallRight = wR;
        gen.wallThickness = wallThickness; gen.detailLevel = detailLevel;
        gen.overrideMaterial = prototypeMaterial; gen.Generate();
    }

    // ─────────────────────────────────────────────────────────────
    //  Vent elbow — placed at the junction where a horizontal branch
    //  meets a vertical drop.  Uses VentElbow (top + selective walls,
    //  no bottom) so the drop can pass through the open bottom.
    //
    //  wB/wF/wL/wR — whether Back/Front/Left/Right walls are present.
    //  Set the wall facing the incoming horizontal branch to false
    //  (open).  For trunk-to-drop (trunk runs ±Z) set wL=wR=true,
    //  wB=wF=false.
    // ─────────────────────────────────────────────────────────────
    private void AddVentElbow(string name, float x, float y, float z, bool wB, bool wF, bool wL, bool wR)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(x, y, z);
        ShipModuleGenerator gen = obj.AddComponent<ShipModuleGenerator>();
        gen.moduleType = ShipModuleGenerator.ModuleType.VentElbow;
        gen.width = ventW; gen.height = ventH; gen.depth = ventW;
        gen.cornerWallBack = wB; gen.cornerWallFront = wF;
        gen.cornerWallLeft = wL; gen.cornerWallRight = wR;
        gen.wallThickness = wallThickness; gen.detailLevel = detailLevel;
        gen.overrideMaterial = prototypeMaterial; gen.Generate();
    }

    // ─────────────────────────────────────────────────────────────
    //  Vent cap — a thin wall that seals a dead-end vent opening.
    //  vY is the vent bottom; the cap is centred at vY + ventH/2
    //  to align with the shaft cross-section (which is bottom-anchored).
    // ─────────────────────────────────────────────────────────────
    private void AddVentCap(string name, float x, float vY, float z)
    {
        MakeBoxOnParent(transform, name, new Vector3(x, vY + ventH / 2f, z), ventW, ventH, wallThickness);
    }

    private void AddVentCross(string name, float x, float y, float z)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(x, y, z);
        ShipModuleGenerator gen = obj.AddComponent<ShipModuleGenerator>();
        gen.moduleType = ShipModuleGenerator.ModuleType.VentCross;
        gen.width = ventW; gen.height = ventH; gen.depth = ventW;
        gen.wallThickness = wallThickness; gen.detailLevel = detailLevel;
        gen.overrideMaterial = prototypeMaterial; gen.Generate();
    }

    private void CutWallForDoor(ShipModuleGenerator parentGen, string wallName, bool isLeftWall, float corWidth, float corHeight, float corDepth, float doorOffsetZ, float doorW, float doorH)
    {
        float hw = corWidth / 2f;
        float hd = corDepth / 2f;
        float wallX = isLeftWall ? -hw + wallThickness / 2f : hw - wallThickness / 2f;
        float backSegLen = (hd + doorOffsetZ) - doorW / 2f;
        if (backSegLen > 0.01f)
            MakeBoxOnParent(parentGen.transform, wallName + "_Back", new Vector3(wallX, corHeight / 2f, -hd + backSegLen / 2f), wallThickness, corHeight, backSegLen);
        float frontSegLen = (hd - doorOffsetZ) - doorW / 2f;
        if (frontSegLen > 0.01f)
            MakeBoxOnParent(parentGen.transform, wallName + "_Front", new Vector3(wallX, corHeight / 2f, hd - frontSegLen / 2f), wallThickness, corHeight, frontSegLen);
        float topSegH = corHeight - doorH;
        if (topSegH > 0.01f)
            MakeBoxOnParent(parentGen.transform, wallName + "_Top", new Vector3(wallX, doorH + topSegH / 2f, doorOffsetZ), wallThickness, topSegH, doorW);
    }

    private void MakeBoxOnParent(Transform parent, string objName, Vector3 pos, float w, float h, float d)
    {
        ProBuilderMesh pb = ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(w, h, d));
        pb.gameObject.name = objName;
        pb.transform.SetParent(parent);
        pb.transform.localPosition = pos;
        pb.transform.localRotation = Quaternion.identity;
        pb.ToMesh(); pb.Refresh();
        if (prototypeMaterial != null)
        {
            MeshRenderer rend = pb.GetComponent<MeshRenderer>();
            if (rend != null) rend.sharedMaterial = prototypeMaterial;
        }
        if (pb.GetComponent<Collider>() == null)
            pb.gameObject.AddComponent<BoxCollider>();
    }

    private void DeleteChildWall(ShipModuleGenerator gen, string wallName)
    {
        if (gen == null) return;
        Transform room = gen.transform;
        for (int i = room.childCount - 1; i >= 0; i--)
        {
            if (room.GetChild(i).name == wallName)
            {
                if (Application.isPlaying) Destroy(room.GetChild(i).gameObject);
                else DestroyImmediate(room.GetChild(i).gameObject);
                return;
            }
        }
    }

    private ShipModuleGenerator AddRoom(string n, float x, float z, float w, float h, float d)
    {
        GameObject o = new GameObject(n); o.transform.SetParent(transform);
        o.transform.localPosition = new Vector3(x, 0, z);
        ShipModuleGenerator g = o.AddComponent<ShipModuleGenerator>();
        g.moduleType = ShipModuleGenerator.ModuleType.Room;
        g.width = w; g.height = h; g.depth = d;
        g.wallThickness = wallThickness; g.detailLevel = detailLevel;
        g.overrideMaterial = prototypeMaterial; g.Generate(); return g;
    }

    private ShipModuleGenerator AddCorridor(string n, float x, float z, float w, float h, float d)
    {
        GameObject o = new GameObject(n); o.transform.SetParent(transform);
        o.transform.localPosition = new Vector3(x, 0, z);
        ShipModuleGenerator g = o.AddComponent<ShipModuleGenerator>();
        g.moduleType = ShipModuleGenerator.ModuleType.Corridor;
        g.width = w; g.height = h; g.depth = d;
        g.wallThickness = wallThickness; g.detailLevel = detailLevel;
        g.overrideMaterial = prototypeMaterial; g.Generate(); return g;
    }

    private void AddCorner(string n, float x, float z, float w, float h, float d, bool wB, bool wF, bool wL, bool wR)
    {
        GameObject o = new GameObject(n); o.transform.SetParent(transform);
        o.transform.localPosition = new Vector3(x, 0, z);
        ShipModuleGenerator g = o.AddComponent<ShipModuleGenerator>();
        g.moduleType = ShipModuleGenerator.ModuleType.CornerPiece;
        g.width = w; g.height = h; g.depth = d; g.wallThickness = wallThickness;
        g.cornerWallBack = wB; g.cornerWallFront = wF;
        g.cornerWallLeft = wL; g.cornerWallRight = wR;
        g.detailLevel = detailLevel; g.overrideMaterial = prototypeMaterial; g.Generate();
    }

    private ShipModuleGenerator AddDoorWall(string n, float x, float z, float w, float h)
    {
        GameObject o = new GameObject(n); o.transform.SetParent(transform);
        o.transform.localPosition = new Vector3(x, 0, z);
        ShipModuleGenerator g = o.AddComponent<ShipModuleGenerator>();
        g.moduleType = ShipModuleGenerator.ModuleType.DoorWall;
        g.width = w; g.height = h; g.doorWidth = kDoorWidth; g.doorHeight = kDoorHeight;
        g.wallThickness = wallThickness; g.detailLevel = detailLevel;
        g.overrideMaterial = prototypeMaterial; g.Generate();
        return g;
    }

    private ShipModuleGenerator AddDoorWallSide(string n, float x, float z, float w, float h)
    {
        GameObject o = new GameObject(n); o.transform.SetParent(transform);
        o.transform.localPosition = new Vector3(x, 0, z);
        o.transform.localRotation = Quaternion.Euler(0, 90, 0);
        ShipModuleGenerator g = o.AddComponent<ShipModuleGenerator>();
        g.moduleType = ShipModuleGenerator.ModuleType.DoorWall;
        g.width = w; g.height = h; g.doorWidth = kDoorWidth; g.doorHeight = kDoorHeight;
        g.wallThickness = wallThickness; g.detailLevel = detailLevel;
        g.overrideMaterial = prototypeMaterial; g.Generate();
        return g;
    }

    private void AddProp(string n, float x, float y, float z, ShipModuleGenerator.ModuleType t, float w, float h, float d)
    {
        GameObject o = new GameObject(n); o.transform.SetParent(transform);
        o.transform.localPosition = new Vector3(x, y, z);
        ShipModuleGenerator g = o.AddComponent<ShipModuleGenerator>();
        g.moduleType = t; g.width = w; g.height = h; g.depth = d;
        g.wallThickness = wallThickness; g.detailLevel = detailLevel;
        g.overrideMaterial = prototypeMaterial; g.Generate();
    }

    // ═══════════════════════════════════════════════════════════════
    //  CUT CEILING FOR VENT — replaces the solid ceiling slab of a
    //  room with 4 segments that leave a rectangular opening at the
    //  room's local XZ origin (where the vent drop descends).
    // ═══════════════════════════════════════════════════════════════

    private void CutCeilingForVent(ShipModuleGenerator roomGen, float holeW, float holeD)
    {
        if (roomGen == null)
        {
            Debug.LogWarning("[ProcGen] CutCeilingForVent: null roomGen — skipping ceiling cut.");
            return;
        }
        DeleteChildWall(roomGen, "Ceiling");

        float w = roomGen.width;
        float d = roomGen.depth;
        float h = roomGen.height;
        float t = wallThickness;
        float ceilY = h - t / 2f;
        float hw = holeW / 2f;
        float hd = holeD / 2f;

        float leftW = w / 2f - hw;
        if (leftW > 0.01f)
            MakeBoxOnParent(roomGen.transform, "Ceiling_L",
                new Vector3(-(hw + leftW / 2f), ceilY, 0), leftW, t, d);

        float rightW = w / 2f - hw;
        if (rightW > 0.01f)
            MakeBoxOnParent(roomGen.transform, "Ceiling_R",
                new Vector3(hw + rightW / 2f, ceilY, 0), rightW, t, d);

        float frontD = d / 2f - hd;
        if (frontD > 0.01f)
            MakeBoxOnParent(roomGen.transform, "Ceiling_F",
                new Vector3(0, ceilY, hd + frontD / 2f), holeW, t, frontD);

        float backD = d / 2f - hd;
        if (backD > 0.01f)
            MakeBoxOnParent(roomGen.transform, "Ceiling_B",
                new Vector3(0, ceilY, -(hd + backD / 2f)), holeW, t, backD);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CUT VENT IN DOOR WALL TOP — removes the solid DoorWall_Top
    //  section from a tall door wall and replaces it with pieces that
    //  leave a vent-sized opening where the vent duct passes through.
    //  Used for rooms whose wall height exceeds vY (= roomHeight).
    // ═══════════════════════════════════════════════════════════════

    private void CutVentInDoorWallTop(Transform doorWallParent, float wallH, float doorH, float vY)
    {
        for (int i = doorWallParent.childCount - 1; i >= 0; i--)
        {
            if (doorWallParent.GetChild(i).name == "DoorWall_Top")
            {
                if (Application.isPlaying) Destroy(doorWallParent.GetChild(i).gameObject);
                else DestroyImmediate(doorWallParent.GetChild(i).gameObject);
                break;
            }
        }

        float topH = wallH - doorH;
        if (topH <= 0.01f) return;

        float origDoorW = kDoorWidth;   // matches doorWidth in AddDoorWall/AddDoorWallSide
        float hw = ventW / 2f;
        float sideW = (origDoorW - ventW) / 2f;
        float belowH = vY - doorH;
        float aboveH = wallH - (vY + ventH);

        if (sideW > 0.01f)
        {
            MakeBoxOnParent(doorWallParent, "DoorWall_Top_L",
                new Vector3(-(hw + sideW / 2f), doorH + topH / 2f, 0),
                sideW, topH, wallThickness);
            MakeBoxOnParent(doorWallParent, "DoorWall_Top_R",
                new Vector3(hw + sideW / 2f, doorH + topH / 2f, 0),
                sideW, topH, wallThickness);
        }

        if (belowH > 0.01f)
            MakeBoxOnParent(doorWallParent, "DoorWall_Top_Bot",
                new Vector3(0, doorH + belowH / 2f, 0),
                ventW, belowH, wallThickness);

        if (aboveH > 0.01f)
            MakeBoxOnParent(doorWallParent, "DoorWall_Top_Abv",
                new Vector3(0, vY + ventH + aboveH / 2f, 0),
                ventW, aboveH, wallThickness);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RNG HELPER — maps System.Random [0,1) to [min,max)
    // ═══════════════════════════════════════════════════════════════
    private float RngRange(System.Random rng, float min, float max)
    {
        return (float)(min + rng.NextDouble() * (max - min));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Axis-aligned bounding-box overlap test used by the procedural
    //  layout to prevent rooms from intersecting each other.
    //  Each entry in 'placed' is (centerX, centerZ, halfWidth, halfDepth).
    // ═══════════════════════════════════════════════════════════════
    private static bool BoundsOverlap(System.Collections.Generic.List<Vector4> placed,
        float cx, float cz, float halfW, float halfD, float pad, int excludeIdx = -1)
    {
        for (int i = 0; i < placed.Count; i++)
        {
            if (i == excludeIdx) continue;
            var p = placed[i];
            if (Mathf.Abs(cx - p.x) < halfW + p.z + pad &&
                Mathf.Abs(cz - p.y) < halfD + p.w + pad)
                return true;
        }
        return false;
    }


    // ═══════════════════════════════════════════════════════════════
    //  DATA STRUCTURES FOR INTELLIGENT PROCEDURAL GENERATION
    // ═══════════════════════════════════════════════════════════════

    private struct PlacedRoom
    {
        public string name;
        public float centerX, centerZ;
        public float width, depth, height;
        public float corridorZ;
        public bool isLeftSide;
        public ShipModuleGenerator generator;
    }

    private struct SpineNode
    {
        public float x, z;
        public float corridorLength;
        public bool hasLeftRoom, hasRightRoom;
        public string leftRoomName, rightRoomName;
    }

    // ─────────────────────────────────────────────────────────────
    //  ProcTryRegister — registers AABB in the bounds list only if
    //  it does not overlap any existing entry (plus safety pad).
    //  Returns true on success.
    // ─────────────────────────────────────────────────────────────
    private bool ProcTryRegister(System.Collections.Generic.List<Vector4> bds,
        float cx, float cz, float hw, float hd, float pad, int excludeIdx = -1)
    {
        if (BoundsOverlap(bds, cx, cz, hw, hd, pad, excludeIdx)) return false;
        bds.Add(new Vector4(cx, cz, hw, hd));
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    //  ProcTryPlaceRoom — intelligent multi-attempt room placement.
    //
    //  Strategy (6 attempts before giving up):
    //    Pass 0: preferred side, 100% size
    //    Pass 1: opposite side,  100% size
    //    Pass 2: preferred side,  80% size
    //    Pass 3: opposite side,   80% size
    //    Pass 4: preferred side,  65% size
    //    Pass 5: opposite side,   65% size
    //
    //  On success the bounds are registered and out params are set.
    // ─────────────────────────────────────────────────────────────
    private bool ProcTryPlaceRoom(
        System.Collections.Generic.List<Vector4> bds,
        float corZ, float baseW, float baseD, bool preferLeft, float pad,
        out float finalCX, out float finalW, out float finalD, out bool isLeft,
        int excludeIdx = -1)
    {
        float[] mults = { 1.0f, 0.8f, 0.65f };
        for (int mi = 0; mi < mults.Length; mi++)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                int side = (pass == 0) ? (preferLeft ? -1 : 1)
                                       : (preferLeft ?  1 : -1);
                float rW = Mathf.Max(3f, baseW * mults[mi]);
                float rD = Mathf.Max(3f, baseD * mults[mi]);
                float cx = side * (HalfCor + rW / 2f);
                if (ProcTryRegister(bds, cx, corZ, rW / 2f, rD / 2f, pad, excludeIdx))
                {
                    finalCX = cx; finalW = rW; finalD = rD;
                    isLeft  = (side == -1);
                    return true;
                }
            }
        }
        finalCX = finalW = finalD = 0f;
        isLeft = false;
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    //  BuildEngFrontWallDynamic — engineering front wall with N
    //  corridor openings.  Handles any branch count (1–3+).
    //
    //  openingXs : world-X centres of each corridor opening (sorted).
    //  openingW  : width of each opening (= corridorWidth).
    //  doorH     : door height (kDoorHeight).
    //
    //  Interior wall segments (between two openings) are split at
    //  the lateral vent level so the vent shaft can pass through
    //  without clipping geometry.  Each opening's above-door region
    //  is also split into below-vent / vent-gap / above-vent panels.
    // ─────────────────────────────────────────────────────────────
    private void BuildEngFrontWallDynamic(float engW, float engH,
        float engFrontZ, float[] openingXs, float openingW, float doorH)
    {
        if (openingXs == null || openingXs.Length == 0) return;
        System.Array.Sort(openingXs);

        float wallZ    = engFrontZ - wallThickness / 2f;
        float halfEngW = engW / 2f;
        float hOp      = openingW / 2f;
        float hvWLocal = ventW / 2f;
        float vTop     = roomHeight + ventH;
        float topH     = engH - doorH;

        // 1. Solid / vent-split wall segments between and outside openings.
        //    Interior segments (between two openings) are always split at the lateral
        //    vent band so the vent trunk can pass through without clipping geometry.
        //    Exterior segments are also split when the spine vent trunk (running at
        //    X=0 with half-width hvWLocal) passes through their X-range.  This fixes
        //    the single-branch case where the VJ_EngFr tee junction sits in an
        //    exterior segment and would otherwise clip through a full-height wall.
        float prevEdge = -halfEngW;
        for (int i = 0; i <= openingXs.Length; i++)
        {
            float segR = (i < openingXs.Length) ? openingXs[i] - hOp : halfEngW;
            float sw   = segR - prevEdge;
            if (sw > 0.01f)
            {
                float cx       = prevEdge + sw / 2f;
                bool  interior = (i > 0 && i < openingXs.Length);
                // Exterior segments also need the vent gap when the spine trunk at X=0
                // (±hvWLocal) runs through this segment's X range.
                bool  ventPassesThroughSeg = (prevEdge < hvWLocal) && (segR > -hvWLocal);
                if (interior || ventPassesThroughSeg)
                {
                    // Split at the vent band so the vent shaft passes through without clipping
                    if (roomHeight > 0.01f)
                        MakeBoxOnParent(transform, "EngFWD_Bot_" + i,
                            new Vector3(cx, roomHeight / 2f, wallZ), sw, roomHeight, wallThickness);
                    {
                        // Clamp above-vent panel to at least wallThickness so no razor-thin gap forms.
                        float topSegH = Mathf.Max(engH - vTop, wallThickness);
                        MakeBoxOnParent(transform, "EngFWD_Top_" + i,
                            new Vector3(cx, engH - topSegH / 2f, wallZ), sw, topSegH, wallThickness);
                    }
                }
                else
                {
                    // Exterior panel with no vent passage — build full height
                    MakeBoxOnParent(transform, "EngFWD_Seg_" + i,
                        new Vector3(cx, engH / 2f, wallZ), sw, engH, wallThickness);
                }
            }
            if (i < openingXs.Length)
                prevEdge = openingXs[i] + hOp;
        }

        // 2. Above-door fill for each opening
        //    Solid side strips + below-vent and above-vent centre panels
        if (topH > 0.01f)
        {
            float belowH = Mathf.Max(0f, roomHeight - doorH);
            // Clamp above-vent panel to at least wallThickness so no razor-thin gap forms.
            float aboveH = Mathf.Max(engH - vTop, wallThickness);
            for (int i = 0; i < openingXs.Length; i++)
            {
                float bx    = openingXs[i];
                float sideW = hOp - hvWLocal;
                if (sideW > 0.01f)
                {
                    // Split at the vent band so the lateral vent shaft can pass through
                    if (belowH > 0.01f)
                    {
                        MakeBoxOnParent(transform, "EngFWD_DTS_LBot_" + i,
                            new Vector3(bx - hvWLocal - sideW / 2f, doorH + belowH / 2f, wallZ),
                            sideW, belowH, wallThickness);
                        MakeBoxOnParent(transform, "EngFWD_DTS_RBot_" + i,
                            new Vector3(bx + hvWLocal + sideW / 2f, doorH + belowH / 2f, wallZ),
                            sideW, belowH, wallThickness);
                    }
                    if (aboveH > 0.01f)
                    {
                        MakeBoxOnParent(transform, "EngFWD_DTS_LTop_" + i,
                            new Vector3(bx - hvWLocal - sideW / 2f, engH - aboveH / 2f, wallZ),
                            sideW, aboveH, wallThickness);
                        MakeBoxOnParent(transform, "EngFWD_DTS_RTop_" + i,
                            new Vector3(bx + hvWLocal + sideW / 2f, engH - aboveH / 2f, wallZ),
                            sideW, aboveH, wallThickness);
                    }
                }
                if (belowH > 0.01f)
                    MakeBoxOnParent(transform, "EngFWD_DT_Bot_" + i,
                        new Vector3(bx, doorH + belowH / 2f, wallZ),
                        ventW, belowH, wallThickness);
                if (aboveH > 0.01f)
                    MakeBoxOnParent(transform, "EngFWD_DT_Abv_" + i,
                        new Vector3(bx, engH - aboveH / 2f, wallZ),
                        ventW, aboveH, wallThickness);
            }
        }

        // 3. Door-frame detail trim
        if (detailLevel >= 1)
        {
            float trim = 0.06f;
            float tz   = wallZ + wallThickness / 2f + trim / 2f;
            for (int i = 0; i < openingXs.Length; i++)
            {
                float bx = openingXs[i];
                MakeBoxOnParent(transform, "EngFWD_Tr_LL_" + i,
                    new Vector3(bx - hOp - trim / 2f, doorH / 2f, tz), trim, doorH, trim);
                MakeBoxOnParent(transform, "EngFWD_Tr_LR_" + i,
                    new Vector3(bx + hOp + trim / 2f, doorH / 2f, tz), trim, doorH, trim);
                MakeBoxOnParent(transform, "EngFWD_Tr_T_" + i,
                    new Vector3(bx, doorH + trim / 2f, tz), openingW + trim * 2f, trim, trim);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  PROCEDURAL LAYOUT — 6-phase AI-driven generation.
    //
    //  Phase 1 : Spine topology    — randomise corridor chain + branches
    //  Phase 2 : Intelligent room placement — multi-attempt with
    //             side-switching + size reduction before giving up
    //  Phase 3 : Geometry build    — rooms, corridors, walls, doors
    //  Phase 4 : Map-aware vents   — junctions only for placed rooms
    //  Phase 5 : Prop scattering   — type-matched to room function
    //  Phase 6 : Final validation  — ceiling check + summary log
    // ═══════════════════════════════════════════════════════════════
    private void GenerateProceduralLayout()
    {
        // On the first attempt capture the base seed; on retries mutate it via an LCG step.
        if (_procRetries == 0)
            _procBaseSeed = seed < 0 ? System.Environment.TickCount : seed;
        int actualSeed = _procBaseSeed;
        for (int i = 0; i < _procRetries; i++)
            actualSeed = (int)(unchecked((long)actualSeed * 6364136223846793005L + 1442695040888963407L) & 0x7FFFFFFF);
        System.Random rng = new System.Random(actualSeed);
        string _sep = new string('═', 52);
        Debug.Log("[ProcGen:AI] " + _sep);
        Debug.Log(string.Format("[ProcGen:AI] <b>INTELLIGENT LAYOUT GENERATION</b>  Seed={0}{1}",
            actualSeed, _procRetries > 0 ? "  <color=#ff9900>(RETRY " + _procRetries + "/5)</color>" : ""));
        Debug.Log("[ProcGen:AI] " + _sep);

        // AABB overlap safety margin.  0.05 is intentionally small: side rooms are
        // designed to abut (slightly overlap) their parent corridor wall for proper
        // connection, so the parent corridor is excluded via excludeIdx rather than
        // relying on a large pad to separate them.  0.05 prevents rooms that would
        // otherwise share a wall face from being falsely rejected.
        const float kPad  = 0.05f;
        float dW    = kDoorWidth;
        float dH    = kDoorHeight;
        float vY    = roomHeight;
        float hvW   = ventW / 2f;
        float dropW = ventW * 0.5f;
        float dropH = ventH + wallThickness + 0.15f;
        float dropY = vY - dropH / 2f;

        // World-space AABB registry: (centerX, centerZ, halfW, halfD)
        var bds = new System.Collections.Generic.List<Vector4>();
        int roomsPlaced = 0, roomsSkipped = 0, ventSegs = 0, ventCutsMade = 0;

        // ════════════════════════════════════════════════════════
        //  PHASE 1 — SPINE TOPOLOGY
        //  Randomise dimensions, corridor chain, cargo position,
        //  and fork branch layout.  All Z positions are calculated
        //  here; no geometry is built yet.
        // ════════════════════════════════════════════════════════
        Debug.Log("[ProcGen:AI] <b>── PHASE 1: Spine Topology Planning ──</b>");

        float dockW   = RngRange(rng, 14f, 18f);
        float dockD   = RngRange(rng, 10f, 14f);
        float dockH   = RngRange(rng,  5f,  6f);
        float cargoW  = RngRange(rng,  7f, 10f);
        float cargoD  = RngRange(rng,  5f,  8f);
        float cargoH  = RngRange(rng, 3.5f, 5f);
        float engW    = RngRange(rng, 10f, 14f);
        float engD    = RngRange(rng,  9f, 12f);
        float engH    = RngRange(rng,  4f,  5f);
        float bridgeW = RngRange(rng,  9f, 12f);
        float bridgeD = RngRange(rng,  7f, 10f);

        int   spineCount    = rng.Next(2, 6);
        // cargoAfterIdx: insert cargo after corridor [cargoAfterIdx-1].
        // Using spineCount (exclusive upper) ensures at least one corridor
        // always follows the CargoBay before Engineering.
        int   cargoAfterIdx = rng.Next(1, spineCount);
        float[] sLen = new float[spineCount];
        for (int i = 0; i < spineCount; i++)
            sLen[i] = RngRange(rng, 6f, 14f);

        float dockZ  = 0f;
        float dockFr = dockD / 2f;
        float[] sBk = new float[spineCount];
        float[] sCZ = new float[spineCount];
        float[] sFr = new float[spineCount];
        float cargoBk = 0f, cargoCZ = 0f, cargoFrZ = 0f;
        float cur = dockFr;
        for (int i = 0; i < spineCount; i++)
        {
            sBk[i] = cur; sCZ[i] = cur + sLen[i] / 2f; sFr[i] = cur + sLen[i];
            cur = sFr[i];
            if (i == cargoAfterIdx - 1)
            {
                cargoBk = cur; cargoCZ = cur + cargoD / 2f;
                cargoFrZ = cur + cargoD; cur = cargoFrZ;
            }
        }

        float engBk = cur;
        float engCZ = engBk + engD / 2f;
        float engFr = engBk + engD;

        // Branch geometry
        int branchCount = rng.Next(1, 4);
        float[] bX       = new float[branchCount];
        int[]   bPat     = new int[branchCount];    // 0=straight, 1=Z-shaped, 2=L-shaped
        int[]   bSideDir = new int[branchCount];    // -1=turn left, +1=turn right
        float[] bStrLen  = new float[branchCount];
        float[] bSideLen = new float[branchCount];
        float[] bFinLen  = new float[branchCount];
        float[] bCor1Z   = new float[branchCount];
        float[] bCor2X   = new float[branchCount];
        float[] bFinBk   = new float[branchCount];
        float[] bFinCZ   = new float[branchCount];
        float[] bTermBk  = new float[branchCount];
        float[] bTermX   = new float[branchCount];

        if (branchCount == 1)
        {
            int ch = rng.Next(3);
            bX[0] = ch == 0 ? -(engW / 4f) : (ch == 2 ? engW / 4f : 0f);
        }
        else if (branchCount == 2)
        { bX[0] = -(engW / 4f); bX[1] = engW / 4f; }
        else
        { bX[0] = -(engW / 3f); bX[1] = 0f; bX[2] = engW / 3f; }
        Debug.Log(string.Format("[ProcGen:AI] Topology: spine={0} corridors | cargo after cor{1} | eng={2:F1}×{3:F1}×{4:F1}h | branches={5} at X=[{6}]",
            spineCount, cargoAfterIdx,
            engW, engD, engH, branchCount,
            string.Join(", ", System.Array.ConvertAll(bX, x => x.ToString("F1")))));

        // Incremental branch-corridor AABB registry used only during Phase 1 geometry
        // planning.  This list is SEPARATE from the main 'bds' list to avoid false
        // positives: each branch's own straight corridor is added to branchSegBds AFTER
        // the Z-shape check, so corner1 (which is adjacent to the straight corridor) is
        // not incorrectly rejected when checking against the current branch's own geometry.
        // Only previously-committed branches populate this list at check time.
        var branchSegBds = new System.Collections.Generic.List<Vector4>();
        const float kBranchSideLenStep = 0.5f;   // shrink step when clamping bSideLen
        const float kBranchSideLenMinTol = 0.1f; // minimum viable sideLen above corridorWidth

        for (int b = 0; b < branchCount; b++)
        {
            bPat[b]     = rng.Next(2);
            bStrLen[b]  = RngRange(rng, 4f, 8f);
            bSideLen[b] = RngRange(rng, 5f, 12f);
            bFinLen[b]  = RngRange(rng, 5f, 10f);
            bSideDir[b] = bX[b] < -0.1f ? -1
                        : bX[b] >  0.1f ? +1
                        : (rng.Next(2) == 0 ? -1 : +1);

            float strFr = engFr + bStrLen[b];

            // ── AABB-based Z-shape fit check ────────────────────────────────
            // The old formula-based bSideLen clamp did not account for the full
            // Z-shape footprint (corner1 + side + corner2 + final corridor) and
            // produced real geometry overlaps between branches.
            //
            // Strategy:
            //   1. Check the CURRENT branch's Z-shape segments against all
            //      PREVIOUSLY committed branch corridors (branchSegBds).
            //      We check BEFORE adding this branch's own straight corridor,
            //      so there is no false-positive from the corner1/straight seam.
            //   2. If the default bSideLen causes overlap, shrink it in 0.5 m
            //      steps until it fits or becomes too small.
            //   3. If no valid length exists, fall back to straight pattern
            //      (bPat = 0) so the layout is always playable.
            // ────────────────────────────────────────────────────────────────
            Debug.Log(string.Format("[ProcGen:AI] Branch {0} @ X={1:F1}: initial pattern={2} | strLen={3:F1}m sideLen={4:F1}m{5} dir={6}",
                b, bX[b], bPat[b] == 1 ? "Z-shape" : "straight",
                bStrLen[b], bSideLen[b], bPat[b] == 1 ? string.Format(" finLen={0:F1}m", bFinLen[b]) : "",
                bSideDir[b] == -1 ? "left" : "right"));

            if (bPat[b] == 1)
            {
                bool  foundFit    = false;
                bool  foundLShape = false;
                int   origDir     = bSideDir[b];
                float winStrLen   = bStrLen[b];
                float winSideLen  = bSideLen[b];
                int   winDir      = bSideDir[b];

                // ── Try Z-shape: 3 forward-lengths × 2 directions × shrinking sideLen ──────────
                // 3 strLen candidates: 100%, 75%, 50% of initial pick (minimum 4 m).
                // Trying shorter forward runs shifts the Z-shape's lateral section to an
                // earlier Z position, potentially avoiding overlaps with sibling branches.
                float[] strCandidates = {
                    bStrLen[b],
                    Mathf.Max(4f, bStrLen[b] * 0.75f),
                    Mathf.Max(4f, bStrLen[b] * 0.5f)
                };

                for (int si = 0; si < strCandidates.Length && !foundFit; si++)
                {
                    float testStrLen = strCandidates[si];
                    float testStrFr  = engFr + testStrLen;

                    if (si > 0)
                        Debug.Log(string.Format(
                            "[ProcGen:AI]   Branch {0}: Z-shape all directions failed at strLen={1:F1}m → trying shorter strLen={2:F1}m.",
                            b, strCandidates[si - 1], testStrLen));

                    for (int dirPass = 0; dirPass < 2 && !foundFit; dirPass++)
                    {
                        int  testDir = dirPass == 0 ? origDir : -origDir;
                        bool gl      = testDir == -1;

                        if (dirPass == 1)
                            Debug.Log(string.Format(
                                "[ProcGen:AI]   Branch {0}: Z-shape dir={1} failed at strLen={2:F1}m — flipping to {3} and retrying.",
                                b, origDir == -1 ? "left" : "right", testStrLen, testDir == -1 ? "left" : "right"));

                        float testSideLen = bSideLen[b]; // reset to original chosen length each pass
                        int   shrinkSteps = 0;

                        while (testSideLen >= corridorWidth + kBranchSideLenMinTol)
                        {
                            float tCor1Z  = testStrFr + HalfCor;
                            float tCor2X  = gl ? bX[b] - testSideLen - corridorWidth
                                               : bX[b] + testSideLen + corridorWidth;
                            float tFinCZ  = tCor1Z + HalfCor + bFinLen[b] / 2f;
                            float tSideCX = gl ? bX[b] - HalfCor - testSideLen / 2f
                                               : bX[b] + HalfCor + testSideLen / 2f;

                            bool overlap =
                                BoundsOverlap(branchSegBds, bX[b],    tCor1Z, corridorWidth / 2f, corridorWidth / 2f, kPad) ||
                                BoundsOverlap(branchSegBds, tSideCX,  tCor1Z, testSideLen / 2f,   corridorWidth / 2f, kPad) ||
                                BoundsOverlap(branchSegBds, tCor2X,   tCor1Z, corridorWidth / 2f, corridorWidth / 2f, kPad) ||
                                BoundsOverlap(branchSegBds, tCor2X,   tFinCZ, corridorWidth / 2f, bFinLen[b] / 2f,   kPad);

                            if (!overlap)
                            {
                                foundFit   = true;
                                winStrLen  = testStrLen;
                                winSideLen = testSideLen;
                                winDir     = testDir;
                                break;
                            }
                            testSideLen -= kBranchSideLenStep;
                            shrinkSteps++;
                        }

                        if (!foundFit)
                            Debug.Log(string.Format(
                                "[ProcGen:AI]   Branch {0}: Z-shape dir={1} strLen={2:F1}m — no sideLen fits after {3} shrink steps.",
                                b, testDir == -1 ? "left" : "right", testStrLen, shrinkSteps));
                    }
                }

                if (foundFit)
                {
                    bStrLen[b]  = winStrLen;
                    bSideLen[b] = winSideLen;
                    bSideDir[b] = winDir;
                    strFr       = engFr + bStrLen[b];
                    bool adjusted = winSideLen < bSideLen[b] ||
                                    System.Math.Abs(winStrLen - strCandidates[0]) > 0.01f;
                    Debug.Log(string.Format(
                        "[ProcGen:AI] Branch {0}: <color=#00ff88><b>✓ Z-shape accepted</b></color> strLen={1:F1}m sideLen={2:F1}m dir={3}{4}.",
                        b, bStrLen[b], bSideLen[b], bSideDir[b] == -1 ? "left" : "right",
                        adjusted ? " <color=#ffcc00>(config adjusted to fit)</color>" : ""));
                }
                else
                {
                    // ── Z-shape failed: try L-shape (straight + corner + side + corner, no Fin corridor) ──
                    Debug.Log(string.Format(
                        "[ProcGen:AI] Branch {0}: <color=#ffcc00>Z-shape exhausted</color> ({1} strLen × 2 dir × N sideLen attempts failed) — <b>trying L-shape</b>.",
                        b, strCandidates.Length));

                    bSideDir[b] = origDir; // reset direction before L-shape search
                    float lStrFr = engFr + bStrLen[b]; // L-shape uses original strLen

                    for (int dirPass = 0; dirPass < 2 && !foundLShape; dirPass++)
                    {
                        int  testDir = dirPass == 0 ? origDir : -origDir;
                        bool gl      = testDir == -1;

                        if (dirPass == 1)
                            Debug.Log(string.Format(
                                "[ProcGen:AI]   Branch {0}: L-shape dir={1} failed — flipping to {2}.",
                                b, origDir == -1 ? "left" : "right", testDir == -1 ? "left" : "right"));

                        float testSideLen = bSideLen[b];

                        while (testSideLen >= corridorWidth + kBranchSideLenMinTol)
                        {
                            float tCor1Z  = lStrFr + HalfCor;
                            float tCor2X  = gl ? bX[b] - testSideLen - corridorWidth
                                               : bX[b] + testSideLen + corridorWidth;
                            float tSideCX = gl ? bX[b] - HalfCor - testSideLen / 2f
                                               : bX[b] + HalfCor + testSideLen / 2f;

                            // L-shape: only C1 + side + C2 (no Fin corridor to check)
                            bool overlap =
                                BoundsOverlap(branchSegBds, bX[b],    tCor1Z, corridorWidth / 2f, corridorWidth / 2f, kPad) ||
                                BoundsOverlap(branchSegBds, tSideCX,  tCor1Z, testSideLen / 2f,   corridorWidth / 2f, kPad) ||
                                BoundsOverlap(branchSegBds, tCor2X,   tCor1Z, corridorWidth / 2f, corridorWidth / 2f, kPad);

                            if (!overlap)
                            {
                                foundLShape    = true;
                                bSideLen[b]    = testSideLen;
                                bSideDir[b]    = testDir;
                                break;
                            }
                            testSideLen -= kBranchSideLenStep;
                        }
                    }

                    if (foundLShape)
                    {
                        bPat[b] = 2;
                        strFr   = engFr + bStrLen[b];
                        Debug.Log(string.Format(
                            "[ProcGen:AI] Branch {0}: <color=#00ff88><b>✓ L-shape accepted</b></color> strLen={1:F1}m sideLen={2:F1}m dir={3}.",
                            b, bStrLen[b], bSideLen[b], bSideDir[b] == -1 ? "left" : "right"));
                    }
                    else
                    {
                        bSideDir[b] = origDir;
                        bPat[b]     = 0;
                        Debug.Log(string.Format(
                            "[ProcGen:AI] Branch {0}: <color=#ff4444><b>✗ Z and L-shape both exhausted</b></color> — falling back to <b>straight</b> pattern.",
                            b));
                    }
                }
            }
            else
            {
                Debug.Log(string.Format("[ProcGen:AI] Branch {0}: straight pattern chosen (random initial roll).", b));
            }

            // Commit geometry for this branch
            if (bPat[b] == 0)
            {
                bTermBk[b] = strFr;
                bTermX[b]  = bX[b];
            }
            else // Z-shape (bPat==1) or L-shape (bPat==2)
            {
                bool gl    = bSideDir[b] == -1;
                bCor1Z[b]  = strFr + HalfCor;
                bCor2X[b]  = gl ? bX[b] - bSideLen[b] - corridorWidth
                                : bX[b] + bSideLen[b] + corridorWidth;

                float sCXb = gl ? bX[b] - HalfCor - bSideLen[b] / 2f
                                : bX[b] + HalfCor + bSideLen[b] / 2f;
                // Register C1, Side, C2 segments (common to both Z and L)
                branchSegBds.Add(new Vector4(bX[b],     bCor1Z[b], corridorWidth / 2f, corridorWidth / 2f)); // C1
                branchSegBds.Add(new Vector4(sCXb,      bCor1Z[b], bSideLen[b] / 2f,   corridorWidth / 2f)); // Side
                branchSegBds.Add(new Vector4(bCor2X[b], bCor1Z[b], corridorWidth / 2f, corridorWidth / 2f)); // C2

                if (bPat[b] == 1) // Z-shape: has final corridor
                {
                    bFinBk[b]  = bCor1Z[b] + HalfCor;
                    bFinCZ[b]  = bFinBk[b] + bFinLen[b] / 2f;
                    bTermBk[b] = bFinBk[b] + bFinLen[b];
                    branchSegBds.Add(new Vector4(bCor2X[b], bFinCZ[b], corridorWidth / 2f, bFinLen[b] / 2f)); // Fin
                }
                else // L-shape: no final corridor — terminal room starts right after C2
                {
                    bTermBk[b] = bCor1Z[b] + HalfCor;
                }
                bTermX[b] = bCor2X[b];
            }

            // Always register the straight corridor for subsequent branches.
            float strCZ_b = engFr + bStrLen[b] / 2f;
            branchSegBds.Add(new Vector4(bX[b], strCZ_b, corridorWidth / 2f, bStrLen[b] / 2f));
        }

        // Branch pattern summary log
        {
            int _zc = 0, _lc = 0, _sc = 0;
            for (int b = 0; b < branchCount; b++) { if (bPat[b] == 1) _zc++; else if (bPat[b] == 2) _lc++; else _sc++; }
            Debug.Log(string.Format(
                "[ProcGen:AI] <b>Phase 1 complete</b>: {0}×Z-shape  {1}×L-shape  {2}×straight-fallback",
                _zc, _lc, _sc));
        }

        // Room pool (Fisher-Yates shuffle for reproducibility)
        string[] pName = {
            "StorageRoom", "Armory", "LifeSupport", "CrewQuarters",
            "SecurityOffice", "MedBay", "NavigationRoom", "MessHall",
            "ReactorRoom", "ScienceLab"
        };
        float[] pRW = { 6f, 4f, 5f, 6f, 4f, 5f, 5f, 7f, 6f, 6f };
        float[] pRD = { 5f, 4f, 4f, 5f, 4f, 5f, 4f, 5f, 6f, 5f };
        int poolSize = pName.Length;
        int[] pidx = new int[poolSize];
        for (int i = 0; i < poolSize; i++) pidx[i] = i;
        for (int i = poolSize - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int tmp = pidx[i]; pidx[i] = pidx[j]; pidx[j] = tmp;
        }
        int pp = 0;

        // ════════════════════════════════════════════════════════
        //  PHASE 2 — INTELLIGENT ROOM PLACEMENT (planning pass)
        //
        //  No geometry is built here.  For each optional room the
        //  system runs up to 6 AABB placement attempts before
        //  deciding to skip the room cleanly.  Skipped rooms leave
        //  NO orphaned corridor walls, door cuts, or vent branches.
        // ════════════════════════════════════════════════════════
        Debug.Log("[ProcGen:AI] <b>── PHASE 2: Intelligent Room Placement ──</b>");

        // Register required rooms first so optionals are tested against them
        bds.Add(new Vector4(0f, dockZ,   dockW  / 2f, dockD  / 2f));
        bds.Add(new Vector4(0f, cargoCZ, cargoW / 2f, cargoD / 2f));
        bds.Add(new Vector4(0f, engCZ,   engW   / 2f, engD   / 2f));

        // Register corridor footprints so rooms cannot encroach on them.
        // Spine corridor i is at bds index (3 + i).
        for (int i = 0; i < spineCount; i++)
            bds.Add(new Vector4(0f, sCZ[i], corridorWidth / 2f, sLen[i] / 2f));

        // For branch final corridors (Z-shaped branches) we need their bds index so
        // that side rooms can exclude their own parent corridor from the overlap test.
        // bStrCorBdsIdx is used for terminal rooms on straight branches.
        int[] bFinCorBdsIdx = new int[branchCount];
        int[] bStrCorBdsIdx = new int[branchCount];
        for (int b = 0; b < branchCount; b++)
        {
            float strCZ = engFr + bStrLen[b] / 2f;
            bStrCorBdsIdx[b] = bds.Count;
            bds.Add(new Vector4(bX[b], strCZ, corridorWidth / 2f, bStrLen[b] / 2f));
            if (bPat[b] == 1) // Z-shape
            {
                bool gl    = bSideDir[b] == -1;
                float sCXb = gl ? bX[b] - HalfCor - bSideLen[b] / 2f
                                : bX[b] + HalfCor + bSideLen[b] / 2f;
                bds.Add(new Vector4(sCXb, bCor1Z[b], bSideLen[b] / 2f, corridorWidth / 2f));
                bds.Add(new Vector4(bX[b],    bCor1Z[b], corridorWidth / 2f, corridorWidth / 2f)); // corner1
                bds.Add(new Vector4(bCor2X[b], bCor1Z[b], corridorWidth / 2f, corridorWidth / 2f)); // corner2
                bFinCorBdsIdx[b] = bds.Count;
                bds.Add(new Vector4(bCor2X[b], bFinCZ[b], corridorWidth / 2f, bFinLen[b] / 2f));
            }
            else if (bPat[b] == 2) // L-shape: C1 + side + C2, no Fin corridor
            {
                bool gl    = bSideDir[b] == -1;
                float sCXb = gl ? bX[b] - HalfCor - bSideLen[b] / 2f
                                : bX[b] + HalfCor + bSideLen[b] / 2f;
                bds.Add(new Vector4(sCXb, bCor1Z[b], bSideLen[b] / 2f, corridorWidth / 2f));         // side
                bds.Add(new Vector4(bX[b],    bCor1Z[b], corridorWidth / 2f, corridorWidth / 2f));   // C1
                bFinCorBdsIdx[b] = bds.Count; // C2 corner serves as "parent" for terminal room exclusion
                bds.Add(new Vector4(bCor2X[b], bCor1Z[b], corridorWidth / 2f, corridorWidth / 2f));  // C2
                // No Fin corridor registered for L-shape
            }
        }

        // ── Spine side rooms ──────────────────────────────────
        bool[]   sHL  = new bool[spineCount], sHR = new bool[spineCount];
        string[] sLNm = new string[spineCount], sRNm = new string[spineCount];
        float[]  sLW  = new float[spineCount],  sLD = new float[spineCount];
        float[]  sRW  = new float[spineCount],  sRD = new float[spineCount];
        float[]  sLX  = new float[spineCount],  sRX = new float[spineCount];

        for (int i = 0; i < spineCount; i++)
        {
            // Spine corridor i was registered at bds index (3 + i).
            // Pass it as excludeIdx so the overlap test ignores the parent corridor
            // (side rooms are designed to touch/overlap their corridor wall).
            int spineCorBdsIdx = 3 + i;

            // First side candidate — prefer left
            if (pp < poolSize && rng.NextDouble() > 0.25)
            {
                int k = pidx[pp];
                float fcx, fw, fd; bool fLeft;
                if (ProcTryPlaceRoom(bds, sCZ[i], pRW[k], pRD[k], true, kPad,
                        out fcx, out fw, out fd, out fLeft, spineCorBdsIdx))
                {
                    pp++;
                    if (fLeft)
                    { sHL[i] = true; sLNm[i] = pName[k]; sLW[i] = fw; sLD[i] = fd; sLX[i] = fcx; }
                    else
                    { sHR[i] = true; sRNm[i] = pName[k]; sRW[i] = fw; sRD[i] = fd; sRX[i] = fcx; }
                    roomsPlaced++;
                    Debug.Log(string.Format("[ProcGen:AI] Spine[{0}] placed '{1}' on {2} side (w={3:F1} d={4:F1}).",
                        i, pName[k], fLeft ? "left" : "right", fw, fd));
                }
                else
                {
                    Debug.LogWarning("[ProcGen] All placement attempts failed for " +
                        pName[k] + " at spine[" + i + "]. Skipping cleanly.");
                    Debug.Log(string.Format("[ProcGen:AI] Spine[{0}] '{1}' skipped — no valid position found after 6 attempts.", i, pName[k]));
                    roomsSkipped++; pp++;
                }
            }

            // Second side candidate — only attempt the side not yet occupied.
            // We do NOT reuse ProcTryPlaceRoom here because that function tries
            // both sides internally, which could collide with the first room.
            // Instead, target exactly the free side with up to 3 size attempts.
            if (pp < poolSize && rng.NextDouble() > 0.25 && !(sHL[i] && sHR[i]))
            {
                int k = pidx[pp];
                bool needLeft  = !sHL[i];  // fill the unoccupied side
                int  side2     = needLeft ? -1 : 1;
                float[] mults2 = { 1.0f, 0.8f, 0.65f };
                bool placed2   = false;
                for (int mi2 = 0; mi2 < mults2.Length && !placed2; mi2++)
                {
                    float rW2 = Mathf.Max(3f, pRW[k] * mults2[mi2]);
                    float rD2 = Mathf.Max(3f, pRD[k] * mults2[mi2]);
                    float cx2 = side2 * (HalfCor + rW2 / 2f);
                    if (ProcTryRegister(bds, cx2, sCZ[i], rW2 / 2f, rD2 / 2f, kPad, spineCorBdsIdx))
                    {
                        pp++;
                        if (needLeft)
                        { sHL[i] = true; sLNm[i] = pName[k]; sLW[i] = rW2; sLD[i] = rD2; sLX[i] = cx2; }
                        else
                        { sHR[i] = true; sRNm[i] = pName[k]; sRW[i] = rW2; sRD[i] = rD2; sRX[i] = cx2; }
                        roomsPlaced++; placed2 = true;
                        Debug.Log(string.Format("[ProcGen:AI] Spine[{0}] placed '{1}' on {2} side attempt {3} (w={4:F1} d={5:F1}).",
                            i, pName[k], needLeft ? "left" : "right", mi2, rW2, rD2));
                    }
                }
                if (!placed2)
                {
                    Debug.LogWarning("[ProcGen] All placement attempts failed for " +
                        pName[k] + " at spine[" + i + "] (second side). Skipping cleanly.");
                    Debug.Log(string.Format("[ProcGen:AI] Spine[{0}] '{1}' (second side) skipped — no valid position.", i, pName[k]));
                    roomsSkipped++; pp++;
                }
            }
        }

        // ── Engineering side rooms ────────────────────────────
        bool   engHR = false, engHL = false;
        string reactNm = "", labNm = "";
        float  reactW_ = 0f, reactD_ = 0f, labW_ = 0f, labD_ = 0f;
        float  reactX  = 0f, labX   = 0f;

        if (pp < poolSize && rng.NextDouble() > 0.35f)
        {
            int k = pidx[pp]; pp++;
            float cx = engW / 2f + pRW[k] / 2f;
            if (ProcTryRegister(bds, cx, engCZ, pRW[k] / 2f, pRD[k] / 2f, kPad, 2))
            {
                engHR = true; reactNm = pName[k];
                reactW_ = pRW[k]; reactD_ = pRD[k]; reactX = cx;
                roomsPlaced++;
                Debug.Log(string.Format("[ProcGen:AI] Eng-Right placed '{0}' (w={1:F1} d={2:F1}).", pName[k], pRW[k], pRD[k]));
            }
            else
            {
                Debug.LogWarning("[ProcGen] " + pName[k] + "_EngR overlaps — skipping.");
                Debug.Log(string.Format("[ProcGen:AI] Eng-Right '{0}' skipped — overlaps engineering hub.", pName[k]));
                roomsSkipped++;
            }
        }

        if (pp < poolSize && rng.NextDouble() > 0.35f)
        {
            int k = pidx[pp]; pp++;
            float cx = -(engW / 2f + pRW[k] / 2f);
            if (ProcTryRegister(bds, cx, engCZ, pRW[k] / 2f, pRD[k] / 2f, kPad, 2))
            {
                engHL = true; labNm = pName[k];
                labW_ = pRW[k]; labD_ = pRD[k]; labX = cx;
                roomsPlaced++;
                Debug.Log(string.Format("[ProcGen:AI] Eng-Left placed '{0}' (w={1:F1} d={2:F1}).", pName[k], pRW[k], pRD[k]));
            }
            else
            {
                Debug.LogWarning("[ProcGen] " + pName[k] + "_EngL overlaps — skipping.");
                Debug.Log(string.Format("[ProcGen:AI] Eng-Left '{0}' skipped — overlaps engineering hub.", pName[k]));
                roomsSkipped++;
            }
        }

        // ── Terminal rooms (one branch always gets Bridge) ────
        int      bridgeBranch = rng.Next(branchCount);
        float[]  bTermW  = new float[branchCount];
        float[]  bTermD  = new float[branchCount];
        string[] bTermNm = new string[branchCount];
        float[]  bTermCZ = new float[branchCount];
        bool[]   bTermExists = new bool[branchCount];

        for (int b = 0; b < branchCount; b++)
        {
            if (b == bridgeBranch)
            { bTermW[b] = bridgeW; bTermD[b] = bridgeD; bTermNm[b] = "Bridge"; }
            else if (pp < poolSize)
            {
                int k = pidx[pp++];
                bTermW[b] = pRW[k]; bTermD[b] = pRD[k]; bTermNm[b] = pName[k];
            }
            else
            { bTermW[b] = 7f; bTermD[b] = 5f; bTermNm[b] = "Terminal_" + b; }
            bTermCZ[b] = bTermBk[b] + bTermD[b] / 2f;

            // Terminal rooms: try original size first, then shrink before giving up (capping corridor)
            // bFinCorBdsIdx serves double duty: for Z-shape it points to the Fin corridor;
            // for L-shape it points to the C2 corner — both are the immediate parent of the terminal.
            int termParentCorBdsIdx = (bPat[b] == 0) ? bStrCorBdsIdx[b] : bFinCorBdsIdx[b];
            float[] termMults  = { 1.0f, 0.85f, 0.70f };
            bool    termFit    = false;
            Debug.Log(string.Format("[ProcGen:AI]   Branch {0} terminal '{1}' at X={2:F1}: checking {3:F1}×{4:F1}...",
                b, bTermNm[b], bTermX[b], bTermW[b], bTermD[b]));
            for (int mi = 0; mi < termMults.Length && !termFit; mi++)
            {
                float tW  = Mathf.Max(4f, bTermW[b] * termMults[mi]);
                float tD  = Mathf.Max(3f, bTermD[b] * termMults[mi]);
                float tCZ = bTermBk[b] + tD / 2f;
                if (mi > 0)
                    Debug.Log(string.Format("[ProcGen:AI]   Branch {0} terminal '{1}': full size blocked — shrinking to {2:F0}% ({3:F1}×{4:F1}).",
                        b, bTermNm[b], termMults[mi] * 100f, tW, tD));
                if (ProcTryRegister(bds, bTermX[b], tCZ, tW / 2f, tD / 2f, kPad, termParentCorBdsIdx))
                {
                    bTermExists[b] = true;
                    bTermW[b] = tW;  bTermD[b] = tD;  bTermCZ[b] = tCZ;
                    roomsPlaced++;
                    termFit = true;
                    string patName  = bPat[b] == 0 ? "straight" : bPat[b] == 1 ? "Z" : "L";
                    string sizeNote = mi == 0 ? "" : string.Format(" <color=#ffcc00>(at {0:F0}% size)</color>", termMults[mi] * 100f);
                    Debug.Log(string.Format("[ProcGen:AI]   Branch {0} terminal '{1}' <color=#00ff88>✓ placed</color> (w={2:F1} d={3:F1} pat={4}){5}.",
                        b, bTermNm[b], tW, tD, patName, sizeNote));
                }
            }
            if (!termFit)
            {
                Debug.LogWarning(string.Format("[ProcGen:AI]   Branch {0} terminal '{1}' <color=#ff4444>✗ no size fits</color> — corridor will be capped.",
                    b, bTermNm[b]));
                roomsSkipped++;
            }
        }

        // ── Branch final-corridor side rooms (Z-shaped branches) ──
        bool[]   bHL  = new bool[branchCount],  bHR  = new bool[branchCount];
        string[] bLNm = new string[branchCount], bRNm = new string[branchCount];
        float[]  bLW  = new float[branchCount],  bLD  = new float[branchCount];
        float[]  bRW  = new float[branchCount],  bRD  = new float[branchCount];
        float[]  bLX  = new float[branchCount],  bRX  = new float[branchCount];

        for (int b = 0; b < branchCount; b++)
        {
            if (bPat[b] != 1) continue;
            if (pp < poolSize && rng.NextDouble() > 0.4f)
            {
                int k = pidx[pp]; pp++;
                float cx = bCor2X[b] - HalfCor - pRW[k] / 2f;
                if (ProcTryRegister(bds, cx, bFinCZ[b], pRW[k] / 2f, pRD[k] / 2f, kPad, bFinCorBdsIdx[b]))
                {
                    bHL[b] = true; bLNm[b] = pName[k]; bLW[b] = pRW[k]; bLD[b] = pRD[k]; bLX[b] = cx; roomsPlaced++;
                    Debug.Log(string.Format("[ProcGen:AI] Branch {0} fin-left '{1}' placed.", b, pName[k]));
                }
                else
                {
                    Debug.LogWarning("[ProcGen] " + pName[k] + "_BL" + b + " overlaps — skipping.");
                    Debug.Log(string.Format("[ProcGen:AI] Branch {0} fin-left '{1}' skipped — overlaps existing geometry.", b, pName[k]));
                    roomsSkipped++;
                }
            }
            if (pp < poolSize && rng.NextDouble() > 0.4f)
            {
                int k = pidx[pp]; pp++;
                float cx = bCor2X[b] + HalfCor + pRW[k] / 2f;
                if (ProcTryRegister(bds, cx, bFinCZ[b], pRW[k] / 2f, pRD[k] / 2f, kPad, bFinCorBdsIdx[b]))
                {
                    bHR[b] = true; bRNm[b] = pName[k]; bRW[b] = pRW[k]; bRD[b] = pRD[k]; bRX[b] = cx; roomsPlaced++;
                    Debug.Log(string.Format("[ProcGen:AI] Branch {0} fin-right '{1}' placed.", b, pName[k]));
                }
                else
                {
                    Debug.LogWarning("[ProcGen] " + pName[k] + "_BR" + b + " overlaps — skipping.");
                    Debug.Log(string.Format("[ProcGen:AI] Branch {0} fin-right '{1}' skipped — overlaps existing geometry.", b, pName[k]));
                    roomsSkipped++;
                }
            }
        }

        // ════════════════════════════════════════════════════════
        //  PHASE 3 — BUILD GEOMETRY
        //  Every wall deletion, door cut, and door wall placement
        //  is gated behind the corresponding existence flag set in
        //  Phase 2.  Skipped rooms produce zero orphaned geometry.
        // ════════════════════════════════════════════════════════
        Debug.Log("[ProcGen:AI] <b>── PHASE 3: Building Geometry ──</b>");
        {
            int _zc3 = 0, _lc3 = 0, _sc3 = 0;
            for (int b = 0; b < branchCount; b++) { if (bPat[b] == 1) _zc3++; else if (bPat[b] == 2) _lc3++; else _sc3++; }
            Debug.Log(string.Format("[ProcGen:AI]   Branches: {0}×Z-shape | {1}×L-shape | {2}×straight  |  Eng-R={3} Eng-L={4}",
                _zc3, _lc3, _sc3, engHR ? "room" : "solid", engHL ? "room" : "solid"));
        }

        // --- Docking Bay ---
        var dockGen = AddRoom("DockingBay", 0f, dockZ, dockW, dockH, dockD);
        DeleteChildWall(dockGen, "Wall_Front");
        CutVentInDoorWallTop(
            AddDoorWall("Door_Dock_Front", 0f, dockFr - wallThickness / 2f, dockW, dockH).transform,
            dockH, dH, vY);

        // --- Spine corridors + optional side rooms ---
        var sCorGen = new ShipModuleGenerator[spineCount];
        var sLGen   = new ShipModuleGenerator[spineCount];
        var sRGen   = new ShipModuleGenerator[spineCount];

        for (int i = 0; i < spineCount; i++)
        {
            sCorGen[i] = AddCorridor("SpineCor_" + i, 0f, sCZ[i], corridorWidth, corridorHeight, sLen[i]);
            if (sHL[i])
            {
                sLGen[i] = AddRoom(sLNm[i] + "_L" + i, sLX[i], sCZ[i], sLW[i], roomHeight, sLD[i]);
                DeleteChildWall(sLGen[i], "Wall_Right");
                DeleteChildWall(sCorGen[i], "Wall_Left");
                CutWallForDoor(sCorGen[i], "Wall_Left", true, corridorWidth, corridorHeight, sLen[i], 0f, dW, dH);
                AddDoorWallSide("Door_" + sLNm[i] + "_" + i, -HalfCor, sCZ[i], sLW[i], roomHeight);
            }
            if (sHR[i])
            {
                sRGen[i] = AddRoom(sRNm[i] + "_R" + i, sRX[i], sCZ[i], sRW[i], roomHeight, sRD[i]);
                DeleteChildWall(sRGen[i], "Wall_Left");
                DeleteChildWall(sCorGen[i], "Wall_Right");
                CutWallForDoor(sCorGen[i], "Wall_Right", false, corridorWidth, corridorHeight, sLen[i], 0f, dW, dH);
                AddDoorWallSide("Door_" + sRNm[i] + "_" + i, HalfCor, sCZ[i], sRW[i], roomHeight);
            }
        }

        // --- Cargo Bay ---
        var cargoGen = AddRoom("CargoBay", 0f, cargoCZ, cargoW, cargoH, cargoD);
        DeleteChildWall(cargoGen, "Wall_Back");
        CutVentInDoorWallTop(
            AddDoorWall("Door_Cargo_Back", 0f, cargoBk + wallThickness / 2f, cargoW, cargoH).transform,
            cargoH, dH, vY);
        DeleteChildWall(cargoGen, "Wall_Front");
        CutVentInDoorWallTop(
            AddDoorWall("Door_Cargo_Front", 0f, cargoFrZ - wallThickness / 2f, cargoW, cargoH).transform,
            cargoH, dH, vY);

        // --- Engineering Hub ---
        var engGen = AddRoom("EngineeringHub", 0f, engCZ, engW, engH, engD);
        DeleteChildWall(engGen, "Wall_Back");
        CutVentInDoorWallTop(
            AddDoorWall("Door_Eng_Back", 0f, engBk + wallThickness / 2f, engW, engH).transform,
            engH, dH, vY);
        DeleteChildWall(engGen, "Wall_Front");
        // Dynamic front wall — adapts to any number of branch openings
        BuildEngFrontWallDynamic(engW, engH, engFr, (float[])bX.Clone(), corridorWidth, dH);

        // Engineering optional side rooms
        ShipModuleGenerator engReactGen = null, engLabGen = null;
        float engVTop = vY + ventH;
        if (engHR)
        {
            DeleteChildWall(engGen, "Wall_Right");
            CutVentInDoorWallTop(
                AddDoorWallSide("Door_Eng_R", engW / 2f - wallThickness / 2f, engCZ, engD, engH).transform,
                engH, dH, vY);
            engReactGen = AddRoom(reactNm + "_EngR", reactX, engCZ, reactW_, roomHeight, reactD_);
            DeleteChildWall(engReactGen, "Wall_Left");
        }
        else
        {
            // No reactor room — keep the original solid Wall_Right intact.
            // No vent branch passes through this wall so no vent-band gap is needed.
            // (The previous split-panel approach left a gap from vY to engVTop with no geometry.)
            Debug.Log("[ProcGen:AI]   Eng right wall: <b>no reactor — solid Wall_Right retained</b> (no vent gap needed).");
        }
        if (engHL)
        {
            DeleteChildWall(engGen, "Wall_Left");
            CutVentInDoorWallTop(
                AddDoorWallSide("Door_Eng_L", -(engW / 2f) + wallThickness / 2f, engCZ, engD, engH).transform,
                engH, dH, vY);
            engLabGen = AddRoom(labNm + "_EngL", labX, engCZ, labW_, roomHeight, labD_);
            DeleteChildWall(engLabGen, "Wall_Right");
        }
        else
        {
            // No lab room — keep the original solid Wall_Left intact.
            Debug.Log("[ProcGen:AI]   Eng left wall: <b>no lab — solid Wall_Left retained</b> (no vent gap needed).");
        }

        // --- Fork branches ---
        var termGen    = new ShipModuleGenerator[branchCount];
        var bFinCorGen = new ShipModuleGenerator[branchCount];
        var bLGen      = new ShipModuleGenerator[branchCount];
        var bRGen      = new ShipModuleGenerator[branchCount];

        for (int b = 0; b < branchCount; b++)
        {
            string bs     = "B" + b + "_";
            bool   goLeft = bSideDir[b] == -1;

            if (bPat[b] == 0)
            {
                // Straight branch: one corridor then optional terminal room
                float strZ = engFr + bStrLen[b] / 2f;
                AddCorridor(bs + "Str", bX[b], strZ, corridorWidth, corridorHeight, bStrLen[b]);

                if (bTermExists[b])
                {
                    termGen[b] = AddRoom(bTermNm[b], bTermX[b], bTermCZ[b], bTermW[b], roomHeight, bTermD[b]);
                    DeleteChildWall(termGen[b], "Wall_Back");
                    AddDoorWall("Door_" + bTermNm[b] + "_Bk",
                        bTermX[b], bTermBk[b] + wallThickness / 2f, bTermW[b], roomHeight);
                }
                else
                {
                    // Terminal room skipped — seal the corridor end with a wall cap
                    MakeBoxOnParent(transform, bs + "TermCap",
                        new Vector3(bTermX[b], corridorHeight / 2f, bTermBk[b] - wallThickness / 2f),
                        corridorWidth, corridorHeight, wallThickness);
                }
            }
            else if (bPat[b] == 1)
            {
                // Z-shaped branch: straight → corner1 → side corridor → corner2 → final corridor
                float strZ = engFr + bStrLen[b] / 2f;
                AddCorridor(bs + "Str", bX[b], strZ, corridorWidth, corridorHeight, bStrLen[b]);

                if (goLeft)
                    AddCorner(bs + "C1", bX[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth,
                        false, true, false, true);
                else
                    AddCorner(bs + "C1", bX[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth,
                        false, true, true, false);

                // Side corridor (rotated 90° — runs along world X)
                float sideCX = goLeft
                    ? bX[b] - HalfCor - bSideLen[b] / 2f
                    : bX[b] + HalfCor + bSideLen[b] / 2f;
                {
                    GameObject so = new GameObject(bs + "Side");
                    so.transform.SetParent(transform);
                    so.transform.localPosition = new Vector3(sideCX, 0f, bCor1Z[b]);
                    so.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    ShipModuleGenerator sg = so.AddComponent<ShipModuleGenerator>();
                    sg.moduleType = ShipModuleGenerator.ModuleType.Corridor;
                    sg.width = corridorWidth; sg.height = corridorHeight; sg.depth = bSideLen[b];
                    sg.wallThickness = wallThickness; sg.detailLevel = detailLevel;
                    sg.overrideMaterial = prototypeMaterial; sg.Generate();
                }

                if (goLeft)
                    AddCorner(bs + "C2", bCor2X[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth,
                        true, false, true, false);
                else
                    AddCorner(bs + "C2", bCor2X[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth,
                        true, false, false, true);

                bFinCorGen[b] = AddCorridor(bs + "Fin", bCor2X[b], bFinCZ[b],
                    corridorWidth, corridorHeight, bFinLen[b]);

                if (bHL[b])
                {
                    bLGen[b] = AddRoom(bLNm[b] + "_BL" + b, bLX[b], bFinCZ[b], bLW[b], roomHeight, bLD[b]);
                    DeleteChildWall(bLGen[b], "Wall_Right");
                    DeleteChildWall(bFinCorGen[b], "Wall_Left");
                    CutWallForDoor(bFinCorGen[b], "Wall_Left", true,
                        corridorWidth, corridorHeight, bFinLen[b], 0f, dW, dH);
                    AddDoorWallSide("Door_" + bLNm[b] + "_BL" + b,
                        bCor2X[b] - HalfCor, bFinCZ[b], bLW[b], roomHeight);
                }
                if (bHR[b])
                {
                    bRGen[b] = AddRoom(bRNm[b] + "_BR" + b, bRX[b], bFinCZ[b], bRW[b], roomHeight, bRD[b]);
                    DeleteChildWall(bRGen[b], "Wall_Left");
                    DeleteChildWall(bFinCorGen[b], "Wall_Right");
                    CutWallForDoor(bFinCorGen[b], "Wall_Right", false,
                        corridorWidth, corridorHeight, bFinLen[b], 0f, dW, dH);
                    AddDoorWallSide("Door_" + bRNm[b] + "_BR" + b,
                        bCor2X[b] + HalfCor, bFinCZ[b], bRW[b], roomHeight);
                }

                if (bTermExists[b])
                {
                    termGen[b] = AddRoom(bTermNm[b], bTermX[b], bTermCZ[b], bTermW[b], roomHeight, bTermD[b]);
                    DeleteChildWall(termGen[b], "Wall_Back");
                    AddDoorWall("Door_" + bTermNm[b] + "_Bk",
                        bTermX[b], bTermBk[b] + wallThickness / 2f, bTermW[b], roomHeight);
                }
                else
                {
                    MakeBoxOnParent(transform, bs + "TermCap",
                        new Vector3(bTermX[b], corridorHeight / 2f, bTermBk[b] - wallThickness / 2f),
                        corridorWidth, corridorHeight, wallThickness);
                    Debug.Log(string.Format("[ProcGen:AI]   Branch {0}: <color=#ffcc00>Z corridor capped</color> (no terminal room).", b));
                }
            }
            else // bPat[b] == 2: L-shaped branch (straight → C1 → side → C2 → terminal, no Fin corridor)
            {
                Debug.Log(string.Format("[ProcGen:AI]   Building branch {0}: <b>L-shape</b> strLen={1:F1}m sideLen={2:F1}m dir={3}.",
                    b, bStrLen[b], bSideLen[b], goLeft ? "left" : "right"));

                float strZ_L = engFr + bStrLen[b] / 2f;
                AddCorridor(bs + "Str", bX[b], strZ_L, corridorWidth, corridorHeight, bStrLen[b]);

                if (goLeft)
                    AddCorner(bs + "C1", bX[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth,
                        false, true, false, true);
                else
                    AddCorner(bs + "C1", bX[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth,
                        false, true, true, false);

                // Side corridor (rotated 90° — runs along world X)
                float sideCX_L = goLeft
                    ? bX[b] - HalfCor - bSideLen[b] / 2f
                    : bX[b] + HalfCor + bSideLen[b] / 2f;
                {
                    GameObject so_L = new GameObject(bs + "Side");
                    so_L.transform.SetParent(transform);
                    so_L.transform.localPosition = new Vector3(sideCX_L, 0f, bCor1Z[b]);
                    so_L.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    ShipModuleGenerator sg_L = so_L.AddComponent<ShipModuleGenerator>();
                    sg_L.moduleType = ShipModuleGenerator.ModuleType.Corridor;
                    sg_L.width = corridorWidth; sg_L.height = corridorHeight; sg_L.depth = bSideLen[b];
                    sg_L.wallThickness = wallThickness; sg_L.detailLevel = detailLevel;
                    sg_L.overrideMaterial = prototypeMaterial; sg_L.Generate();
                }

                if (goLeft)
                    AddCorner(bs + "C2", bCor2X[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth,
                        true, false, true, false);
                else
                    AddCorner(bs + "C2", bCor2X[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth,
                        true, false, false, true);

                // Terminal room connects directly after C2 (no final corridor)
                if (bTermExists[b])
                {
                    termGen[b] = AddRoom(bTermNm[b], bTermX[b], bTermCZ[b], bTermW[b], roomHeight, bTermD[b]);
                    DeleteChildWall(termGen[b], "Wall_Back");
                    AddDoorWall("Door_" + bTermNm[b] + "_Bk",
                        bTermX[b], bTermBk[b] + wallThickness / 2f, bTermW[b], roomHeight);
                }
                else
                {
                    MakeBoxOnParent(transform, bs + "TermCap",
                        new Vector3(bTermX[b], corridorHeight / 2f, bTermBk[b] - wallThickness / 2f),
                        corridorWidth, corridorHeight, wallThickness);
                    Debug.Log(string.Format("[ProcGen:AI]   Branch {0}: <color=#ffcc00>L corridor capped</color> (no terminal room).", b));
                }
            }
        }

        // ════════════════════════════════════════════════════════
        //  PHASE 4 — MAP-AWARE VENT NETWORK
        //  Junction types (Cross/Tee/none) are chosen based on
        //  which side rooms were actually placed.  Branches and
        //  drops are only generated for rooms that exist.
        //  Every dead end receives a VentCap — no open shafts.
        // ════════════════════════════════════════════════════════
        Debug.Log("[ProcGen:AI] <b>── PHASE 4: Vent Network ──</b>");

        // --- Dock: dead-end cap + entry elbow ---
        float dockCapZ = dockZ - dockD / 2f + 1f;
        ConnectVent("VS_DockCap", 0f, vY, dockCapZ, 0f, vY, dockZ - hvW); ventSegs++;
        AddVentCap("VentCap_Dock", 0f, vY, dockCapZ);
        AddVentElbow("VElbow_Dock", 0f, vY, dockZ, false, false, true, true);
        AddVentVertical("VDrop_Dock", 0f, dropY, dockZ, dropW, dropH, dropW);
        CutCeilingForVent(dockGen, dropW, dropW); ventCutsMade++;

        // --- Main spine trunk (DockingBay → Engineering front) ---
        float sv = dockZ + hvW; // leading edge of the next shaft segment

        for (int i = 0; i < spineCount; i++)
        {
            bool hasJ = sHL[i] || sHR[i];
            if (hasJ)
            {
                ConnectVent("VS_ToJ" + i, 0f, vY, sv, 0f, vY, sCZ[i] - hvW); ventSegs++;
                if      (sHL[i] && sHR[i]) AddVentCross("VJ_S" + i, 0f, vY, sCZ[i]);
                else if (sHL[i])           AddVentTee("VJ_S" + i, 0f, vY, sCZ[i], false, false, false, true);
                else                       AddVentTee("VJ_S" + i, 0f, vY, sCZ[i], false, false, true, false);
                sv = sCZ[i] + hvW;

                if (sHL[i])
                {
                    ConnectVent("VB_SL" + i, -hvW, vY, sCZ[i], sLX[i] + hvW, vY, sCZ[i]); ventSegs++;
                    AddVentElbow("VElbow_SL" + i, sLX[i], vY, sCZ[i], true, true, true, false);
                    AddVentVertical("VDrop_SL" + i, sLX[i], dropY, sCZ[i], dropW, dropH, dropW);
                    CutCeilingForVent(sLGen[i], dropW, dropW); ventCutsMade++;
                }
                if (sHR[i])
                {
                    ConnectVent("VB_SR" + i, hvW, vY, sCZ[i], sRX[i] - hvW, vY, sCZ[i]); ventSegs++;
                    AddVentElbow("VElbow_SR" + i, sRX[i], vY, sCZ[i], true, true, false, true);
                    AddVentVertical("VDrop_SR" + i, sRX[i], dropY, sCZ[i], dropW, dropH, dropW);
                    CutCeilingForVent(sRGen[i], dropW, dropW); ventCutsMade++;
                }
            }

            // After this corridor, does CargoBay follow?
            if (i == cargoAfterIdx - 1)
            {
                ConnectVent("VS_ThrCargo" + i, 0f, vY, sv, 0f, vY, cargoFrZ); ventSegs++;
                sv = cargoFrZ;
            }
        }

        // --- Engineering center junction (reactor / lab side rooms) ---
        bool needEngJ = engHR || engHL;
        if (needEngJ)
        {
            ConnectVent("VS_ToEngC", 0f, vY, sv, 0f, vY, engCZ - hvW); ventSegs++;
            if      (engHR && engHL) AddVentCross("VJ_EngC", 0f, vY, engCZ);
            else if (engHL)          AddVentTee("VJ_EngC", 0f, vY, engCZ, false, false, false, true);
            else                     AddVentTee("VJ_EngC", 0f, vY, engCZ, false, false, true, false);
            sv = engCZ + hvW;

            if (engHR)
            {
                ConnectVent("VB_React", hvW, vY, engCZ, reactX - hvW, vY, engCZ); ventSegs++;
                AddVentElbow("VElbow_React", reactX, vY, engCZ, true, true, false, true);
                AddVentVertical("VDrop_React", reactX, dropY, engCZ, dropW, dropH, dropW);
                CutCeilingForVent(engReactGen, dropW, dropW); ventCutsMade++;
            }
            if (engHL)
            {
                ConnectVent("VB_Lab", -hvW, vY, engCZ, labX + hvW, vY, engCZ); ventSegs++;
                AddVentElbow("VElbow_Lab", labX, vY, engCZ, true, true, true, false);
                AddVentVertical("VDrop_Lab", labX, dropY, engCZ, dropW, dropH, dropW);
                CutCeilingForVent(engLabGen, dropW, dropW); ventCutsMade++;
            }
        }

        // --- Engineering front junction → branch lateral shafts ---
        bool centerBranch = branchCount == 1 && Mathf.Abs(bX[0]) < 0.1f;
        float engFrEnd = centerBranch ? engFr + hvW : engFr - hvW;
        ConnectVent("VS_ToEngFr", 0f, vY, sv, 0f, vY, engFrEnd); ventSegs++;

        if (!centerBranch)
        {
            bool openL = false, openR = false, openF = false;
            for (int b = 0; b < branchCount; b++)
            {
                if      (bX[b] < -0.1f) openL = true;
                else if (bX[b] >  0.1f) openR = true;
                else                    openF = true;
            }

            if      (openL && openR && openF) AddVentCross("VJ_EngFr", 0f, vY, engFr);
            else if (openL && openR)          AddVentTee("VJ_EngFr", 0f, vY, engFr, false, true, false, false);
            else if (openL)                   AddVentTee("VJ_EngFr", 0f, vY, engFr, false, true, false, true);
            else if (openR)                   AddVentTee("VJ_EngFr", 0f, vY, engFr, false, true, true, false);
            else                              AddVentTee("VJ_EngFr", 0f, vY, engFr, false, false, true, true);

            for (int b = 0; b < branchCount; b++)
            {
                if (Mathf.Abs(bX[b]) < 0.1f) continue;
                if (bX[b] < 0f)
                {
                    ConnectVent("VL_EngToB" + b, -hvW, vY, engFr, bX[b] + hvW, vY, engFr); ventSegs++;
                    AddVentTee("VJ_B" + b, bX[b], vY, engFr, true, false, true, false);
                }
                else
                {
                    ConnectVent("VR_EngToB" + b, hvW, vY, engFr, bX[b] - hvW, vY, engFr); ventSegs++;
                    AddVentTee("VJ_B" + b, bX[b], vY, engFr, true, false, false, true);
                }
            }
        }

        // --- Per-branch vent shafts ---
        for (int b = 0; b < branchCount; b++)
        {
            float bvStart      = engFr + hvW;
            float termDeep     = bTermBk[b] + bTermD[b] - 1f;
            float termVentEndZ = bTermExists[b] ? bTermCZ[b] - hvW : bTermBk[b] - hvW;

            if (bPat[b] == 0)
            {
                // Straight branch shaft
                ConnectVent("VB" + b + "_Str", bTermX[b], vY, bvStart, bTermX[b], vY, termVentEndZ); ventSegs++;
                if (bTermExists[b])
                {
                    AddVentElbow("VElbow_Term" + b, bTermX[b], vY, bTermCZ[b], false, false, true, true);
                    ConnectVent("VB" + b + "_Run", bTermX[b], vY, bTermCZ[b] + hvW, bTermX[b], vY, termDeep); ventSegs++;
                    AddVentCap("VentCap_Term" + b, bTermX[b], vY, termDeep);
                    AddVentVertical("VDrop_Term" + b, bTermX[b], dropY, bTermCZ[b], dropW, dropH, dropW);
                    CutCeilingForVent(termGen[b], dropW, dropW); ventCutsMade++;
                }
                else
                    AddVentCap("VentCap_Term" + b, bTermX[b], vY, termVentEndZ);
            }
            else if (bPat[b] == 1)
            {
                // Z-shaped branch shaft
                bool goLeft = bSideDir[b] == -1;

                ConnectVent("VB" + b + "_Str", bX[b], vY, bvStart, bX[b], vY, bCor1Z[b] - hvW); ventSegs++;

                if (goLeft)
                    AddVentCorner("VJ_B" + b + "_C1", bX[b], vY, bCor1Z[b], false, true, false, true);
                else
                    AddVentCorner("VJ_B" + b + "_C1", bX[b], vY, bCor1Z[b], false, true, true, false);

                if (goLeft)
                {
                    ConnectVent("VB" + b + "_Side", bX[b] - hvW, vY, bCor1Z[b], bCor2X[b] + hvW, vY, bCor1Z[b]); ventSegs++;
                    AddVentCorner("VJ_B" + b + "_C2", bCor2X[b], vY, bCor1Z[b], true, false, true, false);
                }
                else
                {
                    ConnectVent("VB" + b + "_Side", bX[b] + hvW, vY, bCor1Z[b], bCor2X[b] - hvW, vY, bCor1Z[b]); ventSegs++;
                    AddVentCorner("VJ_B" + b + "_C2", bCor2X[b], vY, bCor1Z[b], true, false, false, true);
                }

                float finSv = bCor1Z[b] + hvW;
                bool  hasFJ = bHL[b] || bHR[b];
                if (hasFJ)
                {
                    ConnectVent("VB" + b + "_Fin1", bCor2X[b], vY, finSv, bCor2X[b], vY, bFinCZ[b] - hvW); ventSegs++;
                    if      (bHL[b] && bHR[b]) AddVentCross("VJ_BFin" + b, bCor2X[b], vY, bFinCZ[b]);
                    else if (bHL[b])            AddVentTee("VJ_BFin" + b, bCor2X[b], vY, bFinCZ[b], false, false, false, true);
                    else                        AddVentTee("VJ_BFin" + b, bCor2X[b], vY, bFinCZ[b], false, false, true, false);

                    if (bHL[b])
                    {
                        ConnectVent("VBL" + b, bCor2X[b] - hvW, vY, bFinCZ[b], bLX[b] + hvW, vY, bFinCZ[b]); ventSegs++;
                        AddVentElbow("VElbow_BL" + b, bLX[b], vY, bFinCZ[b], true, true, true, false);
                        AddVentVertical("VDrop_BL" + b, bLX[b], dropY, bFinCZ[b], dropW, dropH, dropW);
                        CutCeilingForVent(bLGen[b], dropW, dropW); ventCutsMade++;
                    }
                    if (bHR[b])
                    {
                        ConnectVent("VBR" + b, bCor2X[b] + hvW, vY, bFinCZ[b], bRX[b] - hvW, vY, bFinCZ[b]); ventSegs++;
                        AddVentElbow("VElbow_BR" + b, bRX[b], vY, bFinCZ[b], true, true, false, true);
                        AddVentVertical("VDrop_BR" + b, bRX[b], dropY, bFinCZ[b], dropW, dropH, dropW);
                        CutCeilingForVent(bRGen[b], dropW, dropW); ventCutsMade++;
                    }
                    ConnectVent("VB" + b + "_Fin2", bCor2X[b], vY, bFinCZ[b] + hvW, bCor2X[b], vY, termVentEndZ); ventSegs++;
                }
                else
                {
                    ConnectVent("VB" + b + "_Fin1", bCor2X[b], vY, finSv, bCor2X[b], vY, termVentEndZ); ventSegs++;
                }

                if (bTermExists[b])
                {
                    AddVentElbow("VElbow_Term" + b, bCor2X[b], vY, bTermCZ[b], false, false, true, true);
                    ConnectVent("VB" + b + "_Run", bCor2X[b], vY, bTermCZ[b] + hvW, bCor2X[b], vY, termDeep); ventSegs++;
                    AddVentCap("VentCap_Term" + b, bCor2X[b], vY, termDeep);
                    AddVentVertical("VDrop_Term" + b, bCor2X[b], dropY, bTermCZ[b], dropW, dropH, dropW);
                    CutCeilingForVent(termGen[b], dropW, dropW); ventCutsMade++;
                }
                else
                    AddVentCap("VentCap_Term" + b, bCor2X[b], vY, termVentEndZ);
            }
            else // bPat[b] == 2: L-shaped branch shaft
            {
                bool goLeft_v = bSideDir[b] == -1;

                // Straight segment up to C1 corner
                ConnectVent("VB" + b + "_Str", bX[b], vY, bvStart, bX[b], vY, bCor1Z[b] - hvW); ventSegs++;

                // C1: turn from Z into X direction
                if (goLeft_v)
                    AddVentCorner("VJ_B" + b + "_C1", bX[b], vY, bCor1Z[b], false, true, false, true);
                else
                    AddVentCorner("VJ_B" + b + "_C1", bX[b], vY, bCor1Z[b], false, true, true, false);

                // Side shaft (runs in X)
                if (goLeft_v)
                {
                    ConnectVent("VB" + b + "_Side", bX[b] - hvW, vY, bCor1Z[b], bCor2X[b] + hvW, vY, bCor1Z[b]); ventSegs++;
                    AddVentCorner("VJ_B" + b + "_C2", bCor2X[b], vY, bCor1Z[b], true, false, true, false);
                }
                else
                {
                    ConnectVent("VB" + b + "_Side", bX[b] + hvW, vY, bCor1Z[b], bCor2X[b] - hvW, vY, bCor1Z[b]); ventSegs++;
                    AddVentCorner("VJ_B" + b + "_C2", bCor2X[b], vY, bCor1Z[b], true, false, false, true);
                }

                // Forward from C2 toward terminal room (no Fin corridor — straight to terminal)
                float lFinSv = bCor1Z[b] + hvW;
                if (bTermExists[b])
                {
                    ConnectVent("VB" + b + "_Fin1", bCor2X[b], vY, lFinSv, bCor2X[b], vY, bTermCZ[b] - hvW); ventSegs++;
                    AddVentElbow("VElbow_Term" + b, bCor2X[b], vY, bTermCZ[b], false, false, true, true);
                    float termDeep_L = bTermBk[b] + bTermD[b] - 1f;
                    ConnectVent("VB" + b + "_Run", bCor2X[b], vY, bTermCZ[b] + hvW, bCor2X[b], vY, termDeep_L); ventSegs++;
                    AddVentCap("VentCap_Term" + b, bCor2X[b], vY, termDeep_L);
                    AddVentVertical("VDrop_Term" + b, bCor2X[b], dropY, bTermCZ[b], dropW, dropH, dropW);
                    CutCeilingForVent(termGen[b], dropW, dropW); ventCutsMade++;
                }
                else
                {
                    float lCapZ = bTermBk[b] - hvW;
                    ConnectVent("VB" + b + "_Fin1", bCor2X[b], vY, lFinSv, bCor2X[b], vY, lCapZ); ventSegs++;
                    AddVentCap("VentCap_Term" + b, bCor2X[b], vY, lCapZ);
                }
            }
        }

        // ════════════════════════════════════════════════════════
        //  PHASE 5 — PROP SCATTERING
        //  Props are only placed in rooms that were built.
        // ════════════════════════════════════════════════════════
        Debug.Log("[ProcGen:AI] <b>── PHASE 5: Prop Scattering ──</b>");

        // Dock
        AddProp("DockCrate_1",
            RngRange(rng, -dockW / 2f + 1f, -1f), 0f,
            RngRange(rng, dockZ - dockD / 2f + 1f, dockZ),
            ShipModuleGenerator.ModuleType.Crate,
            RngRange(rng, 0.8f, 1.4f), RngRange(rng, 0.6f, 1.1f), RngRange(rng, 0.8f, 1.3f));
        AddProp("DockCrate_2",
            RngRange(rng, 1f, dockW / 2f - 1f), 0f,
            RngRange(rng, dockZ - dockD / 2f + 1f, dockZ + dockD / 2f - 1f),
            ShipModuleGenerator.ModuleType.Crate,
            RngRange(rng, 0.9f, 1.5f), RngRange(rng, 0.7f, 1.2f), RngRange(rng, 0.9f, 1.4f));
        AddProp("DockCrate_3",
            RngRange(rng, -dockW / 2f + 1f, 1f), 0f,
            RngRange(rng, dockZ, dockZ + dockD / 2f - 1f),
            ShipModuleGenerator.ModuleType.Crate,
            RngRange(rng, 0.7f, 1.0f), RngRange(rng, 0.5f, 0.9f), RngRange(rng, 0.7f, 1.0f));

        // Cargo
        AddProp("CargoCrate_1",
            RngRange(rng, -cargoW / 2f + 1f, -0.5f), 0f,
            RngRange(rng, cargoCZ - cargoD / 2f + 1f, cargoCZ),
            ShipModuleGenerator.ModuleType.Crate,
            RngRange(rng, 1.2f, 1.8f), RngRange(rng, 1.0f, 1.5f), RngRange(rng, 1.2f, 1.6f));
        AddProp("CargoCrate_2",
            RngRange(rng, 0.5f, cargoW / 2f - 1f), 0f,
            RngRange(rng, cargoCZ, cargoCZ + cargoD / 2f - 1f),
            ShipModuleGenerator.ModuleType.Crate,
            RngRange(rng, 0.9f, 1.4f), RngRange(rng, 0.7f, 1.1f), RngRange(rng, 0.9f, 1.3f));

        // Spine side rooms
        for (int i = 0; i < spineCount; i++)
        {
            if (sHL[i])
                AddProp("SL" + i + "_Crate",
                    sLX[i] + RngRange(rng, -sLW[i] / 2f + 0.8f, sLW[i] / 2f - 0.8f), 0f,
                    sCZ[i] + RngRange(rng, -sLD[i] / 2f + 0.8f, sLD[i] / 2f - 0.8f),
                    ShipModuleGenerator.ModuleType.Crate,
                    RngRange(rng, 0.8f, 1.2f), RngRange(rng, 0.6f, 1.0f), RngRange(rng, 0.8f, 1.2f));
            if (sHR[i])
                AddProp("SR" + i + "_Console", sRX[i], 0f, sCZ[i],
                    ShipModuleGenerator.ModuleType.Console,
                    RngRange(rng, 1.0f, 1.4f), RngRange(rng, 0.8f, 1.0f), 0.4f);
        }

        // Engineering
        AddProp("Eng_Console_L",
            RngRange(rng, -engW / 2f + 1f, -1f), 0f, engCZ + RngRange(rng, -2f, 0f),
            ShipModuleGenerator.ModuleType.Console,
            RngRange(rng, 1.3f, 1.8f), RngRange(rng, 0.9f, 1.1f), 0.5f);
        AddProp("Eng_Console_R",
            RngRange(rng, 1f, engW / 2f - 1f), 0f, engCZ + RngRange(rng, -2f, 0f),
            ShipModuleGenerator.ModuleType.Console,
            RngRange(rng, 1.3f, 1.8f), RngRange(rng, 0.9f, 1.1f), 0.5f);
        AddProp("Eng_Console_C", 0f, 0f, engCZ + RngRange(rng, 0f, 2f),
            ShipModuleGenerator.ModuleType.Console,
            RngRange(rng, 1.8f, 2.4f), RngRange(rng, 1.0f, 1.2f), 0.6f);
        float epx = RngRange(rng, -engW / 2f + 1.5f, -2f);
        float epz = engCZ + RngRange(rng, -engD / 2f + 1.5f, 0f);
        AddProp("Eng_Pillar1",  epx, 0f, epz,                         ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Eng_Pillar2", -epx, 0f, epz,                         ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Eng_Pillar3",  epx, 0f, epz + RngRange(rng, 3f, 5f), ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Eng_Pillar4", -epx, 0f, epz + RngRange(rng, 3f, 5f), ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        if (engHR)
        {
            AddProp("React_Console", reactX, 0f, engCZ,
                ShipModuleGenerator.ModuleType.Console,
                RngRange(rng, 1.0f, 1.4f), RngRange(rng, 1.0f, 1.3f), 0.5f);
            AddProp("React_Pillar", reactX, 0f, engCZ + RngRange(rng, -1.5f, 1.5f),
                ShipModuleGenerator.ModuleType.Pillar, 0.5f, roomHeight, 0.5f);
        }
        if (engHL)
        {
            AddProp("Lab_Console1",
                labX + RngRange(rng, -labW_ / 2f + 1f, labW_ / 2f - 1f), 0f,
                engCZ + RngRange(rng, -1f, 1f),
                ShipModuleGenerator.ModuleType.Console,
                RngRange(rng, 1.2f, 1.6f), RngRange(rng, 0.9f, 1.1f), 0.5f);
            AddProp("Lab_Console2",
                labX + RngRange(rng, -labW_ / 2f + 1f, labW_ / 2f - 1f), 0f,
                engCZ + RngRange(rng, -1f, 1f),
                ShipModuleGenerator.ModuleType.Console,
                RngRange(rng, 1.2f, 1.6f), RngRange(rng, 0.9f, 1.1f), 0.5f);
        }

        // Terminal rooms and branch side rooms
        for (int b = 0; b < branchCount; b++)
        {
            if (bTermExists[b])
            {
                if (bTermNm[b] == "Bridge")
                {
                    AddProp("Bridge_Console_M",
                        bTermX[b], 0f, bTermCZ[b] + RngRange(rng, 0.5f, 2f),
                        ShipModuleGenerator.ModuleType.Console,
                        RngRange(rng, 2.5f, 3.5f), RngRange(rng, 0.9f, 1.1f), 0.7f);
                    AddProp("Bridge_Console_L",
                        bTermX[b] - RngRange(rng, 1.5f, 3f), 0f, bTermCZ[b],
                        ShipModuleGenerator.ModuleType.Console,
                        RngRange(rng, 1.2f, 1.8f), RngRange(rng, 0.9f, 1.1f), 0.5f);
                    AddProp("Bridge_Console_R",
                        bTermX[b] + RngRange(rng, 1.5f, 3f), 0f, bTermCZ[b],
                        ShipModuleGenerator.ModuleType.Console,
                        RngRange(rng, 1.2f, 1.8f), RngRange(rng, 0.9f, 1.1f), 0.5f);
                    AddProp("Bridge_Pillar_L",
                        bTermX[b] - RngRange(rng, 2.5f, 4f), 0f, bTermCZ[b] - 1f,
                        ShipModuleGenerator.ModuleType.Pillar, 0.3f, roomHeight, 0.3f);
                    AddProp("Bridge_Pillar_R",
                        bTermX[b] + RngRange(rng, 2.5f, 4f), 0f, bTermCZ[b] - 1f,
                        ShipModuleGenerator.ModuleType.Pillar, 0.3f, roomHeight, 0.3f);
                }
                else
                {
                    AddProp("Term" + b + "_Table1",
                        bTermX[b] + RngRange(rng, -bTermW[b] / 2f + 1f, -0.5f), 0f,
                        bTermCZ[b] + RngRange(rng, -bTermD[b] / 2f + 1f, bTermD[b] / 2f - 1f),
                        ShipModuleGenerator.ModuleType.Crate,
                        RngRange(rng, 1.5f, 2.5f), 0.75f, RngRange(rng, 0.8f, 1.2f));
                    AddProp("Term" + b + "_Table2",
                        bTermX[b] + RngRange(rng, 0.5f, bTermW[b] / 2f - 1f), 0f,
                        bTermCZ[b] + RngRange(rng, -bTermD[b] / 2f + 1f, bTermD[b] / 2f - 1f),
                        ShipModuleGenerator.ModuleType.Crate,
                        RngRange(rng, 1.5f, 2.5f), 0.75f, RngRange(rng, 0.8f, 1.2f));
                }
            }

            if (bPat[b] == 1)
            {
                if (bHL[b])
                    AddProp("BL" + b + "_Crate",
                        bLX[b] + RngRange(rng, -bLW[b] / 2f + 0.8f, bLW[b] / 2f - 0.8f), 0f,
                        bFinCZ[b] + RngRange(rng, -bLD[b] / 2f + 0.8f, bLD[b] / 2f - 0.8f),
                        ShipModuleGenerator.ModuleType.Crate,
                        RngRange(rng, 0.8f, 1.2f), RngRange(rng, 0.6f, 1.0f), RngRange(rng, 0.8f, 1.2f));
                if (bHR[b])
                    AddProp("BR" + b + "_Console", bRX[b], 0f, bFinCZ[b],
                        ShipModuleGenerator.ModuleType.Console,
                        RngRange(rng, 1.0f, 1.4f), RngRange(rng, 0.8f, 1.0f), 0.4f);
            }
        }

        // ════════════════════════════════════════════════════════
        //  PHASE 6 — FINAL VALIDATION
        //  Walk every Room module.  If a room has no ceiling child
        //  (can happen if CutCeilingForVent ran on an unregistered
        //  room, or due to floating-point edge cases), add a
        //  fallback seal so the room is properly enclosed.
        // ════════════════════════════════════════════════════════
        Debug.Log("[ProcGen:AI] <b>── PHASE 6: Final Validation ──</b>");
        int ceilFixes = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            ShipModuleGenerator mg = child.GetComponent<ShipModuleGenerator>();
            if (mg == null || mg.moduleType != ShipModuleGenerator.ModuleType.Room) continue;

            bool hasCeiling = false;
            for (int j = 0; j < child.childCount; j++)
            {
                string cn = child.GetChild(j).name;
                if (cn == "Ceiling" || cn.StartsWith("Ceiling_")) { hasCeiling = true; break; }
            }

            if (!hasCeiling)
            {
                ceilFixes++;
                Debug.LogWarning("[ProcGen] Room '" + child.name + "' missing ceiling — adding fallback seal.");
                MakeBoxOnParent(child, "Ceiling",
                    new Vector3(0f, mg.height - wallThickness / 2f, 0f),
                    mg.width, wallThickness, mg.depth);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  PHASE 6b — OVERLAP & GAP DIAGNOSTICS
        //  Scans all generated geometry for intersections and missing
        //  wall coverage.  Reports issues as Debug.LogWarning/LogError
        //  so they appear in the Unity console without breaking generation.
        // ════════════════════════════════════════════════════════════════

        // --- Overlap Detection (cross-module only) ---
        //  Compute a combined bounding box per top-level module child, then
        //  compare module pairs.  Internal pieces (vents, doors, props, eng
        //  wall segments) that intentionally penetrate adjacent modules are
        //  excluded via IsInternalPiece.  Only Room-vs-Room or Room-vs-Corridor
        //  overlaps above 0.5 m³ are flagged as real issues.
        bool IsInternalPiece(string n)
        {
            // Vent shaft pieces — run through rooms/corridors at ceiling height by design
            if (n.StartsWith("VS_") || n.StartsWith("VB_") || n.StartsWith("VJ_") ||
                n.StartsWith("VDrop_") || n.StartsWith("VElbow_") || n.StartsWith("VentCap_") ||
                n.StartsWith("VR_") || n.StartsWith("VL_"))
                return true;
            // Doors — placed at the boundary between two modules
            if (n.StartsWith("Door_"))
                return true;
            // Props (crates, consoles, pillars, furniture) placed inside rooms
            if (n.Contains("Crate") || n.Contains("Console") || n.Contains("Pillar") ||
                n.Contains("Table") || n.Contains("Bed"))
                return true;
            // Engineering wall segments — overlap the engineering room AABB by design
            if (n.StartsWith("EngFWD_") || n.StartsWith("EngFW_") || n.StartsWith("EngHub_") ||
                n.StartsWith("EngTr_"))
                return true;
            // Corridor end caps
            if (n.Contains("TermCap") || n.Contains("CorCap"))
                return true;
            return false;
        }

        var modBounds = new System.Collections.Generic.List<(string name, Bounds bounds)>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Renderer[] rends = child.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) continue;
            Bounds combined = rends[0].bounds;
            for (int r = 1; r < rends.Length; r++) combined.Encapsulate(rends[r].bounds);
            modBounds.Add((child.name, combined));
        }

        int overlapCount = 0;
        for (int i = 0; i < modBounds.Count; i++)
        {
            for (int j = i + 1; j < modBounds.Count; j++)
            {
                // Skip any pair where either object is a vent, door, prop, or eng wall piece —
                // these intentionally intersect adjacent modules and are never true overlaps.
                if (IsInternalPiece(modBounds[i].name) || IsInternalPiece(modBounds[j].name))
                    continue;
                if (!modBounds[i].bounds.Intersects(modBounds[j].bounds)) continue;
                Bounds bA = modBounds[i].bounds, bB = modBounds[j].bounds;
                float ox = Mathf.Max(0, Mathf.Min(bA.max.x, bB.max.x) - Mathf.Max(bA.min.x, bB.min.x));
                float oy = Mathf.Max(0, Mathf.Min(bA.max.y, bB.max.y) - Mathf.Max(bA.min.y, bB.min.y));
                float oz = Mathf.Max(0, Mathf.Min(bA.max.z, bB.max.z) - Mathf.Max(bA.min.z, bB.min.z));
                float vol = ox * oy * oz;
                // 0.5 m³ threshold: adjacent modules (room touching a corridor) produce
                // near-zero AABB overlap because rooms are placed at x = ±(HalfCor + W/2).
                // A genuine cross-module penetration produces much larger volumes.
                if (vol > 0.5f)
                {
                    overlapCount++;
                    Debug.LogWarning(string.Format(
                        "[ProcGen:Diag] OVERLAP: '{0}' and '{1}' intersect (volume={2:F2}m³)",
                        modBounds[i].name, modBounds[j].name, vol));
                }
            }
        }

        // --- Gap Detection (room wall coverage) ---
        int gapCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            ShipModuleGenerator roomModule = child.GetComponent<ShipModuleGenerator>();
            if (roomModule == null || roomModule.moduleType != ShipModuleGenerator.ModuleType.Room) continue;

            string roomPrefix = child.name.Split('_')[0];
            string[] faceNames = { "Wall_Back", "Wall_Front", "Wall_Left", "Wall_Right", "Floor", "Ceiling" };
            foreach (string face in faceNames)
            {
                bool found = false;
                for (int j = 0; j < child.childCount; j++)
                {
                    string cn = child.GetChild(j).name;
                    if (cn == face || cn.StartsWith(face + "_")) { found = true; break; }
                }
                if (!found)
                {
                    bool hasDoorReplacement = false;
                    for (int j = 0; j < transform.childCount; j++)
                    {
                        string pn = transform.GetChild(j).name;

                        // Standard door name match (e.g., Door_MessHall_0 contains room prefix)
                        if (pn.Contains("Door_") && pn.Contains(roomPrefix))
                        { hasDoorReplacement = true; break; }

                        // Handle abbreviated room names in door names:
                        //   DockingBay → Door_Dock_*,  CargoBay → Door_Cargo_*,
                        //   EngineeringHub → Door_Eng_*
                        if (pn.StartsWith("Door_"))
                        {
                            string[] parts = pn.Split('_');
                            if (parts.Length >= 2 && roomPrefix.StartsWith(parts[1]))
                            { hasDoorReplacement = true; break; }
                        }

                        // Engineering hub front wall is replaced by EngFWD_* pieces,
                        // not by a Door_* object.
                        if (roomPrefix == "EngineeringHub" &&
                            (pn.StartsWith("EngFWD_") || pn.StartsWith("EngFW_") ||
                             pn.StartsWith("EngHub_WR_") || pn.StartsWith("EngHub_WL_")))
                        { hasDoorReplacement = true; break; }

                        // Rooms placed as engineering sub-rooms (_EngR / _EngL suffix) have
                        // their inner wall deleted and replaced by the shared Door_Eng_R/L
                        // door wall — which contains "Eng" but not the room prefix.
                        if ((child.name.EndsWith("_EngR") || child.name.EndsWith("_EngL")) &&
                            pn.StartsWith("Door_Eng_"))
                        { hasDoorReplacement = true; break; }
                    }
                    if (!hasDoorReplacement)
                    {
                        gapCount++;
                        Debug.LogWarning(string.Format(
                            "[ProcGen:Diag] GAP: Room '{0}' is missing '{1}' with no door replacement.",
                            child.name, face));
                    }
                }
            }
        }

        // --- Corridor-vs-corridor intersection check ---
        int corridorOverlaps = 0;
        var corridorBounds = new System.Collections.Generic.List<(string name, Bounds bounds)>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            ShipModuleGenerator corridorModule = child.GetComponent<ShipModuleGenerator>();
            if (corridorModule != null && corridorModule.moduleType == ShipModuleGenerator.ModuleType.Corridor)
            {
                Renderer rend = child.GetComponentInChildren<Renderer>();
                if (rend != null)
                    corridorBounds.Add((child.name, rend.bounds));
            }
        }
        for (int i = 0; i < corridorBounds.Count; i++)
        {
            for (int j = i + 1; j < corridorBounds.Count; j++)
            {
                if (corridorBounds[i].bounds.Intersects(corridorBounds[j].bounds))
                {
                    Bounds boundsA = corridorBounds[i].bounds;
                    Bounds boundsB = corridorBounds[j].bounds;
                    float ox = Mathf.Max(0, Mathf.Min(boundsA.max.x, boundsB.max.x) - Mathf.Max(boundsA.min.x, boundsB.min.x));
                    float oz = Mathf.Max(0, Mathf.Min(boundsA.max.z, boundsB.max.z) - Mathf.Max(boundsA.min.z, boundsB.min.z));
                    float area = ox * oz;
                    if (area > 0.5f)
                    {
                        corridorOverlaps++;
                        Debug.LogError(string.Format(
                            "[ProcGen:Diag] CORRIDOR OVERLAP: '{0}' and '{1}' share {2:F1}m² of floor area!",
                            corridorBounds[i].name, corridorBounds[j].name, area));
                    }
                }
            }
        }

        if (overlapCount > 0 || gapCount > 0 || corridorOverlaps > 0)
        {
            Debug.LogError(string.Format(
                "[ProcGen:Diag] <color=#ff4444><b>✗ LAYOUT ISSUES DETECTED</b></color> — {0} overlaps | {1} gaps | {2} corridor intersections.",
                overlapCount, gapCount, corridorOverlaps));
            if (corridorOverlaps > 0)
                Debug.LogError("[ProcGen:Diag]   <b>⚠ CORRIDOR INTERSECTIONS FOUND</b> — this will trigger auto-retry.");
        }
        else
            Debug.Log("[ProcGen:Diag] <color=#00ff88><b>✓ Validation passed</b></color> — no overlaps or gaps detected.");

        // ════════════════════════════════════════════════════════════════
        //  AUTO-RETRY — if the layout is too broken (no rooms placed, or
        //  far too many skipped, or real corridor intersections detected),
        //  clear the generated geometry, mutate the seed and try again.
        //  Players cannot change the seed at runtime, so the generator
        //  must self-correct.  Cap at 5 retries to avoid infinite loops.
        // ════════════════════════════════════════════════════════════════
        bool tooBroken = (roomsPlaced == 0) ||
                         (roomsSkipped > roomsPlaced * 2 && roomsSkipped > 3) ||
                         (corridorOverlaps > 0);
        const int maxRetries = 5;
        if (tooBroken && _procRetries < maxRetries)
        {
            Debug.LogWarning(string.Format(
                "[ProcGen] Layout quality too low ({0} rooms placed, {1} skipped). Auto-retrying with mutated seed...",
                roomsPlaced, roomsSkipped));
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
                else DestroyImmediate(transform.GetChild(i).gameObject);
            }
            _procRetries++;
            GenerateProceduralLayout();
            return;
        }
        if (tooBroken)
            Debug.LogWarning("[ProcGen] Exhausted " + maxRetries + " retries. Using last generated layout.");
        _procRetries = 0;

        // Tally branch patterns for stats and summary
        int _finalZC = 0, _finalLC = 0, _finalSC = 0, _termCapped = 0;
        for (int b = 0; b < branchCount; b++)
        {
            if (bPat[b] == 1) _finalZC++;
            else if (bPat[b] == 2) _finalLC++;
            else _finalSC++;
            if (!bTermExists[b]) _termCapped++;
        }

        // Write last-generation statistics for ShipLayoutTrainer / ShipLayoutScorer
        LastRoomsPlaced      = roomsPlaced;
        LastRoomsSkipped     = roomsSkipped;
        LastOverlapCount     = overlapCount;
        LastGapCount         = gapCount;
        LastCorridorOverlaps = corridorOverlaps;
        LastZShapeCount      = _finalZC;
        LastLShapeCount      = _finalLC;
        LastStraightCount    = _finalSC;
        LastBranchCount      = branchCount;
        LastVentCutsMade     = ventCutsMade;
        LastTerminalsCapped  = _termCapped;
        LastActualSeed       = actualSeed;

        string _sep2 = new string('─', 52);
        Debug.Log("[ProcGen:AI] " + _sep2);
        Debug.Log(string.Format(
            "[ProcGen:AI] <b>GENERATION COMPLETE</b>  Seed={0}{1}",
            actualSeed, _procRetries > 0 ? "  (retry " + _procRetries + ")" : ""));
        Debug.Log(string.Format(
            "[ProcGen:AI]   Spine  : {0} corridors | CargoBay after cor{1}",
            spineCount, cargoAfterIdx - 1));
        Debug.Log(string.Format(
            "[ProcGen:AI]   Rooms  : <color=#00ff88>{0} placed</color> / <color=#ff4444>{1} skipped</color>",
            roomsPlaced, roomsSkipped));
        Debug.Log(string.Format(
            "[ProcGen:AI]   Branches ({0}): <color=#00ff88>{1}×Z-shape</color> | <color=#ffcc00>{2}×L-shape</color> | <color=#ff9988>{3}×straight</color> | {4} terminal(s) capped",
            branchCount, _finalZC, _finalLC, _finalSC, _termCapped));
        Debug.Log(string.Format(
            "[ProcGen:AI]   Vents  : {0} segments | {1} ceiling cuts | {2} fallback seals",
            ventSegs, ventCutsMade, ceilFixes));
        if (overlapCount == 0 && gapCount == 0 && corridorOverlaps == 0)
            Debug.Log("[ProcGen:AI]   Diagnostics: <color=#00ff88><b>✓ CLEAN — no overlaps, gaps, or corridor intersections</b></color>");
        else
            Debug.Log(string.Format(
                "[ProcGen:AI]   Diagnostics: <color=#ff4444>{0} overlap(s)</color> | <color=#ff4444>{1} gap(s)</color> | <color=#ff4444>{2} corridor intersection(s)</color>",
                overlapCount, gapCount, corridorOverlaps));
        Debug.Log("[ProcGen:AI] " + _sep2);
    }
}
