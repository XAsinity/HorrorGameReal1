using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class ShipModuleGenerator : MonoBehaviour
{
    public enum ModuleType
    {
        Room,
        Corridor,
        DoorWall,
        CornerPiece,
        VentShaft,
        VentCorner,
        VentTee,
        VentCross,
        Crate,
        Console,
        Pillar
    }

    [Header("Module Settings")]
    public ModuleType moduleType = ModuleType.Room;

    [Header("Dimensions")]
    public float width = 6f;
    public float height = 3f;
    public float depth = 8f;
    public float wallThickness = 0.15f;

    [Header("Door Wall Settings")]
    public float doorWidth = 1.4f;
    public float doorHeight = 2.4f;

    [Header("Corner Settings")]
    public bool cornerWallBack = true;
    public bool cornerWallFront = false;
    public bool cornerWallLeft = true;
    public bool cornerWallRight = false;

    [Header("Detail Level")]
    [Range(0, 2)]
    public int detailLevel = 1;

    [Header("Material (Optional)")]
    public Material overrideMaterial;

    [ContextMenu("Generate Module")]
    public void Generate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        switch (moduleType)
        {
            case ModuleType.Room: GenerateRoom(); break;
            case ModuleType.Corridor: GenerateCorridor(); break;
            case ModuleType.DoorWall: GenerateDoorWall(); break;
            case ModuleType.CornerPiece: GenerateCornerPiece(); break;
            case ModuleType.VentShaft: GenerateVentShaft(); break;
            case ModuleType.VentCorner: GenerateVentCorner(); break;
            case ModuleType.VentTee: GenerateVentTee(); break;
            case ModuleType.VentCross: GenerateVentCross(); break;
            case ModuleType.Crate: GenerateCrate(); break;
            case ModuleType.Console: GenerateConsole(); break;
            case ModuleType.Pillar: GeneratePillar(); break;
        }
    }

    private void Start()
    {
        if (transform.childCount == 0)
            Generate();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ROOM — fully enclosed box with 6 sides
    // ═══════════════════════════════════════════════════════════════

    private void GenerateRoom()
    {
        float hw = width / 2f;
        float hd = depth / 2f;

        MakeBox("Floor", new Vector3(0, wallThickness / 2f, 0), width, wallThickness, depth);
        MakeBox("Ceiling", new Vector3(0, height - wallThickness / 2f, 0), width, wallThickness, depth);
        MakeBox("Wall_Front", new Vector3(0, height / 2f, hd - wallThickness / 2f), width, height, wallThickness);
        MakeBox("Wall_Back", new Vector3(0, height / 2f, -hd + wallThickness / 2f), width, height, wallThickness);
        MakeBox("Wall_Left", new Vector3(-hw + wallThickness / 2f, height / 2f, 0), wallThickness, height, depth);
        MakeBox("Wall_Right", new Vector3(hw - wallThickness / 2f, height / 2f, 0), wallThickness, height, depth);

        if (detailLevel >= 1)
        {
            float t = 0.04f;
            MakeBox("Trim_L", new Vector3(-hw + wallThickness + t / 2f, 0.04f, 0), t, 0.08f, depth - wallThickness * 2f);
            MakeBox("Trim_R", new Vector3(hw - wallThickness - t / 2f, 0.04f, 0), t, 0.08f, depth - wallThickness * 2f);
        }

        if (detailLevel >= 2)
        {
            float ps = 0.15f;
            float px = hw - wallThickness - ps / 2f;
            float pz = hd - wallThickness - ps / 2f;
            MakeBox("Pillar_FL", new Vector3(-px, height / 2f, pz), ps, height - wallThickness * 2f, ps);
            MakeBox("Pillar_FR", new Vector3(px, height / 2f, pz), ps, height - wallThickness * 2f, ps);
            MakeBox("Pillar_BL", new Vector3(-px, height / 2f, -pz), ps, height - wallThickness * 2f, ps);
            MakeBox("Pillar_BR", new Vector3(px, height / 2f, -pz), ps, height - wallThickness * 2f, ps);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CORRIDOR — floor, ceiling, left wall, right wall (open ends)
    // ═══���═══════════════════════════════════════════════════════════

    private void GenerateCorridor()
    {
        float hw = width / 2f;

        MakeBox("Floor", new Vector3(0, wallThickness / 2f, 0), width, wallThickness, depth);
        MakeBox("Ceiling", new Vector3(0, height - wallThickness / 2f, 0), width, wallThickness, depth);
        MakeBox("Wall_Left", new Vector3(-hw + wallThickness / 2f, height / 2f, 0), wallThickness, height, depth);
        MakeBox("Wall_Right", new Vector3(hw - wallThickness / 2f, height / 2f, 0), wallThickness, height, depth);

        if (detailLevel >= 1)
        {
            int ribCount = Mathf.Max(1, Mathf.FloorToInt(depth / 2f));
            for (int i = 0; i <= ribCount; i++)
            {
                float z = -depth / 2f + (depth / ribCount) * i;
                MakeBox("Rib_" + i, new Vector3(0, height - wallThickness - 0.04f, z),
                    width - wallThickness * 2f, 0.08f, 0.04f);
            }
            MakeBox("Strip_L", new Vector3(-width / 4f, wallThickness + 0.003f, 0), 0.05f, 0.006f, depth);
            MakeBox("Strip_R", new Vector3(width / 4f, wallThickness + 0.003f, 0), 0.05f, 0.006f, depth);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CORNER PIECE — floor + ceiling + selective walls
    // ═══════════════════════════════════════════════════════════════

    private void GenerateCornerPiece()
    {
        float hw = width / 2f;
        float hd = depth / 2f;

        MakeBox("Floor", new Vector3(0, wallThickness / 2f, 0), width, wallThickness, depth);
        MakeBox("Ceiling", new Vector3(0, height - wallThickness / 2f, 0), width, wallThickness, depth);

        if (cornerWallFront)
            MakeBox("Wall_Front", new Vector3(0, height / 2f, hd - wallThickness / 2f), width, height, wallThickness);
        if (cornerWallBack)
            MakeBox("Wall_Back", new Vector3(0, height / 2f, -hd + wallThickness / 2f), width, height, wallThickness);
        if (cornerWallLeft)
            MakeBox("Wall_Left", new Vector3(-hw + wallThickness / 2f, height / 2f, 0), wallThickness, height, depth);
        if (cornerWallRight)
            MakeBox("Wall_Right", new Vector3(hw - wallThickness / 2f, height / 2f, 0), wallThickness, height, depth);
    }

    // ═══════════════════════════════════════════════════════════════
    //  DOOR WALL — wall with a rectangular doorway cut out
    // ═══════════════════════════════════════════════════════════════

    private void GenerateDoorWall()
    {
        float sideWidth = (width - doorWidth) / 2f;
        float topHeight = height - doorHeight;

        if (sideWidth > 0.01f)
        {
            MakeBox("DoorWall_Left",
                new Vector3(-doorWidth / 2f - sideWidth / 2f, height / 2f, 0),
                sideWidth, height, wallThickness);
            MakeBox("DoorWall_Right",
                new Vector3(doorWidth / 2f + sideWidth / 2f, height / 2f, 0),
                sideWidth, height, wallThickness);
        }

        if (topHeight > 0.01f)
        {
            MakeBox("DoorWall_Top",
                new Vector3(0, doorHeight + topHeight / 2f, 0),
                doorWidth, topHeight, wallThickness);
        }

        if (detailLevel >= 1)
        {
            float trim = 0.04f;
            MakeBox("DoorTrim_L",
                new Vector3(-doorWidth / 2f - trim / 2f, doorHeight / 2f, wallThickness / 2f + trim / 2f),
                trim, doorHeight, trim);
            MakeBox("DoorTrim_R",
                new Vector3(doorWidth / 2f + trim / 2f, doorHeight / 2f, wallThickness / 2f + trim / 2f),
                trim, doorHeight, trim);
            MakeBox("DoorTrim_T",
                new Vector3(0, doorHeight + trim / 2f, wallThickness / 2f + trim / 2f),
                doorWidth + trim * 2f, trim, trim);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  VENT SHAFT — open on both ends (-Z and +Z)
    //  Has left wall, right wall, bottom, top. NO end caps.
    //  Vent shafts connect to junction pieces at their ends.
    // ═══════════════════════════════════════════════════════════════

    private void GenerateVentShaft()
    {
        float w = width > 0 ? width : 0.9f;
        float h = height > 0 ? height : 0.7f;
        float d = depth > 0 ? depth : 2f;
        float t = 0.02f;

        MakeBox("Bottom", new Vector3(0, t / 2f, 0), w, t, d);
        MakeBox("Top", new Vector3(0, h - t / 2f, 0), w, t, d);
        MakeBox("Left", new Vector3(-w / 2f + t / 2f, h / 2f, 0), t, h, d);
        MakeBox("Right", new Vector3(w / 2f - t / 2f, h / 2f, 0), t, h, d);

        if (detailLevel >= 1)
        {
            int slats = Mathf.Max(2, Mathf.FloorToInt(d / 1.5f));
            for (int i = 0; i < slats; i++)
            {
                float z = -d / 2f + (d / (slats + 1)) * (i + 1);
                MakeBox("Rib_" + i, new Vector3(0, h - t - 0.01f, z), w - t * 2f, 0.02f, t);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  VENT CORNER — 90° turn piece
    //  Has bottom + top. Only 2 walls (the outer corner).
    //  The 2 open sides connect to vent shafts.
    //
    //  Uses cornerWallBack/Front/Left/Right to choose which 2
    //  walls to build.
    //
    //  Example: turning from +Z to +X
    //    cornerWallFront = true  (wall on +Z, blocks forward)
    //    cornerWallLeft  = true  (wall on -X, blocks left)
    //    cornerWallBack  = false (open on -Z, shaft enters)
    //    cornerWallRight = false (open on +X, shaft exits)
    // ═══════════════════════════════════════════════════════════════

    private void GenerateVentCorner()
    {
        float w = width > 0 ? width : 0.9f;
        float h = height > 0 ? height : 0.7f;
        float d = depth > 0 ? depth : 0.9f;
        float t = 0.02f;

        MakeBox("Bottom", new Vector3(0, t / 2f, 0), w, t, d);
        MakeBox("Top", new Vector3(0, h - t / 2f, 0), w, t, d);

        float hw = w / 2f;
        float hd = d / 2f;

        if (cornerWallFront)
            MakeBox("Wall_Front", new Vector3(0, h / 2f, hd - t / 2f), w, h, t);
        if (cornerWallBack)
            MakeBox("Wall_Back", new Vector3(0, h / 2f, -hd + t / 2f), w, h, t);
        if (cornerWallLeft)
            MakeBox("Wall_Left", new Vector3(-hw + t / 2f, h / 2f, 0), t, h, d);
        if (cornerWallRight)
            MakeBox("Wall_Right", new Vector3(hw - t / 2f, h / 2f, 0), t, h, d);
    }

    // ═══════════════════════════════════════════════════════════════
    //  VENT TEE — T-junction piece
    //  Has bottom + top. Only 1 wall (the dead end).
    //  3 sides open for connecting vent shafts.
    //
    //  Uses cornerWall booleans — set exactly 1 to true.
    // ═══════════════════════════════════════════════════════════════

    private void GenerateVentTee()
    {
        float w = width > 0 ? width : 0.9f;
        float h = height > 0 ? height : 0.7f;
        float d = depth > 0 ? depth : 0.9f;
        float t = 0.02f;

        MakeBox("Bottom", new Vector3(0, t / 2f, 0), w, t, d);
        MakeBox("Top", new Vector3(0, h - t / 2f, 0), w, t, d);

        float hw = w / 2f;
        float hd = d / 2f;

        if (cornerWallFront)
            MakeBox("Wall_Front", new Vector3(0, h / 2f, hd - t / 2f), w, h, t);
        if (cornerWallBack)
            MakeBox("Wall_Back", new Vector3(0, h / 2f, -hd + t / 2f), w, h, t);
        if (cornerWallLeft)
            MakeBox("Wall_Left", new Vector3(-hw + t / 2f, h / 2f, 0), t, h, d);
        if (cornerWallRight)
            MakeBox("Wall_Right", new Vector3(hw - t / 2f, h / 2f, 0), t, h, d);
    }

    // ═══════════════════════════════════════════════════════════════
    //  VENT CROSS — 4-way junction
    //  Has bottom + top only. All 4 sides open.
    // ═══════════════════════════════════════════════════════════════

    private void GenerateVentCross()
    {
        float w = width > 0 ? width : 0.9f;
        float h = height > 0 ? height : 0.7f;
        float d = depth > 0 ? depth : 0.9f;
        float t = 0.02f;

        MakeBox("Bottom", new Vector3(0, t / 2f, 0), w, t, d);
        MakeBox("Top", new Vector3(0, h - t / 2f, 0), w, t, d);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CRATE
    // ═══════════════════════════════════════════════════════════════

    private void GenerateCrate()
    {
        float w = width > 0 ? width : 0.8f;
        float h = height > 0 ? height : 0.6f;
        float d = depth > 0 ? depth : 0.8f;

        MakeBox("Crate_Body", new Vector3(0, h / 2f, 0), w, h, d);

        if (detailLevel >= 1)
        {
            float e = 0.04f;
            MakeBox("Edge_FL", new Vector3(-w / 2f, h / 2f, d / 2f), e, h + e, e);
            MakeBox("Edge_FR", new Vector3(w / 2f, h / 2f, d / 2f), e, h + e, e);
            MakeBox("Edge_BL", new Vector3(-w / 2f, h / 2f, -d / 2f), e, h + e, e);
            MakeBox("Edge_BR", new Vector3(w / 2f, h / 2f, -d / 2f), e, h + e, e);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CONSOLE
    // ═══════════════════════════════════════════════════════════════

    private void GenerateConsole()
    {
        float w = width > 0 ? width : 1.2f;
        float h = height > 0 ? height : 1.0f;
        float d = depth > 0 ? depth : 0.5f;

        MakeBox("Base", new Vector3(0, h * 0.3f, 0), w, h * 0.6f, d);
        MakeBox("Screen", new Vector3(0, h * 0.75f, -d * 0.1f), w * 0.85f, h * 0.3f, 0.03f);
        MakeBox("Keyboard", new Vector3(0, h * 0.58f, d * 0.15f), w * 0.75f, 0.03f, d * 0.3f);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PILLAR
    // ═══════════════════════════════════════════════════════════════

    private void GeneratePillar()
    {
        float w = width > 0 ? width : 0.3f;
        float h = height > 0 ? height : 3f;
        float d = depth > 0 ? depth : 0.3f;

        MakeBox("Shaft", new Vector3(0, h / 2f, 0), w, h, d);

        if (detailLevel >= 1)
        {
            MakeBox("Base", new Vector3(0, 0.05f, 0), w * 1.3f, 0.1f, d * 1.3f);
            MakeBox("Cap", new Vector3(0, h - 0.05f, 0), w * 1.3f, 0.1f, d * 1.3f);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  BOX BUILDER
    // ═══════════════════════════════════════════════════════════════

    private void MakeBox(string objName, Vector3 pos, float w, float h, float d)
    {
        ProBuilderMesh pb = ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(w, h, d));
        pb.gameObject.name = objName;
        pb.transform.SetParent(transform);
        pb.transform.localPosition = pos;
        pb.transform.localRotation = Quaternion.identity;
        pb.ToMesh();
        pb.Refresh();

        if (overrideMaterial != null)
        {
            MeshRenderer rend = pb.GetComponent<MeshRenderer>();
            if (rend != null)
                rend.sharedMaterial = overrideMaterial;
        }

        if (pb.GetComponent<Collider>() == null)
            pb.gameObject.AddComponent<BoxCollider>();
    }
}