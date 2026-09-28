using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

namespace QueComemos.Data
{
    [Serializable]
    public class ShoppingItemData
    {
        public string id;
        public string text;
        public bool done;
        public long createdAt;
    }

    //FirebaseManager.Shopping
    public partial class FirebaseManager
    {
        private DatabaseReference shoppingRef;
        private List<ShoppingItemData> lastShoppingItems;
        private bool hasShoppingData;

        private event Action<List<ShoppingItemData>> OnShoppingListChanged;

        // ==========================================================
        //  Suscripción
        // ==========================================================

        /// <summary>
        /// Suscribe un callback a la lista de la compra del calendario activo.
        /// Si ya había datos, se los entrega enseguida.
        /// </summary>
        public void SubscribeToShoppingList(Action<List<ShoppingItemData>> callback)
        {
            OnShoppingListChanged += callback;
            if (hasShoppingData)
            {
                callback(lastShoppingItems);
            }
        }

        public void UnsubscribeFromShoppingList(Action<List<ShoppingItemData>> callback)
        {
            OnShoppingListChanged -= callback;
        }

        // ==========================================================
        //  Enganche con el calendario activo (se llama desde
        //  SetCalendarId y OnDestroy de FirebaseManager.cs)
        // ==========================================================

        private void AttachShoppingRef(string calendarId)
        {
            DetachShoppingRef();

            // Los datos en caché son del calendario anterior
            lastShoppingItems = null;
            hasShoppingData = false;

            shoppingRef = database.RootReference
                .Child("calendars").Child(calendarId).Child("shoppinglist");
            shoppingRef.ValueChanged += HandleShoppingValueChanged;
        }

        private void DetachShoppingRef()
        {
            if (shoppingRef != null)
            {
                shoppingRef.ValueChanged -= HandleShoppingValueChanged;
                shoppingRef = null;
            }
        }

        private void HandleShoppingValueChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"[FirebaseManager] Error leyendo shoppinglist: {args.DatabaseError.Message}");
                OnError?.Invoke("No se pudo cargar la lista de la compra.");
                return;
            }

            var items = ShoppingFromFirebaseValue(args.Snapshot.Value);
            lastShoppingItems = items;
            hasShoppingData = true;
            OnShoppingListChanged?.Invoke(items);
        }

        // ==========================================================
        //  Operaciones (cada una escribe solo su propio nodo)
        // ==========================================================

        public async Task AddShoppingItemAsync(string text)
        {
            if (!IsReady || shoppingRef == null || string.IsNullOrWhiteSpace(text)) return;

            try
            {
                var newRef = shoppingRef.Push();
                await newRef.SetValueAsync(new Dictionary<string, object>
                {
                    ["text"] = text.Trim(),
                    ["done"] = false,
                    ["createdAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error añadiendo elemento: {e}");
                OnError?.Invoke("No se pudo añadir el elemento.");
            }
        }

        public async Task SetShoppingItemDoneAsync(string itemId, bool done)
        {
            if (!IsReady || shoppingRef == null || string.IsNullOrEmpty(itemId)) return;

            try
            {
                await shoppingRef.Child(itemId).Child("done").SetValueAsync(done);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error actualizando elemento: {e}");
                OnError?.Invoke("No se pudo actualizar la lista.");
            }
        }

        public async Task RemoveShoppingItemAsync(string itemId)
        {
            if (!IsReady || shoppingRef == null || string.IsNullOrEmpty(itemId)) return;

            try
            {
                await shoppingRef.Child(itemId).RemoveValueAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error borrando elemento: {e}");
                OnError?.Invoke("No se pudo borrar el elemento.");
            }
        }

        /// <summary>Borra de golpe todos los elementos marcados como comprados.</summary>
        public async Task ClearDoneShoppingItemsAsync()
        {
            if (!IsReady || shoppingRef == null || lastShoppingItems == null) return;

            var updates = new Dictionary<string, object>();
            foreach (var item in lastShoppingItems.Where(i => i.done))
            {
                updates[item.id] = null; // null en UpdateChildren = borrar
            }

            if (updates.Count == 0) return;

            try
            {
                await shoppingRef.UpdateChildrenAsync(updates);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error borrando comprados: {e}");
                OnError?.Invoke("No se pudieron borrar los elementos.");
            }
        }

        // ==========================================================
        //  Conversión desde Firebase
        // ==========================================================

        private static List<ShoppingItemData> ShoppingFromFirebaseValue(object value)
        {
            var result = new List<ShoppingItemData>();
            if (value is not IDictionary<string, object> dict) return result;

            foreach (var kv in dict)
            {
                if (kv.Value is not IDictionary<string, object> entry) continue;

                string text = entry.TryGetValue("text", out var t) ? t as string : null;
                if (string.IsNullOrWhiteSpace(text)) continue; // ignora nodos sueltos sin texto

                result.Add(new ShoppingItemData
                {
                    id = kv.Key,
                    text = text,
                    done = entry.TryGetValue("done", out var d) && d is bool b && b,
                    createdAt = entry.TryGetValue("createdAt", out var c) && c is long l ? l : 0L,
                });
            }

            // Pendientes primero, comprados al final; dentro de cada grupo, por orden de creación
            result.Sort((x, y) =>
            {
                int cmp = x.done.CompareTo(y.done);
                if (cmp != 0) return cmp;
                cmp = x.createdAt.CompareTo(y.createdAt);
                return cmp != 0 ? cmp : string.CompareOrdinal(x.id, y.id);
            });

            return result;
        }
    }
}
