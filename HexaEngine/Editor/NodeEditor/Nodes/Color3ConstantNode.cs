﻿namespace HexaEngine.Editor.NodeEditor.Nodes
{
    using HexaEngine.Editor.NodeEditor;
    using ImGuiNET;
    using System.Numerics;

    public class Color3ConstantNode : Node
    {
        public Color3ConstantNode(NodeEditor graph, bool removable, bool isStatic) : base(graph, "Color3", removable, isStatic)
        {
            CreatePin("out", PinKind.Output, PinType.Vector3, ImNodesNET.PinShape.CircleFilled);
        }

        public Vector3 Value;

        protected override void DrawContent()
        {
            ImGui.PushItemWidth(100);
            ImGui.ColorEdit3("Value", ref Value);
            ImGui.PopItemWidth();
        }
    }
}