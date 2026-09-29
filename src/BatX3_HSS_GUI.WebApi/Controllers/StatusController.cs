using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BatX3_HSS_GUI.WebApi.Models;
using BatX3_HSS_GUI.Application.Communication.Command;

namespace BatX3_HSS_GUI.WebApi.Controllers;

public class StatusController : Controller
  {
      private readonly ICommandService _commandService;

      public StatusController(ICommandService commandService)
      {
          _commandService = commandService;
      }

      public IActionResult Index()
      {
          return View(new StatusViewModel { IsRunning = _commandService.IsRunning, LocalPort = _commandService.LocalPort });
      }
  }