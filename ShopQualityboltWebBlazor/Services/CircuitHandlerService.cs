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
            _logger.LogInformation("Circuit {CircuitId} connected", circuit.Id);
            return Task.CompletedTask;
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogWarning("Circuit {CircuitId} disconnected", circuit.Id);
            return Task.CompletedTask;
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Circuit {CircuitId} opened", circuit.Id);
            return Task.CompletedTask;
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Circuit {CircuitId} closed", circuit.Id);
            return Task.CompletedTask;
        }
    }
}
