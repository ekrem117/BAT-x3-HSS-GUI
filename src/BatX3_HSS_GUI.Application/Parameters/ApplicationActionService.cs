using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Application.Parameters
{
    public sealed class ApplicationActionService : IApplicationActionService
    {
        private readonly IApplicationActionCatalog _catalog;
        private readonly IParameterService _parameterService;
        private readonly ISystemRuntimeState _systemRuntimeState;

        private readonly object _syncRoot = new();

        private readonly HashSet<string> _activeMomentaryActions = new(StringComparer.Ordinal);

        public ApplicationActionService(IApplicationActionCatalog catalog, IParameterService parameterService, ISystemRuntimeState systemRuntimeState)
        {
            _catalog = catalog;
            _parameterService = parameterService;
            _systemRuntimeState = systemRuntimeState;
        }

        public async Task ExecuteAsync(string actionId, CancellationToken cancellationToken = default)
        {
            ApplicationActionDefinition action = _catalog.GetRequired(actionId);

            if (action.InteractionType != ApplicationActionInteractionType.Trigger)
            {
                throw new InvalidOperationException($"'{action.Id}' momentary bir action'dır ve " + $"{nameof(ExecuteAsync)} ile çalıştırılamaz.");
            }

            EnsureActionIsAllowed(action);

            await ExecuteValueAsync(action, action.Value, cancellationToken);
        }

        public async Task PressAsync(string actionId, CancellationToken cancellationToken = default)
        {
            ApplicationActionDefinition action = _catalog.GetRequired(actionId);

            if (action.InteractionType == ApplicationActionInteractionType.Momentary && action.ReleaseValue is null)
            {
                throw new InvalidOperationException($"Momentary action '{action.Id}' için " + "ReleaseValue tanımlı değildir.");
            }

            EnsureActionIsAllowed(action);

            if (action.InteractionType == ApplicationActionInteractionType.Momentary)
            {
                lock (_syncRoot)
                {
                    _activeMomentaryActions.Add(action.Id);
                }
            }

            await ExecuteValueAsync(action, action.Value, cancellationToken);
        }

        public async Task ReleaseAsync(string actionId, CancellationToken cancellationToken = default)
        {
            ApplicationActionDefinition action = _catalog.GetRequired(actionId);

            switch (action.InteractionType)
            {
                case ApplicationActionInteractionType.Trigger:
                    {
                        return;
                    }

                case ApplicationActionInteractionType.Momentary:
                    {
                        object? releaseValue = action.ReleaseValue;

                        if (releaseValue is null)
                        {
                            throw new InvalidOperationException($"Momentary action '{action.Id}' için " + "ReleaseValue tanımlı değildir.");
                        }

                        await ExecuteValueAsync(action, releaseValue, cancellationToken);

                        lock (_syncRoot)
                        {
                            _activeMomentaryActions.Remove(action.Id);
                        }

                        return;
                    }

                default:
                    {
                        throw new InvalidOperationException("Desteklenmeyen application action interaction tipi: " + $"{action.InteractionType}");
                    }
            }
        }

        public async Task ReleaseAllMomentaryAsync(CancellationToken cancellationToken = default)
        {
            string[] activeActionIds;

            lock (_syncRoot)
            {
                activeActionIds = _activeMomentaryActions.ToArray();
            }

            if (activeActionIds.Length == 0)
            {
                return;
            }

            List<Exception> failures = [];

            foreach (string actionId in activeActionIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ReleaseAsync(actionId, cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {

                    failures.Add(exception);
                }
            }

            if (failures.Count > 0)
            {
                throw new AggregateException("Bir veya daha fazla momentary action " + "fail-safe release edilemedi.", failures);
            }
        }

        private void EnsureActionIsAllowed(ApplicationActionDefinition action)
        {
            if (action.AllowedOperatingModes.Count == 0)
            {
                return;
            }

            SystemOperatingMode? currentMode = _systemRuntimeState.CurrentMode;

            if (currentMode is not null && action.AllowedOperatingModes.Contains(currentMode.Value))
            {
                return;
            }

            string currentModeText = currentMode?.ToString() ?? "Bilinmiyor";

            throw new ApplicationActionNotAllowedException(action.Id, $"'{action.DisplayName}' işlemi mevcut sistem modunda " + $"kullanılamaz. Mevcut mod: {currentModeText}.");
        }

        private async Task ExecuteValueAsync(ApplicationActionDefinition action, object value, CancellationToken cancellationToken)
        {
            await _parameterService.SetAsync(action.ParameterName, value, cancellationToken);
        }
    }
}