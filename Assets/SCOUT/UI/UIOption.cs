public abstract class UIOption : NamedOption
{
    public virtual bool isToggle => false;
    public abstract void DoAction(ScoutModeManager modeManager);
}