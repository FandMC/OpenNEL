namespace OpenNEL.SDK.Plugin;

public interface IPlugin
{
    void OnInitialize();

    void OnUnload() { }
}
