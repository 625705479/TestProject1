using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "TestProject1", Guid = "611bf863c203a120932391c0fe7f3592")]
public class ExpressionEvaluator1 : FTOptix.CoreBase.ExpressionEvaluator
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
    public object Source2
    {
        get
        {
            return (object)Refs.GetVariable("Source2").Value.Value;
        }
        set
        {
            Refs.GetVariable("Source2").SetValue(value);
        }
    }
    public IUAVariable Source2Variable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Source2");
        }
    }
    public object Source1
    {
        get
        {
            return (object)Refs.GetVariable("Source1").Value.Value;
        }
        set
        {
            Refs.GetVariable("Source1").SetValue(value);
        }
    }
    public IUAVariable Source1Variable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Source1");
        }
    }
#endregion
}
