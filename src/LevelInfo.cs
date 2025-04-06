using System.Collections.Generic;
using Chickensoft.Introspection;
using Chickensoft.Serialization;
using Godot;

[Meta, Id("level_info"), Version(LevelsManager.VERSION)]
public partial record LevelInfo {
    // [Save("version")]
    // public required int Version { get; set; }

    [Save("shape_size")]
    public required Vector3I ShapeSize { get; set; }

    [Save("shape_code")]
    public required string ShapeCode { get; set; }

    [Save("default_user_code")]
    public required string DefaultUserCode { get; set; }
}

[Meta, Id("core_levels")]
public partial record CoreLevels {
    [Save("levels")]
    public required List<LevelInfo> Levels { get; set; }
}

[Meta, Id("custom_levels")]
public partial record CustomLevels {
    [Save("levels")]
    public required List<LevelInfo> Levels { get; set; }
}

