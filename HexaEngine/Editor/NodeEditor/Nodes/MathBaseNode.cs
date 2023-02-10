﻿namespace HexaEngine.Editor.NodeEditor.Nodes
{
    using HexaEngine.Editor.Materials.Generator;
    using HexaEngine.Editor.NodeEditor.Pins;
    using ImGuiNET;
    using ImNodesNET;

    public abstract class MathBaseNode : Node
    {
        private bool initialized;
        protected PinType mode = PinType.Float;
        protected string[] names;
        protected PinType[] modes;
        protected int item;

        protected MathBaseNode(int id, string name, bool removable, bool isStatic) : base(id, name, removable, isStatic)
        {
            TitleColor = new(0x0069d5ff);
            TitleHoveredColor = new(0x0078f3ff);
            TitleSelectedColor = new(0x007effff);
            modes = new PinType[] { PinType.Float, PinType.Float2, PinType.Float3, PinType.Float4 };
            names = modes.Select(x => x.ToString()).ToArray();
        }

        public PinType Mode
        {
            get => mode;
            set
            {
                mode = value;
                if (initialized)
                    UpdateMode();
            }
        }

        public override void Initialize(NodeEditor editor)
        {
            Out = AddOrGetPin(new FloatPin(editor.GetUniqueId(), "out", PinShape.QuadFilled, PinKind.Output, mode));
            base.Initialize(editor);
            initialized = true;
        }

        protected virtual void UpdateMode()
        {
            item = Array.IndexOf(modes, mode);
            Out.Type = mode;

            switch (mode)
            {
                case PinType.Float:
                    Type = new(Materials.Generator.Enums.ScalarType.Float);
                    break;

                case PinType.Float2:
                    Type = new(Materials.Generator.Enums.VectorType.Float2);
                    break;

                case PinType.Float3:
                    Type = new(Materials.Generator.Enums.VectorType.Float3);
                    break;

                case PinType.Float4:
                    Type = new(Materials.Generator.Enums.VectorType.Float4);
                    break;
            }
        }

        protected override void DrawContentBeforePins()
        {
            ImGui.PushItemWidth(100);
            if (ImGui.Combo("##Mode", ref item, names, names.Length))
            {
                mode = modes[item];
                UpdateMode();
            }
            ImGui.PopItemWidth();
        }

        [JsonIgnore]
        public SType Type { get; protected set; }

        [JsonIgnore]
        public abstract string Op { get; }

        [JsonIgnore]
        public Pin Out { get; private set; }
    }
}