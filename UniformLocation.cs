using System;

namespace TQ._3D_Test
{
    readonly struct UniformLocation
    {
        readonly uint _location;
        public UniformLocation(uint location) => _location = location;

        public static explicit operator uint(UniformLocation location) => location._location;

        public static explicit operator int(UniformLocation location) => (int)location._location;
    }
}