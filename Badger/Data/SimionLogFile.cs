﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Badger.Data;
using Badger.ViewModels;

namespace Badger.Simion
{
    public class StepData
    {
        public int stepIndex;
        public double expRealTime, episodeSimTime, episodeRealTime;
        public double[] data;

        //this function return whether there is more data to read from the current episode or not
        public bool readStep(BinaryReader logReader, int numLoggedVariables)
        {
            int magicNumber = (int)logReader.ReadInt64();
            stepIndex = (int)logReader.ReadInt64();
            expRealTime = logReader.ReadDouble();
            episodeSimTime = logReader.ReadDouble();
            episodeRealTime = logReader.ReadDouble();
            logReader.ReadChars(sizeof(double) * (SimionLog.HEADER_MAX_SIZE - 5));
            if (magicNumber == SimionLog.EPISODE_END_HEADER) return true;

            //not the final step, we have to read the logged variables
            byte[] buffer = logReader.ReadBytes(sizeof(double) * (int)numLoggedVariables);
            if (buffer.Length == 0)
                return true;
            data = new double[numLoggedVariables];
            Buffer.BlockCopy(buffer, 0, data, 0, numLoggedVariables * sizeof(double));
            return false;
        }
    }
    public class EpisodesData
    {
        public const int episodeTypeEvaluation = 0;
        public const int episodeTypeTraining = 1;

        public int type = 0;
        public int index = 0;
        public int subIndex = 0;
        public int numVariablesLogged = 0;
        public List<StepData> steps = new List<StepData>();
        public EpisodesData() { }
        public void ReadEpisodeHeader(BinaryReader logReader)
        {
            int magicNumber = (int)logReader.ReadInt64();
            type = (int)logReader.ReadInt64();
            index = (int)logReader.ReadInt64();
            numVariablesLogged = (int)logReader.ReadInt64();
            subIndex = (int)logReader.ReadInt64();
            byte[] padding = logReader.ReadBytes(sizeof(double) * (SimionLog.HEADER_MAX_SIZE - 5));
        }
        public Series GetVariableData(Dictionary<string, int> variablesInLog, Report trackParameters)
        {
            Series data = new Series();
            int variableIndex = variablesInLog[trackParameters.Variable];
            foreach (StepData step in steps)
                data.AddValue(step.episodeSimTime
                    , ProcessFunc.Get(trackParameters.ProcessFunc, step.data[variableIndex]));
            data.CalculateStats(trackParameters);
            return data;
        }
        public double GetEpisodeAverage(Dictionary<string, int> variablesInLog, Report trackParameters)
        {
            int variableIndex = variablesInLog[trackParameters.Variable];
            double avg = 0.0;
            if (steps.Count == 0) return 0.0;
            foreach (StepData step in steps)
                avg += ProcessFunc.Get(trackParameters.ProcessFunc, step.data[variableIndex]);
            return avg / steps.Count;
        }
    }
    public class SimionLog
    {
        //BINARY LOG FILE STUFF: constants, reading methods, etc...
        public const int HEADER_MAX_SIZE = 16;

        public const int EXPERIMENT_HEADER = 1;
        public const int EPISODE_HEADER = 2;
        public const int STEP_HEADER = 3;
        public const int EPISODE_END_HEADER = 4;

        public int TotalNumEpisodes = 0;
        public int NumTrainingEpisodes => TrainingEpisodes.Count;
        public int NumEvaluationEpisodes => EvaluationEpisodes.Count;
        public int NumEpisodesPerEvaluation = 1; //to make things easier, we update this number if we find
        int FileFormatVersion = 0;
        public bool BinFileLoadSuccess = false; //true if the binary file was correctly loaded
        public bool LogDescriptorLoadSuccesss = false; //true if the log descriptor was correctly loaded

        public List<EpisodesData> EvaluationEpisodes = new List<EpisodesData>();
        public List<EpisodesData> TrainingEpisodes = new List<EpisodesData>();
        public EpisodesData[] Episodes = null;

        public string ExperimentFileName { get; set; }
        public string BinaryLogFileName { get; set; }
        public string LogDescriptorFileName { get; set; }

        //We keep variables in a list for easy enumeration
        public List<string> VariablesLogged { get; } = new List<string>();
        //And also in a dictionary to be able to get index of any given variable easily
        Dictionary<string, int> VariableIndices = new Dictionary<string, int>();

        /// <summary>
        /// Constructor: the path to the experiment config file is provided and the constructor
        /// sets the path to the log files (descriptor and binary log)
        /// </summary>
        /// <param name="experimentFilePath"></param>
        public SimionLog(string experimentFilePath)
        {
            LogDescriptorFileName = SimionFileData.GetLogFilePath(experimentFilePath, true);
            if (!File.Exists(LogDescriptorFileName))
            {
                //for back-compatibility: if the appropriate log file is not found, check whether one exists
                //with the legacy naming convention: experiment-log.xml
                LogDescriptorFileName = SimionFileData.GetLogFilePath(experimentFilePath, true, true);
                BinaryLogFileName = SimionFileData.GetLogFilePath(experimentFilePath, false, true);
            }
            else
                BinaryLogFileName = SimionFileData.GetLogFilePath(experimentFilePath, false);

            ExperimentFileName = experimentFilePath;
        }

