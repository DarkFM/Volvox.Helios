﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volvox.Helios.Core.Bot;
using Volvox.Helios.Domain.Discord;
using Volvox.Helios.Service.Discord.Guild;
using Volvox.Helios.Service.Discord.UserGuild;
using Volvox.Helios.Service.Extensions;
using Volvox.Helios.Web.ViewModels.Dashboard;

namespace Volvox.Helios.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDiscordUserGuildService _userGuildService;
        private readonly IDiscordGuildService _guildService;
        private readonly IBot _bot;

        public DashboardController(IDiscordUserGuildService userGuildService, IDiscordGuildService guildService, IBot bot)
        {
            _userGuildService = userGuildService;
            _guildService = guildService;
            _bot = bot;
        }

        public async Task<IActionResult> Index()
        {
            var userGuilds = await _userGuildService.GetUserGuilds();

            var guilds = GetGuildDetails(userGuilds.FilterAdministrator());

            var viewModel = new DashboardIndexViewModel
            {
                UserGuilds = guilds
            };

            return View(viewModel);
        }

        [Route("Dashboard/{guildId}")]
        public async Task<IActionResult> Details(ulong guildId)
        {
            var userGuilds = await _userGuildService.GetUserGuilds();

            var guilds = GetGuildDetails(userGuilds.FilterAdministrator(), guildId);

            var viewModel = new DashboardDetailsViewModel
            {
                UserGuilds = guilds,
                Guild = guilds.FirstOrDefault(g => g.Guild.Id == guildId)?.Guild
            };

            return View(viewModel);
        }

        /// <summary>
        ///     Populate the input guilds with details.
        /// </summary>
        /// <param name="guilds">Input list of guilds.</param>
        /// <param name="detailsGuildId">If specified, will only provide details for the guild with the specified id.</param>
        /// <returns>Populated list of guilds.</returns>
        private List<UserGuild> GetGuildDetails(List<UserGuild> guilds, ulong? detailsGuildId = null)
        {
            foreach (var guild in guilds)
            {
                // We only want the details for the specified guild so skip the rest.
                if (detailsGuildId != null && guild.Guild.Id != detailsGuildId)
                {
                    continue;
                }

                var guildId = guild.Guild.Id;

                guild.Guild.Details.IsBotInGuild = _bot.IsBotInGuild(guild.Guild.Id);

                // Set the Volvox logo as the image if the guild does not have an icon.
                if (string.IsNullOrWhiteSpace(guild.Guild.Icon))
                {
                    guild.Guild.ImageUrl = "/images/small/volvox-logo.png";
                }

                // Bot must be in the guild to retrieve details.
                if (guild.Guild.Details.IsBotInGuild)
                {
                    var botGuild = _bot.GetGuild(guildId);

                    guild.Guild.Details.MemberCount = botGuild.MemberCount;
                    guild.Guild.Details.Roles = botGuild.Roles;
                    guild.Guild.Details.Channels = botGuild.Channels;
                }
            }

            return guilds;
        }
    }
}