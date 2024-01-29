using AasCore.Aas3_0;
using NPOI.SS.Formula.Eval;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginVec;

public static class Vws4lsCapabilitySMUtils
{
    public const string SEM_ID_CAP_CUT = "https://arena2036.de/vws4ls/capability/1/0/CutCapability";
    public const string SEM_ID_CAP_STRIP = "https://arena2036.de/vws4ls/capability/1/0/StripCapability";
    public const string SEM_ID_CAP_CRIMP = "https://arena2036.de/vws4ls/capability/1/0/CrimpCapability";

    public static Dictionary<string, List<Property>> ConstraintsByCapability = new Dictionary<string, List<Property>>()
    {
        {
            SEM_ID_CAP_CUT, new List<Property>()
            {
                new Property() {Name = "WireType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "WireCrossSectionArea", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "WireNominalLength", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
            }
        },
        {
            SEM_ID_CAP_STRIP, new List<Property>()
            {
                new Property() {Name = "NominalStrippingLength", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "CenterStripping", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "Layer", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "IncisionMonitoring", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
            }
        },
        {
            SEM_ID_CAP_CRIMP, new List<Property>()
            {
                new Property() {Name = "WireType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "WireCrossSectionArea", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "TerminalPartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "CrimpForceMonitoring", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
            }
        }
    };
    
    public class Property
    {
        public string Name { get; set; }
        public DataTypeDefXsd ValueType { get; set; }
        public ConstraintType ConstraintType { get; set; }
    }

    public enum ConstraintType
    {
        FixedValue = 0,
        Range = 1,
        List = 2
    }
}
