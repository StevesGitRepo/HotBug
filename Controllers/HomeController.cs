﻿using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Principal;
using HotBug.Extensions;
using HotBug.Models;
using HotBug.Models.ViewModels;
using HotBug.Services.Interfaces;
using HotBug.Models.Enums;
using HotBug.Models.ChartModels;
using Microsoft.AspNetCore.Authorization;

namespace HotBug.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHBCompanyInfoService _companyInfoService;
        private readonly IHBProjectService _projectService;


        [ActivatorUtilitiesConstructor]
        public HomeController(ILogger<HomeController> logger,
                              IHBCompanyInfoService companyInfoService,
                              IHBProjectService projectService)

        {
            _logger = logger;
            _companyInfoService = companyInfoService;
            _projectService = projectService;
        }


        public IActionResult Index()
        {
            return View();
        }

        //GET Action
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            DashboardViewModel model = new();
            int companyId = User.Identity.GetCompanyId()!.Value;
            model.Company = await _companyInfoService.GetCompanyInfoByIdAsync(companyId);
            model.Projects = (await _companyInfoService.GetAllProjectsAsync(companyId)).Where(p => p.Archived == false).ToList();
            model.Tickets = model.Projects.SelectMany(p => p.Tickets).Where(p=>p.Archived == false).ToList();
            model.Members = model.Company.Members.ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<JsonResult> AmCharts()
        {

            AmChartData amChartData = new();
            List<AmItem> amItems = new();

            int companyId = User.Identity.GetCompanyId()!.Value;

            List<Project> projects = (await _companyInfoService.GetAllProjectsAsync(companyId)).Where(p => p.Archived == false).ToList();

            foreach (Project project in projects)
            {
                AmItem item = new();

                item.Project = project.Name;
                item.Tickets = project.Tickets.Count;
                item.Developers = (await _projectService.GetProjectMembersByRoleAsync(project.Id, nameof(Roles.Developer))).Count();

                amItems.Add(item);
            }

            amChartData.Data = amItems.ToArray();


            return Json(amChartData.Data);
        }

        [HttpPost]
        public async Task<JsonResult> GglProjectTickets()
        {
            int companyId = User.Identity.GetCompanyId()!.Value;

            List<Project> projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);

            List<object> chartData = new();
            chartData.Add(new object[] { "ProjectName", "TicketCount" });

            foreach (Project prj in projects)
            {
                chartData.Add(new object[] { prj.Name, prj.Tickets.Count() });
            }

            return Json(chartData);
        }

        [HttpPost]
        public async Task<JsonResult> GglProjectPriority()
        {
            int companyId = User.Identity.GetCompanyId()!.Value;

            List<Project> projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);

            List<object> chartData = new();
            chartData.Add(new object[] { "Priority", "Count" });


            foreach (string priority in Enum.GetNames(typeof(HBProjectPriority)))
            {
                int priorityCount = (await _projectService.GetAllProjectsByPriority(companyId, priority)).Count();
                chartData.Add(new object[] { priority, priorityCount });
            }

            return Json(chartData);
        }


        [HttpPost]
        public async Task<JsonResult> PlotlyBarChart()
        {
            PlotlyBarData plotlyData = new();
            List<PlotlyBar> barData = new();

            int companyId = User.Identity.GetCompanyId()!.Value;

            List<Project> projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);

            //Bar One
            PlotlyBar barOne = new()
            {
                X = projects.Select(p => p.Name).ToArray(),
                Y = projects.SelectMany(p => p.Tickets).GroupBy(t => t.ProjectId).Select(g => g.Count()).ToArray(),
                Name = "Tickets",
                Type = "bar"
            };

            //Bar Two
            PlotlyBar barTwo = new()
            {
                X = projects.Select(p => p.Name).ToArray(),
                Y = projects.Select(async p => (await _projectService.GetProjectMembersByRoleAsync(p.Id, nameof(Roles.Developer))).Count).Select(c => c.Result).ToArray(),
                Name = "Developers",
                Type = "bar"
            };

            barData.Add(barOne);
            barData.Add(barTwo);
            plotlyData.Data = barData;

            return Json(plotlyData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}