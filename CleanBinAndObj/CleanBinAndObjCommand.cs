﻿using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CleanBinAndObj
{
    /// <summary>
    ///     Command handler
    /// </summary>
    internal sealed class CleanBinAndObjCommand
    {
        /// <summary>
        ///     Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        ///     Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("58cab930-ec55-4b8b-876a-c6208cc246c4");

        /// <summary>
        ///     Pane to output command log messages
        /// </summary>
        private static IVsOutputWindowPane _vsOutputWindowPane;

        /// <summary>
        ///     VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CleanBinAndObjCommand" /> class.
        ///     Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CleanBinAndObjCommand(Package package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(CleanBinAndObj, menuCommandId);
                commandService.AddCommand(menuItem);

                var outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                var paneGuid = new Guid("98BD962F-305C-4D95-9687-A8477D16D6B2");
                const string customTitle = "Clean bin and obj";
                outWindow.CreatePane(ref paneGuid, customTitle, 1, 1);
                outWindow.GetPane(ref paneGuid, out _vsOutputWindowPane);
            }
        }

        /// <summary>
        ///     Gets the instance of the command.
        /// </summary>
        public static CleanBinAndObjCommand Instance { get; private set; }

        /// <summary>
        ///     Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => package;

        /// <summary>
        ///     Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new CleanBinAndObjCommand(package);
        }

        private void CleanBinAndObj(object sender, EventArgs e)
        {
            var dte = (DTE) ServiceProvider.GetService(typeof(DTE));
            var solutionFullName = dte.Solution.FullName;
            var solutionRootPath = Path.GetDirectoryName(solutionFullName);

            var binDirectories = Directory.EnumerateDirectories(solutionRootPath, "bin", SearchOption.AllDirectories);
            var objDirectories = Directory.EnumerateDirectories(solutionRootPath, "obj", SearchOption.AllDirectories);

            var directoriesToClean = binDirectories.Concat(objDirectories).OrderBy(x => x).ToArray();

            WriteToOutput($"Starting... Directories to clean: {directoriesToClean.Length}");
            _vsOutputWindowPane.Activate(); // Brings this pane into view

            foreach (var directoryToClean in directoriesToClean)
            {
                WriteToOutput($"Cleaning {directoryToClean}");
                var di = new DirectoryInfo(directoryToClean);
                foreach (var file in di.EnumerateFiles()) file.Delete();

                foreach (var dir in di.EnumerateDirectories()) dir.Delete(true);

                WriteToOutput($"Cleaned  {directoryToClean}");
            }

            WriteToOutput("Finished");
        }

        private static void WriteToOutput(string message)
        {
            _vsOutputWindowPane.OutputString($"{DateTime.Now:HH:mm:ss.ffff}: {message}{Environment.NewLine}");
        }
    }
}