namespace TQ._3D_Test
{
    readonly struct AttributeLocation
    {
        readonly uint _location;
        public AttributeLocation(uint location) => _location = location;

        public static explicit operator uint(AttributeLocation location) => location._location;
    }
}