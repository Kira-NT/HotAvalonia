namespace HotAvalonia;

/// <summary>
/// Represents an MSBuild task.
/// </summary>
public abstract class MSBuildTask : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Executes the task.
    /// </summary>
    /// <returns><c>true</c> if the task executed successfully; otherwise, <c>false</c>.</returns>
    public sealed override bool Execute()
    {
        try
        {
            ExecuteCore();
        }
        catch (Exception e)
        {
            Log.LogErrorFromException(e);
        }
        return !Log.HasLoggedErrors;
    }

    /// <summary>
    /// When overridden in a derived class, executes the core logic of the task.
    /// </summary>
    protected abstract void ExecuteCore();
}
