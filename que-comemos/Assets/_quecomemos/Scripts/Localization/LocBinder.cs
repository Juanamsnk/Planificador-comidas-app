using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace QueComemos.Localization
{
    /// <summary>
    /// Enlaza textos de UXML con claves de Loc.
    ///
    /// En el UXML se escribe la clave con prefijo "@" en lugar del texto:
    ///     &lt;ui:Label text="@settings.account"/&gt;
    ///     &lt;ui:Button tooltip="@calendar.pin"/&gt;
    ///     &lt;ui:TextField placeholder-text="@edit.dishPlaceholder"/&gt;
    ///
    /// Bind(root) resuelve todos los "@clave" del árbol y recuerda dónde
    /// estaban para volver a traducirlos cuando cambie el idioma.
    /// Crea una instancia por UIDocument y llama a Dispose() en OnDisable.
    /// </summary>
    public sealed class LocBinder : IDisposable
    {
        public const string KeyPrefix = "@";

        private enum Target
        {
            Text,
            Tooltip,
            Placeholder
        }

        private struct Binding
        {
            public VisualElement element;
            public Target target;
            public string key;
        }

        private readonly List<Binding> bindings = new List<Binding>();
        private bool disposed;

        public LocBinder()
        {
            Loc.LanguageChanged += Refresh;
        }

        /// <summary>Busca "@claves" en root y en todos sus descendientes y las traduce.</summary>
        public void Bind(VisualElement root)
        {
            if (root == null)
                return;

            Visit(root);

            foreach (var element in root.Query<VisualElement>().ToList())
                Visit(element);
        }

        /// <summary>Vuelve a aplicar el idioma actual a todo lo enlazado.</summary>
        public void Refresh()
        {
            bindings.RemoveAll(b => b.element == null || b.element.panel == null);

            foreach (var binding in bindings)
                Apply(binding);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Loc.LanguageChanged -= Refresh;
            bindings.Clear();
        }

        #region Internals

        private void Visit(VisualElement element)
        {
            // Tooltip: vale para cualquier elemento.
            TryAdd(element, Target.Tooltip, element.tooltip);

            // Label y Button (incluye la etiqueta interna de Toggle y TextField).
            // Se evita TextElement a secas para no tocar el texto que escribe el usuario.
            if (element is Label || element is Button)
            {
                TryAdd(element, Target.Text, ((TextElement)element).text);
            }
            else if (element is TextField textField)
            {
                TryAdd(element, Target.Placeholder, textField.textEdition.placeholder);
            }
        }

        private void TryAdd(VisualElement element, Target target, string value)
        {
            if (string.IsNullOrEmpty(value) ||
                !value.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return;
            }

            var binding = new Binding
            {
                element = element,
                target = target,
                key = value.Substring(KeyPrefix.Length)
            };

            bindings.Add(binding);
            Apply(binding);
        }

        private static void Apply(Binding binding)
        {
            string text = Loc.T(binding.key);

            switch (binding.target)
            {
                case Target.Text:
                    ((TextElement)binding.element).text = text;
                    break;

                case Target.Tooltip:
                    binding.element.tooltip = text;
                    break;

                case Target.Placeholder:
                    ((TextField)binding.element).textEdition.placeholder = text;
                    break;
            }
        }

        #endregion
    }
}
