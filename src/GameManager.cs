using System.Threading.Tasks;
using Godot;
using Lua;

public partial class GameManager : Node
{
    public ShapePreview SampleShapePreview { get; private set; }
    public ShapePreview UserShapePreview { get; private set; }

    public CodeEditor CodeEditor { get; private set; }

    GameManager()
    {
        Find.GameManager = this;
    }

    override public void _Ready()
    {
        var output = GetNode<Output>("%Output");

        SampleShapePreview = GetNode<ShapePreview>("%SampleShapePreview");
        UserShapePreview = GetNode<ShapePreview>("%UserShapePreview");

        CodeEditor = GetNode<CodeEditor>("%CodeEditor");
        CodeEditor.RunPressed += async () => {
            await UserShapePreview.ColorShape(async (pos) => {
                var code = $"x={pos.X}\ny={pos.Y}\nz={pos.Z}\n\n" + CodeEditor.Text;

                LuaValue[] luaValues = await Find.LuaManager.Run(code);

                if (luaValues.Length <= 0)
                {
                    Find.Output.Error($"Nothing returned on {pos}.");
                    return Colors.Purple * 8f;
                }

                if (!luaValues[0].TryRead(out LuaColor luaColor))
                {
                    Find.Output.Error($"The returned value on {pos} is invalid. (it's probably not a color)");
                    return Colors.Purple * 8f;
                }

                return luaColor.GodotColor;
            });

            if (DoShapesMatch())
                Win();
            else
                Find.Output.Error("Shapes don't match.");
        };

        Find.LevelsManager.LoadLevel();
    }

    public override void _Process(double delta)
    {
        SampleShapePreview.GetNode<Label>("../ZoomLabel").Text = $"Zoom: {SampleShapePreview.desiredOrbitDistance}";
        UserShapePreview.GetNode<Label>("../ZoomLabel").Text = $"Zoom: {UserShapePreview.desiredOrbitDistance}";
    
        if (Input.IsActionJustPressed("debug_skip_level"))
            Win();
    }

    private async void Win()
    {
        Find.Confetti.Trigger();

        await Task.Delay(3000);

        // doesnt account for custom levels
        if (Find.LevelsManager.CurrentLevelID >= Find.LevelsManager.CoreLevels.Levels.Count - 1)
        {
            if (Find.LevelsManager.CustomLevelsAvailable)
            {
                Find.LevelsManager.StartCustomLevels();
                return;
            }
            EndGame();
            return;
        }

        Find.LevelsManager.NextLevel();
    }

    private void EndGame()
    {
        GD.Print("game end");
    }

    private bool DoShapesMatch()
    {
        for (var x = 0; x < Find.LevelsManager.CurrentLevel.ShapeSize.X; x++)
        {
            for (var y = 0; y < Find.LevelsManager.CurrentLevel.ShapeSize.Y; y++)
            {
                for (var z = 0; z < Find.LevelsManager.CurrentLevel.ShapeSize.Z; z++)
                {
                    if (UserShapePreview.colors[x][y][z] != SampleShapePreview.colors[x][y][z])
                        return false;
                }
            }
        }
        return true;
    }
}
