using System;

namespace TQ._3D_Test
{
    readonly struct AttributeIndex
    {
        readonly uint _index;
        public AttributeIndex(uint index) => _index = index;

        public static explicit operator uint(AttributeIndex attributeIndex) => attributeIndex._index;
    }
}