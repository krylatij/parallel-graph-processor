using System.Collections.Concurrent;
using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.Workers
{
    public class WorkState
    {
        private BlockingCollection<WorkItem> Queue { get; init; }

        private volatile int WorkersCount;

        public ManualResetEvent IsCompleted { get; } = new (false);
        
        public bool TryTake(out WorkItem item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (Queue.TryTake(out item, millisecondsTimeout, cancellationToken))
            {
                Interlocked.Increment(ref WorkersCount);
                return true;
            }

            return false;
        }

        public void RemoveWorker()
        {
            Interlocked.Decrement(ref WorkersCount);

            if (WorkersCount == 0 && Queue.Count == 0)
            {
                Queue.CompleteAdding();
                IsCompleted.Set();
            }
        }
 
    }


    public class WorkKeeper
    {
        public BlockingCollection<WorkItem> TraversingQueue { get; init; }

        public int TotalItemsTraversed;

        public ManualResetEventSlim TraversingCompletedEvent { get; } = new(false);

        private ManualResetEventSlim TraverserIdleHanles { get; set; }



        
        public BlockingCollection<WorkItem> ProcessingQueue { get; init; }

        public int TotalItemsProcessed;

        public ManualResetEventSlim ProcessingCompletedEvent { get; } = new(false);

        public void CompleteTraversing()
        {
            TraversingQueue.CompleteAdding();
            TraversingCompletedEvent.Set();
        }

        public void CompletedProcessing()
        {
            ProcessingQueue.CompleteAdding();
            ProcessingCompletedEvent.Set();
        }
    }
}
