namespace RondiTrack.Exceptions;

public class WorkflowException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
