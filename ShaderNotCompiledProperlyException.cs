using System;

namespace TQ._3D_Test
{
    [Serializable]
    internal class ShaderNotCompiledProperlyException : Exception
    {
        public int AbnormalStatus { get; }

        public ShaderNotCompiledProperlyException(int abnormalStatus, string infoLog)
            : base(infoLog)
            => AbnormalStatus = abnormalStatus;
    }
}