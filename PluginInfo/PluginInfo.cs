using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NDesk.Options;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PluginInfo
{
	[ApiVersion(1, 23)]
	public class PluginInfo : TerrariaPlugin
	{
		public override string Author => "Enerdy";

		public override string Description => "Get information about installed TShock plugins.";

		public override string Name => "Plugin Information";

		public override Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

		public PluginInfo(Main game) : base(game)
		{

		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, onInitialize);
			}
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
		}

		void onInitialize(EventArgs e)
		{
			Commands.ChatCommands.Add(new Command("plugininfo.use", DoInfo, "plugin", "plugins", "pi")
			{
				HelpText = Description
			});
		}

		async void DoInfo(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendInfoMessage($"Syntax: {Commands.Specifier}plugin <find/info/list> [params...]");
				e.Player.SendInfoMessage("Type {0}plugin <find/info/list> -? OR -h OR --help for command details.",
					Commands.Specifier);
				return;
			}

			string command = e.Parameters[0].ToLowerInvariant();
			e.Parameters.RemoveAt(0);
			switch (command)
			{
				// Find Command
				case "-f":
				case "-s":
				case "find":
				case "search":
					await FindCommand(e);
					return;

				// Info Command
				case "-i":
				case "info":
				case "information":
				case "version":
					await InfoCommand(e);
					return;

				// List Command
				case "-l":
				case "all":
				case "list":
				case "ls":
					await ListCommand(e);
					return;

				default:
					e.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}plugin <find/info/list> [params...]",
						Commands.Specifier);
					e.Player.SendInfoMessage("Type {0}plugin <find/info/list> -? OR -h OR --help for command details.",
						Commands.Specifier);
					break;
			}
		}

		Task FindCommand(CommandArgs e)
		{
			return Task.Run(() =>
			{
				if (e.Parameters.Count == 0)
				{
					e.Player.SendErrorMessage("You must specify a search term!");
					return;
				}

				bool help = false;
				string author = "";
				string name = "";
				var o = new OptionSet()
				{
					{ "a|author=", v => author = v },
					{ "h|?|help", v => help = true },
					{ "n|name=", v => name = v }
				};

				List<string> parsedParams;
				try
				{
					parsedParams = o.Parse(e.Parameters);
				}
				catch (OptionException ex)
				{
					e.Player.SendErrorMessage(ex.Message);
					return;
				}

				if (help)
				{
					// Do FindCommand.Help

					return;
				}

				if (String.IsNullOrWhiteSpace(author) && String.IsNullOrWhiteSpace(name))
				{
					// If nothing was set, use plugin name as default
					name = String.Join(" ", parsedParams);
				}

				List<TerrariaPlugin> results = ServerApi.Plugins.Select(p => p.Plugin).Where(p =>
					p.Author.StartsWith(author, StringComparison.OrdinalIgnoreCase)
					&& p.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)).ToList();

				ListPlugins(results, e.Player, parsedParams);
			});
		}

		Task InfoCommand(CommandArgs e)
		{
			return Task.Run(() =>
			{
				if (e.Parameters.Count == 0)
				{
					e.Player.SendInfoMessage("No plugin name was provided, so own info will be displayed.");
					DisplayPluginInfo(e.Player, this);
					return;
				}

				bool help = false;
				var o = new OptionSet()
				{
					{ "h|?|help", v => help = true }
				};

				// Should only ever support boolean options, so no catching is required
				var parsedParams = o.Parse(e.Parameters);

				if (help)
				{
					// Do InfoCommand.Help

					return;
				}

				if (parsedParams.Count == 0)
				{
					// Mirror top behaviour
					e.Player.SendInfoMessage("No plugin name was provided, so own info will be displayed.");
					DisplayPluginInfo(e.Player, this);
					return;
				}

				string pluginName = String.Join(" ", parsedParams);
				TerrariaPlugin result = ServerApi.Plugins.FirstOrDefault(p =>
					p.Plugin.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))?.Plugin;

				if (result == null)
				{
					e.Player.SendErrorMessage($"A plugin named '{pluginName}' is not currently installed.");
				}
				else
				{
					DisplayPluginInfo(e.Player, result);
				}
			});
		}

		Task ListCommand(CommandArgs e)
		{
			/* All this command does is list all currently installed plugins
			 * Output format depends on the receiver */
			 List<TerrariaPlugin> plugins = ServerApi.Plugins
				.Select(p => p.Plugin)
				.OrderBy(p => p.Name)
				.ToList();

			 return Task.Run(() => ListPlugins(plugins, e.Player, e.Parameters));
		}

		void DisplayPluginInfo(TSPlayer receiver, TerrariaPlugin plugin)
		{
			string format;
			if (receiver.RealPlayer)
			{
				format = $"{TShock.Utils.ColorTag("{0}", Color.Violet)}: {{1}}";
			}
			else
			{
				format = "{0}: {1}";
			}

			receiver.SendInfoMessage(format, "Name", $"{plugin.Name} (v{plugin.Version.ToString()})");
			receiver.SendInfoMessage(format, "Author", plugin.Author);
			receiver.SendInfoMessage(format, "Description", plugin.Description);
		}

		void ListPlugins(List<TerrariaPlugin> plugins, TSPlayer receiver, List<string> args)
		{
			if (receiver is TSServerPlayer)
			{
				// Get rid of the annoying console colon
				Console.WriteLine();

				// Assume console, use a string table
				TableHelper.PrintLine();
				TableHelper.PrintRow("Name", "Author", "Version");
				TableHelper.PrintLine();
				foreach (TerrariaPlugin p in plugins)
				{
					TableHelper.PrintRow(p.Name,
										 p.Author,
										 p.Version.ToString());
				}
				TableHelper.PrintLine();
			}

			else if (receiver.RealPlayer)
			{
				// Use TShock Pagination
				List<string> lines = PaginationTools.BuildLinesFromTerms(plugins.Select(p => p.Name));

				int pageNum;
				if (!PaginationTools.TryParsePageNumber(args, 1, receiver, out pageNum))
					return;

				PaginationTools.SendPage(receiver, pageNum, lines,
					new PaginationTools.Settings
					{
						HeaderFormat = "Plugin List ({0}/{1}):",
						FooterFormat = $"Type {Commands.Specifier}plugin <find/list> {{0}} for more.",
						NothingToDisplayString = "No plugins were found."
					});
			}

			else
			{
				// Assume outside source, send raw comma-separated list
				receiver.SendInfoMessage($"Plugin count: {plugins.Count}");
				receiver.SendInfoMessage(String.Join(", ", plugins.Select(p => p.Name)));
			}
		}
	}
}
