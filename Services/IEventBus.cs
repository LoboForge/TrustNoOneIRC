using System.Collections.Concurrent;
using LoboForge.TNOIRC.Services;

public static class EventBus
{
    private static readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public static void Publish<T>(T @event) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var delegates))
        {
            foreach (var handler in delegates.OfType<Action<T>>())
            {
                try
                {
                    handler(@event);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EventBus Error] {ex.Message}");
                }
            }

        }
    }

    public static void Subscribe<T>(Action<T> handler) where T : class
    {
        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
        lock (list)
        {
            if (!list.Contains(handler))
                list.Add(handler);
        }
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
        {
            lock (list)
            {
                list.Remove(handler);
            }
        }
    }
}
