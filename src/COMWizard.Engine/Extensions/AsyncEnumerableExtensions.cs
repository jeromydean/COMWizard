using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace COMWizard.Engine.Extensions
{
  internal static class AsyncEnumerableExtensions
  {
    public static async IAsyncEnumerable<T> Merge<T>(this IEnumerable<IAsyncEnumerable<T>> sources,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
      {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
      });

      List<Task> readers = new List<Task>();

      foreach (IAsyncEnumerable<T> source in sources)
      {
        Task reader = Task.Run(async () =>
        {
          try
          {
            await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
              await channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
            }
          }
          catch (OperationCanceledException) { }
        }, cancellationToken);

        readers.Add(reader);
      }

      _ = Task.WhenAll(readers).ContinueWith(
        _ => channel.Writer.TryComplete(),
        CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);

      await foreach (T item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
      {
        yield return item;
      }
    }

    public static async IAsyncEnumerable<T> Interleave<T>(this IEnumerable<IAsyncEnumerable<T>> sources,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      List<IAsyncEnumerator<T>> enumerators = new List<IAsyncEnumerator<T>>();
      try
      {
        foreach (IAsyncEnumerable<T> source in sources)
        {
          IAsyncEnumerator<T> e = source.GetAsyncEnumerator(cancellationToken);
          enumerators.Add(e);
        }

        List<Task<bool>> moveNextTasks = new List<Task<bool>>();
        foreach (IAsyncEnumerator<T> e in enumerators)
        {
          moveNextTasks.Add(e.MoveNextAsync().AsTask());
        }

        while (moveNextTasks.Count > 0)
        {
          cancellationToken.ThrowIfCancellationRequested();

          Task<bool> finished = await Task.WhenAny(moveNextTasks).ConfigureAwait(false);
          int index = moveNextTasks.IndexOf(finished);

          bool hasItem = await finished.ConfigureAwait(false);
          if (hasItem)
          {
            yield return enumerators[index].Current;
            moveNextTasks[index] = enumerators[index].MoveNextAsync().AsTask();
          }
          else
          {
            moveNextTasks.RemoveAt(index);
            await enumerators[index].DisposeAsync().ConfigureAwait(false);
            enumerators.RemoveAt(index);
          }
        }
      }
      finally
      {
        foreach (IAsyncEnumerator<T> e in enumerators)
        {
          await e.DisposeAsync().ConfigureAwait(false);
        }
      }
    }
  }
}