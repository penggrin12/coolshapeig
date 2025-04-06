using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

// TODO: use MultiMeshInstance3D
// TODO: animations
// TODO: fix camera origin (something aint right rn)

public partial class ShapePreview : Control
{
    [Export] public Vector3I ShapeSize { get; set; } = Vector3I.One * 3;

    private Node World => GetNode<Node>("%World");
    private Node3D Voxels => World.GetNode<Node3D>("%Voxels");
    private MeshInstance3D outlineMesh;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    private float orbitDistance = 4.5f;
    public float desiredOrbitDistance = 4.5f;
    public Vector3 orbitOrigin = Vector3.Zero;

    private Vector2 lastMousePosition = Vector2.Zero;
    private bool holding = false;

    public List<List<List<Color>>> colors = [[[]]];

    public override void _Ready()
    {
        UpdateCameraPosition();
        outlineMesh = new()
        {
            MaterialOverride = new ShaderMaterial() { Shader = GD.Load<Shader>("res://assets/shaders/selection.gdshader") },
            Mesh = new BoxMesh() {  Size = Vector3.One * 1.005f },
        };

        Find.LevelsManager.LevelAboutToChange += () => {
            outlineMesh.GetParent()?.RemoveChild(outlineMesh);
            Find.Tooltip.Hide();
        };
    }

    public override void _Process(double delta)
    {
        var camera = World.GetViewport().GetCamera3D();
        var hoverRay = World.GetNode<RayCast3D>("%HoverRay");
        hoverRay.GlobalPosition = camera.GlobalPosition;

        if (holding)
        {
            if (Input.IsActionJustPressed("zoom_in"))
                desiredOrbitDistance = Mathf.Clamp(desiredOrbitDistance - 1f, 1f, 15f);
            if (Input.IsActionJustPressed("zoom_out"))
                desiredOrbitDistance = Mathf.Clamp(desiredOrbitDistance + 1f, 1f, 15f);
        }
        orbitDistance = Mathf.Lerp(orbitDistance, desiredOrbitDistance, 0.2f);
        UpdateCameraPosition();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if ((@event is InputEventMouseButton mouseButtonEvent) && ((mouseButtonEvent.ButtonIndex == MouseButton.Left) || (mouseButtonEvent.ButtonIndex == MouseButton.Right)))
        {
            if (mouseButtonEvent.Pressed && !holding)
            {
                lastMousePosition = GetViewport().GetMousePosition();
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
            else if (!mouseButtonEvent.Pressed && holding)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                Input.WarpMouse(lastMousePosition);
            }

            holding = mouseButtonEvent.Pressed;
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            if (holding)
            {
                yaw -= mouseMotion.Relative.X * 0.005f;
                pitch -= mouseMotion.Relative.Y * 0.005f;

                pitch = Mathf.Clamp(pitch, -Mathf.Pi / 2 + 0.1f, Mathf.Pi / 2 - 0.1f);
            }
            else
            {
                var hoverRay = World.GetNode<RayCast3D>("%HoverRay");
                var camera = World.GetViewport().GetCamera3D();
                Vector2 mouse2DPosition = GetLocalMousePosition();
                var from = camera.ProjectRayOrigin(mouse2DPosition);
                var to = from + camera.ProjectRayNormal(mouse2DPosition) * 10000f;
                hoverRay.TargetPosition = to;
                
                if (hoverRay.IsColliding())
                {
                    var voxel = (StaticBody3D)hoverRay.GetCollider();
                    Color color = colors[Mathf.FloorToInt(voxel.Position.X)][Mathf.FloorToInt(voxel.Position.Y)][Mathf.FloorToInt(voxel.Position.Z)];

                    Find.Tooltip.SetText($"[outline_color={color.Inverted().ToHtml()}][outline_size=2][color={color.ToHtml()}][{color.R8} {color.G8} {color.B8}][/color][/outline_size][/outline_color]\n[color=#ff2e2e]{Mathf.FloorToInt(voxel.Position.X)}[/color] [color=#2eff35]{Mathf.FloorToInt(voxel.Position.Y)}[/color] [color=#2e8fff]{Mathf.FloorToInt(voxel.Position.Z)}[/color]");
                    Find.Tooltip.SetPosition(GetGlobalMousePosition());
                    Find.Tooltip.Show();

                    if (outlineMesh.GetParent() != voxel)
                    {
                        outlineMesh.GetParent()?.RemoveChild(outlineMesh);
                        voxel.AddChild(outlineMesh);
                    }
                    (outlineMesh.MaterialOverride as ShaderMaterial).SetShaderParameter("outline_color", color.Inverted());
                }
                else
                {
                    outlineMesh.GetParent()?.RemoveChild(outlineMesh);
                    Find.Tooltip.Hide();
                }
            }
        }
    }

    private void UpdateCameraPosition()
    {
        var camera = GetNode<Node>("%World").GetViewport().GetCamera3D();

        float x = orbitDistance * Mathf.Cos(pitch) * Mathf.Sin(yaw);
        float y = orbitDistance * Mathf.Sin(pitch);
        float z = orbitDistance * Mathf.Cos(pitch) * Mathf.Cos(yaw);

        camera.GlobalTransform = new Transform3D(camera.GlobalTransform.Basis, orbitOrigin + new Vector3(x, y, z));
        camera.LookAt(orbitOrigin, Vector3.Up);
    }

    public async Task Make()
    {
        colors.Clear();
        foreach (Node child in Voxels.GetChildren())
        {
            child.QueueFree();
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        orbitOrigin = new Vector3((ShapeSize.Y / 2) + 0.5f, (ShapeSize.Y / 2) + 0.5f, (ShapeSize.Y / 2) + 0.5f);

        for (int x = 0; x < ShapeSize.X; x++)
        {
            colors.Add([]);
            for (int y = 0; y < ShapeSize.Y; y++)
            {
                colors[x].Add([]);
                for (int z = 0; z < ShapeSize.Z; z++)
                {
                    colors[x][y].Add(Colors.White);
                    var voxel = GD.Load<PackedScene>("res://scenes/voxel.tscn").Instantiate<StaticBody3D>();
                    voxel.Name = $"{x};{y};{z}";
                    voxel.GetNode<MeshInstance3D>("Mesh").MaterialOverride = voxel.GetNode<MeshInstance3D>("Mesh").MaterialOverride.Duplicate() as ShaderMaterial;
                    voxel.Position = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    Voxels.AddChild(voxel);
                }
            }
        }
    }
    
    public async Task ColorShape(Func<Vector3I, Task<Color>> func)
    {
        for (int x = 0; x < ShapeSize.X; x++)
        {
            for (int y = 0; y < ShapeSize.Y; y++)
            {
                for (int z = 0; z < ShapeSize.Z; z++)
                {
                    SetVoxel(new Vector3I(x, y, z), await func(new Vector3I(x, y, z)));
                }
            }
        }
    }

    public void SetVoxel(Vector3I position, Color color)
    {
        colors[position.X][position.Y][position.Z] = color;

        MeshInstance3D voxel = Voxels.GetNode<MeshInstance3D>($"{position.X};{position.Y};{position.Z}/Mesh");
        if (voxel is null)
        {
            GD.PushError($"Voxel {position} not found. Current shape size: {ShapeSize}.");
            return;
        }

        (voxel.MaterialOverride as ShaderMaterial).SetShaderParameter("color", color);
    }
}
