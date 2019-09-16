﻿using System;
using System.Runtime.Serialization;

namespace Juniper.ConfigurationManagement
{
    [Serializable]
    public class PackageDefineSymbol : ISerializable
    {
        public readonly string Name;
        public readonly string CompilerDefine;

        public PackageDefineSymbol(string name, string compilerDefine)
        {
            Name = name;
            CompilerDefine = compilerDefine;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(CompilerDefine), CompilerDefine);
        }

        protected PackageDefineSymbol(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString(nameof(Name));
            CompilerDefine = info.GetString(nameof(CompilerDefine));
        }
    }
}
