namespace OS.Core.ResponseWrappers.Models
{
    /// <summary>
    /// Location header model
    /// </summary>
    internal class Location
    {
        private Location() { }
        public string? Action { get; }
        public string? Controller { get; }
        public object? RouteValues { get; }

        /// <summary>
        /// Creates a new <see cref="Location"/> instance with the provided <paramref name="action"/>, <paramref name="controller"/> and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="controller"></param>
        /// <param name="routeValues"></param>
        internal Location(string action, string controller, object? routeValues)
        {
            Action = action;
            Controller = controller;
            RouteValues = routeValues;
        }
    }
}
