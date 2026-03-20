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

    private float HalfCor { get { return corridorWidth / 2f; } }
    private float ventW = 0.9f;
    private float ventH = 0.7f;
    private const float kDoorWidth = 1.4f;
    private const float kDoorHeight = 2.4f;

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
        float cx, float cz, float halfW, float halfD, float pad)
    {
        for (int i = 0; i < placed.Count; i++)
        {
            var p = placed[i];
            if (Mathf.Abs(cx - p.x) < halfW + p.z + pad &&
                Mathf.Abs(cz - p.y) < halfD + p.w + pad)
                return true;
        }
        return false;
    }


    // ═══════════════════════════════════════════════════════════════
    //  PROCEDURAL LAYOUT — rewritten for true topology randomization.
    //  Different seeds produce ships with different numbers of spine
    //  corridors, different cargo positions, different branch counts
    //  and patterns, and different room assignments throughout.
    // ═══════════════════════════════════════════════════════════════
    private void GenerateProceduralLayout()
    {
        int actualSeed = seed < 0 ? System.Environment.TickCount : seed;
        System.Random rng = new System.Random(actualSeed);
        Debug.Log("Procedural ship generated with seed: " + actualSeed);

        float dW    = kDoorWidth;
        float dH    = kDoorHeight;
        float vY    = roomHeight;
        float hvW   = ventW / 2f;
        float dropW = ventW * 0.5f;
        float dropH = ventH + wallThickness + 0.15f;
        float dropY = vY - dropH / 2f;

        // ── Room pool ─────────────────────────────────────────────
        // W = AddRoom width param (local X extent of room module)
        // D = AddRoom depth param (local Z extent) — also used for X-axis
        // positioning: leftX = -(HalfCor + D/2),  rightX = +(HalfCor + D/2)
        // This matches the convention in the hardcoded GenerateShipLayout().
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
            int t = pidx[i]; pidx[i] = pidx[j]; pidx[j] = t;
        }
        int pp = 0; // pool pointer

        // ── Required room dimensions ──────────────────────────────
        float dockW  = RngRange(rng, 14f, 18f), dockD  = RngRange(rng, 10f, 14f), dockH  = RngRange(rng,  5f,  6f);
        float cargoW = RngRange(rng,  7f, 10f), cargoD = RngRange(rng,  5f,  8f), cargoH = RngRange(rng, 3.5f, 5f);
        float engW   = RngRange(rng, 10f, 14f), engD   = RngRange(rng,  9f, 12f), engH   = RngRange(rng,  4f,  5f);
        float bridgeW = RngRange(rng, 9f, 12f), bridgeD = RngRange(rng, 7f, 10f);

        // ══════════════════════════════════════════════════════════
        //  PHASE 1 — SPINE Z-POSITIONS
        // ══════════════════════════════════════════════════════════

        // Variable number of corridor segments (2–5) and random CargoBay position
        int spineCount    = rng.Next(2, 6);
        int cargoAfterIdx = rng.Next(1, spineCount); // insert cargo AFTER corridor [cargoAfterIdx-1]

        float[] sLen = new float[spineCount];
        for (int i = 0; i < spineCount; i++)
            sLen[i] = RngRange(rng, 6f, 14f);

        float   dockZ   = 0f;
        float   dockFr  = dockD / 2f;
        float[] sBk     = new float[spineCount];
        float[] sCZ     = new float[spineCount];
        float[] sFr     = new float[spineCount];
        float   cargoBk = 0f, cargoCZ = 0f, cargoFrZ = 0f;

        float cur = dockFr;
        for (int i = 0; i < spineCount; i++)
        {
            sBk[i]  = cur;
            sCZ[i]  = cur + sLen[i] / 2f;
            sFr[i]  = cur + sLen[i];
            cur     = sFr[i];
            if (i == cargoAfterIdx - 1)
            {
                cargoBk  = cur;
                cargoCZ  = cur + cargoD / 2f;
                cargoFrZ = cur + cargoD;
                cur      = cargoFrZ;
            }
        }

        float engBk = cur;
        float engCZ = engBk + engD / 2f;
        float engFr = engBk + engD;

        // ══════════════════════════════════════════════════════════
        //  PHASE 2 — BRANCH GEOMETRY
        // ══════════════════════════════════════════════════════════

        int branchCount = rng.Next(1, 4); // 1–3 branches

        float[] bX       = new float[branchCount];
        int[]   bPat     = new int[branchCount];   // 0=short, 1=Z-shaped
        int[]   bSideDir = new int[branchCount];   // -1=left turn, +1=right turn
        float[] bStrLen  = new float[branchCount];
        float[] bSideLen = new float[branchCount];
        float[] bFinLen  = new float[branchCount];
        float[] bCor1Z   = new float[branchCount]; // Z of corner-1 piece (Z-shaped)
        float[] bCor2X   = new float[branchCount]; // X of corner-2 / final corridor (Z-shaped)
        float[] bFinBk   = new float[branchCount]; // back-Z of final corridor (Z-shaped)
        float[] bFinCZ   = new float[branchCount]; // center-Z of final corridor (Z-shaped)
        float[] bTermBk  = new float[branchCount]; // back-Z of terminal room
        float[] bTermX   = new float[branchCount]; // X center of terminal room

        if (branchCount == 1)
        {
            int ch = rng.Next(3);
            bX[0] = ch == 0 ? -(engW / 4f) : (ch == 2 ? engW / 4f : 0f);
        }
        else if (branchCount == 2)
        {
            bX[0] = -(engW / 4f);
            bX[1] =   engW / 4f;
        }
        else
        {
            bX[0] = -(engW / 3f);
            bX[1] = 0f;
            bX[2] =   engW / 3f;
        }

        for (int b = 0; b < branchCount; b++)
        {
            bPat[b]     = rng.Next(2);
            bStrLen[b]  = RngRange(rng, 4f, 8f);
            bSideLen[b] = RngRange(rng, 5f, 12f);
            bFinLen[b]  = RngRange(rng, 5f, 10f);
            // Turn direction: left branches bend further left, right branches further right,
            // centre branches (|bX| < 0.1) pick randomly so the Z-shape can open either way.
            if      (bX[b] < -0.1f) bSideDir[b] = -1;
            else if (bX[b] >  0.1f) bSideDir[b] = +1;
            else                    bSideDir[b]  = rng.Next(2) == 0 ? -1 : +1;

            float strFr = engFr + bStrLen[b];
            if (bPat[b] == 0)
            {
                bTermBk[b] = strFr;
                bTermX[b]  = bX[b];
            }
            else
            {
                bCor1Z[b]  = strFr + HalfCor;
                bCor2X[b]  = bSideDir[b] == -1
                    ? bX[b] - bSideLen[b] - corridorWidth
                    : bX[b] + bSideLen[b] + corridorWidth;
                bFinBk[b]  = bCor1Z[b] + HalfCor;
                bFinCZ[b]  = bFinBk[b] + bFinLen[b] / 2f;
                bTermBk[b] = bFinBk[b] + bFinLen[b];
                bTermX[b]  = bCor2X[b];
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PHASE 3 — ROOM POOL ASSIGNMENT
        // ══════════════════════════════════════════════════════════

        // Spine side rooms
        bool[]   sHL  = new bool[spineCount],  sHR  = new bool[spineCount];
        string[] sLNm = new string[spineCount], sRNm = new string[spineCount];
        float[]  sLW  = new float[spineCount],  sLD  = new float[spineCount];
        float[]  sRW  = new float[spineCount],  sRD  = new float[spineCount];
        float[]  sLX  = new float[spineCount],  sRX  = new float[spineCount];

        for (int i = 0; i < spineCount; i++)
        {
            if (pp < poolSize && rng.NextDouble() > 0.25)
            {
                int k = pidx[pp++];
                sHL[i] = true; sLNm[i] = pName[k];
                sLW[i] = pRW[k]; sLD[i] = pRD[k];
                sLX[i] = -(HalfCor + sLD[i] / 2f);
            }
            if (pp < poolSize && rng.NextDouble() > 0.25)
            {
                int k = pidx[pp++];
                sHR[i] = true; sRNm[i] = pName[k];
                sRW[i] = pRW[k]; sRD[i] = pRD[k];
                sRX[i] = HalfCor + sRD[i] / 2f;
            }
        }

        // Engineering side rooms (reactor right, lab left — following hardcoded convention)
        bool   engHR = false, engHL = false;
        string reactNm = "", labNm = "";
        float  reactW_ = 0f, reactD_ = 0f, labW_ = 0f, labD_ = 0f;
        float  reactX  = 0f, labX   = 0f;

        if (pp < poolSize && rng.NextDouble() > 0.35f)
        {
            int k = pidx[pp++]; engHR = true;
            reactNm = pName[k]; reactW_ = pRW[k]; reactD_ = pRD[k];
            reactX  = engW / 2f + reactD_ / 2f;
        }
        if (pp < poolSize && rng.NextDouble() > 0.35f)
        {
            int k = pidx[pp++]; engHL = true;
            labNm = pName[k]; labW_ = pRW[k]; labD_ = pRD[k];
            labX  = -(engW / 2f + labD_ / 2f);
        }

        // Terminal rooms (one branch always gets Bridge)
        int      bridgeBranch = rng.Next(branchCount);
        float[]  bTermW  = new float[branchCount];
        float[]  bTermD  = new float[branchCount];
        string[] bTermNm = new string[branchCount];
        float[]  bTermCZ = new float[branchCount];

        for (int b = 0; b < branchCount; b++)
        {
            if (b == bridgeBranch)
            {
                bTermW[b] = bridgeW; bTermD[b] = bridgeD; bTermNm[b] = "Bridge";
            }
            else if (pp < poolSize)
            {
                int k = pidx[pp++];
                bTermW[b] = pRW[k]; bTermD[b] = pRD[k]; bTermNm[b] = pName[k];
            }
            else
            {
                bTermW[b] = 7f; bTermD[b] = 5f; bTermNm[b] = "Terminal_" + b;
            }
            bTermCZ[b] = bTermBk[b] + bTermD[b] / 2f;
        }

        // Branch final-corridor side rooms (Z-shaped branches only)
        bool[]   bHL  = new bool[branchCount],   bHR  = new bool[branchCount];
        string[] bLNm = new string[branchCount],  bRNm = new string[branchCount];
        float[]  bLW  = new float[branchCount],   bLD  = new float[branchCount];
        float[]  bRW  = new float[branchCount],   bRD  = new float[branchCount];
        float[]  bLX  = new float[branchCount],   bRX  = new float[branchCount];

        for (int b = 0; b < branchCount; b++)
        {
            if (bPat[b] == 1)
            {
                if (pp < poolSize && rng.NextDouble() > 0.4f)
                {
                    int k = pidx[pp++]; bHL[b] = true;
                    bLNm[b] = pName[k]; bLW[b] = pRW[k]; bLD[b] = pRD[k];
                    bLX[b]  = bCor2X[b] - HalfCor - bLD[b] / 2f;
                }
                if (pp < poolSize && rng.NextDouble() > 0.4f)
                {
                    int k = pidx[pp++]; bHR[b] = true;
                    bRNm[b] = pName[k]; bRW[b] = pRW[k]; bRD[b] = pRD[k];
                    bRX[b]  = bCor2X[b] + HalfCor + bRD[b] / 2f;
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  BUILD — ROOMS
        // ══════════════════════════════════════════════════════════

        // Tracks placed room XZ footprints as (centerX, centerZ, halfWidth, halfDepth).
        // Used to prevent optional rooms from overlapping already-placed geometry.
        var roomBounds = new System.Collections.Generic.List<Vector4>();

        // --- Docking Bay ---
        var dockGen = AddRoom("DockingBay", 0f, dockZ, dockW, dockH, dockD);
        DeleteChildWall(dockGen, "Wall_Front");
        CutVentInDoorWallTop(
            AddDoorWall("Door_Dock_Front", 0f, dockFr - wallThickness / 2f, dockW, dockH).transform,
            dockH, dH, vY);
        roomBounds.Add(new Vector4(0f, dockZ, dockW / 2f, dockD / 2f));

        // --- Spine corridors + optional side rooms ---
        var sCorGen = new ShipModuleGenerator[spineCount];
        var sLGen   = new ShipModuleGenerator[spineCount];
        var sRGen   = new ShipModuleGenerator[spineCount];

        for (int i = 0; i < spineCount; i++)
        {
            sCorGen[i] = AddCorridor("SpineCor_" + i, 0f, sCZ[i], corridorWidth, corridorHeight, sLen[i]);
            // Corridor bounds are intentionally NOT registered — side rooms attach
            // directly to corridors and would always fail the overlap test otherwise.

            if (sHL[i])
            {
                if (!BoundsOverlap(roomBounds, sLX[i], sCZ[i], sLW[i] / 2f, sLD[i] / 2f, 0.05f))
                {
                    sLGen[i] = AddRoom(sLNm[i] + "_L" + i, sLX[i], sCZ[i], sLW[i], roomHeight, sLD[i]);
                    DeleteChildWall(sLGen[i], "Wall_Right");
                    DeleteChildWall(sCorGen[i], "Wall_Left");
                    CutWallForDoor(sCorGen[i], "Wall_Left", true, corridorWidth, corridorHeight, sLen[i], 0f, dW, dH);
                    AddDoorWallSide("Door_" + sLNm[i] + "_" + i, -HalfCor, sCZ[i], sLW[i], roomHeight);
                    roomBounds.Add(new Vector4(sLX[i], sCZ[i], sLW[i] / 2f, sLD[i] / 2f));
                }
                else
                {
                    sHL[i] = false;
                    Debug.LogWarning("[ProcGen] Skipped " + sLNm[i] + "_L" + i + " — overlaps existing room");
                }
            }
            if (sHR[i])
            {
                if (!BoundsOverlap(roomBounds, sRX[i], sCZ[i], sRW[i] / 2f, sRD[i] / 2f, 0.05f))
                {
                    sRGen[i] = AddRoom(sRNm[i] + "_R" + i, sRX[i], sCZ[i], sRW[i], roomHeight, sRD[i]);
                    DeleteChildWall(sRGen[i], "Wall_Left");
                    DeleteChildWall(sCorGen[i], "Wall_Right");
                    CutWallForDoor(sCorGen[i], "Wall_Right", false, corridorWidth, corridorHeight, sLen[i], 0f, dW, dH);
                    AddDoorWallSide("Door_" + sRNm[i] + "_" + i, HalfCor, sCZ[i], sRW[i], roomHeight);
                    roomBounds.Add(new Vector4(sRX[i], sCZ[i], sRW[i] / 2f, sRD[i] / 2f));
                }
                else
                {
                    sHR[i] = false;
                    Debug.LogWarning("[ProcGen] Skipped " + sRNm[i] + "_R" + i + " — overlaps existing room");
                }
            }
        }

        // --- Cargo Bay (inline on spine) ---
        var cargoGen = AddRoom("CargoBay", 0f, cargoCZ, cargoW, cargoH, cargoD);
        DeleteChildWall(cargoGen, "Wall_Back");
        CutVentInDoorWallTop(
            AddDoorWall("Door_Cargo_Back", 0f, cargoBk + wallThickness / 2f, cargoW, cargoH).transform,
            cargoH, dH, vY);
        DeleteChildWall(cargoGen, "Wall_Front");
        CutVentInDoorWallTop(
            AddDoorWall("Door_Cargo_Front", 0f, cargoFrZ - wallThickness / 2f, cargoW, cargoH).transform,
            cargoH, dH, vY);
        roomBounds.Add(new Vector4(0f, cargoCZ, cargoW / 2f, cargoD / 2f));

        // --- Engineering Hub ---
        var engGen = AddRoom("EngineeringHub", 0f, engCZ, engW, engH, engD);
        DeleteChildWall(engGen, "Wall_Back");
        CutVentInDoorWallTop(
            AddDoorWall("Door_Eng_Back", 0f, engBk + wallThickness / 2f, engW, engH).transform,
            engH, dH, vY);
        DeleteChildWall(engGen, "Wall_Front");
        roomBounds.Add(new Vector4(0f, engCZ, engW / 2f, engD / 2f));

        // Engineering front wall — varies by branch count
        if (branchCount == 1)
        {
            // Single door: pass same X twice; BuildEngFrontWall produces one opening
            BuildEngFrontWall(engW, engH, engFr, bX[0], bX[0], corridorWidth, dH, dW);
        }
        else if (branchCount == 2)
        {
            BuildEngFrontWall(engW, engH, engFr, bX[0], bX[1], corridorWidth, dH, dW);
        }
        else
        {
            // 3 branches — build manual wall segments with 3 door openings
            float wz   = engFr - wallThickness / 2f;
            float half = engW / 2f;
            float hOp  = corridorWidth / 2f;
            // Left solid segment
            float lw = half + bX[0] - hOp;
            if (lw > 0.01f) MakeBoxOnParent(transform, "EngFW3_L",  new Vector3(-half + lw / 2f,              engH / 2f, wz), lw,  engH, wallThickness);
            // Gap between door 0 and door 1 — lateral vent shaft passes through here,
            // so split into below-vent and above-vent sections (no wall in the vent band).
            float mw1 = bX[1] - hOp - (bX[0] + hOp);
            if (mw1 > 0.01f)
            {
                float mx1  = bX[0] + hOp + mw1 / 2f;
                float vTop = vY + ventH;
                if (vY > 0.01f)
                    MakeBoxOnParent(transform, "EngFW3_M1_Bot", new Vector3(mx1, vY / 2f, wz), mw1, vY, wallThickness);
                if (engH - vTop > 0.01f)
                    MakeBoxOnParent(transform, "EngFW3_M1_Top", new Vector3(mx1, vTop + (engH - vTop) / 2f, wz), mw1, engH - vTop, wallThickness);
            }
            // Gap between door 1 and door 2 — same vent-band split treatment.
            float mw2 = bX[2] - hOp - (bX[1] + hOp);
            if (mw2 > 0.01f)
            {
                float mx2  = bX[1] + hOp + mw2 / 2f;
                float vTop = vY + ventH;
                if (vY > 0.01f)
                    MakeBoxOnParent(transform, "EngFW3_M2_Bot", new Vector3(mx2, vY / 2f, wz), mw2, vY, wallThickness);
                if (engH - vTop > 0.01f)
                    MakeBoxOnParent(transform, "EngFW3_M2_Top", new Vector3(mx2, vTop + (engH - vTop) / 2f, wz), mw2, engH - vTop, wallThickness);
            }
            // Right solid segment
            float rw = half - (bX[2] + hOp);
            if (rw > 0.01f) MakeBoxOnParent(transform, "EngFW3_R",  new Vector3(half - rw / 2f,              engH / 2f, wz), rw,  engH, wallThickness);

            // Above each door opening: solid from doorH to vY (below vent),
            // vent gap from vY to vY+ventH, solid from vY+ventH to engH.
            // Without these sections the full-height corridor openings leave
            // the room open from door-top all the way up to the ceiling.
            {
                float vTop3   = vY + ventH;
                float belowVH = vY - dH;
                float aboveVH = engH - vTop3;
                for (int bi = 0; bi < 3; bi++)
                {
                    if (belowVH > 0.01f)
                        MakeBoxOnParent(transform, "EngFW3_DT" + bi + "_Bot",
                            new Vector3(bX[bi], dH + belowVH / 2f, wz), corridorWidth, belowVH, wallThickness);
                    if (aboveVH > 0.01f)
                        MakeBoxOnParent(transform, "EngFW3_DT" + bi + "_Top",
                            new Vector3(bX[bi], vTop3 + aboveVH / 2f, wz), corridorWidth, aboveVH, wallThickness);
                }
            }
        }

        // Engineering optional side rooms
        ShipModuleGenerator engReactGen = null, engLabGen = null;
        if (engHR)
        {
            if (!BoundsOverlap(roomBounds, reactX, engCZ, reactW_ / 2f, reactD_ / 2f, 0.05f))
            {
                DeleteChildWall(engGen, "Wall_Right");
                CutVentInDoorWallTop(
                    AddDoorWallSide("Door_Eng_R", engW / 2f - wallThickness / 2f, engCZ, engD, engH).transform,
                    engH, dH, vY);
                engReactGen = AddRoom(reactNm + "_EngR", reactX, engCZ, reactW_, roomHeight, reactD_);
                DeleteChildWall(engReactGen, "Wall_Left");
                roomBounds.Add(new Vector4(reactX, engCZ, reactW_ / 2f, reactD_ / 2f));
            }
            else
            {
                engHR = false;
                Debug.LogWarning("[ProcGen] Skipped " + reactNm + "_EngR — overlaps existing room");
            }
        }
        if (engHL)
        {
            if (!BoundsOverlap(roomBounds, labX, engCZ, labW_ / 2f, labD_ / 2f, 0.05f))
            {
                DeleteChildWall(engGen, "Wall_Left");
                CutVentInDoorWallTop(
                    AddDoorWallSide("Door_Eng_L", -(engW / 2f) + wallThickness / 2f, engCZ, engD, engH).transform,
                    engH, dH, vY);
                engLabGen = AddRoom(labNm + "_EngL", labX, engCZ, labW_, roomHeight, labD_);
                DeleteChildWall(engLabGen, "Wall_Right");
                roomBounds.Add(new Vector4(labX, engCZ, labW_ / 2f, labD_ / 2f));
            }
            else
            {
                engHL = false;
                Debug.LogWarning("[ProcGen] Skipped " + labNm + "_EngL — overlaps existing room");
            }
        }

        // --- Fork branches ---
        var termGen   = new ShipModuleGenerator[branchCount];
        var bFinCorGen = new ShipModuleGenerator[branchCount]; // final corridor gen (Z-shaped only)
        var bLGen     = new ShipModuleGenerator[branchCount];
        var bRGen     = new ShipModuleGenerator[branchCount];

        for (int b = 0; b < branchCount; b++)
        {
            string bs = "B" + b + "_";

            if (bPat[b] == 0)
            {
                // ── Short branch: straight corridor → terminal room ──
                float strZ = engFr + bStrLen[b] / 2f;
                AddCorridor(bs + "Str", bX[b], strZ, corridorWidth, corridorHeight, bStrLen[b]);
                // Corridor bounds intentionally not registered — rooms attach to corridors.

                // Terminal room — check for overlap before placing
                if (!BoundsOverlap(roomBounds, bTermX[b], bTermCZ[b], bTermW[b] / 2f, bTermD[b] / 2f, 0.05f))
                {
                    termGen[b] = AddRoom(bTermNm[b], bTermX[b], bTermCZ[b], bTermW[b], roomHeight, bTermD[b]);
                    DeleteChildWall(termGen[b], "Wall_Back");
                    AddDoorWall("Door_" + bTermNm[b] + "_Bk", bTermX[b], bTermBk[b] + wallThickness / 2f, bTermW[b], roomHeight);
                    roomBounds.Add(new Vector4(bTermX[b], bTermCZ[b], bTermW[b] / 2f, bTermD[b] / 2f));
                }
                else
                {
                    Debug.LogWarning("[ProcGen] Skipped terminal " + bTermNm[b] + " (branch " + b + ") — overlaps existing room");
                }
            }
            else
            {
                // ── Z-shaped branch: straight → corner1 → side corridor → corner2 → final corridor → terminal ──
                float strZ   = engFr + bStrLen[b] / 2f;
                AddCorridor(bs + "Str", bX[b], strZ, corridorWidth, corridorHeight, bStrLen[b]);
                // Corridor bounds intentionally not registered — rooms attach to corridors.

                // Corner 1 — turns from +Z into ±X
                bool goLeft = bSideDir[b] == -1;
                if (goLeft)
                    AddCorner(bs + "C1", bX[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth, false, true, false, true);
                else
                    AddCorner(bs + "C1", bX[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth, false, true, true, false);

                // Side corridor (rotated 90° — runs along world X)
                // Because the corridor is rotated 90°, its world X extent = depth, Z extent = width.
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

                // Corner 2 — turns from ±X back into +Z
                if (goLeft)
                    AddCorner(bs + "C2", bCor2X[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth, true, false, true, false);
                else
                    AddCorner(bs + "C2", bCor2X[b], bCor1Z[b], corridorWidth, corridorHeight, corridorWidth, true, false, false, true);

                // Final corridor
                bFinCorGen[b] = AddCorridor(bs + "Fin", bCor2X[b], bFinCZ[b], corridorWidth, corridorHeight, bFinLen[b]);

                // Final-corridor side rooms — check for overlap before placing
                if (bHL[b])
                {
                    if (!BoundsOverlap(roomBounds, bLX[b], bFinCZ[b], bLW[b] / 2f, bLD[b] / 2f, 0.05f))
                    {
                        bLGen[b] = AddRoom(bLNm[b] + "_BL" + b, bLX[b], bFinCZ[b], bLW[b], roomHeight, bLD[b]);
                        DeleteChildWall(bLGen[b], "Wall_Right");
                        DeleteChildWall(bFinCorGen[b], "Wall_Left");
                        CutWallForDoor(bFinCorGen[b], "Wall_Left", true, corridorWidth, corridorHeight, bFinLen[b], 0f, dW, dH);
                        AddDoorWallSide("Door_" + bLNm[b] + "_BL" + b, bCor2X[b] - HalfCor, bFinCZ[b], bLW[b], roomHeight);
                        roomBounds.Add(new Vector4(bLX[b], bFinCZ[b], bLW[b] / 2f, bLD[b] / 2f));
                    }
                    else
                    {
                        bHL[b] = false;
                        Debug.LogWarning("[ProcGen] Skipped " + bLNm[b] + "_BL" + b + " — overlaps existing room");
                    }
                }
                if (bHR[b])
                {
                    if (!BoundsOverlap(roomBounds, bRX[b], bFinCZ[b], bRW[b] / 2f, bRD[b] / 2f, 0.05f))
                    {
                        bRGen[b] = AddRoom(bRNm[b] + "_BR" + b, bRX[b], bFinCZ[b], bRW[b], roomHeight, bRD[b]);
                        DeleteChildWall(bRGen[b], "Wall_Left");
                        DeleteChildWall(bFinCorGen[b], "Wall_Right");
                        CutWallForDoor(bFinCorGen[b], "Wall_Right", false, corridorWidth, corridorHeight, bFinLen[b], 0f, dW, dH);
                        AddDoorWallSide("Door_" + bRNm[b] + "_BR" + b, bCor2X[b] + HalfCor, bFinCZ[b], bRW[b], roomHeight);
                        roomBounds.Add(new Vector4(bRX[b], bFinCZ[b], bRW[b] / 2f, bRD[b] / 2f));
                    }
                    else
                    {
                        bHR[b] = false;
                        Debug.LogWarning("[ProcGen] Skipped " + bRNm[b] + "_BR" + b + " — overlaps existing room");
                    }
                }

                // Terminal room — check for overlap before placing
                if (!BoundsOverlap(roomBounds, bTermX[b], bTermCZ[b], bTermW[b] / 2f, bTermD[b] / 2f, 0.05f))
                {
                    termGen[b] = AddRoom(bTermNm[b], bTermX[b], bTermCZ[b], bTermW[b], roomHeight, bTermD[b]);
                    DeleteChildWall(termGen[b], "Wall_Back");
                    AddDoorWall("Door_" + bTermNm[b] + "_Bk", bTermX[b], bTermBk[b] + wallThickness / 2f, bTermW[b], roomHeight);
                    roomBounds.Add(new Vector4(bTermX[b], bTermCZ[b], bTermW[b] / 2f, bTermD[b] / 2f));
                }
                else
                {
                    Debug.LogWarning("[ProcGen] Skipped terminal " + bTermNm[b] + " (branch " + b + ") — overlaps existing room");
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  BUILD — VENT NETWORK
        // ══════════════════════════════════════════════════════════

        // --- Dock: dead-end cap + elbow ---
        // The Docking Bay is always taller than roomHeight, so the trunk vent runs
        // at vY = 3.2 m inside the room — well below the dock ceiling.  A VDrop +
        // CutCeilingForVent pair would create a ceiling hole at dockH (~5–6 m) with
        // no vent shaft connecting back down to the trunk, leaving a visible gap.
        // The VElbow's open bottom already provides a visible vent-access opening from
        // inside the dock room, so no additional drop piece is needed here.
        float dockCapZ = dockZ - dockD / 2f + 1f;
        ConnectVent("VS_DockCap", 0f, vY, dockCapZ, 0f, vY, dockZ - hvW);
        AddVentCap("VentCap_Dock", 0f, vY, dockCapZ);
        AddVentElbow("VElbow_Dock", 0f, vY, dockZ, false, false, true, true);

        // --- Main spine vent (dock → engineering front) ---
        // sv tracks the leading edge of the next shaft to emit
        float sv = dockZ + hvW;

        for (int i = 0; i < spineCount; i++)
        {
            bool hasJ = sHL[i] || sHR[i];
            if (hasJ)
            {
                ConnectVent("VS_ToJ" + i, 0f, vY, sv, 0f, vY, sCZ[i] - hvW);
                if      (sHL[i] && sHR[i]) AddVentCross("VJ_S" + i, 0f, vY, sCZ[i]);
                else if (sHL[i])           AddVentTee("VJ_S" + i, 0f, vY, sCZ[i], false, false, false, true);
                else                       AddVentTee("VJ_S" + i, 0f, vY, sCZ[i], false, false, true, false);
                sv = sCZ[i] + hvW;

                if (sHL[i] && sLGen[i] != null)
                {
                    ConnectVent("VB_SL" + i, -hvW, vY, sCZ[i], sLX[i] + hvW, vY, sCZ[i]);
                    AddVentElbow("VElbow_SL" + i, sLX[i], vY, sCZ[i], true, true, true, false);
                    AddVentVertical("VDrop_SL" + i, sLX[i], dropY, sCZ[i], dropW, dropH, dropW);
                    CutCeilingForVent(sLGen[i], dropW, dropW);
                }
                if (sHR[i] && sRGen[i] != null)
                {
                    ConnectVent("VB_SR" + i, hvW, vY, sCZ[i], sRX[i] - hvW, vY, sCZ[i]);
                    AddVentElbow("VElbow_SR" + i, sRX[i], vY, sCZ[i], true, true, false, true);
                    AddVentVertical("VDrop_SR" + i, sRX[i], dropY, sCZ[i], dropW, dropH, dropW);
                    CutCeilingForVent(sRGen[i], dropW, dropW);
                }
            }

            // After this corridor, was cargo inserted?
            if (i == cargoAfterIdx - 1)
            {
                ConnectVent("VS_ThrCargo" + i, 0f, vY, sv, 0f, vY, cargoFrZ);
                sv = cargoFrZ;
            }
        }

        // --- Engineering center junction (reactor / lab) ---
        bool needEngJ = engHR || engHL;
        if (needEngJ)
        {
            ConnectVent("VS_ToEngC", 0f, vY, sv, 0f, vY, engCZ - hvW);
            if      (engHR && engHL) AddVentCross("VJ_EngC", 0f, vY, engCZ);
            else if (engHL)          AddVentTee("VJ_EngC", 0f, vY, engCZ, false, false, false, true);
            else                     AddVentTee("VJ_EngC", 0f, vY, engCZ, false, false, true, false);
            sv = engCZ + hvW;

            if (engHR && engReactGen != null)
            {
                ConnectVent("VB_React", hvW, vY, engCZ, reactX - hvW, vY, engCZ);
                AddVentElbow("VElbow_React", reactX, vY, engCZ, true, true, false, true);
                AddVentVertical("VDrop_React", reactX, dropY, engCZ, dropW, dropH, dropW);
                CutCeilingForVent(engReactGen, dropW, dropW);
            }
            if (engHL && engLabGen != null)
            {
                ConnectVent("VB_Lab", -hvW, vY, engCZ, labX + hvW, vY, engCZ);
                AddVentElbow("VElbow_Lab", labX, vY, engCZ, true, true, true, false);
                AddVentVertical("VDrop_Lab", labX, dropY, engCZ, dropW, dropH, dropW);
                CutCeilingForVent(engLabGen, dropW, dropW);
            }
        }

        // --- Engineering front junction → branch laterals ---
        // Place the EngFront junction and lateral shafts to each branch start.
        // A branch is "centre" when its X offset is < 0.1 (half corridorWidth ≈ 1.5);
        // using 0.1 as a near-zero threshold guards against floating-point imprecision.
        bool centerBranch = branchCount == 1 && Mathf.Abs(bX[0]) < 0.1f;

        // Connect remaining spine shaft to Engineering front.
        // For a single center branch there is no junction piece at engFr, so the
        // shaft must reach engFr+hvW to connect seamlessly to the branch vent.
        float engFrEnd = centerBranch ? engFr + hvW : engFr - hvW;
        ConnectVent("VS_ToEngFr", 0f, vY, sv, 0f, vY, engFrEnd);

        if (!centerBranch)
        {
            // Determine open directions
            bool openL = false, openR = false, openF = false;
            for (int b = 0; b < branchCount; b++)
            {
                if (bX[b] < -0.1f) openL = true;
                else if (bX[b] > 0.1f) openR = true;
                else openF = true;
            }

            if (openL && openR && openF)
                AddVentCross("VJ_EngFr", 0f, vY, engFr); // all 4 directions open
            else if (openL && openR)
                AddVentTee("VJ_EngFr", 0f, vY, engFr, false, true, false, false);
            else if (openL)
                AddVentTee("VJ_EngFr", 0f, vY, engFr, false, true, false, true);
            else if (openR)
                AddVentTee("VJ_EngFr", 0f, vY, engFr, false, true, true, false);
            else // openF only — should not happen since we'd be in centerBranch
                AddVentTee("VJ_EngFr", 0f, vY, engFr, false, false, true, true);

            // Lateral shafts to each non-center branch start
            for (int b = 0; b < branchCount; b++)
            {
                if (Mathf.Abs(bX[b]) < 0.1f) continue; // center branch handled below
                bool left = bX[b] < 0f;
                if (left)
                {
                    ConnectVent("VL_EngToB" + b, -hvW, vY, engFr, bX[b] + hvW, vY, engFr);
                    AddVentTee("VJ_B" + b, bX[b], vY, engFr, true, false, true, false);
                    // Opens: front(+Z to branch) + right(+X toward EngFr junction)
                }
                else
                {
                    ConnectVent("VR_EngToB" + b, hvW, vY, engFr, bX[b] - hvW, vY, engFr);
                    AddVentTee("VJ_B" + b, bX[b], vY, engFr, true, false, false, true);
                    // Opens: front(+Z to branch) + left(-X toward EngFr junction)
                }
            }
        }
        // (for center single branch, no lateral needed — spine goes straight into branch)

        // --- Per-branch vents ---
        for (int b = 0; b < branchCount; b++)
        {
            float bvStart = engFr + hvW; // branch vent start Z (just past start junction)
            float termDeep = bTermBk[b] + bTermD[b] - 1f; // deep dead-end inside terminal room

            if (bPat[b] == 0)
            {
                // Short branch: straight shaft → terminal elbow → run → cap
                ConnectVent("VB" + b + "_Str", bTermX[b], vY, bvStart, bTermX[b], vY, bTermCZ[b] - hvW);
                AddVentElbow("VElbow_Term" + b, bTermX[b], vY, bTermCZ[b], false, false, true, true);
                ConnectVent("VB" + b + "_Run", bTermX[b], vY, bTermCZ[b] + hvW, bTermX[b], vY, termDeep);
                AddVentCap("VentCap_Term" + b, bTermX[b], vY, termDeep);
                AddVentVertical("VDrop_Term" + b, bTermX[b], dropY, bTermCZ[b], dropW, dropH, dropW);
                if (termGen[b] != null) CutCeilingForVent(termGen[b], dropW, dropW);
            }
            else
            {
                // Z-shaped branch
                bool goLeft = bSideDir[b] == -1;

                // 1. Straight shaft from branch start to corner 1
                ConnectVent("VB" + b + "_Str", bX[b], vY, bvStart, bX[b], vY, bCor1Z[b] - hvW);

                // 2. Corner 1
                if (goLeft)
                    AddVentCorner("VJ_B" + b + "_C1", bX[b], vY, bCor1Z[b], false, true, false, true);
                else
                    AddVentCorner("VJ_B" + b + "_C1", bX[b], vY, bCor1Z[b], false, true, true, false);

                // 3. Side shaft
                if (goLeft)
                {
                    ConnectVent("VB" + b + "_Side", bX[b] - hvW, vY, bCor1Z[b], bCor2X[b] + hvW, vY, bCor1Z[b]);
                    AddVentCorner("VJ_B" + b + "_C2", bCor2X[b], vY, bCor1Z[b], true, false, true, false);
                    // C2 opens: right(+X from side shaft) + front(+Z to final corridor)
                }
                else
                {
                    ConnectVent("VB" + b + "_Side", bX[b] + hvW, vY, bCor1Z[b], bCor2X[b] - hvW, vY, bCor1Z[b]);
                    AddVentCorner("VJ_B" + b + "_C2", bCor2X[b], vY, bCor1Z[b], true, false, false, true);
                    // C2 opens: left(-X from side shaft) + front(+Z to final corridor)
                }

                // 4. Final corridor shaft (with optional junction for side rooms)
                float finSv = bCor1Z[b] + hvW; // start of final corridor vent
                bool hasFJ  = bHL[b] || bHR[b];
                if (hasFJ)
                {
                    ConnectVent("VB" + b + "_Fin1", bCor2X[b], vY, finSv, bCor2X[b], vY, bFinCZ[b] - hvW);
                    if      (bHL[b] && bHR[b]) AddVentCross("VJ_BFin" + b, bCor2X[b], vY, bFinCZ[b]);
                    else if (bHL[b])            AddVentTee("VJ_BFin" + b, bCor2X[b], vY, bFinCZ[b], false, false, false, true);
                    else                        AddVentTee("VJ_BFin" + b, bCor2X[b], vY, bFinCZ[b], false, false, true, false);

                    if (bHL[b] && bLGen[b] != null)
                    {
                        ConnectVent("VBL" + b, bCor2X[b] - hvW, vY, bFinCZ[b], bLX[b] + hvW, vY, bFinCZ[b]);
                        AddVentElbow("VElbow_BL" + b, bLX[b], vY, bFinCZ[b], true, true, true, false);
                        AddVentVertical("VDrop_BL" + b, bLX[b], dropY, bFinCZ[b], dropW, dropH, dropW);
                        CutCeilingForVent(bLGen[b], dropW, dropW);
                    }
                    if (bHR[b] && bRGen[b] != null)
                    {
                        ConnectVent("VBR" + b, bCor2X[b] + hvW, vY, bFinCZ[b], bRX[b] - hvW, vY, bFinCZ[b]);
                        AddVentElbow("VElbow_BR" + b, bRX[b], vY, bFinCZ[b], true, true, false, true);
                        AddVentVertical("VDrop_BR" + b, bRX[b], dropY, bFinCZ[b], dropW, dropH, dropW);
                        CutCeilingForVent(bRGen[b], dropW, dropW);
                    }
                    ConnectVent("VB" + b + "_Fin2", bCor2X[b], vY, bFinCZ[b] + hvW, bCor2X[b], vY, bTermCZ[b] - hvW);
                }
                else
                {
                    ConnectVent("VB" + b + "_Fin1", bCor2X[b], vY, finSv, bCor2X[b], vY, bTermCZ[b] - hvW);
                }

                // 5. Terminal room vent
                AddVentElbow("VElbow_Term" + b, bCor2X[b], vY, bTermCZ[b], false, false, true, true);
                ConnectVent("VB" + b + "_Run", bCor2X[b], vY, bTermCZ[b] + hvW, bCor2X[b], vY, termDeep);
                AddVentCap("VentCap_Term" + b, bCor2X[b], vY, termDeep);
                AddVentVertical("VDrop_Term" + b, bCor2X[b], dropY, bTermCZ[b], dropW, dropH, dropW);
                if (termGen[b] != null) CutCeilingForVent(termGen[b], dropW, dropW);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  BUILD — PROPS
        // ══════════════════════════════════════════════════════════

        // Dock
        AddProp("DockCrate_1", RngRange(rng, -dockW / 2f + 1f, -1f),           0f, RngRange(rng, dockZ - dockD / 2f + 1f, dockZ),             ShipModuleGenerator.ModuleType.Crate,   RngRange(rng, 0.8f, 1.4f), RngRange(rng, 0.6f, 1.1f), RngRange(rng, 0.8f, 1.3f));
        AddProp("DockCrate_2", RngRange(rng, 1f,  dockW / 2f - 1f),            0f, RngRange(rng, dockZ - dockD / 2f + 1f, dockZ + dockD / 2f - 1f), ShipModuleGenerator.ModuleType.Crate,   RngRange(rng, 0.9f, 1.5f), RngRange(rng, 0.7f, 1.2f), RngRange(rng, 0.9f, 1.4f));
        AddProp("DockCrate_3", RngRange(rng, -dockW / 2f + 1f, 1f),            0f, RngRange(rng, dockZ, dockZ + dockD / 2f - 1f),             ShipModuleGenerator.ModuleType.Crate,   RngRange(rng, 0.7f, 1.0f), RngRange(rng, 0.5f, 0.9f), RngRange(rng, 0.7f, 1.0f));

        // Cargo
        AddProp("CargoCrate_1", RngRange(rng, -cargoW / 2f + 1f, -0.5f), 0f, RngRange(rng, cargoCZ - cargoD / 2f + 1f, cargoCZ),         ShipModuleGenerator.ModuleType.Crate, RngRange(rng, 1.2f, 1.8f), RngRange(rng, 1.0f, 1.5f), RngRange(rng, 1.2f, 1.6f));
        AddProp("CargoCrate_2", RngRange(rng,  0.5f,  cargoW / 2f - 1f), 0f, RngRange(rng, cargoCZ, cargoCZ + cargoD / 2f - 1f),         ShipModuleGenerator.ModuleType.Crate, RngRange(rng, 0.9f, 1.4f), RngRange(rng, 0.7f, 1.1f), RngRange(rng, 0.9f, 1.3f));

        // Spine side rooms
        for (int i = 0; i < spineCount; i++)
        {
            if (sHL[i] && sLGen[i] != null)
                AddProp("SL" + i + "_Crate", sLX[i] + RngRange(rng, -sLW[i] / 2f + 0.8f, sLW[i] / 2f - 0.8f), 0f,
                    sCZ[i] + RngRange(rng, -sLD[i] / 2f + 0.8f, sLD[i] / 2f - 0.8f),
                    ShipModuleGenerator.ModuleType.Crate, RngRange(rng, 0.8f, 1.2f), RngRange(rng, 0.6f, 1.0f), RngRange(rng, 0.8f, 1.2f));
            if (sHR[i] && sRGen[i] != null)
                AddProp("SR" + i + "_Console", sRX[i], 0f, sCZ[i],
                    ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.0f, 1.4f), RngRange(rng, 0.8f, 1.0f), 0.4f);
        }

        // Engineering
        AddProp("Eng_Console_L", RngRange(rng, -engW / 2f + 1f, -1f), 0f, engCZ + RngRange(rng, -2f, 0f),
            ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.3f, 1.8f), RngRange(rng, 0.9f, 1.1f), 0.5f);
        AddProp("Eng_Console_R", RngRange(rng,  1f,  engW / 2f - 1f), 0f, engCZ + RngRange(rng, -2f, 0f),
            ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.3f, 1.8f), RngRange(rng, 0.9f, 1.1f), 0.5f);
        AddProp("Eng_Console_C", 0f, 0f, engCZ + RngRange(rng, 0f, 2f),
            ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.8f, 2.4f), RngRange(rng, 1.0f, 1.2f), 0.6f);
        float epx = RngRange(rng, -engW / 2f + 1.5f, -2f);
        float epz = engCZ + RngRange(rng, -engD / 2f + 1.5f, 0f);
        AddProp("Eng_Pillar1",  epx, 0f, epz,                        ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Eng_Pillar2", -epx, 0f, epz,                        ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Eng_Pillar3",  epx, 0f, epz + RngRange(rng, 3f, 5f), ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        AddProp("Eng_Pillar4", -epx, 0f, epz + RngRange(rng, 3f, 5f), ShipModuleGenerator.ModuleType.Pillar, 0.4f, engH, 0.4f);
        if (engHR && engReactGen != null)
        {
            AddProp("React_Console", reactX, 0f, engCZ, ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.0f, 1.4f), RngRange(rng, 1.0f, 1.3f), 0.5f);
            AddProp("React_Pillar",  reactX, 0f, engCZ + RngRange(rng, -1.5f, 1.5f), ShipModuleGenerator.ModuleType.Pillar, 0.5f, roomHeight, 0.5f);
        }
        if (engHL && engLabGen != null)
        {
            AddProp("Lab_Console1", labX + RngRange(rng, -labW_ / 2f + 1f, labW_ / 2f - 1f), 0f, engCZ + RngRange(rng, -1f, 1f), ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.2f, 1.6f), RngRange(rng, 0.9f, 1.1f), 0.5f);
            AddProp("Lab_Console2", labX + RngRange(rng, -labW_ / 2f + 1f, labW_ / 2f - 1f), 0f, engCZ + RngRange(rng, -1f, 1f), ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.2f, 1.6f), RngRange(rng, 0.9f, 1.1f), 0.5f);
        }

        // Terminal rooms
        for (int b = 0; b < branchCount; b++)
        {
            if (termGen[b] == null) continue;
            if (bTermNm[b] == "Bridge")
            {
                AddProp("Bridge_Console_M", bTermX[b], 0f, bTermCZ[b] + RngRange(rng, 0.5f, 2f),
                    ShipModuleGenerator.ModuleType.Console, RngRange(rng, 2.5f, 3.5f), RngRange(rng, 0.9f, 1.1f), 0.7f);
                AddProp("Bridge_Console_L", bTermX[b] - RngRange(rng, 1.5f, 3f), 0f, bTermCZ[b],
                    ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.2f, 1.8f), RngRange(rng, 0.9f, 1.1f), 0.5f);
                AddProp("Bridge_Console_R", bTermX[b] + RngRange(rng, 1.5f, 3f), 0f, bTermCZ[b],
                    ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.2f, 1.8f), RngRange(rng, 0.9f, 1.1f), 0.5f);
                AddProp("Bridge_Pillar_L",  bTermX[b] - RngRange(rng, 2.5f, 4f), 0f, bTermCZ[b] - 1f,
                    ShipModuleGenerator.ModuleType.Pillar, 0.3f, roomHeight, 0.3f);
                AddProp("Bridge_Pillar_R",  bTermX[b] + RngRange(rng, 2.5f, 4f), 0f, bTermCZ[b] - 1f,
                    ShipModuleGenerator.ModuleType.Pillar, 0.3f, roomHeight, 0.3f);
            }
            else
            {
                AddProp("Term" + b + "_Table1", bTermX[b] + RngRange(rng, -bTermW[b] / 2f + 1f, -0.5f), 0f, bTermCZ[b] + RngRange(rng, -bTermD[b] / 2f + 1f, bTermD[b] / 2f - 1f),
                    ShipModuleGenerator.ModuleType.Crate, RngRange(rng, 1.5f, 2.5f), 0.75f, RngRange(rng, 0.8f, 1.2f));
                AddProp("Term" + b + "_Table2", bTermX[b] + RngRange(rng,  0.5f,  bTermW[b] / 2f - 1f), 0f, bTermCZ[b] + RngRange(rng, -bTermD[b] / 2f + 1f, bTermD[b] / 2f - 1f),
                    ShipModuleGenerator.ModuleType.Crate, RngRange(rng, 1.5f, 2.5f), 0.75f, RngRange(rng, 0.8f, 1.2f));
            }

            // Branch final-corridor side room props (Z-shaped)
            if (bPat[b] == 1)
            {
                if (bHL[b] && bLGen[b] != null)
                    AddProp("BL" + b + "_Crate", bLX[b] + RngRange(rng, -bLW[b] / 2f + 0.8f, bLW[b] / 2f - 0.8f), 0f,
                        bFinCZ[b] + RngRange(rng, -bLD[b] / 2f + 0.8f, bLD[b] / 2f - 0.8f),
                        ShipModuleGenerator.ModuleType.Crate, RngRange(rng, 0.8f, 1.2f), RngRange(rng, 0.6f, 1.0f), RngRange(rng, 0.8f, 1.2f));
                if (bHR[b] && bRGen[b] != null)
                    AddProp("BR" + b + "_Console", bRX[b], 0f, bFinCZ[b],
                        ShipModuleGenerator.ModuleType.Console, RngRange(rng, 1.0f, 1.4f), RngRange(rng, 0.8f, 1.0f), 0.4f);
            }
        }

        Debug.Log("Procedural ship ready. Seed " + actualSeed +
            " | spine=" + spineCount + " | cargo after cor" + (cargoAfterIdx - 1) +
            " | branches=" + branchCount +
            " | bridge on branch " + bridgeBranch);
    }
}
