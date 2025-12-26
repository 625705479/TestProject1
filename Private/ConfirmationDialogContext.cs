using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "TestProject1", Guid = "8be70b385fa6b0c193148c04d2961b0b")]
public class ConfirmationDialogContext : UAObject
{
#region Children properties
    //-------------------------------------------
    // WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
    //-------------------------------------------
    public FTOptix.CoreBase.MethodInvocation OnConfirm
    {
        get
        {
            return (FTOptix.CoreBase.MethodInvocation)Refs.GetObject("OnConfirm");
        }
    }
    public FTOptix.CoreBase.MethodInvocation OnCancel
    {
        get
        {
            return (FTOptix.CoreBase.MethodInvocation)Refs.GetObject("OnCancel");
        }
    }
    public UAManagedCore.LocalizedText Message
    {
        get
        {
            return (UAManagedCore.LocalizedText)Refs.GetVariable("Message").Value.Value;
        }
        set
        {
            Refs.GetVariable("Message").SetValue(value);
        }
    }
    public IUAVariable MessageVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Message");
        }
    }
#endregion
}
