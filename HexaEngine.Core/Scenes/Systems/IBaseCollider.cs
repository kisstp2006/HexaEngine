﻿namespace HexaEngine.Core.Scenes.Systems
{
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using HexaEngine.Core.Physics;
    using HexaEngine.Core.Scenes;

    public interface IBaseCollider : IComponent
    {
        BodyHandle BodyHandle { get; }

        CompoundChild? CompoundChild { get; }

        bool HasBody { get; }

        bool HasShape { get; }

        bool InCompound { get; }

        float Mass { get; set; }

        ICompoundCollider? ParentCollider { get; }

        TypedIndex ShapeIndex { get; }

        float SleepThreshold { get; set; }

        StaticHandle StaticHandle { get; }

        ColliderType Type { get; set; }

        void BuildCompound(ref CompoundBuilder builder);

        void CreateBody();

        void CreateShape();

        void DestroyBody();

        void DestroyCompound();

        void DestroyShape();

        void SetCompoundData(ICompoundCollider parentCollider, CompoundChild compoundChild);
    }
}