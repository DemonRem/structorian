using Structorian.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Structorian
{
    public class PatchData
    {
        public long _offset;
        public int _dataSize;
        public object _dataValue;
        public object _oldValue;
        public StructField _structField;
    }
}
