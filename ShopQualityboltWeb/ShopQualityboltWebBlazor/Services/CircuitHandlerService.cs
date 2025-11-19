using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ShopQualityboltWebBlazor.Services
{
    public class CircuitHandlerService : CircuitHandler
    {
        private readonly ILogger<CircuitHandlerService> _logger;

        public CircuitHandlerService(ILogger<CircuitHandlerService> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Circuit {CircuitId} connected", circuit.Id);
            return Task.CompletedTask;
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // Only log as Information, not Warning - disconnections are normal
            _logger.LogInformation("Circuit {CircuitId} disconnected", circuit.Id);
            return Task.CompletedTask;
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Circuit {CircuitId} opened", circuit.Id);
            return Task.CompletedTask;
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Circuit {CircuitId} closed", circuit.Id);
            return Task.CompletedTask;
        }
    }
}
