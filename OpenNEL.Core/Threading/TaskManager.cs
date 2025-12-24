namespace OpenNEL.Core.Threading;

public class TaskManager
{
    private static TaskManager? _instance;
    private TaskFactory? _factory;

    public static TaskManager Instance => _instance ??= new TaskManager();

    private TaskManager()
    {
    }

    public TaskFactory GetFactory()
    {
        return _factory ??= new TaskFactory();
    }
}
