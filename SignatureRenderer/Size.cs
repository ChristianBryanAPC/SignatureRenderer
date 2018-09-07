using System;
using System.Collections.Generic;
using System.Text;

namespace SignatureRenderer
{
    public class Size
    {
        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public Size(uint width, uint height)
        {
            Width = width;
            Height = height;
        }
    }
}
