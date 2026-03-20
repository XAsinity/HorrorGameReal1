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
        MakeBoxOnParent(transform, "VentCap_Dock", new Vector3(0, vY, dockZ - dockD / 2f + 1f), ventW, ventH, wallThickness);
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
        MakeBoxOnParent(transform, "VentCap_Mess", new Vector3(messX, vY, messBackEdge + messD - 1f), ventW, ventH, wallThickness);
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
        MakeBoxOnParent(transform, "VentCap_Bridge", new Vector3(bridgeX, vY, bridgeBackEdge + bridgeD - 1f), ventW, ventH, wallThickness);
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
}