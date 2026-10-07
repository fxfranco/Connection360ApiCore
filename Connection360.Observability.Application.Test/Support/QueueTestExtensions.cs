using Connection360.Observability.Application.Pipeline;

namespace Connection360.Observability.Application.Test.Support
{
    public static class QueueTestExtensions
    {
        /// <summary>Saca de la cola todo lo que haya en este momento (sin esperar).</summary>
        public static List<T> DrainAll<T>(this TelemetryQueue<T> queue)
        {
            var items = new List<T>();
            while (queue.Reader.TryRead(out T? item))
            {
                items.Add(item);
            }

            return items;
        }
    }
}
