﻿using LogParsers.Base;
using LogParsers.Base.Helpers;
using Logshark.ArtifactProcessorModel;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor
{
    /// <summary>
    /// Tableau Server TSM Artifact Processor
    /// Processes the Tableau Server 10.5+ TSM-generated log format
    /// </summary>
    public sealed class ServerTsmLogProcessor : IArtifactProcessor
    {
        private static readonly ISet<string> RequiredCollectionsStatic = new HashSet<string>
        {
            ParserConstants.ConfigCollectionName
        };

        private static readonly ISet<Regex> SupportedFilePatternsStatic = new HashSet<Regex>
        {
            new Regex(@"^.*\.(log|txt|yml|csv|properties|conf|zip).*$", RegexOptions.Compiled)
        };

        private static readonly ISet<Type> SupportedPluginInterfacesStatic = new HashSet<Type>
        {
            typeof(IServerTsmPlugin)
        };

        #region IArtifactProcessor Implementation

        public string ArtifactType => "Server TSM";
        public ISet<string> RequiredCollections => RequiredCollectionsStatic;
        public ISet<Regex> SupportedFilePatterns => SupportedFilePatternsStatic;
        public ISet<Type> SupportedPluginInterfaces => SupportedPluginInterfacesStatic;

        public bool CanProcess(string rootLogLocation)
        {
            try
            {
                return Directory.GetDirectories(rootLogLocation).Any(HasTabadminAgentLogs);
            }
            catch
            {
                return false;
            }
        }

        public string ComputeArtifactHash(string rootLogLocation)
        {
            return HashUtility.ComputeDirectoryHash(rootLogLocation);
        }

        public IDictionary<string, object> GetAdditionalFileMetadata(LogFileContext fileContext)
        {
            return new Dictionary<string, object>
            {
                // Store the hostname in the "worker" field to maintain compatibility with Server "classic" logsets.
                { "worker", GetHostname(fileContext) }
            };
        }

        public IParserFactory GetParserFactory(string rootLogLocation)
        {
            return IsNewFormat(rootLogLocation)
                ? (IParserFactory) new ServerTsmParserFactory(rootLogLocation)
                : new ServerTsmLegacyParserFactory(rootLogLocation);
            
        }

        #endregion IArtifactProcessor Implementation

        /// <summary>
        /// Indicates whether a given directory contains a subdirectory that matches a known pattern for tabadminagent logs.
        /// </summary>
        /// <param name="directory">An absolute path to a directory.</param>
        /// <returns>True if directory contains a tabadminagent log subfolder.</returns>
        private static bool HasTabadminAgentLogs(string directory)
        {
            try
            {
                return Directory.GetDirectories(directory, "tabadminagent_*", SearchOption.TopDirectoryOnly).Any();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Given a log file path, attempt to glean the hostname from from it.
        /// This is leveraging the fact that in TSM logsets, the top-level folder for each node is named after the node's hostname.
        /// </summary>
        /// <returns>Hostname of worker node.</returns>
        private static string GetHostname(LogFileContext fileContext)
        {
            return ParserUtil.GetParentLogDirs(fileContext.FilePath, fileContext.RootLogDirectory).FirstOrDefault();
        }

        private static bool IsNewFormat(string baseDir)
        {
            return Directory
                .GetDirectories(baseDir)
                .Any(machineDir => machineDir.ToLower().EndsWith("node1"));
        }
    }
}