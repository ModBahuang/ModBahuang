
using UnityEngine;

namespace Hylas
{

#pragma warning disable 649
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable InconsistentNaming

    /* Avoid serialize issue caused by unhollower */
    internal struct Rect
    {
        public Vector2 position;
        public Vector2 size;

        public static implicit operator UnityEngine.Rect(Rect v)
        {
            return new UnityEngine.Rect(v.position, v.size);
        }
    }

    internal struct Vector2
    {
        public float x;
        public float y;

        public static implicit operator UnityEngine.Vector2(Vector2 v)
        {
            return new UnityEngine.Vector2(v.x, v.y);
        }
    }
    internal struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static implicit operator UnityEngine.Vector4(Vector4 v)
        {
            return new UnityEngine.Vector4(v.x, v.y, v.z, v.w);
        }
    }


    internal struct SpriteParam
    {
        public Rect rect; 
        public Vector2 pivot; 
        public float pixelsPerUnit; 
        public uint extrude; 
        public SpriteMeshType meshType; 
        public Vector4 border;
        public bool generateFallbackPhysicsShape;
    }

    // ReSharper restore MemberCanBePrivate.Global
    // ReSharper restore UnassignedField.Global
    // ReSharper restore InconsistentNaming
#pragma warning restore 649
}
