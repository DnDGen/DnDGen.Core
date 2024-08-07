﻿using System.IO;

namespace DnDGen.Infrastructure.Tables
{
    internal interface StreamLoader
    {
        Stream LoadFor(string assemblyName, string filename);
    }
}