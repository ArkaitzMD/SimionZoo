#pragma once

class CNamedVarSet;
typedef CNamedVarSet CState;
typedef CNamedVarSet CAction;
typedef CNamedVarSet CReward;
class CParameters;
class CTimer;

#define MAX_PROGRESS_MSG_LEN 1024

//utility class to get a experiment time reference
//for example, we can use this reference to know if some feature vector has already been calculated in this time-step
class CExperimentTime
{
public:
	unsigned int m_episodeIndex; //[1..g_numEpisodes]
	unsigned int m_step; //]1..g_numStepsPerEpisode]

	CExperimentTime(unsigned int episodeIndex, unsigned int step){ m_episodeIndex = episodeIndex; m_step = step; }

	CExperimentTime& operator=(CExperimentTime& exp);
	bool operator==(CExperimentTime& exp);
};



class CExperiment
{
	unsigned int m_episodeIndex; //[1..g_numEpisodes]
	unsigned int m_step; //]1..g_numStepsPerEpisode]
	unsigned int m_trainingEpisodeIndex; //[1..m_numTrainingEpisodes]
	unsigned int m_evalEpisodeIndex; //[1..1+m_numTrainingEpisodes/ evalFreq]

	//episode stuff
	unsigned int m_totalNumEpisodes;
	unsigned int m_numTrainingEpisodes;
	unsigned int m_numEvaluationEpisodes;
	unsigned int m_evalFreq;
	//steps
	unsigned int m_numSteps;

	double m_progUpdateFreq; //in seconds: time between progress updates
	CTimer* m_pProgressTimer;
	/*__int64 m_counterFreq;
	__int64 m_lastProgressReportCounter;*/

	char m_progressMsg[MAX_PROGRESS_MSG_LEN];
	void reset();
public:
	CExperiment(CParameters* pParameters);
	~CExperiment();

	void getExperimentTime(CExperimentTime& ref) { ref.m_episodeIndex = m_episodeIndex; ref.m_step = m_step; }


	//STEP
	bool isValidStep(){ return m_step > 0 && m_step <= m_numSteps; }
	unsigned int getStep(){ return m_step; }
	bool isFirstStep(){ return m_step == 1; }
	bool isLastStep(){ return m_step == m_numSteps; }
	void nextStep();

	//EPISODES
	unsigned int getEpisodeIndex(){ return m_episodeIndex; }
	unsigned int getRelativeEpisodeIndex(){ if (!isEvaluationEpisode()) return m_trainingEpisodeIndex; return m_evalEpisodeIndex; }
	unsigned int getTrainingEpisodeIndex(){ return m_trainingEpisodeIndex; }
	unsigned int getEvaluationEpisodeIndex(){ return m_evalEpisodeIndex; }
	void nextEpisode();
	//true if is the first evaluation episode or the first training episode
	bool isFirstEpisode();
	//true if is the last evaluation episode or the last training episode
	bool isLastEpisode();
	bool isValidEpisode(){ return m_episodeIndex > 0 && m_episodeIndex <= m_totalNumEpisodes; }

	bool isEvaluationEpisode();

	unsigned int getTotalNumEpisodes(){ return m_totalNumEpisodes; }
	unsigned int getNumSteps(){ return m_numSteps; }
	void setNumSteps(unsigned int numSteps){ m_numSteps = numSteps; }

	double getExperimentProgress(); //[0,1]
	double getEpisodeProgress(); //[0,1]

	const char* getProgressString();


	unsigned int m_randomSeed;

	void timestep(CState *s, CAction *a,CState *s_p, CReward* pReward);
};