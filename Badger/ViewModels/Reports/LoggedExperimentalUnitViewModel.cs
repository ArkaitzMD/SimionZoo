﻿using System.Xml;
using System.Collections.Generic;
using Badger.Simion;
using Badger.Data;
using System;
using System.IO;
using Caliburn.Micro;

namespace Badger.ViewModels
{
    public class LoggedExperimentalUnitViewModel : SelectableTreeItem
    {
        private string m_name;
        public string Name
        {
            get { return m_name; }
            set { m_name = value; NotifyOfPropertyChange(() => Name); }
        }

        public string ExperimentFileName;
        public string LogDescriptorFileName;
        public string LogFileName;

        private Dictionary<string, string> m_forkValues = new Dictionary<string, string>();
        public Dictionary<string, string> forkValues
        {
            get { return m_forkValues; }
            set { m_forkValues = value; NotifyOfPropertyChange(() => forkValues); }
        }
        public string ForkValuesAsString
        {
            get { return Utility.DictionaryAsString(forkValues); }
        }

        public bool ContainsForks(BindableCollection<string> forks)
        {
            foreach (string fork in forks)
            {
                if (fork!=ReportsWindowViewModel.GroupByExperimentId && !forkValues.ContainsKey(fork))
                    return false;
            }
            return true;
        }

        public List<string> VariablesInLog { get; set; }


        /// <summary>
        /// Fake constructor for testing purposes
        /// </summary>
        /// <param name="filename"></param>
        public LoggedExperimentalUnitViewModel(string filename)
        {
            ExperimentFileName = filename;
            Name = Utility.GetFilename(filename);
        }

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="configNode"></param>
        /// <param name="baseDirectory"></param>
        /// <param name="updateFunction"></param>

        public LoggedExperimentalUnitViewModel(XmlNode configNode, string baseDirectory
            , SimionFileData.LoadUpdateFunction updateFunction = null)
        {
            //Experiment Name
            if (configNode.Attributes != null)
            {
                if (configNode.Attributes.GetNamedItem(XMLConfig.nameAttribute) != null)
                    Name = configNode.Attributes[XMLConfig.nameAttribute].Value;
            }

            //Initalize the paths to the log files
            if (configNode.Attributes.GetNamedItem(XMLConfig.pathAttribute) == null)
                throw new Exception("Malformed experiment batch file: cannot get the path to an experimental unit");

            ExperimentFileName= baseDirectory + configNode.Attributes[XMLConfig.pathAttribute].Value;
            LogDescriptorFileName = SimionFileData.GetLogFilePath(ExperimentFileName, true);
            if (!File.Exists(LogDescriptorFileName))
            {
                //for back-compatibility: if the appropriate log file is not found, check whether one exists
                //with the legacy naming convention: experiment-log.xml
                LogDescriptorFileName = SimionFileData.GetLogFilePath(ExperimentFileName, true, true);
                LogFileName = SimionFileData.GetLogFilePath(ExperimentFileName, false, true);
            }
            else
                LogFileName = SimionFileData.GetLogFilePath(ExperimentFileName, false);

            //FORKS
            //load the value of each fork used in this experimental unit
            foreach (XmlNode fork in configNode.ChildNodes)
            {
                string forkName = fork.Attributes[XMLConfig.aliasAttribute].Value;
                foreach (XmlNode value in fork.ChildNodes)
                {
                    string forkValue = value.Attributes.GetNamedItem("Value").InnerText; // The value is in the attribute named "Value"
                    forkValues[forkName] = forkValue;
                }
            }
            //update progress
            updateFunction?.Invoke();
        }

        public bool PreviousLogExists()
        {
            if (File.Exists(LogFileName))
            {
                FileInfo fileInfo = new FileInfo(LogFileName);
                if (fileInfo.Length > 0)
                    return true;
            }
            return false;
        }

        public void LoadLogDescriptor()
        {
            VariablesInLog = SimionLogDescriptor.LoadLogDescriptor(LogDescriptorFileName);
        }
        public int GetVariableIndex(string variableName)
        {
            int index = 0;
            foreach (string variable in VariablesInLog)
            {
                if (variable == variableName)
                    return index;
                index++;
            }
            return -1;
        }
        /// <summary>
        /// Reads the log file and returns in a track the data for each of the reports.
        /// </summary>
        /// <param name="reports">Parameters of each of the reporters: variable, type, ...</param>
        /// <returns></returns>
        public Track LoadTrackData(List<Report> reports)
        {
            SimionLog Log = new SimionLog();
            Log.LoadBinaryLog(LogFileName);

            if (!Log.SuccessfulLoad || Log.TotalNumEpisodes == 0) return null;

            Track track = new Track(forkValues,LogFileName,LogDescriptorFileName,ExperimentFileName);
            SeriesGroup dataSeries;
            int variableIndex;
            foreach (Report report in reports)
            {
                variableIndex = GetVariableIndex(report.Variable);
                switch(report.Type)
                {
                    case ReportType.LastEvaluation:
                        EpisodesData lastEpisode = Log.EvaluationEpisodes[Log.EvaluationEpisodes.Count - 1];
                        dataSeries = new SeriesGroup(report);
                        Series series = Log.GetEpisodeData(lastEpisode, report, variableIndex);
                        if (series != null)
                        {
                            dataSeries.AddSeries(series);
                            track.AddVariableData(report, dataSeries);
                        }
                        break;
                    case ReportType.EvaluationAverages:
                        track.AddVariableData(report
                            , Log.GetAveragedData(Log.EvaluationEpisodes, report,variableIndex));
                        break;
                    case ReportType.AllEvaluationEpisodes:
                    case ReportType.AllTrainingEpisodes:
                        dataSeries = new SeriesGroup(report);
                        List<EpisodesData> episodes;
                        if (report.Type == ReportType.AllEvaluationEpisodes)
                            episodes = Log.EvaluationEpisodes;
                        else episodes = Log.TrainingEpisodes;
                        foreach(EpisodesData episode in episodes)
                        {
                            Series subSeries = Log.GetEpisodeData(episode, report, variableIndex);
                            if (subSeries != null)
                            {
                                subSeries.Id = episode.index.ToString();
                                dataSeries.AddSeries(subSeries);
                            }
                        }
                        track.AddVariableData(report, dataSeries);
                        break;
                }
            }
            return track;
        }
    }
}
