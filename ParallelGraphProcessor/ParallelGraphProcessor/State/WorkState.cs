using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ParallelGraphProcessor.State;

public abstract class WorkState<T>
{
    private readonly ILogger<WorkState<T>> _logger;

    private readonly BlockingCollection<T> _queue;

    private Func<bool>? _preconditionToComplete;

    private volatile int _totalItemsProcessed;

    private volatile int _workersCounter;

    public int TotalItemsProcessed => _totalItemsProcessed;

    public int Count => _queue.Count;

    public int BoundedCapacity => _queue.BoundedCapacity;

    public WorkState(int queueSize, ILogger<WorkState<T>> logger)
    {
        _logger = logger;
        _queue = new BlockingCollection<T>(new ConcurrentQueue<T>(), queueSize);
    }

    public ManualResetEvent IsCompletedEvent { get; } = new(false);

    public bool IsCompleted => _queue.IsCompleted;

    public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if (_queue.TryTake(out item, millisecondsTimeout, cancellationToken))
        {
            Interlocked.Increment(ref _workersCounter);
            return true;
        }

        return false;
    }

    public void Add(T item, CancellationToken cancellationToken)
    {
        _queue.Add(item, cancellationToken);
    }

    public void Commit()
    {
        Interlocked.Decrement(ref _workersCounter);
        Interlocked.Increment(ref _totalItemsProcessed);

        if (_workersCounter == 0 && _queue.Count == 0)
        {
            if (_preconditionToComplete != null && !_preconditionToComplete())
            {
                // we expect that producing is not completed yet and new items will be added to the queue
                _logger.LogWarning("Queue is empty, workers are idle, but a precondition didn't succeeded.");
            }
            else
            {
                _queue.CompleteAdding();
                IsCompletedEvent.Set();
            }
        }
    }

    public void RegisterPrecondition(Func<bool> preconditionToComplete)
    {
        _preconditionToComplete = preconditionToComplete;
    }
}