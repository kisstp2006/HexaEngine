﻿namespace HexaEngine.Materials.Nodes.Buildin
{
    using HexaEngine.Materials;
    using HexaEngine.Materials.Nodes;
    using HexaEngine.Materials.Pins;

    public class ReflectNode : FuncCallNodeBase
    {
        public ReflectNode(int id, bool removable, bool isStatic) : base(id, "Reflect", removable, isStatic)
        {
        }

        public override void Initialize(NodeEditor editor)
        {
            AddOrGetPin(new FloatPin(editor.GetUniqueId(), "Left", PinShape.QuadFilled, PinKind.Input, mode, 1));
            AddOrGetPin(new FloatPin(editor.GetUniqueId(), "Right", PinShape.QuadFilled, PinKind.Input, mode, 1));
            base.Initialize(editor);
            UpdateMode();
        }

        public override string Op { get; } = "reflect";
    }
}