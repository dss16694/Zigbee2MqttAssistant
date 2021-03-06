﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zigbee2MqttAssistant.Models;
using Zigbee2MqttAssistant.Models.Devices;
using Zigbee2MqttAssistant.Services;

namespace Zigbee2MqttAssistant.Controllers
{
	[IgnoreAntiforgeryToken]
	public class HomeController : Controller
	{
		private readonly IBridgeStateService _stateService;
		private readonly IBridgeOperationService _operationService;
		private readonly ISettingsService _settings;

		public HomeController(IBridgeStateService stateService, IBridgeOperationService operationService, ISettingsService settings)
		{
			_stateService = stateService;
			_operationService = operationService;
			_settings = settings;
		}

		public IActionResult Index()
		{
			var state = _stateService.CurrentState;

			return View((state, _settings.CurrentSettings));
		}

		public IActionResult Devices()
		{
			var state = _stateService.CurrentState;

			return PartialView("_PartialDevices", (state, _settings.CurrentSettings));
		}

		[HttpPost]
		public async Task<IActionResult> Reset()
		{
			await _operationService.Reset();

			return RedirectToAction("Index");
		}

		public IActionResult Device(string id)
		{
			id = Uri.UnescapeDataString(id);

			var device = _stateService.FindDeviceById(id, out var state);

			if (device == null)
			{
				return NotFound();
			}

			// find route to coordinator
			var routeToCoordinator = new List<ZigbeeDevice>();
			var reachCoordinator = false;

			DeviceDetailsViewModel vm = new DeviceDetailsViewModel.Builder
			{
				Device = device,
				RouteToCoordinator = routeToCoordinator.ToImmutableArray(),
				RouteReachCoordinator = reachCoordinator,
				BridgeState = state
			};

			return View(vm);
		}

		[HttpPost]
		public async Task<IActionResult> RenameDevice(string id, string newName)
		{
			id = Uri.UnescapeDataString(id);
			var device = await _operationService.RenameDeviceById(id, newName);

			if (device == null)
			{
				return NotFound();
			}

			return RedirectToAction("Index");
		}

		[HttpPost]
		public async Task<IActionResult> ConfigureDevice(string id)
		{
			id = Uri.UnescapeDataString(id);
			await _operationService.Configure(id);
			return RedirectToAction("Device", new { id });
		}

		[HttpPost]
		public async Task<IActionResult> BindDevice(string id, string targetId)
		{
			id = Uri.UnescapeDataString(id);
			await _operationService.Bind(id, targetId);
			return RedirectToAction("Device", new {id});
		}

		[HttpPost]
		public async Task<IActionResult> UnbindDevice(string id, string targetId)
		{
			id = Uri.UnescapeDataString(id);
			await _operationService.Unbind(id, targetId);
			return RedirectToAction("Device", new { id });
		}

		[HttpPost]
		public async Task<IActionResult> RemoveDevice(string id, bool forceRemove)
		{
			id = Uri.UnescapeDataString(id);
			var device = await _operationService.RemoveDeviceById(id, forceRemove);

			if (device == null)
			{
				return NotFound();
			}

			return RedirectToAction("Index");
		}

		public IActionResult Status()
		{
			return View(_stateService.CurrentState);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		[HttpPost]
		public async Task<IActionResult> AllowJoin(bool permitJoin)
		{
			await _operationService.AllowJoin(permitJoin);

			return RedirectToAction("Status");
		}

		[HttpPost]
		public async Task<IActionResult> OtaUpdateDevice(string id)
		{
			id = Uri.UnescapeDataString(id);
			await _operationService.OtaUpdateDevice(id);

			return RedirectToAction("Status");
		}

		[HttpPost]
		public async Task<IActionResult> TouchLinkReset()
		{
			await _operationService.TouchLinkReset().ConfigureAwait(false);
			return RedirectToAction("Status");
		}

		public async Task<IActionResult> SetLogLevel(string level)
		{
			await _operationService.SetLogLevel(level);

			return RedirectToAction("Status");
		}

		public IActionResult Map()
		{
			return View();
		}

		public async Task<IActionResult> Scan()
		{
			await _operationService.ManualRefreshNetworkScan();
			return View("Map");
		}
	}
}
