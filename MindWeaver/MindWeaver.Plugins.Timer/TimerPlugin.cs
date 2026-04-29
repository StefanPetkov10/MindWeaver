using MindWeaver.Contracts;
using MindWeaver.Plugins.Timer.Components;

namespace MindWeaver.Plugins.Timer
{
    /// <summary>
    /// Plugin registration entry point discovered by the host's <c>PluginLoaderService</c>.
    /// Returns the <see cref="Pomodoro"/> Blazor component for dynamic rendering.
    /// </summary>
    public sealed class TimerPlugin : IWidgetPlugin
    {
        /// <inheritdoc />
        public string Name => "Pomodoro Focus Timer";

        /// <inheritdoc />
        public Type GetComponentType() => typeof(Pomodoro);
    }
}
