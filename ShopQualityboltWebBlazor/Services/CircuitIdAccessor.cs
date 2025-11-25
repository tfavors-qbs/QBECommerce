using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Collections.Concurrent;

namespace ShopQualityboltWebBlazor.Services
{
    /// <summary>
    /// Provides access to the current Blazor Server circuit ID using async context
    /// </summary>
    public class CircuitIdAccessor
    {
        private static readonly AsyncLocal<string?> _currentCircuitId = new();
        private static readonly ConcurrentDictionary<string, string> _circuitIds = new();

        public string? CircuitId
        {
            get => _currentCircuitId.Value;
            set => _currentCircuitId.Value = value;
        }

        public void SetCircuitId(string circuitId)
        {
            _currentCircuitId.Value = circuitId;
            _circuitIds[circuitId] = circuitId;
        }
    }

    /// <summary>
    /// Circuit handler that tracks the circuit ID for token management
    /// </summary>
    public class TrackingCircuitHandler : CircuitHandler
    {
        private readonly CircuitIdAccessor _circuitIdAccessor;

        public TrackingCircuitHandler(CircuitIdAccessor circuitIdAccessor)
        {
            _circuitIdAccessor = circuitIdAccessor;
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _circuitIdAccessor.SetCircuitId(circuit.Id);
            return base.OnCircuitOpenedAsync(circuit, cancellationToken);
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _circuitIdAccessor.SetCircuitId(circuit.Id);
            return base.OnConnectionUpAsync(circuit, cancellationToken);
        }
    }
}