        /// <summary>
        /// This method loads the list of variables in the log file from the log descriptor
        /// </summary>
        /// <returns></returns>
        public void LoadLogDescriptor()
        {
            XmlDocument logDescriptor = new XmlDocument();
            if (File.Exists(LogDescriptorFileName))
            {
                try
                {
                    logDescriptor.Load(LogDescriptorFileName);
                    XmlNode node = logDescriptor.FirstChild;
                    if (node.Name == XMLConfig.descriptorRootNodeName)
                    {
                        foreach (XmlNode child in node.ChildNodes)
                        {
                            if (child.Name == XMLConfig.descriptorStateVarNodeName
                                || child.Name == XMLConfig.descriptorActionVarNodeName
                                || child.Name == XMLConfig.descriptorRewardVarNodeName
                                || child.Name == XMLConfig.descriptorStatVarNodeName)
                            {
                                string variableName = child.InnerText;
                                VariableIndices[variableName] = VariablesLogged.Count;
                                VariablesLogged.Add(variableName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDescriptorLoadSuccesss = false;
                    throw new Exception("Error loading log descriptor: " + LogDescriptorFileName + ex.Message);
                }
                LogDescriptorLoadSuccesss = true;
            }
            else LogDescriptorLoadSuccesss = false;
        }
        /// <summary>
        /// Read the binary log file. To know whether the log information has been succesfully loaded
        /// or not, BinFileLoadSuccess can be checked after calling this method.
        /// </summary>
        /// <returns></returns>
        public bool LoadBinaryLog()
        {
            try
            {
                using (FileStream logFile = File.OpenRead(BinaryLogFileName))
                {
                    using (BinaryReader binaryReader = new BinaryReader(logFile))
                    {
                        ReadExperimentLogHeader(binaryReader);
                        Episodes = new EpisodesData[TotalNumEpisodes];

                        for (int i = 0; i < TotalNumEpisodes; i++)
                        {
                            Episodes[i] = new EpisodesData();
                            EpisodesData episodeData = Episodes[i];

                            episodeData.ReadEpisodeHeader(binaryReader);
                            //if we find an episode subindex greater than the current max, we update it
                            //Episode subindex= Episode within an evaluation
                            if (episodeData.subIndex > NumEpisodesPerEvaluation)
                                NumEpisodesPerEvaluation = episodeData.subIndex;

                            //count evaluation and training episodes
                            if (episodeData.type == 0)
                                EvaluationEpisodes.Add(episodeData);
                            else
                                TrainingEpisodes.Add(episodeData);

                            StepData stepData = new StepData();
                            bool bLastStep = stepData.readStep(binaryReader, episodeData.numVariablesLogged);

                            while (!bLastStep)
                            {
                                //we only add the step if it's not the last one
                                //last steps don't contain any info but the end marker
                                episodeData.steps.Add(stepData);

                                stepData = new StepData();
                                bLastStep = stepData.readStep(binaryReader, episodeData.numVariablesLogged);
                            }
                        }
                        BinFileLoadSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                BinFileLoadSuccess = false;
            }
            return BinFileLoadSuccess;
        }

        public delegate void StepAction(int auxId, int stepIndex, double value);
        public delegate void ScalarValueAction(double action);
        public delegate double EpisodeFunc(EpisodesData episode, int varIndex);

   
        public Series GetEpisodeData(EpisodesData episode, Report trackParameters)
        {
            return episode.GetVariableData(VariableIndices, trackParameters);
        }

        public SeriesGroup GetAveragedData(List<EpisodesData> episodes, Report trackParameters)
        {
            SeriesGroup data = new SeriesGroup(trackParameters);
            Series xYSeries = new Series();

            foreach (EpisodesData episode in episodes)
            {
                xYSeries.AddValue(episode.index
                    , episode.GetEpisodeAverage(VariableIndices, trackParameters));
            }
            xYSeries.CalculateStats(trackParameters);
            data.AddSeries(xYSeries);
            return data;
        }

        private void ReadExperimentLogHeader(BinaryReader logReader)
        {
            int magicNumber = (int)logReader.ReadInt64();
            FileFormatVersion = (int)logReader.ReadInt64();
            TotalNumEpisodes = (int)logReader.ReadInt64();
            byte[] padding = logReader.ReadBytes(sizeof(double) * (SimionLog.HEADER_MAX_SIZE - 3));
        }
    }

    /*
struct ExperimentHeader
{
__int64 magicNumber = EXPERIMENT_HEADER;
__int64 fileVersion = CLogger::BIN_FILE_VERSION;
__int64 numEpisodes = 0;

__int64 padding[HEADER_MAX_SIZE - 3]; //extra space
ExperimentHeader()
{
 memset(padding, 0, sizeof(padding));
}
};
*/


    /*
    struct EpisodeHeader
    {
    __int64 magicNumber = EPISODE_HEADER;
    __int64 episodeType;
    __int64 episodeIndex;
    __int64 numVariablesLogged;
    //Added in version 2: if the episode belongs to an evaluation, the number of episodes per evaluation might be >1
    //the episodeSubIndex will be in [1..numEpisodesPerEvaluation]
    __int64 episodeSubIndex;

    __int64 padding[HEADER_MAX_SIZE - 5]; //extra space
    EpisodeHeader()
    {
        memset(padding, 0, sizeof(padding));
    }
};*/

    /*
struct StepHeader
{
__int64 magicNumber = STEP_HEADER;
__int64 stepIndex;
double experimentRealTime;
double episodeSimTime;
double episodeRealTime;

__int64 padding[HEADER_MAX_SIZE - 5]; //extra space
StepHeader()
{
    memset(padding, 0, sizeof(padding));
}
};
    */
}
