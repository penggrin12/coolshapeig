using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Lua;
using Lua.Internal;
using Lua.Standard;

[LuaObject]
public partial class LuaColor
{
    public Color GodotColor { get; private set; }

    public LuaColor()
    {

    }

    public LuaColor(Color color)
    {
        GodotColor = color;
    }

    public LuaColor(float r, float g, float b)
    {
        GodotColor = new(r, g, b);
    }

    [LuaMember("r")] public int R => GodotColor.R8;
    [LuaMember("g")] public int G => GodotColor.G8;
    [LuaMember("b")] public int B => GodotColor.B8;

    [LuaMember("new")]
    public static LuaColor New(int r, int g, int b)
    {
        return new LuaColor(
            Mathf.Clamp(r, 0, 255) / 255f,
            Mathf.Clamp(g, 0, 255) / 255f,
            Mathf.Clamp(b, 0, 255) / 255f
        );
    }

    [LuaMetamethod(LuaObjectMetamethod.ToString)]
    public override string ToString()
    {
        return $"[{GodotColor.R8} {GodotColor.G8} {GodotColor.B8}]";
    }
}

public static class LuaValueExtensions
{
    public async static Task<string> BetterToString(this LuaValue value, LuaState state, CancellationToken? cancellationToken)
    {
        cancellationToken ??= CancellationToken.None;

        if (value.Type == LuaValueType.UserData)
        {
            var metaTable = value.Read<ILuaUserData>().Metatable;
            if ((metaTable is not null) && metaTable.ContainsKey("__tostring"))
            {
                return (await metaTable["__tostring"].Read<LuaFunction>().InvokeAsync(state, [value], cancellationToken: cancellationToken.Value))[0].ToString();
            }
        }
        else if (value.Type == LuaValueType.Table)
        {
            var table = value.Read<LuaTable>();
            var metaTable = table.Metatable;

            if ((metaTable is not null) && metaTable.ContainsKey("__tostring"))
            {
                return (await metaTable["__tostring"].Read<LuaFunction>().InvokeAsync(state, [value], cancellationToken: cancellationToken.Value))[0].ToString();
            }
            else if (table.ArrayLength > 0)
            {
                string[] array = new string[table.ArrayLength];
                for (var j = 0; j < table.ArrayLength; j++)
                {
                    array[j] = await table[j + 1].BetterToString(state, cancellationToken);
                }
                return $"{{{array.Join(", ")}}}";
            }
            else if (table.HashMapCount > 0)
            {
                // TODO: doesnt really work because
                // the lua state is currently running

                // var lastValue = state.Environment["table"];
                // var keysAndValues = await state.DoStringAsync(@"
                //     local keys={}
                //     local values={}
                //     for k,v in pairs(table) do
                //         table.insert(keys,k)
                //         table.insert(values,v)
                //     end
                //     return keys,values
                // ");
                // state.Environment["table"] = lastValue;
                
                // LuaTable keysTable = keysAndValues[0].Read<LuaTable>();
                // LuaTable valuesTable = keysAndValues[1].Read<LuaTable>();

                // string[] resultArray = new string[table.HashMapCount];
                // for (var j = 0; j < table.HashMapCount; j++)
                // {
                //     var tableKey = await keysTable[j - 1].BetterToString(state, cancellationToken);
                //     var tableValue = await valuesTable[j - 1].BetterToString(state, cancellationToken);

                //     resultArray[j] = $"{{{tableKey}: {tableValue}}}";
                // }

                // return $"{{{resultArray.Join(", ")}}}";
            }
        }

        return value.ToString();
    }
}

public partial class LuaManager : Node
{
    public static Dictionary<string, LuaColor> BakedColors { get; private set; } = [];
    public static List<string> BlacklistedFunctions { get; private set; } = [
        "rawequal", "rawset", "rawlen", "rawget", "pcall", "xpcall", "dofile",
        "load", "loadfile", "setmetatable", "getmetatable", "collectgarbage"
    ];
    public LuaState State { get; private set; }

    LuaManager()
    {
        Find.LuaManager = this;

        foreach (var property in typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.Public))
        {
            BakedColors.Add(property.Name.ToSnakeCase().ToUpper(), new LuaColor((Color)property.GetValue(null)));
        }
    }

    private async ValueTask<int> Print(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        using PooledArray<LuaValue> methodBuffer = new(1);

        var argumentStrings = await Task.WhenAll(context.Arguments.ToImmutableArray().Select(x => x.BetterToString(State, cancellationToken)));
        Find.Output.Print(string.Join(' ', argumentStrings));

        return 0;
    }

    public override void _Ready()
    {
        State = LuaState.Create();
        State.OpenBasicLibrary();
        State.OpenMathLibrary();

        State.Environment["print"] = new LuaFunction("print", Print);
        State.Environment["ternary"] = new LuaFunction("ternary", (context, buffer, cancellationToken) =>
        {
            buffer.Span[0] = context.GetArgument(context.GetArgument(0).ToBoolean() ? 1 : 2);
            return new(1);
        });

        foreach (var blacklistedFunctionName in BlacklistedFunctions)
        {
            State.Environment[blacklistedFunctionName] = LuaValue.Nil;
        }

        State.Environment["Color"] = new LuaColor();
        foreach ((string colorName, LuaColor color) in BakedColors)
        {
            State.Environment[colorName] = color;
        }
    }

    public async Task<LuaValue[]> Run(string code)
    {
        try
        {
            return await State.DoStringAsync(code, "");
        }
        catch (LuaRuntimeException e)
        {
            Find.Output.Error(e.LuaTraceback.ToString().TrimStart() + "\n" + e.Message.ToString());
            throw;
        }
        catch (LuaParseException e)
        {
            Find.Output.Error(e.Message);
            throw;
        }
    }
}
