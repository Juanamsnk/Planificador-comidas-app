using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using QueComemos.Localization;
using UnityEngine;

namespace QueComemos.Data
{
    [Serializable]
    public class RecipeData
    {
        public string id;
        public string name;
        public string url;
        public long createdAt;
    }

    //FirebaseManager.Recipes
    public partial class FirebaseManager
    {
        private DatabaseReference recipesRef;
        private List<RecipeData> lastRecipes;
        private bool hasRecipesData;

        private event Action<List<RecipeData>> OnRecipesChanged;

        // ==========================================================
        //  Suscripción
        // ==========================================================

        /// <summary>
        /// Suscribe un callback a las recetas del calendario activo.
        /// Si ya había datos, se los entrega enseguida.
        /// </summary>
        public void SubscribeToRecipes(Action<List<RecipeData>> callback)
        {
            OnRecipesChanged += callback;
            if (hasRecipesData)
            {
                callback(lastRecipes);
            }
        }

        public void UnsubscribeFromRecipes(Action<List<RecipeData>> callback)
        {
            OnRecipesChanged -= callback;
        }

        // ==========================================================
        //  Enganche con el calendario activo (se llama desde
        //  SetCalendarId y OnDestroy de FirebaseManager.cs)
        // ==========================================================

        private void AttachRecipesRef(string calendarId)
        {
            DetachRecipesRef();

            // Los datos en caché son del calendario anterior
            lastRecipes = null;
            hasRecipesData = false;

            recipesRef = database.RootReference
                .Child("calendars").Child(calendarId).Child("recipes");
            recipesRef.ValueChanged += HandleRecipesValueChanged;
        }

        private void DetachRecipesRef()
        {
            if (recipesRef != null)
            {
                recipesRef.ValueChanged -= HandleRecipesValueChanged;
                recipesRef = null;
            }
        }

        private void HandleRecipesValueChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"[FirebaseManager] Error leyendo recipes: {args.DatabaseError.Message}");
                OnError?.Invoke(Loc.T("err.recipeLoad"));
                return;
            }

            var items = RecipesFromFirebaseValue(args.Snapshot.Value);
            lastRecipes = items;
            hasRecipesData = true;
            OnRecipesChanged?.Invoke(items);
        }

        // ==========================================================
        //  Operaciones (cada una escribe solo su propio nodo)
        // ==========================================================

        public async Task AddRecipeAsync(string name, string url)
        {
            if (!IsReady || recipesRef == null ||
                string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) return;

            try
            {
                var newRef = recipesRef.Push();
                await newRef.SetValueAsync(new Dictionary<string, object>
                {
                    ["name"] = name.Trim(),
                    ["url"] = url.Trim(),
                    ["createdAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error guardando receta: {e}");
                OnError?.Invoke(Loc.T("err.recipeAdd"));
            }
        }

        public async Task RemoveRecipeAsync(string recipeId)
        {
            if (!IsReady || recipesRef == null || string.IsNullOrEmpty(recipeId)) return;

            try
            {
                await recipesRef.Child(recipeId).RemoveValueAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error borrando receta: {e}");
                OnError?.Invoke(Loc.T("err.recipeDelete"));
            }
        }

        // ==========================================================
        //  Conversión desde Firebase
        // ==========================================================

        private static List<RecipeData> RecipesFromFirebaseValue(object value)
        {
            var result = new List<RecipeData>();
            if (value is not IDictionary<string, object> dict) return result;

            foreach (var kv in dict)
            {
                if (kv.Value is not IDictionary<string, object> entry) continue;

                string name = entry.TryGetValue("name", out var n) ? n as string : null;
                string url = entry.TryGetValue("url", out var u) ? u as string : null;

                // ignora nodos sueltos incompletos
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) continue;

                result.Add(new RecipeData
                {
                    id = kv.Key,
                    name = name,
                    url = url,
                    createdAt = entry.TryGetValue("createdAt", out var c) && c is long l ? l : 0L,
                });
            }

            // Las más recientes primero
            result.Sort((x, y) =>
            {
                int cmp = y.createdAt.CompareTo(x.createdAt);
                return cmp != 0 ? cmp : string.CompareOrdinal(x.id, y.id);
            });

            return result;
        }
    }
}
