using System.Collections.Generic;

namespace DisasterReady
{
    /// <summary>Mochila de emergencia: capacidad limitada por espacios.</summary>
    public class Inventory
    {
        readonly List<ItemData> items = new List<ItemData>();

        public int Capacity { get; }
        public int Used { get; private set; }
        public int Free => Capacity - Used;
        public IReadOnlyList<ItemData> Items => items;

        public Inventory(int capacity)
        {
            Capacity = capacity;
        }

        public bool Has(string id)
        {
            foreach (var item in items)
                if (item.Id == id) return true;
            return false;
        }

        public int UsefulCount
        {
            get
            {
                int n = 0;
                foreach (var item in items) if (item.Useful) n++;
                return n;
            }
        }

        public int UselessCount => items.Count - UsefulCount;

        /// <summary>Intenta guardar el objeto. Si no se puede, devuelve el motivo.</summary>
        public bool TryAdd(ItemData item, out string reason)
        {
            if (Has(item.Id))
            {
                reason = "Ya tienes " + item.Name + " en la mochila.";
                return false;
            }
            if (item.Size > Free)
            {
                reason = $"Mochila llena: {item.Name} necesita {item.Size} espacios y quedan {Free}. Pulsa Q para sacar el último objeto.";
                return false;
            }
            items.Add(item);
            Used += item.Size;
            reason = "";
            return true;
        }

        /// <summary>Saca el último objeto guardado.</summary>
        public bool TryRemoveLast(out ItemData item)
        {
            if (items.Count == 0)
            {
                item = default;
                return false;
            }
            item = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            Used -= item.Size;
            return true;
        }
    }
}
