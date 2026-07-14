namespace VerticalSliceDemo.Domains
{
    /// <summary>Input failed validation; <see cref="Errors"/> maps field name to error messages.</summary>
    public sealed class ValidationException(Dictionary<string, string[]> errors) : Exception("One or more validation errors occurred.")
    {
        public Dictionary<string, string[]> Errors { get; } = errors;
    }

    /// <summary>A referenced resource (e.g. an order) was not found.</summary>
    public sealed class NotFoundException(string message) : Exception(message);

    /// <summary>The request conflicts with current state (e.g. a shipment already exists).</summary>
    public sealed class ConflictException(string message) : Exception(message);
}
