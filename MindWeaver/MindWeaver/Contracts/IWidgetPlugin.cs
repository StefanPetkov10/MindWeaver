namespace MindWeaver.Contracts
{
    /// <summary>
    /// Contract that every external plugin assembly must implement.
    /// The host discovers all types satisfying this interface at runtime
    /// and renders them as first-class dashboard widgets.
    /// </summary>
    public interface IWidgetPlugin
    {
        /// <summary>
        /// Human-readable display name shown in the plugin card header.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the concrete <see cref="Type"/> of the Blazor component
        /// that the host should render via <c>&lt;DynamicComponent&gt;</c>.
        /// </summary>
        Type GetComponentType();
    }
}
