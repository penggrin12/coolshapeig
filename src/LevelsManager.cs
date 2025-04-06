using Godot;
using System.Text.Json;
using Chickensoft.Serialization;
using Chickensoft.Serialization.Godot;

// TODO: add a way to start playing custom levels whenever
// TODO: add a proper way to create custom levels

public partial class LevelsManager : Node
{
    public const byte VERSION = 1;

    [Signal] public delegate void LevelAboutToChangeEventHandler();

    public CoreLevels CoreLevels { get; set; } = new() { Levels = [] };
    public CustomLevels CustomLevels { get; set; } = new() { Levels = [] };
    [Export] public int CurrentLevelID { get; set; } = 0;
    public LevelInfo CurrentLevel => IsInCustomLevel ? CustomLevels.Levels[CurrentLevelID] : CoreLevels.Levels[CurrentLevelID];
    public bool IsInCustomLevel { get; private set; } = false;
    public bool CustomLevelsAvailable => CustomLevels.Levels.Count > 0;

    private readonly JsonSerializerOptions jsonOptions = new() {
        WriteIndented = true,
        TypeInfoResolver = new SerializableTypeResolver(),
        Converters = { new SerializableTypeConverter() }
    };

    LevelsManager()
    {
        GodotSerialization.Setup();
        Find.LevelsManager = this;
    }

    public override void _Ready()
    {
        CoreLevels = JsonSerializer.Deserialize<CoreLevels>(FileAccess.GetFileAsString("res://core_levels.json"), jsonOptions);

        if (FileAccess.FileExists("user://custom_levels.json"))
        {
            CustomLevels = JsonSerializer.Deserialize<CustomLevels>(FileAccess.GetFileAsString("user://custom_levels.json"), jsonOptions);

            // foreach (var customLevel in CustomLevels.Levels.Select((Value, Index) => new { Index, Value }))
            // {
            //     if (customLevel.Value.Version != VERSION)
            //     {
            //         throw new Exception($"Custom level ({customLevel.Index}) version is too {(customLevel.Value.Version < VERSION ? "old" : "new")} (level: {customLevel.Value.Version}, current: {VERSION})");
            //     }
            // }
        }
        else
        {
            SaveCustomLevels();
        }
    }

    private void SaveCustomLevels()
    {
        string json = JsonSerializer.Serialize(CustomLevels, jsonOptions);
        FileAccess.Open("user://custom_levels.json", FileAccess.ModeFlags.Write).StoreString(json);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("add_custom_level"))
        {
            LevelInfo level = new()
            {
                // Version = VERSION,
                ShapeSize = new Vector3I(8, 8, 8), // creator has to change this manually
                ShapeCode = Find.GameManager.CodeEditor.Text,
                DefaultUserCode = "return WHITE" // creator has to change this manually aswell
            };
            CustomLevels.Levels.Add(level);
            SaveCustomLevels();
        }
    }

    public async void StartCustomLevels()
    {
        IsInCustomLevel = true;

        EmitSignal(SignalName.LevelAboutToChange);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        CurrentLevelID = 0;
        LoadLevel();
    }

    public async void NextLevel()
    {
        EmitSignal(SignalName.LevelAboutToChange);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        CurrentLevelID++;
        LoadLevel();
    }

    public async void LoadLevel()
    {
        LevelInfo level = CurrentLevel;

        Find.GameManager.SampleShapePreview.ShapeSize = level.ShapeSize;
        await Find.GameManager.SampleShapePreview.Make();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await Find.GameManager.SampleShapePreview.ColorShape(async pos => (await Find.LuaManager.Run($"x={pos.X}\ny={pos.Y}\nz={pos.Z}\n\n" + level.ShapeCode))[0].Read<LuaColor>().GodotColor);

        Find.GameManager.UserShapePreview.ShapeSize = level.ShapeSize;
        await Find.GameManager.UserShapePreview.Make();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Find.GameManager.CodeEditor.Text = level.DefaultUserCode;
        await Find.GameManager.UserShapePreview.ColorShape(async pos => (await Find.LuaManager.Run($"x={pos.X}\ny={pos.Y}\nz={pos.Z}\n\n" + level.DefaultUserCode))[0].Read<LuaColor>().GodotColor);
    }
}
