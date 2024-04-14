﻿namespace HexaEngine.Tests
{
    using HexaEngine.Mathematics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;

    public class MathematicsTests
    {
        [Test]
        public void ProjectVec2()
        {
            Vector2 vector = new(1, 2);
            Vector2 onto = new(10, 5);
            Vector2 projectedVectorA = MathUtil.Project(vector, onto);
            Vector2 projectedVectorB = Project(vector, onto);

            Assert.That(projectedVectorA, Is.EqualTo(projectedVectorB));
        }

        [Test]
        public void ProjectVec2Zero()
        {
            Vector2 vector = new(1, 2);
            Vector2 onto = new(0, 0);
            Vector2 normalizedVector = MathUtil.Project(vector, onto);
            Vector2 normalizedVec = Project(vector, onto);

            Assert.That(normalizedVector, Is.EqualTo(normalizedVec));
        }

        private static Vector2 Project(Vector2 vector, Vector2 onto)
        {
            float dot = Vector2.Dot(vector, onto);
            float ontoLengthSquared = onto.LengthSquared();

            // Handle edge case when onto vector is zero
            if (ontoLengthSquared < float.Epsilon)
            {
                return Vector2.Zero;
            }

            return onto * (dot / ontoLengthSquared);
        }

        [Test]
        public void NormalizeVec4()
        {
            Vector4 vector = new(1, 2, 3, 4);
            Vector4 normalizedVector = MathUtil.Normalize(vector);
            Vector4 normalizedVec = Vector4.Normalize(vector);

            Assert.That(normalizedVector, Is.EqualTo(normalizedVec));
        }

        [Test]
        public void NormalizeVec3()
        {
            Vector3 vector = new(1, 2, 3);
            Vector3 normalizedVector = MathUtil.Normalize(vector);
            Vector3 normalizedVec = Vector3.Normalize(vector);

            Assert.That(normalizedVector, Is.EqualTo(normalizedVec));
        }

        [Test]
        public void NormalizeVec2()
        {
            Vector2 vector = new(1, 2);
            Vector2 normalizedVector = MathUtil.Normalize(vector);
            Vector2 normalizedVec = Vector2.Normalize(vector);

            Assert.That(normalizedVector, Is.EqualTo(normalizedVec));
        }

        [Test]
        public void NormalizeVec4D()
        {
            Vector4D vector = new(1, 2, 3, 4);
            Vector4D normalizedVector = MathUtil.Normalize(vector);
            Vector4D normalizedVec = Vector4D.Normalize(vector);

            Assert.That(normalizedVector, Is.EqualTo(normalizedVec));
        }

        [Test]
        public void NormalizeVec3D()
        {
            Vector3D vector = new(1, 2, 3);
            Vector3D normalizedVector = MathUtil.Normalize(vector);
            Vector3D normalizedVec = Vector3D.Normalize(vector);

            Assert.That(normalizedVector, Is.EqualTo(normalizedVec));
        }

        [Test]
        public void NormalizeVec2D()
        {
            Vector2D vector = new(1, 2);
            Vector2D normalizedVector = MathUtil.Normalize(vector);
            Vector2D normalizedVec = Vector2D.Normalize(vector);

            Assert.That(normalizedVector, Is.EqualTo(normalizedVec));
        }

        [Test]
        public void DotVec4()
        {
            Vector4 vector = new(1, 2, 3, 4);
            double dot0 = MathUtil.Dot(vector, vector);
            double dot1 = Vector4.Dot(vector, vector);

            Assert.That(dot0, Is.EqualTo(dot1));
        }

        [Test]
        public void DotVec3()
        {
            Vector3 vector = new(1, 2, 3);
            double dot0 = MathUtil.Dot(vector, vector);
            double dot1 = Vector3.Dot(vector, vector);

            Assert.That(dot0, Is.EqualTo(dot1));
        }

        [Test]
        public void DotVec2()
        {
            Vector2 vector = new(1, 2);
            double dot0 = MathUtil.Dot(vector, vector);
            double dot1 = Vector2.Dot(vector, vector);

            Assert.That(dot0, Is.EqualTo(dot1));
        }

        [Test]
        public void DotVec4D()
        {
            Vector4D vector = new(1, 2, 3, 4);
            double dot0 = MathUtil.Dot(vector, vector);
            double dot1 = Vector4D.Dot(vector, vector);

            Assert.That(dot0, Is.EqualTo(dot1));
        }

        [Test]
        public void DotVec3D()
        {
            Vector3D vector = new(1, 2, 3);
            double dot0 = MathUtil.Dot(vector, vector);
            double dot1 = Vector3D.Dot(vector, vector);

            Assert.That(dot0, Is.EqualTo(dot1));
        }

        [Test]
        public void DotVec2D()
        {
            Vector2D vector = new(1, 2);
            double dot0 = MathUtil.Dot(vector, vector);
            double dot1 = Vector2D.Dot(vector, vector);

            Assert.That(dot0, Is.EqualTo(dot1));
        }
    }
}