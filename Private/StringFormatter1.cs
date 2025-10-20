using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "TestProject1", Guid = "5c43f51785599b8b9d3c807e2c3078c6")]
public class StringFormatter1 : FTOptix.CoreBase.StringFormatter
{
#region Children properties
    //-------------------------------------------
    // WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
    //-------------------------------------------
    public object Source0
    {
        get
        {
            return (object)Refs.GetVariable("Source0").Value.Value;
        }
        set
        {
            Refs.GetVariable("Source0").SetValue(value);
        }
    }
    public IUAVariable Source0Variable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Source0");
        }
    }
#endregion
}
