using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Configuration.Runtime;
using Microsoft.AspNetCore.Mvc;

namespace BatX3_HSS_GUI.WebApi.Controllers;

public class SettingsController : Controller
{
    private readonly ISettingsService _settingsService;
    private readonly IRuntimeSettingsApplyService _runtimeSettingsApplyService;

    public SettingsController(
        ISettingsService settingsService,
        IRuntimeSettingsApplyService runtimeSettingsApplyService)
    {
        _settingsService = settingsService;
        _runtimeSettingsApplyService = runtimeSettingsApplyService;
    }

    public IActionResult Index()
    {
        return View(_settingsService.GetNetworkSettings());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(NetworkSettings model)
    {
        IReadOnlyList<string> errors = NetworkSettingsValidator.Validate(model);

        if (errors.Count > 0)
        {
            TempData["HasError"] = true;
            TempData["StatusMessage"] = string.Join(Environment.NewLine, errors);

            return View("Index", model);
        }

        try
        {
            InputSettings inputSettings = _settingsService.GetInputSettings();

            RuntimeSettingsApplyResult result = await _runtimeSettingsApplyService.ApplyAsync(model, inputSettings);

            TempData["HasError"] = false;
            TempData["StatusMessage"] = result.NetworkReconfigured
                ? "Ayarlar kaydedildi ve ağ bağlantıları yeniden yapılandırıldı."
                : "Ayarlar kaydedildi.";
        }
        catch (RuntimeSettingsApplyException exception)
        {
            TempData["HasError"] = true;
            TempData["StatusMessage"] = exception.RollbackSucceeded
                ? "Ayarlar uygulanamadı. Önceki çalışan ayarlara geri dönüldü."
                : "Ayarlar uygulanamadı ve geri alma işlemi tamamlanamadı. Sistem Durumu ekranını kontrol edin.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RestoreDefaults()
    {
        NetworkSettings defaults = _settingsService.GetDefaultNetworkSettings();

        TempData["HasError"] = false;
        TempData["StatusMessage"] = "Varsayılan ayarlar forma yüklendi. Etkinleştirmek için Kaydet'e basın.";

        return View("Index", defaults);
    }
}
