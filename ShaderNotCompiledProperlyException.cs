using System;

namespace TQ._3D_Test
{
    [Serializable]
    internal class ShaderNotCompiledProperlyException : Exception
    {
        private int _abnormalStatus;

        public ShaderNotCompiledProperlyException(int abnormalStatus, string infoLog)
            : base(infoLog)
            => _abnormalStatus = abnormalStatus;
    }
}