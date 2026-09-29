using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Diagnostics;
using BatX3_HSS_GUI.Application.Input;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Application.Parameters.UI;
using BatX3_HSS_GUI.Application.Synchronization;
using Microsoft.Extensions.DependencyInjection;

namespace BatX3_HSS_GUI.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddSingleton<
                IParameterCatalog,
                StaticParameterCatalog>();

            services.AddSingleton<
                IParameterUiCatalog,
                StaticParameterUiCatalog>();

            services.AddSingleton<
                IParameterService,
                ParameterService>();

            services.AddSingleton<
                ISystemRuntimeState,
                SystemRuntimeState>();

            services.AddSingleton<
                IApplicationActionCatalog,
                StaticApplicationActionCatalog>();

            services.AddSingleton<
                IApplicationActionService,
                ApplicationActionService>();

            services.AddSingleton<
                ISystemModeService,
                SystemModeService>();

            services.AddSingleton<
                InputSettingsValidator>();

            services.AddSingleton<
                IFrameSynchronizer,
                FrameSynchronizer>();

            services.AddSingleton<
                IChannelHealthService,
                ChannelHealthService>();

            services.AddSingleton<
               IManualInputActionSink,
               ApplicationActionManualInputSink>();

            services.AddSingleton<
                IManualInputCoordinator,
                ManualInputCoordinator>();

            return services;
        }
    }
}