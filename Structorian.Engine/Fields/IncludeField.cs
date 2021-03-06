using System;
using System.IO;

namespace Structorian.Engine.Fields
{
    class IncludeField: StructField
    {
        public IncludeField(StructDef structDef) : base(structDef, "struct", false)
        {
        }

        public override void LoadData(BinaryReader reader, StructInstance instance)
        {
            StructDef structDef = GetIncludedStruct();

            if (structDef.FieldLike)
                instance.PushAddedCellHandler(cell => cell.Tag = Tag);
            bool oldHidden = instance.HideAddedCells(_hidden);
            
            try
            {
                structDef.LoadInstanceData(instance, reader.BaseStream);
            }
            finally
            {
                if (structDef.FieldLike)
                    instance.PopAddedCellHandler();
                instance.HideAddedCells(oldHidden);
            }

            if (GetBoolAttribute("replace"))
                instance.SetNodeName(structDef.Name);
        }

        private StructDef GetIncludedStruct()
        {
            return GetStructAttribute("struct");
        }

        public override bool ProvidesChildren()
        {
            return GetIncludedStruct().HasChildProvidingFields();
        }

        public override int GetDataSize()
        {
            return GetIncludedStruct().GetDataSize();
        }
    }
}
